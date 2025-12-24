using AllowanceTracker.Serverless.Abstractions.Configuration;
using Microsoft.Extensions.Configuration;

namespace AllowanceTracker.AWS.Adapter.Configuration;

/// <summary>
/// AWS Lambda implementation of ICloudConfiguration
/// Reads configuration from Lambda environment variables
/// </summary>
public class AwsConfiguration : ICloudConfiguration
{
    private readonly IConfiguration _configuration;

    public AwsConfiguration(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public string CloudProvider => "AWS";

    public string DatabaseConnectionString =>
        _configuration["DATABASE_CONNECTION_STRING"]
        ?? _configuration["ConnectionStrings:DefaultConnection"]
        ?? throw new InvalidOperationException("Database connection string not configured. Set DATABASE_CONNECTION_STRING environment variable");

    public string JwtSecretKey =>
        _configuration["JWT_SECRET_KEY"]
        ?? _configuration["Jwt:SecretKey"]
        ?? throw new InvalidOperationException("JWT secret key not configured. Set JWT_SECRET_KEY environment variable");

    public string JwtIssuer =>
        _configuration["JWT_ISSUER"]
        ?? _configuration["Jwt:Issuer"]
        ?? "AllowanceTracker";

    public string JwtAudience =>
        _configuration["JWT_AUDIENCE"]
        ?? _configuration["Jwt:Audience"]
        ?? "AllowanceTracker";

    public string? SendGridApiKey =>
        _configuration["SENDGRID_API_KEY"]
        ?? _configuration["SendGrid:ApiKey"];

    public string? MonitoringInstrumentationKey =>
        _configuration["AWS_XRAY_DAEMON_ADDRESS"];

    public string Environment =>
        _configuration["LAMBDA_ENVIRONMENT"]
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
