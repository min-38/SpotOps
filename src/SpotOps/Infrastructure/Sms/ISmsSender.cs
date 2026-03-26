namespace SpotOps.Infrastructure.Sms;

public interface ISmsSender
{
    Task SendAsync(string toPhoneNumber, string message, CancellationToken cancellationToken = default);
}

