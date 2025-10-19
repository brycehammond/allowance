using AllowanceTracker.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AllowanceTracker.Functions;

/// <summary>
/// Azure Function that processes weekly allowances for all eligible children.
/// Runs daily at 10:00 AM UTC to check for children due for allowance payment.
/// </summary>
public class WeeklyAllowanceFunction
{
    private readonly IAllowanceService _allowanceService;
    private readonly ILogger<WeeklyAllowanceFunction> _logger;

    public WeeklyAllowanceFunction(
        IAllowanceService allowanceService,
        ILogger<WeeklyAllowanceFunction> logger)
    {
        _allowanceService = allowanceService;
        _logger = logger;
    }

    /// <summary>
    /// Timer trigger that runs daily at 10:00 AM UTC.
    /// NCRONTAB format: {second} {minute} {hour} {day} {month} {day-of-week}
    /// "0 0 10 * * *" = At 10:00:00 AM every day
    /// </summary>
    [Function("ProcessWeeklyAllowances")]
    public async Task Run([TimerTrigger("0 0 10 * * *")] TimerInfo timer)
    {
        _logger.LogInformation("Weekly Allowance Function triggered at: {Time}", DateTime.UtcNow);

        if (timer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {NextSchedule}", timer.ScheduleStatus.Next);
        }

        try
        {
            _logger.LogInformation("Starting weekly allowance processing");

            await _allowanceService.ProcessAllPendingAllowancesAsync();

            _logger.LogInformation("Successfully completed weekly allowance processing at: {Time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing weekly allowances at: {Time}", DateTime.UtcNow);
            throw; // Re-throw to trigger Azure Function retry policy
        }
    }

    /// <summary>
    /// Manual trigger endpoint for testing purposes.
    /// Can be invoked via HTTP POST to manually trigger allowance processing.
    /// </summary>
    [Function("ProcessWeeklyAllowancesManual")]
    public async Task<HttpResponseData> RunManual(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("Manual weekly allowance processing triggered at: {Time}", DateTime.UtcNow);

        try
        {
            await _allowanceService.ProcessAllPendingAllowancesAsync();

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                message = "Weekly allowances processed successfully",
                timestamp = DateTime.UtcNow
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in manual allowance processing");

            var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new
            {
                message = "Error processing allowances",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });

            return response;
        }
    }
}
