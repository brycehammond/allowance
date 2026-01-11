using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AllowanceTracker.Services;

/// <summary>
/// Background job that handles scheduled tasks including:
/// - Generating recurring task instances for tasks due that day (daily, weekly, monthly)
/// - Checking and marking expired savings goal challenges as failed
/// Runs hourly to ensure timely processing.
/// </summary>
public class RecurringTasksJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RecurringTasksJob> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public RecurringTasksJob(
        IServiceProvider serviceProvider,
        ILogger<RecurringTasksJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RecurringTasksJob started");

        // Wait a bit before first run to let the application start up
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();

                var generatedCount = await taskService.GenerateRecurringTasksAsync();

                if (generatedCount > 0)
                {
                    _logger.LogInformation(
                        "Generated {Count} recurring task instances",
                        generatedCount);
                }
                else
                {
                    _logger.LogDebug("No recurring tasks to generate");
                }

                // Check for expired savings goal challenges
                var savingsGoalService = scope.ServiceProvider.GetRequiredService<ISavingsGoalService>();
                await savingsGoalService.CheckExpiredChallengesAsync();
                _logger.LogDebug("Checked expired savings goal challenges");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in recurring tasks job");
            }

            // Wait for the next check interval
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("RecurringTasksJob stopped");
    }
}
