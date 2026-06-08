using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CivicPulse.Core.Interfaces;

namespace CivicPulse.Web.Services;

public class SlaMonitoringService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<SlaMonitoringService> _logger;

    public SlaMonitoringService(IServiceScopeFactory serviceScopeFactory, ILogger<SlaMonitoringService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SLA Monitoring Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);

                using var scope = _serviceScopeFactory.CreateScope();
                var slaService = scope.ServiceProvider.GetRequiredService<ISlaService>();
                await slaService.CheckAllSlasAsync();

                _logger.LogInformation("SLA check completed.");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SLA monitoring service.");
            }
        }

        _logger.LogInformation("SLA Monitoring Service stopped.");
    }
}
