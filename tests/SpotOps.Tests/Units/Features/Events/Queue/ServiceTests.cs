using SpotOps.Features.Events.Queue;

namespace SpotOps.Tests.Units.Features.Events.Queue;

public class QueueServiceTests
{
    // 테스트 간 간섭 방지를 위해 eventId를 매번 새로 생성
    private static Guid NewEventId() => Guid.NewGuid();
    private static Guid NewUserId() => Guid.NewGuid();

    // 성공: 첫 참여자는 첫 번째 우선순위를 가짐
    [Fact]
    public void Join_FirstUser_AssignsPositionOne()
    {
        // Arrange
        var service = new QueueService();
        var eventId = NewEventId();
        var userId = NewUserId();

        // Act
        var res = service.Join(eventId, userId);

        // Assert
        Assert.Equal(1, res.Position);
        Assert.Equal(0, res.WaitingAhead);

        Assert.NotEqual(Guid.Empty, res.QueueEntryId);
    }

    // 성공: 여러 참여자는 증가하는 우선순위를 가짐
    [Fact]
    public void Join_MultipleUsers_AssignsIncreasingPositions()
    {
        // Arrange
        var service = new QueueService();
        var eventId = NewEventId();

        // Act
        var r1 = service.Join(eventId, NewUserId());
        var r2 = service.Join(eventId, NewUserId());
        var r3 = service.Join(eventId, NewUserId());

        // Assert
        Assert.Equal(1, r1.Position);
        Assert.Equal(2, r2.Position);
        Assert.Equal(3, r3.Position);
    }

    // 성공: 같은 유저가 중복 참여 시 기존 항목 반환
    [Fact]
    public void Join_SameUserTwice_ReturnsExistingEntry()
    {
        // Arrange
        var service = new QueueService();
        var eventId = NewEventId();
        var userId = NewUserId();

        // Act
        var first = service.Join(eventId, userId);
        var second = service.Join(eventId, userId);

        // Assert
        Assert.Equal(first.QueueEntryId, second.QueueEntryId);
        Assert.Equal(first.Position, second.Position);
        Assert.Equal(first.WaitingAhead, second.WaitingAhead);
    }

    // 성공: 대기 중인 참여자만 초대 가능
    [Fact]
    public void InviteNextBatch_InvitesOnlyWaitingUsers()
    {
        // Arrange
        var service = new QueueService();
        var eventId = NewEventId();

        var e1 = service.Join(eventId, NewUserId()); // 첫 번째 대기자
        var e2 = service.Join(eventId, NewUserId()); // 두 번째 대기자
        var e3 = service.Join(eventId, NewUserId()); // 세 번째 대기자

        // Act
        var invited = service.InviteNextBatch(eventId, batchSize: 2, selectionWindowSec: 180);

        // Assert
        Assert.Equal(2, invited); // 2명 초대됨

        var s1 = service.GetStatus(eventId, e1.QueueEntryId); // 첫 번째 대기자 상태
        var s2 = service.GetStatus(eventId, e2.QueueEntryId); // 두 번째 대기자 상태
        var s3 = service.GetStatus(eventId, e3.QueueEntryId); // 세 번째 대기자 상태

        Assert.True(s1.Invited); // 첫 번째 대기자 초대됨
        Assert.True(s2.Invited); // 두 번째 대기자 초대됨
        Assert.False(s3.Invited); // 세 번째 대기자 초대되지 않음

        Assert.Equal(QueueEntryStatus.Invited, s1.Status); // 첫 번째 대기자 초대됨
        Assert.Equal(QueueEntryStatus.Invited, s2.Status); // 두 번째 대기자 초대됨
        Assert.Equal(QueueEntryStatus.Waiting, s3.Status); // 세 번째 대기자 대기 중

        Assert.NotNull(s1.SessionToken); // 첫 번째 대기자 세션 토큰 있음
        Assert.NotNull(s2.SessionToken); // 두 번째 대기자 세션 토큰 있음
        Assert.Null(s3.SessionToken); // 세 번째 대기자 세션 토큰 없음
    }

    // 성공: 초대 만료된 유저는 재참여 시 새 큐 엔트리를 부여받는다.
    [Fact]
    public void Join_WhenPreviousInviteExpired_AllowsRejoin()
    {
        // Arrange
        var service = new QueueService();
        var eventId = NewEventId();
        var userId = NewUserId();
        var first = service.Join(eventId, userId);
        service.InviteNextBatch(eventId, batchSize: 1, selectionWindowSec: 1);

        // Act
        Thread.Sleep(1100);
        var second = service.Join(eventId, userId);

        // Assert
        Assert.NotEqual(first.QueueEntryId, second.QueueEntryId);
    }

    // 실패: 알 수 없는 이벤트 ID 접근 시 예외 발생
    [Fact]
    public void GetStatus_UnknownEvent_Throws()
    {
        // Arrange
        var service = new QueueService();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            service.GetStatus(Guid.NewGuid(), Guid.NewGuid()));
    }

    // 실패: 존재하지 않는 큐 엔트리 조회는 예외를 던진다.
    [Fact]
    public void GetStatus_UnknownQueueEntry_Throws()
    {
        // Arrange
        var service = new QueueService();
        var eventId = NewEventId();
        service.Join(eventId, NewUserId());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            service.GetStatus(eventId, Guid.NewGuid()));
    }

    // 경계: 큐가 없으면 배치 초대 수는 0이다.
    [Fact]
    public void InviteNextBatch_ZeroOrMissingQueue_ReturnsZero()
    {
        // Arrange
        var service = new QueueService();

        // Act
        var invited = service.InviteNextBatch(Guid.NewGuid(), batchSize: 10, selectionWindowSec: 180);

        // Assert
        Assert.Equal(0, invited);
    }

    // 경계: 초대 만료 후 상태 조회 시 Expired로 반영된다.
    [Fact]
    public void GetStatus_ExpiredInvite_TransitionsToExpired()
    {
        // Arrange
        var service = new QueueService();
        var eventId = NewEventId();
        var entry = service.Join(eventId, NewUserId());
        service.InviteNextBatch(eventId, batchSize: 1, selectionWindowSec: 1);

        // Act
        Thread.Sleep(1100);
        var status = service.GetStatus(eventId, entry.QueueEntryId);

        // Assert
        Assert.Equal(QueueEntryStatus.Expired, status.Status);
        Assert.False(status.Invited);
        Assert.Null(status.SessionToken);
        Assert.Null(status.SessionExpiresAtUtc);
    }

    // 경계: 앞 대기자가 만료되면 뒤 대기자의 조회 순번은 압축되어 갱신된다.
    [Fact]
    public void GetStatus_WhenFrontEntryExpires_LivePositionIsCompacted()
    {
        // Arrange
        var service = new QueueService();
        var eventId = NewEventId();
        var user1 = service.Join(eventId, NewUserId());
        var user2 = service.Join(eventId, NewUserId());
        service.InviteNextBatch(eventId, batchSize: 1, selectionWindowSec: 1);

        // Act
        Thread.Sleep(1100);
        var expiredFront = service.GetStatus(eventId, user1.QueueEntryId);
        var waitingSecond = service.GetStatus(eventId, user2.QueueEntryId);

        // Assert
        Assert.Equal(QueueEntryStatus.Expired, expiredFront.Status);
        Assert.Equal(QueueEntryStatus.Waiting, waitingSecond.Status);
        Assert.Equal(1, waitingSecond.Position);
        Assert.Equal(0, waitingSecond.WaitingAhead);
    }

    // 성공: async 경로도 sync와 동일하게 순번을 부여한다.
    [Fact]
    public async Task JoinAsync_MultipleUsers_AssignsIncreasingPositions()
    {
        // Arrange
        var service = new QueueService();
        var eventId = NewEventId();

        // Act
        var r1 = await service.JoinAsync(eventId, NewUserId());
        var r2 = await service.JoinAsync(eventId, NewUserId());
        var r3 = await service.JoinAsync(eventId, NewUserId());

        // Assert
        Assert.Equal(1, r1.Position);
        Assert.Equal(2, r2.Position);
        Assert.Equal(3, r3.Position);
    }

    // 성공: async 경로에서 배치 초대/상태 조회가 sync와 동일하게 동작한다.
    [Fact]
    public async Task InviteNextBatchAsync_AndGetStatusAsync_MatchSyncBehavior()
    {
        // Arrange
        var service = new QueueService();
        var eventId = NewEventId();

        var e1 = await service.JoinAsync(eventId, NewUserId());
        var e2 = await service.JoinAsync(eventId, NewUserId());
        var e3 = await service.JoinAsync(eventId, NewUserId());

        // Act
        var invited = await service.InviteNextBatchAsync(eventId, batchSize: 2, selectionWindowSec: 180);
        var s1 = await service.GetStatusAsync(eventId, e1.QueueEntryId);
        var s2 = await service.GetStatusAsync(eventId, e2.QueueEntryId);
        var s3 = await service.GetStatusAsync(eventId, e3.QueueEntryId);

        // Assert
        Assert.Equal(2, invited);
        Assert.True(s1.Invited);
        Assert.True(s2.Invited);
        Assert.False(s3.Invited);
        Assert.Equal(QueueEntryStatus.Waiting, s3.Status);
        Assert.NotNull(s1.SessionToken);
        Assert.NotNull(s2.SessionToken);
        Assert.Null(s3.SessionToken);
    }

    [Fact]
    public async Task ValidateSelectionSession_WhenInvitedAndTokenMatches_ReturnsTrue()
    {
        var service = new QueueService();
        var eventId = NewEventId();
        var userId = NewUserId();

        var join = service.Join(eventId, userId);
        service.InviteNextBatch(eventId, batchSize: 1, selectionWindowSec: 120);
        var status = service.GetStatus(eventId, join.QueueEntryId);

        Assert.True(status.Invited);
        Assert.NotNull(status.SessionToken);

        var ok = await service.ValidateSelectionSessionAsync(eventId, userId, status.SessionToken);
        Assert.True(ok);
    }

    [Fact]
    public async Task ValidateSelectionSession_WhenTokenWrong_ReturnsFalse()
    {
        var service = new QueueService();
        var eventId = NewEventId();
        var userId = NewUserId();

        var join = service.Join(eventId, userId);
        service.InviteNextBatch(eventId, batchSize: 1, selectionWindowSec: 120);
        var status = service.GetStatus(eventId, join.QueueEntryId);
        Assert.True(status.Invited);

        var ok = await service.ValidateSelectionSessionAsync(eventId, userId, "not-the-token");
        Assert.False(ok);
    }
}
