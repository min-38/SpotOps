using System.Collections.Concurrent;
using SpotOps.Infrastructure.Redis;
using StackExchange.Redis;

namespace SpotOps.Features.Events.Queue;

public sealed class QueueService
{
    // 테스트/로컬 fallback 인메모리 저장소
    private static readonly ConcurrentDictionary<Guid, List<QueueState>> _queues = new();

    private readonly RedisJsonStore<List<QueueState>>? _store;
    private readonly IDatabase? _lockDb;
    private readonly RedisKeyBuilder _keys;
    private static readonly TimeSpan RedisLockTtl = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RedisLockWaitTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan RedisLockRetryDelay = TimeSpan.FromMilliseconds(50);

    // Unit 테스트용(기존 테스트 호환)
    public QueueService()
    {
        _keys = new RedisKeyBuilder("spotops");
        _store = null;
        _lockDb = null;
    }

    // 운영용 Redis 저장소
    public QueueService(IConnectionMultiplexer multiplexer, RedisOptions redisOptions, RedisKeyBuilder keys)
    {
        _keys = keys;
        var db = multiplexer.GetDatabase(redisOptions.Db);
        _store = new RedisJsonStore<List<QueueState>>(db, redisOptions.DefaultTtlHours);
        _lockDb = db;
    }

    // 큐 엔트리에 참여
    public QueueJoinResponse Join(Guid eventId, Guid userId)
    {
        if (_store is not null)
            return JoinAsync(eventId, userId).GetAwaiter().GetResult();

        var queue = _queues.GetOrAdd(eventId, _ => new List<QueueState>());

        lock (queue)
        {
            ExpireInvitedEntries(queue, DateTime.UtcNow);

            var existing = queue.FirstOrDefault(x => x.UserId == userId && x.Status is QueueEntryStatus.Waiting or QueueEntryStatus.Invited);
            if (existing is not null)
            {
                var livePosition = GetLivePosition(queue, existing);
                var waitingAhead = livePosition > 0 ? livePosition - 1 : 0;
                return new QueueJoinResponse(existing.QueueEntryId, livePosition, waitingAhead);
            }

            var state = new QueueState
            {
                QueueEntryId = Guid.NewGuid(),
                EventId = eventId,
                UserId = userId,
                Position = queue.Count + 1,
                Status = QueueEntryStatus.Waiting
            };

            queue.Add(state);

            var newLivePosition = GetLivePosition(queue, state);
            var newWaitingAhead = newLivePosition > 0 ? newLivePosition - 1 : 0;
            return new QueueJoinResponse(state.QueueEntryId, newLivePosition, newWaitingAhead);
        }
    }

    // Queue Entry 상태 조회
    public QueueStatusResponse GetStatus(Guid eventId, Guid queueEntryId)
    {
        if (_store is not null)
            return GetStatusAsync(eventId, queueEntryId).GetAwaiter().GetResult();

        if (!_queues.TryGetValue(eventId, out var queue))
            throw new InvalidOperationException("Queue not found.");

        lock (queue)
        {
            ExpireInvitedEntries(queue, DateTime.UtcNow);

            var idx = queue.FindIndex(x => x.QueueEntryId == queueEntryId);
            if (idx < 0)
                throw new InvalidOperationException("Queue entry not found.");

            var state = queue[idx];
            var livePosition = GetLivePosition(queue, state);
            var waitingAhead = livePosition > 0 ? livePosition - 1 : 0;

            return new QueueStatusResponse(
                QueueEntryId: state.QueueEntryId,
                Status: state.Status,
                Position: livePosition,
                WaitingAhead: waitingAhead,
                Invited: state.Status == QueueEntryStatus.Invited,
                SessionToken: state.SessionToken,
                SessionExpiresAtUtc: state.SessionExpiresAtUtc);
        }
    }

    // 다음 배치 초대
    public int InviteNextBatch(Guid eventId, int batchSize, int selectionWindowSec)
    {
        if (_store is not null)
            return InviteNextBatchAsync(eventId, batchSize, selectionWindowSec).GetAwaiter().GetResult();

        if (!_queues.TryGetValue(eventId, out var queue))
            return 0;

        lock (queue)
        {
            var now = DateTime.UtcNow;

            ExpireInvitedEntries(queue, now);

            var invited = 0;
            for (var i = 0; i < queue.Count && invited < batchSize; i++)
            {
                var q = queue[i];
                if (q.Status != QueueEntryStatus.Waiting) continue;

                q.Status = QueueEntryStatus.Invited;
                q.SessionToken = Guid.NewGuid().ToString("N");
                q.SessionExpiresAtUtc = now.AddSeconds(selectionWindowSec);
                invited++;
            }

            return invited;
        }
    }

    // 큐 엔트리에 참여(비동기)
    public async Task<QueueJoinResponse> JoinAsync(Guid eventId, Guid userId)
    {
        if (_store is null)
            return Join(eventId, userId);

        return await WithEventLockAsync(eventId, async () =>
        {
            var queueKey = GetQueueKey(eventId);
            var queue = await _store.GetAsync(queueKey) ?? [];
            ExpireInvitedEntries(queue, DateTime.UtcNow);

            var existing = queue.FirstOrDefault(x => x.UserId == userId && x.Status is QueueEntryStatus.Waiting or QueueEntryStatus.Invited);
            if (existing is not null)
            {
                var livePosition = GetLivePosition(queue, existing);
                var waitingAhead = livePosition > 0 ? livePosition - 1 : 0;
                return new QueueJoinResponse(existing.QueueEntryId, livePosition, waitingAhead);
            }

            var state = new QueueState
            {
                QueueEntryId = Guid.NewGuid(),
                EventId = eventId,
                UserId = userId,
                Position = queue.Count + 1,
                Status = QueueEntryStatus.Waiting
            };

            queue.Add(state);
            await _store.SetAsync(queueKey, queue);

            var newLivePosition = GetLivePosition(queue, state);
            var newWaitingAhead = newLivePosition > 0 ? newLivePosition - 1 : 0;
            return new QueueJoinResponse(state.QueueEntryId, newLivePosition, newWaitingAhead);
        });
    }

    // Queue Entry 상태 조회
    public async Task<QueueStatusResponse> GetStatusAsync(Guid eventId, Guid queueEntryId)
    {
        if (_store is null)
            return GetStatus(eventId, queueEntryId);

        return await WithEventLockAsync(eventId, async () =>
        {
            var queueKey = GetQueueKey(eventId);
            var queue = await _store.GetAsync(queueKey) ?? [];
            if (queue.Count == 0)
                throw new InvalidOperationException("Queue not found.");

            ExpireInvitedEntries(queue, DateTime.UtcNow);

            var idx = queue.FindIndex(x => x.QueueEntryId == queueEntryId);
            if (idx < 0)
                throw new InvalidOperationException("Queue entry not found.");

            await _store.SetAsync(queueKey, queue);

            var state = queue[idx];
            var livePosition = GetLivePosition(queue, state);
            var waitingAhead = livePosition > 0 ? livePosition - 1 : 0;

            return new QueueStatusResponse(
                QueueEntryId: state.QueueEntryId,
                Status: state.Status,
                Position: livePosition,
                WaitingAhead: waitingAhead,
                Invited: state.Status == QueueEntryStatus.Invited,
                SessionToken: state.SessionToken,
                SessionExpiresAtUtc: state.SessionExpiresAtUtc);
        });
    }

    // 다음 배치 초대
    public async Task<int> InviteNextBatchAsync(Guid eventId, int batchSize, int selectionWindowSec)
    {
        if (_store is null)
            return InviteNextBatch(eventId, batchSize, selectionWindowSec);

        return await WithEventLockAsync(eventId, async () =>
        {
            var queueKey = GetQueueKey(eventId);
            var queue = await _store.GetAsync(queueKey) ?? [];
            if (queue.Count == 0)
                return 0;

            var now = DateTime.UtcNow;
            ExpireInvitedEntries(queue, now);

            var invited = 0;
            for (var i = 0; i < queue.Count && invited < batchSize; i++)
            {
                var q = queue[i];
                if (q.Status != QueueEntryStatus.Waiting) continue;

                q.Status = QueueEntryStatus.Invited;
                q.SessionToken = Guid.NewGuid().ToString("N");
                q.SessionExpiresAtUtc = now.AddSeconds(selectionWindowSec);
                invited++;
            }

            await _store.SetAsync(queueKey, queue);
            return invited;
        });
    }

    // 큐에서 대기 중인 엔트리의 실시간 위치 계산
    private static int GetLivePosition(List<QueueState> queue, QueueState state)
    {
        if (state.Status != QueueEntryStatus.Waiting)
            return 0;

        var waiting = queue
            .Where(x => x.Status == QueueEntryStatus.Waiting)
            .OrderBy(x => x.Position)
            .ToList();

        var index = waiting.FindIndex(x => x.QueueEntryId == state.QueueEntryId);
        return index < 0 ? 0 : index + 1;
    }

    private static void ExpireInvitedEntries(List<QueueState> queue, DateTime nowUtc)
    {
        for (var i = 0; i < queue.Count; i++)
        {
            var q = queue[i];
            if (q.Status == QueueEntryStatus.Invited
                && q.SessionExpiresAtUtc is not null
                && q.SessionExpiresAtUtc <= nowUtc)
            {
                q.Status = QueueEntryStatus.Expired;
                q.SessionToken = null;
                q.SessionExpiresAtUtc = null;
            }
        }
    }

    private string GetQueueKey(Guid eventId) => _keys.QueueEvent(eventId);

    private async Task<T> WithEventLockAsync<T>(Guid eventId, Func<Task<T>> action)
    {
        if (_lockDb is null)
            throw new InvalidOperationException("Redis lock database is not initialized.");

        var lockKey = $"{GetQueueKey(eventId)}:lock";
        var lockToken = Guid.NewGuid().ToString("N");
        var waitStartedAt = DateTime.UtcNow;

        while (DateTime.UtcNow - waitStartedAt < RedisLockWaitTimeout)
        {
            if (await _lockDb.LockTakeAsync(lockKey, lockToken, RedisLockTtl))
            {
                try
                {
                    return await action();
                }
                finally
                {
                    await _lockDb.LockReleaseAsync(lockKey, lockToken);
                }
            }

            await Task.Delay(RedisLockRetryDelay);
        }

        throw new InvalidOperationException("Queue is busy. Please retry.");
    }

    private sealed class QueueState
    {
        public Guid QueueEntryId { get; init; }
        public Guid EventId { get; init; }
        public Guid UserId { get; init; }
        public int Position { get; init; }
        public QueueEntryStatus Status { get; set; }
        public string? SessionToken { get; set; }
        public DateTime? SessionExpiresAtUtc { get; set; }
    }
}
