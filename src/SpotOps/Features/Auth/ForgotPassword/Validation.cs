using System.Net.Mail;

namespace SpotOps.Features.Auth.ForgotPassword;

public static class ForgotPasswordValidation
{
    public static bool ValidateForgotPasswordRequest(ForgotPasswordRequest request)
    {
        var email = (request.Email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            return false;
        return true;
    }

    public static bool IsValidEmail(string email)
    {
        return MailAddress.TryCreate(email, out _);
    }
}
