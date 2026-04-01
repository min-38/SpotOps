using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SpotOps.Infrastructure.PortOne;

public sealed class PortOneIvVerifyService : IPortOneIvVerifyService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly PortOneOptions _opt;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _accessToken;
    private readonly ILogger<PortOneIvVerifyService> _logger;

    public PortOneIvVerifyService(IHttpClientFactory httpFactory, IOptions<PortOneOptions> options, ILogger<PortOneIvVerifyService> logger)
    {
        _httpFactory = httpFactory;
        _opt = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// 포트원 본인인증 채널 검증.
    /// </summary>
    /// <param name="identityVerificationId">본인인증 ID.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>본인인증 결과.</returns>
    public async Task<(bool Success, JsonElement? IdentityVerification, string? ErrorCode)> VerifyAsync(
        string identityVerificationId,
        CancellationToken ct = default)
    {
        var id = (identityVerificationId ?? string.Empty).Trim();
        if (id.Length == 0)
            return (false, null, "AUTH_VERIFY_IV_INVALID_ID");

        if (string.IsNullOrWhiteSpace(_opt.ApiSecret))
            return (false, null, "AUTH_VERIFY_IV_FAILED");

        // 포트원 API 클라이언트 생성
        var http = _httpFactory.CreateClient("PortOne");
        await EnsureAccessTokenAsync(http, ct);
        
        // 본인인증 조회 요청 전송
        using var response = await SendIdentityVerificationRequestAsync(http, id, ct);

        _logger.LogInformation("PortOne Verify Endpoint response: {response}", response);

        return await ParseVerifyResponseAsync(response, ct);
    }

    /// <summary>
    /// 본인인증 조회 요청 전송.
    /// </summary>
    /// <param name="http">HTTP 클라이언트.</param>
    /// <param name="identityVerificationId">본인인증 ID.</param>
    /// <param name="ct">취소 토큰.</param>
    /// <returns>본인인증 조회 응답.</returns>
    private async Task<HttpResponseMessage> SendIdentityVerificationRequestAsync(
        HttpClient http,
        string identityVerificationId,
        CancellationToken ct)
    {
        // 본인인증 조회 요청 생성
        using var req = CreateIdentityVerificationRequest(identityVerificationId);
        var response = await http.SendAsync(req, ct);
        if (response.StatusCode != HttpStatusCode.Unauthorized) // 401 Unauthorized이면 토큰 만료/무효 등으로 처리
            return response;

        response.Dispose(); // 기존 응답 해제

        // 토큰 초기화
        _accessToken = null;
        // 토큰 재발급
        await EnsureAccessTokenAsync(http, ct);

        // 본인인증 조회 요청 생성
        using var retry = CreateIdentityVerificationRequest(identityVerificationId); // 재발급된 토큰으로 요청 생성
        return await http.SendAsync(retry, ct); // 재발급된 토큰으로 요청 전송
    }

    /// <summary>
    /// 본인인증 조회 요청 생성.
    /// </summary>
    /// <param name="identityVerificationId">본인인증 ID.</param>
    /// <returns>본인인증 조회 요청 메시지.</returns>
    private HttpRequestMessage CreateIdentityVerificationRequest(string identityVerificationId)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"identity-verifications/{Uri.EscapeDataString(identityVerificationId)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return request;
    }

    /// <summary>
    /// 본인인증 조회 응답 파싱.
    /// </summary>
    /// <param name="response">본인인증 조회 응답.</param>
    /// <param name="ct">취소 토큰.</param>
    /// <returns>본인인증 조회 결과 (성공/실패, 본인인증 정보, 에러 코드).</returns>
    private static async Task<(bool Success, JsonElement? IdentityVerification, string? ErrorCode)> ParseVerifyResponseAsync(
        HttpResponseMessage response,
        CancellationToken ct)
    {
        // 응답 본문을 스트림으로 읽어 JSON 파싱을 준비
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        // 응답 상태 코드 확인
        if (!response.IsSuccessStatusCode)
        {
            var code = doc.RootElement.TryGetProperty("type", out var t) ? t.GetString() : null;
            return (false, null, code ?? "AUTH_VERIFY_IV_FAILED");
        }

        // status가 VERIFIED가 아니면 오류 반환
        if (doc.RootElement.TryGetProperty("status", out var status) && status.GetString() != "VERIFIED")
            return (false, null, "AUTH_VERIFY_IV_INVALID_RESPONSE");

        return (true, doc.RootElement.Clone(), null);
    }

    /// <summary>
    /// 포트원 API 토큰 확인.
    /// </summary>
    /// <param name="http">HTTP 클라이언트.</param>
    /// <param name="ct">취소 토큰.</param>
    /// <returns>포트원 API 토큰.</returns>
    private async Task EnsureAccessTokenAsync(HttpClient http, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_accessToken))
            return;

        await _tokenLock.WaitAsync(ct);
        try
        {
            if (!string.IsNullOrEmpty(_accessToken))
                return;

            using var login = await http.PostAsJsonAsync(
                "login/api-secret",
                new { apiSecret = _opt.ApiSecret },
                ct);

            login.EnsureSuccessStatusCode();
            await using var stream = await login.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (!doc.RootElement.TryGetProperty("accessToken", out var at))
                throw new InvalidOperationException("PortOne login response missing accessToken.");

            _accessToken = at.GetString();
        }
        finally
        {
            _tokenLock.Release();
        }
    }
}
