using System.ComponentModel.DataAnnotations;

namespace SpotOps.Models;

public class User
{
    // PK
    public Guid Id { get; set; } = Guid.NewGuid();

    // 이메일
    [Required]
    public string Email { get; set; } = "";

    // 비밀번호 해시
    [Required]
    public string PasswordHash { get; set; } = "";

    // 이름
    [Required]
    public string Name { get; set; } = "";

    // 전화번호
    [Required]
    public string? Phone { get; set; }

    // 마지막 로그인 일시
    public DateTime? LastLoginAt { get; set; } = null;

    // 계정 활성화 여부
    public bool IsActive { get; set; } = true;

    // 계정 생성 일시
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 계정 수정 일시
    public DateTime? UpdatedAt { get; set; } = null;

    // 계정 삭제 일시 (soft delete)
    public DateTime? DeletedAt { get; set; } = null;
}
