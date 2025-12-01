using MailUptime.Services;
using Microsoft.AspNetCore.Mvc;

namespace MailUptime.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IMailUptimeService _MailUptimeService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IMailUptimeService MailUptimeService, ILogger<DashboardController> logger)
    {
        _MailUptimeService = MailUptimeService;
        _logger = logger;
    }

    /// <summary>
    /// Gets status information for all configured mailboxes
    /// </summary>
    /// <returns>List of mailbox statuses</returns>
    /// <response code="200">Returns the list of all mailbox statuses</response>
    [HttpGet("mailboxes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAllMailboxStatuses()
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Endpoint"] = "GetAllMailboxStatuses"
        });

        _logger.LogInformation("API call: Dashboard GetAllMailboxStatuses");

        try
        {
            var statuses = _MailUptimeService.GetAllMailboxStatuses().ToList();
            
            _logger.LogInformation("Dashboard returned status for {MailboxCount} mailboxes", statuses.Count);
            _logger.LogDebug("Mailboxes: {Mailboxes}", string.Join(", ", statuses.Select(s => s.Name)));
            
            return Ok(statuses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve mailbox statuses for dashboard");
            return StatusCode(500, new { message = "Failed to retrieve mailbox statuses" });
        }
    }
}
