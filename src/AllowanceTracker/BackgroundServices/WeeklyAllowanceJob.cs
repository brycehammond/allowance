using AllowanceTracker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AllowanceTracker.BackgroundServices;

public class WeeklyAllowanceJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WeeklyAllowanceJob> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24); // Check daily

    public WeeklyAllowanceJob(
        IServiceProvider serviceProvider,
        ILogger<WeeklyAllowanceJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Weekly Allowance Job started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAllowancesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing weekly allowances");
            }

            // Wait for next check interval
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Weekly Allowance Job stopped");
    }

    private async Task ProcessAllowancesAsync()
    {
        _logger.LogInformation("Starting weekly allowance processing");

        using var scope = _serviceProvider.CreateScope();
        var allowanceService = scope.ServiceProvider.GetRequiredService<IAllowanceService>();

        await allowanceService.ProcessAllPendingAllowancesAsync();

        _logger.LogInformation("Completed weekly allowance processing");
    }
}
