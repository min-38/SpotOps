using Microsoft.EntityFrameworkCore;
using SpotOps.Data;

namespace SpotOps.Features.Me.Profile;

public sealed class MyProfileService
{
    private readonly AppDbContext _db;

    public MyProfileService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<MyProfileDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new MyProfileDto(u.Id, u.Email, u.Name, u.Phone, u.PhoneVerifiedAt, u.Role, u.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(MyProfileDto? Profile, string? ErrorCode, string? ErrorMessage)> UpdateAsync(
        Guid userId,
        UpdateMyProfileRequest req,
        CancellationToken cancellationToken = default)
    {
        var name = req.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return (null, "ME_PROFILE_NAME_REQUIRED", "이름은 비어 있을 수 없어요.");

        if (name.Length > 100)
            return (null, "ME_PROFILE_NAME_TOO_LONG", "이름이 너무 길어요.");

        var phone = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim();
        if (phone is not null && phone.Length > 30)
            return (null, "ME_PROFILE_PHONE_TOO_LONG", "전화번호가 너무 길어요.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return (null, "ME_PROFILE_NOT_FOUND", "사용자 정보를 찾을 수 없어요.");

        user.Name = name;
        if (!string.Equals(user.Phone, phone, StringComparison.Ordinal))
            user.PhoneVerifiedAt = null;
        user.Phone = phone;
        await _db.SaveChangesAsync(cancellationToken);

        return (new MyProfileDto(user.Id, user.Email, user.Name, user.Phone, user.PhoneVerifiedAt, user.Role, user.CreatedAt), null, null);
    }
}

