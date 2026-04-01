namespace SpotOps.Models;

public sealed class UserVerification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }

    // 인증 제공자 (예: PASS)
    public VerificationProvider Provider { get; set; } = VerificationProvider.Pass;
    
    // 인증 상태
    public VerificationStatus Status { get; set; } = VerificationStatus.Pending;
    
    // "이 인증 시도(이력 레코드)"가 성공한 시각.
    // User.VerifiedAt 과 달리, 건별 인증 이력 추적용 값이다.
    public DateTime? VerifiedAt { get; set; }

    // PASS/본인확인 결과 정보

    // 이름
    public string? Name { get; set; }
    
    // 생년월일
    public DateOnly? BirthDate { get; set; }
    
    // 성별
    public VerificationGender? Gender { get; set; }
    
    // 외국인 여부
    public bool? IsForeigner { get; set; }
    
    // 통신사
    public string? Telecom { get; set; }
    
    // 전화번호
    public string? PhoneNumber { get; set; }

    // 연계 식별자
    public string? Ci { get; set; }
    public string? Di { get; set; }

    // 외부 트랜잭션 참조값
    public string? ProviderTransactionId { get; set; }

    // 생성 일시
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 수정 일시
    public DateTime? UpdatedAt { get; set; } = null;
}

public enum VerificationProvider
{
    Pass
}

public enum VerificationStatus
{
    Pending,
    Verified,
    Failed
}

public enum VerificationGender
{
    Male,
    Female,
    Unknown
}
