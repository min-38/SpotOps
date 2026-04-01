using Microsoft.AspNetCore.DataProtection;

namespace SpotOps.Features.Auth.Register;

public interface ISensitiveDataProtector
{
    string? Protect(string? value);
    string? Unprotect(string? protectedValue);
}

public sealed class SensitiveDataProtector : ISensitiveDataProtector
{
    private readonly IDataProtector _protector;

    public SensitiveDataProtector(IDataProtectionProvider dataProtectionProvider)
    {
        // 데이터 보호 제공자 생성
        _protector = dataProtectionProvider.CreateProtector("SpotOps.Auth.Register.SensitiveData.v1");
    }

    public string? Protect(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return _protector.Protect(value.Trim());
    }

    public string? Unprotect(string? protectedValue)
    {
        if (string.IsNullOrWhiteSpace(protectedValue))
            return null;

        try
        {
            return _protector.Unprotect(protectedValue);
        }
        catch
        {
            return null;
        }
    }
}
