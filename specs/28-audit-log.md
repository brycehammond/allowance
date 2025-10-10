# Audit Log & Activity Tracking Specification

## Overview

This specification implements comprehensive audit logging for all critical actions within the Allowance Tracker application. The audit trail provides accountability, security monitoring, and compliance tracking by recording who did what, when, and from where.

## Goals

1. **Comprehensive Logging**: Track all critical actions (login, transactions, settings changes)
2. **Change Tracking**: Store before/after values for all modifications
3. **Security Monitoring**: IP address, user agent, and timestamp for all actions
4. **Audit Viewer**: Parents-only interface to view activity logs
5. **Filtering**: Search and filter logs by user, action, date, entity type
6. **Export**: Export audit logs to CSV for compliance/analysis
7. **Retention Policy**: Automatic cleanup of logs older than 1 year
8. **TDD Approach**: 15 comprehensive tests

## Technology Stack

- **Backend**: ASP.NET Core 8.0 with Entity Framework Core
- **Database**: PostgreSQL with JSONB for change tracking
- **Middleware**: Custom audit logging middleware
- **Background Job**: IHostedService for log retention cleanup
- **Export**: CsvHelper for CSV generation
- **Testing**: xUnit, FluentAssertions, Moq

---

## Phase 1: Database Schema

### 1.1 AuditLog Model

```csharp
namespace AllowanceTracker.Models;

/// <summary>
/// Audit trail of all critical actions in the system
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }

    /// <summary>
    /// User who performed the action
    /// </summary>
    public Guid? UserId { get; set; }
    public virtual ApplicationUser? User { get; set; }

    /// <summary>
    /// Family context for the action
    /// </summary>
    public Guid? FamilyId { get; set; }
    public virtual Family? Family { get; set; }

    /// <summary>
    /// Action performed
    /// </summary>
    public AuditAction Action { get; set; }

    /// <summary>
    /// Type of entity affected
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the affected entity
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// JSON representation of changes (before/after)
    /// </summary>
    public string? ChangesJson { get; set; }

    /// <summary>
    /// IP address of the user
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string (browser/device info)
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional context or notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Was this action successful?
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Error message if action failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum AuditAction
{
    // Authentication
    Login = 1,
    Logout = 2,
    LoginFailed = 3,
    PasswordChanged = 4,
    TwoFactorEnabled = 5,
    TwoFactorDisabled = 6,

    // User Management
    UserCreated = 10,
    UserUpdated = 11,
    UserDeleted = 12,
    ChildAdded = 13,
    ChildRemoved = 14,

    // Transactions
    TransactionCreated = 20,
    TransactionUpdated = 21,
    TransactionDeleted = 22,

    // Allowance
    AllowanceUpdated = 30,
    AllowancePaid = 31,

    // Savings Goals
    SavingsGoalCreated = 40,
    SavingsGoalUpdated = 41,
    SavingsGoalCompleted = 42,
    SavingsGoalDeleted = 43,

    // Chores
    ChoreCreated = 50,
    ChoreCompleted = 51,
    ChoreApproved = 52,
    ChoreRejected = 53,

    // Settings
    SettingsChanged = 60,
    FamilySettingsChanged = 61,

    // Security
    RecoveryCodeUsed = 70,
    TrustedDeviceAdded = 71,
    TrustedDeviceRevoked = 72,

    // Data Export
    DataExported = 80,
    AuditLogExported = 81
}
```

### 1.2 Database Configuration

```csharp
public class AllowanceContext : DbContext
{
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EntityType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.ChangesJson)
                .HasColumnType("jsonb"); // PostgreSQL JSONB

            entity.Property(e => e.IpAddress)
                .HasMaxLength(45); // IPv6 max length

            entity.Property(e => e.UserAgent)
                .HasMaxLength(500);

            entity.Property(e => e.Notes)
                .HasMaxLength(1000);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Family)
                .WithMany()
                .HasForeignKey(e => e.FamilyId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes for common queries
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.FamilyId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
        });
    }
}
```

---

## Phase 2: Audit Logging Service

### 2.1 IAuditLogService Interface

```csharp
namespace AllowanceTracker.Services;

public interface IAuditLogService
{
    /// <summary>
    /// Log an audit event
    /// </summary>
    Task LogAsync(AuditLogEntry entry);

    /// <summary>
    /// Get audit logs with filtering
    /// </summary>
    Task<AuditLogSearchResultDto> GetLogsAsync(
        Guid familyId,
        AuditLogFilterDto filter);

    /// <summary>
    /// Get audit logs for a specific entity
    /// </summary>
    Task<List<AuditLogDto>> GetEntityHistoryAsync(
        string entityType,
        Guid entityId);

    /// <summary>
    /// Export audit logs to CSV
    /// </summary>
    Task<byte[]> ExportLogsAsync(
        Guid familyId,
        AuditLogFilterDto filter);

    /// <summary>
    /// Delete logs older than retention period
    /// </summary>
    Task<int> CleanupOldLogsAsync(int retentionDays = 365);

    /// <summary>
    /// Get audit statistics
    /// </summary>
    Task<AuditStatisticsDto> GetStatisticsAsync(
        Guid familyId,
        DateTime? startDate = null,
        DateTime? endDate = null);
}
```

### 2.2 DTOs

```csharp
namespace AllowanceTracker.DTOs;

public record AuditLogEntry(
    Guid? UserId,
    Guid? FamilyId,
    AuditAction Action,
    string EntityType,
    Guid? EntityId,
    object? ChangesBefore,
    object? ChangesAfter,
    string? IpAddress,
    string? UserAgent,
    string? Notes = null,
    bool Success = true,
    string? ErrorMessage = null);

public record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string? UserName,
    Guid? FamilyId,
    AuditAction Action,
    string ActionName,
    string EntityType,
    Guid? EntityId,
    string? Changes,
    string? IpAddress,
    string? UserAgent,
    string? Notes,
    bool Success,
    string? ErrorMessage,
    DateTime CreatedAt);

public record AuditLogFilterDto(
    Guid? UserId = null,
    AuditAction? Action = null,
    string? EntityType = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    bool? SuccessOnly = null,
    int Page = 1,
    int PageSize = 50);

public record AuditLogSearchResultDto(
    List<AuditLogDto> Logs,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public record AuditStatisticsDto(
    int TotalEvents,
    int LoginCount,
    int TransactionCount,
    int FailedAttempts,
    Dictionary<AuditAction, int> ActionCounts,
    Dictionary<string, int> UserActivityCounts);
```

### 2.3 AuditLogService Implementation

```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Services;

public class AuditLogService : IAuditLogService
{
    private readonly AllowanceContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        AllowanceContext context,
        ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(AuditLogEntry entry)
    {
        try
        {
            var changesJson = SerializeChanges(entry.ChangesBefore, entry.ChangesAfter);

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = entry.UserId,
                FamilyId = entry.FamilyId,
                Action = entry.Action,
                EntityType = entry.EntityType,
                EntityId = entry.EntityId,
                ChangesJson = changesJson,
                IpAddress = entry.IpAddress,
                UserAgent = entry.UserAgent,
                Notes = entry.Notes,
                Success = entry.Success,
                ErrorMessage = entry.ErrorMessage,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Don't throw - logging should never break the application
            _logger.LogError(ex, "Failed to write audit log for action {Action}", entry.Action);
        }
    }

    public async Task<AuditLogSearchResultDto> GetLogsAsync(
        Guid familyId,
        AuditLogFilterDto filter)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.FamilyId == familyId)
            .AsNoTracking();

        // Apply filters
        query = ApplyFilters(query, filter);

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination
        var pageSize = Math.Min(filter.PageSize, 100);
        var skip = (filter.Page - 1) * pageSize;
        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        // Map to DTOs
        var logDtos = logs.Select(MapToDto).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new AuditLogSearchResultDto(
            logDtos,
            totalCount,
            filter.Page,
            pageSize,
            totalPages);
    }

    public async Task<List<AuditLogDto>> GetEntityHistoryAsync(
        string entityType,
        Guid entityId)
    {
        var logs = await _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

        return logs.Select(MapToDto).ToList();
    }

    public async Task<byte[]> ExportLogsAsync(
        Guid familyId,
        AuditLogFilterDto filter)
    {
        // Get all matching logs
        var allLogsFilter = filter with { Page = 1, PageSize = int.MaxValue };
        var result = await GetLogsAsync(familyId, allLogsFilter);

        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.WriteRecords(result.Logs.Select(log => new
        {
            Timestamp = log.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            User = log.UserName ?? "System",
            Action = log.ActionName,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            Changes = log.Changes,
            IpAddress = log.IpAddress,
            Success = log.Success ? "Yes" : "No",
            Error = log.ErrorMessage
        }));

        await writer.FlushAsync();

        // Log the export action
        await LogAsync(new AuditLogEntry(
            null,
            familyId,
            AuditAction.AuditLogExported,
            "AuditLog",
            null,
            null,
            null,
            null,
            null,
            $"Exported {result.TotalCount} audit log entries"
        ));

        return memoryStream.ToArray();
    }

    public async Task<int> CleanupOldLogsAsync(int retentionDays = 365)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        var oldLogs = await _context.AuditLogs
            .Where(a => a.CreatedAt < cutoffDate)
            .ToListAsync();

        var count = oldLogs.Count;

        if (count > 0)
        {
            _context.AuditLogs.RemoveRange(oldLogs);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} audit logs older than {Days} days",
                count, retentionDays);
        }

        return count;
    }

    public async Task<AuditStatisticsDto> GetStatisticsAsync(
        Guid familyId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.AuditLogs
            .Where(a => a.FamilyId == familyId)
            .AsNoTracking();

        if (startDate.HasValue)
            query = query.Where(a => a.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.CreatedAt <= endDate.Value);

        var logs = await query.ToListAsync();

        var totalEvents = logs.Count;
        var loginCount = logs.Count(a => a.Action == AuditAction.Login);
        var transactionCount = logs.Count(a => a.Action == AuditAction.TransactionCreated);
        var failedAttempts = logs.Count(a => !a.Success);

        var actionCounts = logs
            .GroupBy(a => a.Action)
            .ToDictionary(g => g.Key, g => g.Count());

        var userActivityCounts = await _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.FamilyId == familyId && a.UserId != null)
            .GroupBy(a => new { a.UserId, a.User!.FirstName, a.User.LastName })
            .Select(g => new
            {
                UserName = $"{g.Key.FirstName} {g.Key.LastName}",
                Count = g.Count()
            })
            .ToDictionaryAsync(x => x.UserName, x => x.Count);

        return new AuditStatisticsDto(
            totalEvents,
            loginCount,
            transactionCount,
            failedAttempts,
            actionCounts,
            userActivityCounts);
    }

    private IQueryable<AuditLog> ApplyFilters(
        IQueryable<AuditLog> query,
        AuditLogFilterDto filter)
    {
        if (filter.UserId.HasValue)
            query = query.Where(a => a.UserId == filter.UserId.Value);

        if (filter.Action.HasValue)
            query = query.Where(a => a.Action == filter.Action.Value);

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
            query = query.Where(a => a.EntityType == filter.EntityType);

        if (filter.StartDate.HasValue)
            query = query.Where(a => a.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
        {
            var endOfDay = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(a => a.CreatedAt <= endOfDay);
        }

        if (filter.SuccessOnly.HasValue)
            query = query.Where(a => a.Success == filter.SuccessOnly.Value);

        return query;
    }

    private AuditLogDto MapToDto(AuditLog log)
    {
        return new AuditLogDto(
            log.Id,
            log.UserId,
            log.User != null ? $"{log.User.FirstName} {log.User.LastName}" : null,
            log.FamilyId,
            log.Action,
            log.Action.ToString(),
            log.EntityType,
            log.EntityId,
            log.ChangesJson,
            log.IpAddress,
            log.UserAgent,
            log.Notes,
            log.Success,
            log.ErrorMessage,
            log.CreatedAt);
    }

    private string? SerializeChanges(object? before, object? after)
    {
        if (before == null && after == null)
            return null;

        var changes = new
        {
            Before = before,
            After = after
        };

        return JsonSerializer.Serialize(changes);
    }
}
```

---

## Phase 3: Audit Logging Middleware

### 3.1 AuditLoggingMiddleware

```csharp
namespace AllowanceTracker.Middleware;

public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    public AuditLoggingMiddleware(
        RequestDelegate next,
        ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService)
    {
        // Only log API requests
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        var ipAddress = GetIpAddress(context);
        var userAgent = context.Request.Headers["User-Agent"].ToString();

        try
        {
            await _next(context);

            // Log successful requests for specific actions
            if (ShouldLog(context))
            {
                await LogRequest(context, auditLogService, ipAddress, userAgent, true, null);
            }
        }
        catch (Exception ex)
        {
            // Log failed requests
            await LogRequest(context, auditLogService, ipAddress, userAgent, false, ex.Message);
            throw;
        }
    }

    private bool ShouldLog(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Log POST, PUT, PATCH, DELETE operations
        var method = context.Request.Method;
        return method is "POST" or "PUT" or "PATCH" or "DELETE";
    }

    private async Task LogRequest(
        HttpContext context,
        IAuditLogService auditLogService,
        string? ipAddress,
        string? userAgent,
        bool success,
        string? errorMessage)
    {
        var userId = GetUserId(context);
        var action = DetermineAction(context);
        var entityInfo = ExtractEntityInfo(context);

        await auditLogService.LogAsync(new AuditLogEntry(
            userId,
            null, // FamilyId will be set by service layer
            action,
            entityInfo.EntityType,
            entityInfo.EntityId,
            null,
            null,
            ipAddress,
            userAgent,
            $"{context.Request.Method} {context.Request.Path}",
            success,
            errorMessage
        ));
    }

    private Guid? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId)
            ? userId
            : null;
    }

    private string? GetIpAddress(HttpContext context)
    {
        // Check for forwarded IP (behind proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private AuditAction DetermineAction(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var method = context.Request.Method;

        return path switch
        {
            var p when p.Contains("/transactions") && method == "POST" => AuditAction.TransactionCreated,
            var p when p.Contains("/transactions") && method == "DELETE" => AuditAction.TransactionDeleted,
            var p when p.Contains("/allowance") => AuditAction.AllowanceUpdated,
            var p when p.Contains("/children") && method == "POST" => AuditAction.ChildAdded,
            var p when p.Contains("/settings") => AuditAction.SettingsChanged,
            _ => AuditAction.SettingsChanged // Default
        };
    }

    private (string EntityType, Guid? EntityId) ExtractEntityInfo(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        string entityType = "Unknown";
        Guid? entityId = null;

        for (int i = 0; i < segments.Length; i++)
        {
            if (Guid.TryParse(segments[i], out var id))
            {
                entityId = id;
                if (i > 0)
                {
                    entityType = segments[i - 1];
                }
                break;
            }
        }

        return (entityType, entityId);
    }
}

// Extension method to register middleware
public static class AuditLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuditLoggingMiddleware>();
    }
}
```

---

## Phase 4: Background Job for Retention

### 4.1 AuditLogCleanupJob

```csharp
namespace AllowanceTracker.BackgroundServices;

public class AuditLogCleanupJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditLogCleanupJob> _logger;
    private readonly int _retentionDays;

    public AuditLogCleanupJob(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<AuditLogCleanupJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _retentionDays = configuration.GetValue<int>("AuditLog:RetentionDays", 365);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AuditLogCleanupJob started with {RetentionDays} day retention",
            _retentionDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();

                var deletedCount = await auditLogService.CleanupOldLogsAsync(_retentionDays);

                if (deletedCount > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} old audit log entries", deletedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during audit log cleanup");
            }

            // Run once per day at 2 AM
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
```

---

## Phase 5: Test Cases (15 Tests)

```csharp
namespace AllowanceTracker.Tests.Services;

public class AuditLogServiceTests
{
    // Logging Tests (4)
    [Fact] LogAsync_ValidEntry_CreatesLog
    [Fact] LogAsync_WithChanges_SerializesCorrectly
    [Fact] LogAsync_Exception_DoesNotThrow
    [Fact] LogAsync_StoresIpAddressAndUserAgent

    // Query Tests (5)
    [Fact] GetLogs_WithFilters_ReturnsFilteredResults
    [Fact] GetLogs_WithPagination_ReturnsCorrectPage
    [Fact] GetLogs_OrdersByCreatedAtDescending
    [Fact] GetEntityHistory_ReturnsAllLogsForEntity
    [Fact] GetLogs_FilterByDateRange_ReturnsLogsInRange

    // Export Tests (2)
    [Fact] ExportLogs_GeneratesValidCsv
    [Fact] ExportLogs_LogsExportAction

    // Cleanup Tests (2)
    [Fact] CleanupOldLogs_RemovesExpiredLogs
    [Fact] CleanupOldLogs_KeepsRecentLogs

    // Statistics Tests (2)
    [Fact] GetStatistics_CalculatesCorrectly
    [Fact] GetStatistics_GroupsByActionAndUser
}
```

---

## Success Metrics

- ✅ All 15 tests passing
- ✅ All critical actions logged automatically
- ✅ Logs queryable and filterable efficiently
- ✅ CSV export generates valid files
- ✅ Retention policy removes old logs
- ✅ Middleware captures API requests
- ✅ Parents can view audit trail
- ✅ No performance impact on normal operations

---

**Total Implementation Time**: 1-2 weeks
