using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AllowanceTracker.Data;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using AllowanceTracker.Handlers;
using AllowanceTracker.Helpers;
using SendGrid;

namespace AllowanceTracker.Lambda;

/// <summary>
/// Startup class for configuring dependency injection in AWS Lambda
/// </summary>
public class Startup
{
    private static IServiceProvider? _serviceProvider;
    private static readonly object _lock = new object();

    public static IServiceProvider ServiceProvider
    {
        get
        {
            if (_serviceProvider == null)
            {
                lock (_lock)
                {
                    if (_serviceProvider == null)
                    {
                        _serviceProvider = ConfigureServices();
                    }
                }
            }
            return _serviceProvider;
        }
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Build configuration from environment variables and appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Add Entity Framework with PostgreSQL (Amazon RDS)
        var connectionString = configuration["DATABASE_CONNECTION_STRING"]
            ?? configuration["ConnectionStrings:DefaultConnection"]
            ?? throw new InvalidOperationException("Connection string not found");

        services.AddDbContext<AllowanceContext>(options =>
            options.UseNpgsql(connectionString));

        // Add Identity (required for AccountService)
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options => {
            options.SignIn.RequireConfirmedAccount = false;
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
        })
        .AddEntityFrameworkStores<AllowanceContext>()
        .AddDefaultTokenProviders();

        // Register all application services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IAllowanceService, AllowanceService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ITransactionAnalyticsService, TransactionAnalyticsService>();
        services.AddScoped<ISavingsAccountService, SavingsAccountService>();
        services.AddScoped<IFamilyService, FamilyService>();
        services.AddScoped<IChildManagementService, ChildManagementService>();
        services.AddScoped<IWishListService, WishListService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ICategoryBudgetService, CategoryBudgetService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Add SendGrid email service
        var sendGridApiKey = configuration["SENDGRID_API_KEY"] ?? configuration["SendGrid:ApiKey"] ?? "";
        services.AddSingleton<ISendGridClient>(new SendGridClient(sendGridApiKey));
        services.AddScoped<IEmailService, SendGridEmailService>();

        // Register cloud-agnostic handlers
        services.AddScoped<AuthHandler>();
        services.AddScoped<ChildrenHandler>();

        // Register AuthorizationHelper for JWT validation
        services.AddSingleton<AuthorizationHelper>();

        return services.BuildServiceProvider();
    }
}
