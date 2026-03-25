using System.Text.Json;

namespace SpotOps.Features.Payments;

public static class PortOnePaymentResponseParser
{
    /// <summary>단건 조회 JSON에서 결제 루트 객체를 찾는다.</summary>
    public static JsonElement GetPaymentRoot(JsonDocument doc)
    {
        var root = doc.RootElement;
        return root.TryGetProperty("payment", out var wrapped) ? wrapped : root;
    }

    public static bool IsPaid(JsonElement payment)
    {
        if (!payment.TryGetProperty("status", out var status))
            return false;
        if (status.ValueKind == JsonValueKind.String)
            return status.GetString() == "PAID";
        if (status.ValueKind == JsonValueKind.Object)
            return status.TryGetProperty("paid", out _);
        return false;
    }

    public static bool TryGetTotalAmount(JsonElement payment, out long total)
    {
        total = 0;
        if (!payment.TryGetProperty("amount", out var amount))
            return false;
        if (amount.TryGetProperty("total", out var t))
        {
            if (t.ValueKind == JsonValueKind.Number)
            {
                total = t.GetInt64();
                return true;
            }
            if (t.ValueKind == JsonValueKind.String && long.TryParse(t.GetString(), out var v))
            {
                total = v;
                return true;
            }
        }
        return false;
    }
}
