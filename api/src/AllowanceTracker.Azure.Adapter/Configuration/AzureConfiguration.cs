using AllowanceTracker.Serverless.Abstractions.Configuration;
using Microsoft.Extensions.Configuration;

namespace AllowanceTracker.Azure.Adapter.Configuration;

/// <summary>
/// Azure Functions implementation of ICloudConfiguration
/// Reads configuration from Azure Functions app settings and environment variables
/// </summary>
public class AzureConfiguration : ICloudConfiguration
{
    private readonly IConfiguration _configuration;

    public AzureConfiguration(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public string CloudProvider => "Azure";

    public string DatabaseConnectionString =>
        _configuration["SqlConnectionString"]
        ?? _configuration["ConnectionStrings:DefaultConnection"]
        ?? throw new InvalidOperationException("Database connection string not configured. Set SqlConnectionString or ConnectionStrings:DefaultConnection");

    public string JwtSecretKey =>
        _configuration["Jwt:SecretKey"]
        ?? throw new InvalidOperationException("JWT secret key not configured. Set Jwt:SecretKey");

    public string JwtIssuer =>
        _configuration["Jwt:Issuer"]
        ?? "AllowanceTracker";

    public string JwtAudience =>
        _configuration["Jwt:Audience"]
        ?? "AllowanceTracker";

    public string? SendGridApiKey =>
        _configuration["SendGrid:ApiKey"];

    public string? MonitoringInstrumentationKey =>
        _configuration["APPINSIGHTS_INSTRUMENTATIONKEY"]
        ?? _configuration["ApplicationInsights:InstrumentationKey"];

    public string Environment =>
        _configuration["AZURE_FUNCTIONS_ENVIRONMENT"]
        ?? _configuration["ASPNETCORE_ENVIRONMENT"]
        ?? "Production";

    public string? GetValue(string key)
    {
        return _configuration[key];
    }

    public T GetValue<T>(string key, T defaultValue)
    {
        var value = _configuration[key];
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }
}
