namespace SpotOps.Features.Events;

public enum EventSaleStatus
{
    Scheduled,
    OnSale,
    Closed
}

public static class EventSaleStatusResolver
{
    public static EventSaleStatus Resolve(DateTime saleStartAt, DateTime saleEndAt, DateTime nowUtc)
    {
        if (nowUtc < saleStartAt)
            return EventSaleStatus.Scheduled;
        if (nowUtc > saleEndAt)
            return EventSaleStatus.Closed;
        return EventSaleStatus.OnSale;
    }
}
