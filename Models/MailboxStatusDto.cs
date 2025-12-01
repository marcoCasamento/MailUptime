namespace MailUptime.Models;

public class MailboxStatusDto
{
    public string Name { get; set; } = string.Empty;
    public bool PatternMatched { get; set; }
    public bool FailPatternMatched { get; set; }
    public DateTime? LastChecked { get; set; }
    public DateTime? LastReceivedDate { get; set; }
    public string? LastMatchedSubject { get; set; }
    public string? LastFailedSubject { get; set; }
    public string? Error { get; set; }
    public bool HasPatternConfiguration { get; set; }
    public bool HasFailPatternConfiguration { get; set; }
    public bool HasSenderConfiguration { get; set; }
    public List<string> ExpectedSenders { get; set; } = new();
}
