using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AllowanceTracker.Data;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using AllowanceTracker.Handlers;
using AllowanceTracker.Helpers;
using SendGrid;

var builder = FunctionsApplication.CreateBuilder(args);

// Configure Functions to use ASP.NET Core integration for better HTTP support
builder.ConfigureFunctionsWebApplication();

// Add Application Insights
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Add Entity Framework with SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

builder.Services.AddDbContext<AllowanceContext>(options =>
    options.UseSqlServer(connectionString));

// Add Identity (required for AccountService)
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options => {
    options.SignIn.RequireConfirmedAccount = false; // MVP simplification
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AllowanceContext>()
.AddDefaultTokenProviders();

// Register all application services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAllowanceService, AllowanceService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ITransactionAnalyticsService, TransactionAnalyticsService>();
builder.Services.AddScoped<ISavingsAccountService, SavingsAccountService>();
builder.Services.AddScoped<IFamilyService, FamilyService>();
builder.Services.AddScoped<IChildManagementService, ChildManagementService>();
builder.Services.AddScoped<IWishListService, WishListService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICategoryBudgetService, CategoryBudgetService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Add SendGrid email service
var sendGridApiKey = builder.Configuration["SendGrid:ApiKey"] ?? "";
builder.Services.AddSingleton<ISendGridClient>(new SendGridClient(sendGridApiKey));
builder.Services.AddScoped<IEmailService, SendGridEmailService>();

// Register cloud-agnostic handlers
builder.Services.AddScoped<AuthHandler>();
builder.Services.AddScoped<ChildrenHandler>();

// Register AuthorizationHelper for JWT validation
builder.Services.AddSingleton<AuthorizationHelper>();

builder.Build().Run();
