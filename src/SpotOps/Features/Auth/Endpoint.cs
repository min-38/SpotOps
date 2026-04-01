using SpotOps.Features.Auth.Login;
using SpotOps.Features.Auth.Logout;
using SpotOps.Features.Auth.ForgotPassword;
using SpotOps.Features.Auth.ResetPassword;
using SpotOps.Features.Auth.Register;

namespace SpotOps.Features.Auth;

public static partial class AuthEndpoints
{
    public static void Map(WebApplication app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Auth");

        LoginEndpoint.Map(auth);
        LogoutEndpoint.Map(auth);
        RegisterEndpoint.Map(auth);
        ForgotPasswordEndpoint.Map(auth);
        ResetPasswordEndpoint.Map(auth);
    }
}
