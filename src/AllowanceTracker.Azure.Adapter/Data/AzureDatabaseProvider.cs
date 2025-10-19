using AllowanceTracker.Serverless.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Azure.Adapter.Data;

/// <summary>
/// Azure implementation of IDatabaseProvider
/// Configures Entity Framework Core to use SQL Server (Azure SQL Database)
/// </summary>
public class AzureDatabaseProvider : IDatabaseProvider
{
    public string ProviderName => "SqlServer";

    public void ConfigureDatabase(DbContextOptionsBuilder optionsBuilder, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));
        }

        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            // Enable retry logic for transient errors (common in cloud databases)
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);

            // Set command timeout
            sqlOptions.CommandTimeout(30);

            // Use Azure SQL-specific optimizations
            sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        });

        // Enable sensitive data logging in development only
        // This should be controlled by configuration in production
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
            "JsonColumns" => true, // SQL Server 2016+
            "FullTextSearch" => true,
            "SpatialData" => true,
            _ => false
        };
    }
}
