using System.Net.Mail;
using System.Text.RegularExpressions;

namespace SpotOps.Features.Auth;

public static class RegisterValidation
{
    public static bool IvValidate(PortOneIvVerifyRequest request)
    {
        var identityVerificationId = (request.IdentityVerificationId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(identityVerificationId))
            return false;
        return true;
    }

    public static bool RegisterValidate(RegisterRequest request)
    {
        var verificationToken = (request.VerificationToken ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(verificationToken))
            return false;
        
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var gender = (request.Gender ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(gender))
            return false;

        var birthday = (request.Birthday ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(birthday))
            return false;

        var uniqueKey = (request.UniqueKey ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(uniqueKey))
            return false;

        var phone = (request.Phone ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        var email = (request.Email ?? string.Empty).Trim();
        if (!IsValidEmail(email))
            return false;

        var password = (request.Password ?? string.Empty).Trim();
        if (!IsStrongPassword(password))
            return false;

        var passwordConfirmation = (request.PasswordConfirmation ?? string.Empty).Trim();
        if (!string.Equals(password, passwordConfirmation, StringComparison.Ordinal))
            return false;

        return true;
    }

    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
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
