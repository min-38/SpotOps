using System.Text.Json;

namespace SpotOps.Infrastructure.PortOne;

/// <summary>
/// 포트원 본인인증 채널 검증.
/// <see href="https://developers.portone.io/opi/ko/integration/identity-verification/readme?v=v2"/>
/// </summary>
public interface IPortOneIvVerifyService
{
    Task<(bool Success, JsonElement? IdentityVerification, string? ErrorCode)> VerifyAsync(
        string identityVerificationId,
        CancellationToken cancellationToken = default);
}
