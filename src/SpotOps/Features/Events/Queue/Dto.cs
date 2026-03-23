namespace SpotOps.Features.Events.Queue;

public enum QueueEntryStatus
{
    Waiting,
    Invited,
    Expired,
    Done
}

public sealed record QueueJoinResponse(
    Guid QueueEntryId,
    int Position,
    int WaitingAhead
);

public sealed record QueueStatusResponse(
    Guid QueueEntryId,
    QueueEntryStatus Status,
    int Position,
    int WaitingAhead,
    bool Invited,
    string? SessionToken,
    DateTime? SessionExpiresAtUtc
);
