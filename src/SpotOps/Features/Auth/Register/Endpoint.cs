using SpotOps.Contracts;
using SpotOps.Models;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace SpotOps.Features.Auth.Register;

public static class RegisterEndpoint
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/api/auth").WithTags("Auth");
        g.MapPost("/register", RegisterAsync).AllowAnonymous();
    }

    private static async Task<IResult> RegisterAsync(
        RegisterDto body,
        RegisterService register,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateInput(body);
        if (validationErrors.Count > 0)
            return Results.Json(
                ApiResponse<object?>.Fail(
                    "AUTH_VALIDATION_FAILED",
                    "입력값이 올바르지 않아요.",
                    validationErrors),
                statusCode: StatusCodes.Status400BadRequest);

        var (success, errorCode, errorMessage) = await register.RegisterAsync(body, cancellationToken);
        if (!success)
            return Results.Json(
                ApiResponse<object?>.Fail(errorCode ?? "AUTH_REGISTER_FAILED", errorMessage),
                statusCode: StatusCodes.Status400BadRequest);

        return Results.Json(ApiResponse<object?>.Ok(null), statusCode: StatusCodes.Status201Created);
    }

    private static Dictionary<string, string[]> ValidateInput(RegisterDto body)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        static void AddError(Dictionary<string, List<string>> map, string field, string message)
        {
            if (!map.TryGetValue(field, out var list))
            {
                list = [];
                map[field] = list;
            }
            list.Add(message);
        }

        var email = (body.Email ?? string.Empty).Trim();
        var password = body.Password ?? string.Empty;
        var name = (body.Name ?? string.Empty).Trim();
        var phone = string.IsNullOrWhiteSpace(body.Phone) ? null : body.Phone.Trim();
        var businessNumber = string.IsNullOrWhiteSpace(body.BusinessNumber) ? null : body.BusinessNumber.Trim();
        var companyName = string.IsNullOrWhiteSpace(body.CompanyName) ? null : body.CompanyName.Trim();

        if (!IsValidEmail(email))
            AddError(errors, "email", "올바른 이메일 형식이 아니에요.");

        if (!IsStrongPassword(password))
            AddError(errors, "password", "비밀번호는 영문 대/소문자, 숫자, 특수문자를 포함해 8~32자여야 해요.");

        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            AddError(errors, "name", "이름은 1~100자여야 해요.");

        if (phone is not null && phone.Length > 30)
            AddError(errors, "phone", "전화번호 형식이 올바르지 않아요.");

        if (body.Role == UserRole.Organizer)
        {
            if (string.IsNullOrWhiteSpace(businessNumber))
                AddError(errors, "businessNumber", "주최자 가입 시 사업자번호는 필수예요.");
            if (string.IsNullOrWhiteSpace(companyName))
                AddError(errors, "companyName", "주최자 가입 시 회사명은 필수예요.");
        }

        return errors.ToDictionary(k => k.Key, v => v.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsValidEmail(string email)
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

    private static bool IsStrongPassword(string password)
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
