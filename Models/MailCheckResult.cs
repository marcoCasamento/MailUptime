namespace MailUptime.Models;

public class MailCheckResult
{
    public bool PatternMatched { get; set; }
    public bool FailPatternMatched { get; set; }
    public DateTime? LastChecked { get; set; }
    public DateTime? LastReceivedDate { get; set; }
    public string? LastMatchedSubject { get; set; }
    public string? LastFailedSubject { get; set; }
    public string? Error { get; set; }
}
