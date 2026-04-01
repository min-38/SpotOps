using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SpotOps.Features.Auth.JWT;

public static class DependencyInjection
{
    public static IServiceCollection AddJwtFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Configure(o =>
            {
                o.Issuer = configuration["JWT_ISSUER"] ?? "spotops";
                o.Audience = configuration["JWT_AUDIENCE"] ?? "spotops-client";
                o.Secret = configuration["JWT_SECRET"] ?? string.Empty;
                o.AccessTokenExpiresInSeconds = long.TryParse(configuration["JWT_ACCESS_TOKEN_EXPIRES_SECONDS"], out var accessExp)
                    ? accessExp
                    : 60 * 60 * 2;
                o.RefreshTokenExpiresInDays = int.TryParse(configuration["JWT_REFRESH_TOKEN_EXPIRES_DAYS"], out var refreshExp)
                    ? refreshExp
                    : 14;
            });

        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        return services;
    }
}
