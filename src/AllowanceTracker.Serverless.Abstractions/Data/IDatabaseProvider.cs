using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Serverless.Abstractions.Data;

/// <summary>
/// Cloud-agnostic database provider abstraction
/// Configures Entity Framework Core for different cloud providers
/// </summary>
public interface IDatabaseProvider
{
    /// <summary>
    /// Configure DbContext options for the specific cloud provider's database
    /// </summary>
    /// <param name="optionsBuilder">EF Core options builder</param>
    /// <param name="connectionString">Connection string from cloud configuration</param>
    void ConfigureDatabase(DbContextOptionsBuilder optionsBuilder, string connectionString);

    /// <summary>
    /// Get the database provider name (for logging/diagnostics)
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Whether this provider supports certain features (e.g., migrations, sequences)
    /// </summary>
    bool SupportsFeature(string featureName);
}
