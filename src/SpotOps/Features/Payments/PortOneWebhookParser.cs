using System.Text.Json;

namespace SpotOps.Features.Payments;

/// <summary>
/// 포트원 결제모듈 V2 웹훅(예: 버전 2024-04-25) 중 <c>Transaction.Paid</c> 파싱.
/// <see href="https://developers.portone.io/opi/ko/integration/webhook/readme-v2?v=v2"/>
/// </summary>
public static class PortOneWebhookParser
{
    public static bool TryGetTransactionPaid(
        string rawJson,
        out string? paymentId,
        out string? transactionId,
        out string? storeId)
    {
        paymentId = transactionId = storeId = null;
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;
        if (!root.TryGetProperty("type", out var typeEl) || typeEl.GetString() != "Transaction.Paid")
            return false;

        if (!root.TryGetProperty("data", out var data))
            return false;

        if (data.TryGetProperty("paymentId", out var pid) && pid.ValueKind == JsonValueKind.String)
            paymentId = pid.GetString();
        if (data.TryGetProperty("transactionId", out var tid) && tid.ValueKind == JsonValueKind.String)
            transactionId = tid.GetString();
        if (data.TryGetProperty("storeId", out var sid) && sid.ValueKind == JsonValueKind.String)
            storeId = sid.GetString();

        return !string.IsNullOrEmpty(paymentId);
    }
}
