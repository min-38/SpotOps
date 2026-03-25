namespace SpotOps.Features.Payments;

public sealed record PaymentPrepareResponse(
    string PaymentId,
    string StoreId,
    long TotalAmount,
    string OrderName);
