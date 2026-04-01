using Microsoft.Extensions.DependencyInjection;
using SpotOps.Features.Auth.ForgotPassword;
using SpotOps.Features.Auth.Login;
using SpotOps.Features.Auth.Logout;
using SpotOps.Features.Auth.ResetPassword;
using SpotOps.Features.Auth.Register;

namespace SpotOps.Features.Auth;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthFeature(this IServiceCollection services)
    {
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<ILogoutService, LogoutService>();
        services.AddScoped<IForgotPasswordService, ForgotPasswordService>();
        services.AddScoped<IResetPasswordService, ResetPasswordService>();
        services.AddScoped<IRegisterService, RegisterService>();
        services.AddSingleton<ISensitiveDataProtector, SensitiveDataProtector>();
        return services;
    }
}
