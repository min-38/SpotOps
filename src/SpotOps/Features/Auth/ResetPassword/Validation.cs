using System.Net.Mail;
using System.Text.RegularExpressions;

namespace SpotOps.Features.Auth.ResetPassword;

public static class ResetPasswordValidation
{
    public static bool ResetTokenValidate(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 16)
            return false;
        return true;
    }

    public static bool NewPasswordValidate(ResetPasswordRequest request)
    {
        var newPassword = (request.NewPassword ?? string.Empty).Trim();
        var newPasswordConfirmation = (request.NewPasswordConfirmation ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(newPasswordConfirmation))
            return false;

        if (!IsStrongPassword(newPassword))
            return false;

        if (!string.Equals(newPassword, newPasswordConfirmation, StringComparison.Ordinal))
            return false;

        return true;
    }

    public static bool IsStrongPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || password.Length > 32)
            return false;

        var hasUpper = Regex.IsMatch(password, "[A-Z]");
        var hasLower = Regex.IsMatch(password, "[a-z]");
        var hasDigit = Regex.IsMatch(password, "[0-9]");
        var hasSpecial = Regex.IsMatch(password, "[^a-zA-Z0-9]");
        return hasUpper && hasLower && hasDigit && hasSpecial;
    }
}
