namespace SpotOps.Features.Auth.JWT;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "spotops";
    public string Audience { get; set; } = "spotops-client";
    public string Secret { get; set; } = string.Empty;
    public long AccessTokenExpiresInSeconds { get; set; } = 60 * 60 * 2;
    public int RefreshTokenExpiresInDays { get; set; } = 14;
}
