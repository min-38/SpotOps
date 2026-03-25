namespace SpotOps.Infrastructure.PortOne;

public sealed class PortOneOptions
{
    /// <summary>포트원 콘솔의 storeId (연동 정보).</summary>
    public string StoreId { get; set; } = "";

    /// <summary>API Secret — REST V2 로그인 및 단건 조회에 사용.</summary>
    public string ApiSecret { get; set; } = "";

    /// <summary>웹훅 시그릿. 설정 시 Standard Webhooks 서명 검증(추가 구현 가능). 비어 있으면 API 단건 조회로만 검증.</summary>
    public string WebhookSecret { get; set; } = "";

    public string ApiBaseUrl { get; set; } = "https://api.portone.io";
}
