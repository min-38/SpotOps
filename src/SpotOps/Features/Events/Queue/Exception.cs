namespace SpotOps.Features.Events.Queue;

public static class QueueException
{
    public sealed class QueueBusyException : Exception
    {
        public QueueBusyException() : base("Queue is busy. Please retry.")
        {
        }
    }
}
