using System.Text.Json;

namespace SpotOps.Infrastructure.PortOne;

/// <summary>
/// 포트원 결제 단건 조회 등 REST V2 호출.
/// <see href="https://developers.portone.io/opi/ko/integration/webhook/readme-v2?v=v2"/>
/// </summary>
public interface IPortOnePaymentApi
{
    Task<JsonDocument?> GetPaymentAsync(string paymentId, CancellationToken cancellationToken = default);
}
