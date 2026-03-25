using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SpotOps.Infrastructure.PortOne;

public sealed class PortOnePaymentApiClient : IPortOnePaymentApi
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly PortOneOptions _opt;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _accessToken;

    public PortOnePaymentApiClient(IHttpClientFactory httpFactory, IOptions<PortOneOptions> options)
    {
        _httpFactory = httpFactory;
        _opt = options.Value;
    }

    public async Task<JsonDocument?> GetPaymentAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_opt.ApiSecret))
            return null;

        var http = _httpFactory.CreateClient("PortOne");
        await EnsureAccessTokenAsync(http, cancellationToken);

        using var req = new HttpRequestMessage(HttpMethod.Get, "payments/" + Uri.EscapeDataString(paymentId));
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var res = await http.SendAsync(req, cancellationToken);
        if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _accessToken = null;
            await EnsureAccessTokenAsync(http, cancellationToken);
            using var retry = new HttpRequestMessage(HttpMethod.Get, "payments/" + Uri.EscapeDataString(paymentId));
            retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            res = await http.SendAsync(retry, cancellationToken);
        }

        if (!res.IsSuccessStatusCode)
            return null;

        await using var stream = await res.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private async Task EnsureAccessTokenAsync(HttpClient http, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_accessToken))
            return;

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(_accessToken))
                return;

            using var login = await http.PostAsJsonAsync(
                "login/api-secret",
                new { apiSecret = _opt.ApiSecret },
                cancellationToken);

            login.EnsureSuccessStatusCode();
            await using var stream = await login.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
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
