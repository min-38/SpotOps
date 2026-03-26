namespace SpotOps.Infrastructure.Email;

public sealed class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public string FromEmail { get; set; } = "noreply@example.com";
    public string FromName { get; set; } = "SpotOps";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool EnableSsl { get; set; } = false;
}

