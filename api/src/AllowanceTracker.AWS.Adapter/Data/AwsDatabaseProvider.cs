using AllowanceTracker.Serverless.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.AWS.Adapter.Data;

/// <summary>
/// AWS implementation of IDatabaseProvider
/// Configures Entity Framework Core to use PostgreSQL (Amazon RDS for PostgreSQL)
/// </summary>
public class AwsDatabaseProvider : IDatabaseProvider
{
    public string ProviderName => "Npgsql";

    public void ConfigureDatabase(DbContextOptionsBuilder optionsBuilder, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));
        }

        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            // Enable retry logic for transient errors (common in cloud databases)
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);

            // Set command timeout
            npgsqlOptions.CommandTimeout(30);

            // Use query splitting for better performance
            npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        });

        // Enable sensitive data logging in development only
        #if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
        #endif
    }

    public bool SupportsFeature(string featureName)
    {
        return featureName switch
        {
            "Transactions" => true,
            "Sequences" => true,
            "StoredProcedures" => true,
            "JsonColumns" => true, // PostgreSQL has excellent JSON support
            "FullTextSearch" => true,
            "Arrays" => true, // PostgreSQL-specific feature
            "JSONB" => true, // PostgreSQL-specific feature
            _ => false
        };
    }
}
