namespace SpotOps.Infrastructure.Sms;

/// <summary>
/// 개발/초기 운영용 SMS 대체 구현.
/// 실제 발송 대신 서버 로그에만 남긴다.
/// </summary>
public sealed class LoggingSmsSender : ISmsSender
{
    private readonly ILogger<LoggingSmsSender> _logger;

    public LoggingSmsSender(ILogger<LoggingSmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toPhoneNumber, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "SMS (mock) => to:{ToPhoneNumber}, message:{Message}",
            toPhoneNumber,
            message);

        return Task.CompletedTask;
    }
}

