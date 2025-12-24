namespace AllowanceTracker.Serverless.Abstractions.Configuration;

/// <summary>
/// Cloud-specific configuration abstraction
/// Each cloud provider implements this to provide environment-specific settings
/// </summary>
public interface ICloudConfiguration
{
    /// <summary>
    /// Cloud provider name (AWS, Azure, GCP)
    /// </summary>
    string CloudProvider { get; }

    /// <summary>
    /// Database connection string
    /// </summary>
    string DatabaseConnectionString { get; }

    /// <summary>
    /// JWT secret key for token signing
    /// </summary>
    string JwtSecretKey { get; }

    /// <summary>
    /// JWT issuer
    /// </summary>
    string JwtIssuer { get; }

    /// <summary>
    /// JWT audience
    /// </summary>
    string JwtAudience { get; }

    /// <summary>
    /// SendGrid API key for email service
    /// </summary>
    string? SendGridApiKey { get; }

    /// <summary>
    /// Application Insights / CloudWatch / Cloud Logging instrumentation key
    /// </summary>
    string? MonitoringInstrumentationKey { get; }

    /// <summary>
    /// Environment name (Development, Staging, Production)
    /// </summary>
    string Environment { get; }

    /// <summary>
    /// Get configuration value by key
    /// </summary>
    string? GetValue(string key);

    /// <summary>
    /// Get configuration value with default
    /// </summary>
    T GetValue<T>(string key, T defaultValue);
}
