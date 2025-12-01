namespace MailUptime.Models;

public class MailboxConfiguration
{
    public string Name { get; set; } = string.Empty;
    
    // These properties can be null to inherit from MailboxSettings
    public MailProtocol? Protocol { get; set; }
    public string? Host { get; set; }
    public int? Port { get; set; }
    public bool? UseSsl { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int? PollingFrequencySeconds { get; set; }
    public List<string>? ExpectedSenderEmails { get; set; }
    
    // These properties are always specific to the report configuration
    public string? ExpectedSubjectPattern { get; set; }
    public string? ExpectedBodyPattern { get; set; }
    public string? FailSubjectPattern { get; set; }
    public string? FailBodyPattern { get; set; }
    
    // Method to get effective configuration with inheritance
    public MailboxConfiguration GetEffectiveConfiguration(MailboxSettings defaults)
    {
        return new MailboxConfiguration
        {
            Name = this.Name,
            Protocol = this.Protocol ?? defaults.Protocol ?? MailProtocol.Imap,
            Host = this.Host ?? defaults.Host ?? string.Empty,
            Port = this.Port ?? defaults.Port ?? 993,
            UseSsl = this.UseSsl ?? defaults.UseSsl ?? true,
            Username = this.Username ?? defaults.Username ?? string.Empty,
            Password = this.Password ?? defaults.Password ?? string.Empty,
            PollingFrequencySeconds = this.PollingFrequencySeconds ?? defaults.PollingFrequencySeconds ?? 60,
            ExpectedSenderEmails = this.ExpectedSenderEmails ?? defaults.ExpectedSenderEmails ?? new(),
            ExpectedSubjectPattern = this.ExpectedSubjectPattern,
            ExpectedBodyPattern = this.ExpectedBodyPattern,
            FailSubjectPattern = this.FailSubjectPattern,
            FailBodyPattern = this.FailBodyPattern
        };
    }
}

public enum MailProtocol
{
    Imap,
    Pop3
}
