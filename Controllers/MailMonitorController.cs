using MailUptime.Services;
using Microsoft.AspNetCore.Mvc;

namespace MailUptime.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Mail Monitoring")]
public class MailUptimeController : ControllerBase
{
    private readonly IMailUptimeService _MailUptimeService;
    private readonly ILogger<MailUptimeController> _logger;

    public MailUptimeController(IMailUptimeService MailUptimeService, ILogger<MailUptimeController> logger)
    {
        _MailUptimeService = MailUptimeService;
        _logger = logger;
    }

    /// <summary>
    /// Checks if mail was received today for the specified mailbox
    /// </summary>
    /// <param name="mailboxName">The name of the mailbox to check</param>
    /// <returns>Returns 200 if mail was received today, 503 otherwise</returns>
    /// <response code="200">Mail was received today</response>
    /// <response code="503">No mail received today or an error occurred</response>
    [HttpGet("received-today/{mailboxName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult CheckReceivedToday(string mailboxName)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MailboxName"] = mailboxName,
            ["Endpoint"] = "CheckReceivedToday"
        });

        _logger.LogInformation("API call: CheckReceivedToday for {MailboxName}", mailboxName);

        var result = _MailUptimeService.GetMailStatus(mailboxName);

        if (!string.IsNullOrEmpty(result.Error))
        {
            _logger.LogWarning("CheckReceivedToday failed for {MailboxName}: {Error}", mailboxName, result.Error);
            return StatusCode(503, new { message = result.Error });
        }

        if (result.PatternMatched)
        {
            _logger.LogInformation("CheckReceivedToday: Report received for {MailboxName}, returning 200 OK", mailboxName);
            return Ok(new 
            { 
                message = "Report received today",
                lastChecked = result.LastChecked,
                lastReceivedDate = result.LastReceivedDate
            });
        }

        _logger.LogInformation("CheckReceivedToday: No report received for {MailboxName}, returning 503", mailboxName);
        return StatusCode(503, new { message = "No report received today" });
    }

    /// <summary>
    /// Checks if a mail matching the expected pattern was received for the specified mailbox
    /// </summary>
    /// <param name="mailboxName">The name of the mailbox to check</param>
    /// <returns>Returns 200 if pattern was matched, 503 otherwise</returns>
    /// <response code="200">Pattern was matched in a received mail</response>
    /// <response code="503">Pattern not matched or an error occurred</response>
    [HttpGet("pattern-matched/{mailboxName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult CheckPatternMatched(string mailboxName)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MailboxName"] = mailboxName,
            ["Endpoint"] = "CheckPatternMatched"
        });

        _logger.LogInformation("API call: CheckPatternMatched for {MailboxName}", mailboxName);

        var result = _MailUptimeService.GetMailStatus(mailboxName);

        if (!string.IsNullOrEmpty(result.Error))
        {
            _logger.LogWarning("CheckPatternMatched failed for {MailboxName}: {Error}", mailboxName, result.Error);
            return StatusCode(503, new { message = result.Error });
        }

        if (result.PatternMatched)
        {
            _logger.LogInformation("CheckPatternMatched: Pattern matched for {MailboxName}, returning 200 OK", mailboxName);
            return Ok(new 
            { 
                message = "Pattern matched",
                lastChecked = result.LastChecked,
                lastReceivedDate = result.LastReceivedDate,
                lastMatchedSubject = result.LastMatchedSubject
            });
        }

        _logger.LogInformation("CheckPatternMatched: Pattern not matched for {MailboxName}, returning 503", mailboxName);
        return StatusCode(503, new { message = "Pattern not matched" });
    }

    /// <summary>
    /// Checks if a mail matching the fail pattern was received for the specified mailbox
    /// </summary>
    /// <param name="mailboxName">The name of the mailbox to check</param>
    /// <returns>Returns 503 if fail pattern was matched (indicating a problem), 200 if no failures detected</returns>
    /// <response code="200">No failure pattern detected</response>
    /// <response code="503">Failure pattern was matched, indicating an issue</response>
    [HttpGet("fail-pattern-matched/{mailboxName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult CheckFailPatternMatched(string mailboxName)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MailboxName"] = mailboxName,
            ["Endpoint"] = "CheckFailPatternMatched"
        });

        _logger.LogInformation("API call: CheckFailPatternMatched for {MailboxName}", mailboxName);

        var result = _MailUptimeService.GetMailStatus(mailboxName);

        if (!string.IsNullOrEmpty(result.Error))
        {
            _logger.LogWarning("CheckFailPatternMatched request failed for {MailboxName}: {Error}", mailboxName, result.Error);
            return StatusCode(503, new { message = result.Error });
        }

        if (result.FailPatternMatched)
        {
            _logger.LogWarning("CheckFailPatternMatched: FAILURE detected for {MailboxName}, Subject: {Subject}, returning 503", 
                mailboxName, result.LastFailedSubject);
            return StatusCode(503, new 
            { 
                message = "Failure pattern matched - issue detected",
                lastChecked = result.LastChecked,
                lastFailedSubject = result.LastFailedSubject
            });
        }

        _logger.LogInformation("CheckFailPatternMatched: No failures detected for {MailboxName}, returning 200 OK", mailboxName);
        return Ok(new { message = "No failures detected" });
    }

    /// <summary>
    /// Gets the full status information for the specified mailbox
    /// </summary>
    /// <param name="mailboxName">The name of the mailbox to check</param>
    /// <returns>Returns detailed status information about the mailbox</returns>
    /// <response code="200">Status information retrieved successfully</response>
    [HttpGet("status/{mailboxName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetStatus(string mailboxName)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MailboxName"] = mailboxName,
            ["Endpoint"] = "GetStatus"
        });

        _logger.LogInformation("API call: GetStatus for {MailboxName}", mailboxName);

        var result = _MailUptimeService.GetMailStatus(mailboxName);
        
        _logger.LogDebug("GetStatus returning full status for {MailboxName}: PatternMatched={PatternMatched}, FailPatternMatched={FailPatternMatched}", 
            mailboxName, result.PatternMatched, result.FailPatternMatched);
        
        return Ok(result);
    }
}
