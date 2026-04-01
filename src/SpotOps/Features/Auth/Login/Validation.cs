using System.Net.Mail;

namespace SpotOps.Features.Auth.Login;

public static class LoginValidation
{
    public static bool ValidateLoginRequest(LoginRequest request)
    {
        var email = (request.Email ?? string.Empty).Trim();
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email))
            return false;

        // 이메일 형식 검증
        try
        {
            _ = new MailAddress(email);
        }
        catch
        {
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(password))
            return false;

        return true;
    }

    public static bool ValidateRefreshTokenRequest(RefreshTokenRequest request)
    {
        var refreshToken = (request.RefreshToken ?? string.Empty).Trim();
        if (refreshToken.Length < 16)
            return false;

        return true;
    }
}
