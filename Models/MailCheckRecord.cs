namespace MailUptime.Models;

public class MailCheckRecord
{
    public int Id { get; set; }
    public string MailboxIdentifier { get; set; } = string.Empty;
    public DateTime Day { get; set; }
    public bool PatternMatched { get; set; }
    public bool FailPatternMatched { get; set; }
    public DateTime LastCheckTime { get; set; }
    public DateTime? LastReceivedTime { get; set; }
    public string? LastMatchedSubject { get; set; }
    public string? LastFailedSubject { get; set; }
}
