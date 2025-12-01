namespace MailUptime.Models;

public class MailboxSettings
{
    // Default settings that can be inherited by ReportConfig instances
    public MailProtocol? Protocol { get; set; }
    public string? Host { get; set; }
    public int? Port { get; set; }
    public bool? UseSsl { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int? PollingFrequencySeconds { get; set; }
    public List<string>? ExpectedSenderEmails { get; set; }
    
    public List<MailboxConfiguration> ReportConfig { get; set; } = new();
}
