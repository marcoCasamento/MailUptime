namespace MailUptime.Services;

public class MailUptimeBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MailUptimeBackgroundService> _logger;

    public MailUptimeBackgroundService(IServiceProvider serviceProvider, ILogger<MailUptimeBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Service"] = "MailUptimeBackgroundService"
        });

        _logger.LogInformation("Mail Monitor Background Service is starting");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var MailUptimeService = scope.ServiceProvider.GetRequiredService<IMailUptimeService>();

            _logger.LogInformation("Mail Monitor service resolved, beginning monitoring");
            await MailUptimeService.StartMonitoringAsync(stoppingToken);
            
            _logger.LogInformation("Mail Monitor Background Service has stopped normally");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Mail Monitor Background Service cancellation requested");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Mail Monitor Background Service encountered a fatal error and is stopping");
            throw;
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Mail Monitor Background Service StartAsync called");
        await base.StartAsync(cancellationToken);
        _logger.LogInformation("Mail Monitor Background Service started successfully");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Mail Monitor Background Service StopAsync called");
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Mail Monitor Background Service stopped successfully");
    }
}
