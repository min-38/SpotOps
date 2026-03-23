using System.Collections.Concurrent;

namespace SpotOps.Features.Events.Queue;

public sealed class QueueService
{
    // 임시 인메모리 저장소 (나중에 DB/Redis로 교체)
    private static readonly ConcurrentDictionary<Guid, List<QueueState>> _queues = new();

    // 큐 엔트리에 참여
    public QueueJoinResponse Join(Guid eventId, Guid userId)
    {
        // 이벤트 큐가 없으면 생성
        var queue = _queues.GetOrAdd(eventId, _ => new List<QueueState>());

        // 큐 접근 동기화
        lock (queue)
        {
            // 초대 만료 처리
            ExpireInvitedEntries(queue, DateTime.UtcNow);

            // 같은 유저 중복 참여 방지
            var existing = queue.FirstOrDefault(x => x.UserId == userId && x.Status is QueueEntryStatus.Waiting or QueueEntryStatus.Invited);
            if (existing is not null)
            {
                var livePosition = GetLivePosition(queue, existing);
                var waitingAhead = livePosition > 0 ? livePosition - 1 : 0;
                return new QueueJoinResponse(existing.QueueEntryId, livePosition, waitingAhead);
            }

            // 순서 지정
            var position = queue.Count + 1;

            var state = new QueueState(
                QueueEntryId: Guid.NewGuid(),
                EventId: eventId,
                UserId: userId,
                Position: position,
                Status: QueueEntryStatus.Waiting,
                SessionToken: null,
                SessionExpiresAtUtc: null);

            queue.Add(state);
            var newLivePosition = GetLivePosition(queue, state);
            var newWaitingAhead = newLivePosition > 0 ? newLivePosition - 1 : 0;
            return new QueueJoinResponse(state.QueueEntryId, newLivePosition, newWaitingAhead);
        }
    }

    // Queue Entry 상태 조회
    public QueueStatusResponse GetStatus(Guid eventId, Guid queueEntryId)
    {
        // 이벤트 큐가 없으면 예외
        if (!_queues.TryGetValue(eventId, out var queue))
            throw new InvalidOperationException("Queue not found.");

        lock (queue)
        {
            ExpireInvitedEntries(queue, DateTime.UtcNow);

            // 큐 엔트리 찾기
            var idx = queue.FindIndex(x => x.QueueEntryId == queueEntryId);
            if (idx < 0)
                throw new InvalidOperationException("Queue entry not found.");

            // 큐 엔트리 상태 조회
            var state = queue[idx];
            // 라이브 순서 조회
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
        if (!_queues.TryGetValue(eventId, out var queue))
            return 0;

        lock (queue)
        {
            var now = DateTime.UtcNow;

            ExpireInvitedEntries(queue, now);

            var invited = 0;

            // 초대된 엔트리 수가 배치 크기보다 작을 때까지 반복
            for (var i = 0; i < queue.Count && invited < batchSize; i++)
            {
                var q = queue[i];
                if (q.Status != QueueEntryStatus.Waiting) continue; // 대기 상태가 아니면 스킵

                queue[i] = q with
                {
                    Status = QueueEntryStatus.Invited, // 초대 상태로 변경
                    SessionToken = Guid.NewGuid().ToString("N"), // 세션 토큰 생성
                    SessionExpiresAtUtc = now.AddSeconds(selectionWindowSec) // 세션 만료 시간 설정
                };
                invited++;
            }

            return invited;
        }
    }

    // 실제 유저의 순서
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

    // 초대 만료 처리
    private static void ExpireInvitedEntries(List<QueueState> queue, DateTime nowUtc)
    {
        for (var i = 0; i < queue.Count; i++)
        {
            var q = queue[i];
            if (q.Status == QueueEntryStatus.Invited  // 초대 상태이고
                && q.SessionExpiresAtUtc is not null // 세션 만료 시간이 설정되어 있고
                && q.SessionExpiresAtUtc <= nowUtc // 세션 만료 시간이 현재 시간 이후고
            )
            {
                queue[i] = q with
                {
                    Status = QueueEntryStatus.Expired,
                    SessionToken = null,
                    SessionExpiresAtUtc = null
                };
            }
        }
    }

    private sealed record QueueState(
        Guid QueueEntryId,
        Guid EventId,
        Guid UserId,
        int Position,
        QueueEntryStatus Status,
        string? SessionToken,
        DateTime? SessionExpiresAtUtc);
}
