# Transaction Request & Approval Workflow Specification

## Overview

This specification introduces a transaction request system where children request spending approval from parents before money is deducted. This teaches children to ask permission for purchases, gives parents oversight, and creates a collaborative decision-making process about money.

## Goals

1. **Spending Approval**: Children request, parents approve/reject purchases
2. **Budget Collaboration**: Parents can set auto-approval rules for small amounts
3. **Real-time Notifications**: SignalR alerts for new requests and decisions
4. **Request History**: Track all approval decisions with reasoning
5. **Category-Based Rules**: Auto-approve based on transaction category
6. **TDD Approach**: 25 comprehensive tests

## Technology Stack

- **Backend**: ASP.NET Core 8.0 with Entity Framework Core
- **Database**: PostgreSQL with enum support
- **Testing**: xUnit, FluentAssertions, Moq
- **Real-time**: SignalR for instant notifications
- **UI**: Blazor Server components

---

## Phase 1: Database Schema

### 1.1 TransactionRequest Model

```csharp
namespace AllowanceTracker.Models;

/// <summary>
/// Child's request for spending approval
/// </summary>
public class TransactionRequest
{
    public Guid Id { get; set; }

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    public Guid FamilyId { get; set; }
    public virtual Family Family { get; set; } = null!;

    /// <summary>
    /// Requested amount to spend
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Spending category
    /// </summary>
    public TransactionCategory Category { get; set; }

    /// <summary>
    /// What child wants to buy
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Optional merchant/store name
    /// </summary>
    public string? Merchant { get; set; }

    /// <summary>
    /// Why child wants this
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Current request status
    /// </summary>
    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    /// <summary>
    /// When child submitted request
    /// </summary>
    public DateTime RequestedAt { get; set; }

    /// <summary>
    /// When parent reviewed request
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// Parent who reviewed
    /// </summary>
    public Guid? ReviewedById { get; set; }
    public virtual ApplicationUser? ReviewedBy { get; set; }

    /// <summary>
    /// Parent's comments on decision
    /// </summary>
    public string? ReviewNotes { get; set; }

    /// <summary>
    /// Transaction created if approved
    /// </summary>
    public Guid? TransactionId { get; set; }
    public virtual Transaction? Transaction { get; set; }

    /// <summary>
    /// Auto-approved by rule?
    /// </summary>
    public bool IsAutoApproved { get; set; } = false;

    /// <summary>
    /// Priority level (normal, urgent)
    /// </summary>
    public RequestPriority Priority { get; set; } = RequestPriority.Normal;

    /// <summary>
    /// Request expires after this date
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

public enum RequestStatus
{
    Pending = 1,        // Awaiting parent review
    Approved = 2,       // Parent approved
    Rejected = 3,       // Parent rejected
    Cancelled = 4,      // Child cancelled
    Expired = 5         // Request expired
}

public enum RequestPriority
{
    Normal = 1,
    Urgent = 2
}
```

### 1.2 ApprovalRule Model

```csharp
namespace AllowanceTracker.Models;

/// <summary>
/// Auto-approval rules set by parents
/// </summary>
public class ApprovalRule
{
    public Guid Id { get; set; }

    public Guid FamilyId { get; set; }
    public virtual Family Family { get; set; } = null!;

    public Guid? ChildId { get; set; }
    public virtual Child? Child { get; set; }

    /// <summary>
    /// Rule name/description
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Max amount to auto-approve
    /// </summary>
    public decimal MaxAmount { get; set; }

    /// <summary>
    /// Specific category (null = all categories)
    /// </summary>
    public TransactionCategory? Category { get; set; }

    /// <summary>
    /// Max auto-approvals per day
    /// </summary>
    public int MaxPerDay { get; set; } = 3;

    /// <summary>
    /// Max total amount per day
    /// </summary>
    public decimal MaxDailyTotal { get; set; } = 0;

    /// <summary>
    /// Is rule active?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Days of week rule applies (null = all days)
    /// </summary>
    public string? DaysOfWeek { get; set; } // JSON: [0,6] for Sun/Sat

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid CreatedById { get; set; }
    public virtual ApplicationUser CreatedBy { get; set; } = null!;
}
```

### 1.3 Update Child Model

```csharp
namespace AllowanceTracker.Models;

public class Child
{
    // ... existing properties ...

    /// <summary>
    /// Number of pending requests
    /// </summary>
    public int PendingRequestsCount { get; set; } = 0;

    // Navigation properties
    public virtual ICollection<TransactionRequest> TransactionRequests { get; set; } = new List<TransactionRequest>();
}
```

### 1.4 Database Migration

```bash
dotnet ef migrations add AddTransactionApprovalSystem
```

```csharp
public partial class AddTransactionApprovalSystem : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create TransactionRequests table
        migrationBuilder.CreateTable(
            name: "TransactionRequests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                Category = table.Column<int>(type: "integer", nullable: false),
                Description = table.Column<string>(type: "varchar(500)", nullable: false),
                Merchant = table.Column<string>(type: "varchar(200)", nullable: true),
                Reason = table.Column<string>(type: "text", nullable: true),
                Status = table.Column<int>(type: "integer", nullable: false),
                RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ReviewedById = table.Column<Guid>(type: "uuid", nullable: true),
                ReviewNotes = table.Column<string>(type: "text", nullable: true),
                TransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                IsAutoApproved = table.Column<bool>(type: "boolean", nullable: false),
                Priority = table.Column<int>(type: "integer", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TransactionRequests", x => x.Id);
                table.ForeignKey(
                    name: "FK_TransactionRequests_Children_ChildId",
                    column: x => x.ChildId,
                    principalTable: "Children",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_TransactionRequests_Families_FamilyId",
                    column: x => x.FamilyId,
                    principalTable: "Families",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_TransactionRequests_AspNetUsers_ReviewedById",
                    column: x => x.ReviewedById,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_TransactionRequests_Transactions_TransactionId",
                    column: x => x.TransactionId,
                    principalTable: "Transactions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // Create ApprovalRules table
        migrationBuilder.CreateTable(
            name: "ApprovalRules",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                ChildId = table.Column<Guid>(type: "uuid", nullable: true),
                Name = table.Column<string>(type: "varchar(200)", nullable: false),
                MaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                Category = table.Column<int>(type: "integer", nullable: true),
                MaxPerDay = table.Column<int>(type: "integer", nullable: false),
                MaxDailyTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                DaysOfWeek = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ApprovalRules", x => x.Id);
                table.ForeignKey(
                    name: "FK_ApprovalRules_Families_FamilyId",
                    column: x => x.FamilyId,
                    principalTable: "Families",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ApprovalRules_Children_ChildId",
                    column: x => x.ChildId,
                    principalTable: "Children",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ApprovalRules_AspNetUsers_CreatedById",
                    column: x => x.CreatedById,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        // Create indexes
        migrationBuilder.CreateIndex(
            name: "IX_TransactionRequests_ChildId",
            table: "TransactionRequests",
            column: "ChildId");

        migrationBuilder.CreateIndex(
            name: "IX_TransactionRequests_FamilyId_Status",
            table: "TransactionRequests",
            columns: new[] { "FamilyId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_TransactionRequests_RequestedAt",
            table: "TransactionRequests",
            column: "RequestedAt");

        migrationBuilder.CreateIndex(
            name: "IX_ApprovalRules_FamilyId_IsActive",
            table: "ApprovalRules",
            columns: new[] { "FamilyId", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_ApprovalRules_ChildId",
            table: "ApprovalRules",
            column: "ChildId");

        // Add PendingRequestsCount to Children
        migrationBuilder.AddColumn<int>(
            name: "PendingRequestsCount",
            table: "Children",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }
}
```

---

## Phase 2: Service Layer (TDD)

### 2.1 ITransactionRequestService Interface

```csharp
namespace AllowanceTracker.Services;

public interface ITransactionRequestService
{
    // Request Management
    Task<TransactionRequest> CreateRequestAsync(CreateTransactionRequestDto dto, Guid childUserId);
    Task<TransactionRequest> GetRequestAsync(Guid requestId);
    Task<List<TransactionRequest>> GetPendingRequestsForFamilyAsync(Guid familyId);
    Task<List<TransactionRequest>> GetRequestsForChildAsync(Guid childId, RequestStatus? status = null);
    Task<TransactionRequest> CancelRequestAsync(Guid requestId, Guid childUserId);

    // Parent Actions
    Task<TransactionRequest> ApproveRequestAsync(Guid requestId, string? reviewNotes, Guid parentUserId);
    Task<TransactionRequest> RejectRequestAsync(Guid requestId, string reviewNotes, Guid parentUserId);

    // Auto-Approval
    Task<bool> CheckAutoApprovalAsync(Guid requestId);
    Task ProcessAutoApprovalsAsync();

    // Approval Rules
    Task<ApprovalRule> CreateApprovalRuleAsync(CreateApprovalRuleDto dto, Guid parentUserId);
    Task<List<ApprovalRule>> GetApprovalRulesAsync(Guid familyId);
    Task<ApprovalRule> UpdateApprovalRuleAsync(Guid ruleId, UpdateApprovalRuleDto dto, Guid parentUserId);
    Task DeleteApprovalRuleAsync(Guid ruleId, Guid parentUserId);

    // Statistics
    Task<RequestStatistics> GetRequestStatisticsAsync(Guid childId, DateTime? startDate = null);
}
```

### 2.2 Data Transfer Objects

```csharp
namespace AllowanceTracker.DTOs;

public record CreateTransactionRequestDto(
    Guid ChildId,
    decimal Amount,
    TransactionCategory Category,
    string Description,
    string? Merchant = null,
    string? Reason = null,
    RequestPriority Priority = RequestPriority.Normal,
    DateTime? ExpiresAt = null);

public record CreateApprovalRuleDto(
    Guid FamilyId,
    Guid? ChildId,
    string Name,
    decimal MaxAmount,
    TransactionCategory? Category = null,
    int MaxPerDay = 3,
    decimal MaxDailyTotal = 0,
    int[]? DaysOfWeek = null);

public record UpdateApprovalRuleDto(
    string? Name = null,
    decimal? MaxAmount = null,
    int? MaxPerDay = null,
    decimal? MaxDailyTotal = null,
    bool? IsActive = null);

public record RequestStatistics(
    int TotalRequests,
    int Approved,
    int Rejected,
    int Pending,
    int Cancelled,
    decimal TotalApprovedAmount,
    decimal TotalRejectedAmount,
    double ApprovalRate,
    double AverageResponseTimeHours,
    int AutoApprovedCount);
```

### 2.3 TransactionRequestService Implementation

```csharp
namespace AllowanceTracker.Services;

public class TransactionRequestService : ITransactionRequestService
{
    private readonly AllowanceContext _context;
    private readonly ITransactionService _transactionService;
    private readonly IHubContext<FamilyHub> _hubContext;
    private readonly ILogger<TransactionRequestService> _logger;

    public TransactionRequestService(
        AllowanceContext context,
        ITransactionService transactionService,
        IHubContext<FamilyHub> hubContext,
        ILogger<TransactionRequestService> logger)
    {
        _context = context;
        _transactionService = transactionService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<TransactionRequest> CreateRequestAsync(
        CreateTransactionRequestDto dto,
        Guid childUserId)
    {
        // Validate child
        var child = await _context.Children
            .Include(c => c.Family)
            .FirstOrDefaultAsync(c => c.Id == dto.ChildId && c.UserId == childUserId)
            ?? throw new NotFoundException("Child not found");

        // Validate amount
        if (dto.Amount <= 0)
            throw new ValidationException("Amount must be greater than zero");

        // Check if child has sufficient balance
        if (child.CurrentBalance < dto.Amount)
            throw new InsufficientFundsException(
                $"Insufficient balance. Available: {child.CurrentBalance:C}, Requested: {dto.Amount:C}");

        var now = DateTime.UtcNow;
        var request = new TransactionRequest
        {
            Id = Guid.NewGuid(),
            ChildId = dto.ChildId,
            FamilyId = child.FamilyId!.Value,
            Amount = dto.Amount,
            Category = dto.Category,
            Description = dto.Description,
            Merchant = dto.Merchant,
            Reason = dto.Reason,
            Status = RequestStatus.Pending,
            RequestedAt = now,
            Priority = dto.Priority,
            ExpiresAt = dto.ExpiresAt ?? now.AddDays(7) // Default 7 day expiration
        };

        _context.TransactionRequests.Add(request);
        child.PendingRequestsCount++;

        await _context.SaveChangesAsync();

        // Check for auto-approval
        var autoApproved = await CheckAutoApprovalAsync(request.Id);

        if (!autoApproved)
        {
            // Notify parents via SignalR
            await _hubContext.Clients
                .Group($"family-{child.FamilyId}")
                .SendAsync("NewTransactionRequest", request.Id, child.Id, dto.Amount);
        }

        _logger.LogInformation(
            "Child {ChildId} created transaction request {RequestId} for {Amount}",
            dto.ChildId, request.Id, dto.Amount);

        return request;
    }

    public async Task<TransactionRequest> ApproveRequestAsync(
        Guid requestId,
        string? reviewNotes,
        Guid parentUserId)
    {
        var request = await _context.TransactionRequests
            .Include(r => r.Child)
            .FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new NotFoundException("Request not found");

        if (request.Status != RequestStatus.Pending)
            throw new InvalidOperationException($"Cannot approve request with status {request.Status}");

        // Verify parent belongs to family
        var parent = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == parentUserId && u.FamilyId == request.FamilyId)
            ?? throw new UnauthorizedAccessException("Not authorized to approve this request");

        // Check if child still has sufficient balance
        if (request.Child.CurrentBalance < request.Amount)
            throw new InsufficientFundsException(
                $"Child no longer has sufficient balance. Current: {request.Child.CurrentBalance:C}");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Create the transaction
            var transactionDto = new CreateTransactionDto(
                request.ChildId,
                request.Amount,
                TransactionType.Debit,
                request.Category,
                $"{request.Description} (Approved by {parent.FirstName})");

            var createdTransaction = await _transactionService.CreateTransactionAsync(transactionDto);

            // Update request
            request.Status = RequestStatus.Approved;
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewedById = parentUserId;
            request.ReviewNotes = reviewNotes;
            request.TransactionId = createdTransaction.Id;

            // Update child's pending count
            request.Child.PendingRequestsCount--;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Notify via SignalR
            await _hubContext.Clients
                .Group($"family-{request.FamilyId}")
                .SendAsync("RequestApproved", requestId, request.ChildId);

            _logger.LogInformation(
                "Request {RequestId} approved by {ParentId}. Transaction {TransactionId} created.",
                requestId, parentUserId, createdTransaction.Id);

            return request;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<TransactionRequest> RejectRequestAsync(
        Guid requestId,
        string reviewNotes,
        Guid parentUserId)
    {
        var request = await _context.TransactionRequests
            .Include(r => r.Child)
            .FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new NotFoundException("Request not found");

        if (request.Status != RequestStatus.Pending)
            throw new InvalidOperationException($"Cannot reject request with status {request.Status}");

        // Verify parent belongs to family
        var parent = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == parentUserId && u.FamilyId == request.FamilyId)
            ?? throw new UnauthorizedAccessException("Not authorized to reject this request");

        if (string.IsNullOrWhiteSpace(reviewNotes))
            throw new ValidationException("Review notes are required when rejecting a request");

        request.Status = RequestStatus.Rejected;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedById = parentUserId;
        request.ReviewNotes = reviewNotes;

        // Update child's pending count
        request.Child.PendingRequestsCount--;

        await _context.SaveChangesAsync();

        // Notify via SignalR
        await _hubContext.Clients
            .Group($"family-{request.FamilyId}")
            .SendAsync("RequestRejected", requestId, request.ChildId, reviewNotes);

        _logger.LogInformation(
            "Request {RequestId} rejected by {ParentId}",
            requestId, parentUserId);

        return request;
    }

    public async Task<bool> CheckAutoApprovalAsync(Guid requestId)
    {
        var request = await _context.TransactionRequests
            .Include(r => r.Child)
            .FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new NotFoundException("Request not found");

        if (request.Status != RequestStatus.Pending)
            return false;

        // Get applicable rules
        var now = DateTime.UtcNow;
        var dayOfWeek = (int)now.DayOfWeek;

        var rules = await _context.ApprovalRules
            .Where(r => r.FamilyId == request.FamilyId &&
                       r.IsActive &&
                       (r.ChildId == null || r.ChildId == request.ChildId) &&
                       (r.Category == null || r.Category == request.Category))
            .ToListAsync();

        foreach (var rule in rules)
        {
            // Check days of week
            if (!string.IsNullOrEmpty(rule.DaysOfWeek))
            {
                var allowedDays = System.Text.Json.JsonSerializer.Deserialize<int[]>(rule.DaysOfWeek);
                if (allowedDays != null && !allowedDays.Contains(dayOfWeek))
                    continue;
            }

            // Check amount threshold
            if (request.Amount > rule.MaxAmount)
                continue;

            // Check daily limits
            var todayStart = now.Date;
            var todayEnd = todayStart.AddDays(1);

            var todayAutoApprovals = await _context.TransactionRequests
                .Where(r => r.ChildId == request.ChildId &&
                           r.IsAutoApproved &&
                           r.RequestedAt >= todayStart &&
                           r.RequestedAt < todayEnd)
                .ToListAsync();

            // Check max per day
            if (todayAutoApprovals.Count >= rule.MaxPerDay)
                continue;

            // Check daily total
            if (rule.MaxDailyTotal > 0)
            {
                var todayTotal = todayAutoApprovals.Sum(r => r.Amount);
                if (todayTotal + request.Amount > rule.MaxDailyTotal)
                    continue;
            }

            // Rule matches! Auto-approve
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var transactionDto = new CreateTransactionDto(
                    request.ChildId,
                    request.Amount,
                    TransactionType.Debit,
                    request.Category,
                    $"{request.Description} (Auto-approved)");

                var createdTransaction = await _transactionService.CreateTransactionAsync(transactionDto);

                request.Status = RequestStatus.Approved;
                request.ReviewedAt = DateTime.UtcNow;
                request.ReviewNotes = $"Auto-approved by rule: {rule.Name}";
                request.TransactionId = createdTransaction.Id;
                request.IsAutoApproved = true;

                request.Child.PendingRequestsCount--;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Notify via SignalR
                await _hubContext.Clients
                    .Group($"family-{request.FamilyId}")
                    .SendAsync("RequestAutoApproved", requestId, request.ChildId);

                _logger.LogInformation(
                    "Request {RequestId} auto-approved by rule {RuleId}",
                    requestId, rule.Id);

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        return false;
    }

    public async Task<List<TransactionRequest>> GetPendingRequestsForFamilyAsync(Guid familyId)
    {
        return await _context.TransactionRequests
            .Include(r => r.Child)
            .Include(r => r.Child.User)
            .Where(r => r.FamilyId == familyId && r.Status == RequestStatus.Pending)
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.RequestedAt)
            .ToListAsync();
    }

    public async Task<TransactionRequest> CancelRequestAsync(Guid requestId, Guid childUserId)
    {
        var request = await _context.TransactionRequests
            .Include(r => r.Child)
            .FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new NotFoundException("Request not found");

        if (request.Child.UserId != childUserId)
            throw new UnauthorizedAccessException("Not authorized to cancel this request");

        if (request.Status != RequestStatus.Pending)
            throw new InvalidOperationException("Can only cancel pending requests");

        request.Status = RequestStatus.Cancelled;
        request.Child.PendingRequestsCount--;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Request {RequestId} cancelled by child", requestId);

        return request;
    }

    public async Task<RequestStatistics> GetRequestStatisticsAsync(
        Guid childId,
        DateTime? startDate = null)
    {
        startDate ??= DateTime.UtcNow.AddMonths(-3);

        var requests = await _context.TransactionRequests
            .Where(r => r.ChildId == childId && r.RequestedAt >= startDate)
            .ToListAsync();

        var total = requests.Count;
        var approved = requests.Count(r => r.Status == RequestStatus.Approved);
        var rejected = requests.Count(r => r.Status == RequestStatus.Rejected);
        var pending = requests.Count(r => r.Status == RequestStatus.Pending);
        var cancelled = requests.Count(r => r.Status == RequestStatus.Cancelled);

        var totalApprovedAmount = requests
            .Where(r => r.Status == RequestStatus.Approved)
            .Sum(r => r.Amount);

        var totalRejectedAmount = requests
            .Where(r => r.Status == RequestStatus.Rejected)
            .Sum(r => r.Amount);

        var approvalRate = total > 0 ? (double)approved / total * 100 : 0;

        var reviewedRequests = requests
            .Where(r => r.ReviewedAt.HasValue)
            .ToList();

        var avgResponseTime = reviewedRequests.Any()
            ? reviewedRequests.Average(r => (r.ReviewedAt!.Value - r.RequestedAt).TotalHours)
            : 0;

        var autoApprovedCount = requests.Count(r => r.IsAutoApproved);

        return new RequestStatistics(
            total,
            approved,
            rejected,
            pending,
            cancelled,
            totalApprovedAmount,
            totalRejectedAmount,
            Math.Round(approvalRate, 2),
            Math.Round(avgResponseTime, 2),
            autoApprovedCount);
    }

    public async Task<ApprovalRule> CreateApprovalRuleAsync(
        CreateApprovalRuleDto dto,
        Guid parentUserId)
    {
        // Validate family
        var family = await _context.Families.FindAsync(dto.FamilyId)
            ?? throw new NotFoundException("Family not found");

        // Validate child if specified
        if (dto.ChildId.HasValue)
        {
            var child = await _context.Children
                .FirstOrDefaultAsync(c => c.Id == dto.ChildId && c.FamilyId == dto.FamilyId)
                ?? throw new NotFoundException("Child not found");
        }

        var now = DateTime.UtcNow;
        var rule = new ApprovalRule
        {
            Id = Guid.NewGuid(),
            FamilyId = dto.FamilyId,
            ChildId = dto.ChildId,
            Name = dto.Name,
            MaxAmount = dto.MaxAmount,
            Category = dto.Category,
            MaxPerDay = dto.MaxPerDay,
            MaxDailyTotal = dto.MaxDailyTotal,
            IsActive = true,
            DaysOfWeek = dto.DaysOfWeek != null
                ? System.Text.Json.JsonSerializer.Serialize(dto.DaysOfWeek)
                : null,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedById = parentUserId
        };

        _context.ApprovalRules.Add(rule);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created approval rule {RuleId} for family {FamilyId}",
            rule.Id, dto.FamilyId);

        return rule;
    }
}
```

### 2.4 Test Cases (25 Tests)

```csharp
namespace AllowanceTracker.Tests.Services;

public class TransactionRequestServiceTests
{
    // Create Request Tests (5)
    [Fact]
    public async Task CreateRequest_ValidData_CreatesSuccessfully()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var dto = new CreateTransactionRequestDto(
            child.Id,
            25m,
            TransactionCategory.Toys,
            "LEGO Star Wars Set",
            Merchant: "Target",
            Reason: "Birthday gift with my savings");

        // Act
        var request = await _requestService.CreateRequestAsync(dto, child.UserId);

        // Assert
        request.Should().NotBeNull();
        request.Status.Should().Be(RequestStatus.Pending);
        request.Amount.Should().Be(25m);
        request.Description.Should().Be("LEGO Star Wars Set");
        request.Merchant.Should().Be("Target");

        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.PendingRequestsCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateRequest_InsufficientBalance_ThrowsException()
    {
        // Arrange
        var child = await CreateChild(balance: 10m);
        var dto = new CreateTransactionRequestDto(
            child.Id, 50m, TransactionCategory.Toys, "Expensive toy");

        // Act & Assert
        await Assert.ThrowsAsync<InsufficientFundsException>(
            () => _requestService.CreateRequestAsync(dto, child.UserId));
    }

    [Fact]
    public async Task CreateRequest_NegativeAmount_ThrowsException()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var dto = new CreateTransactionRequestDto(
            child.Id, -10m, TransactionCategory.Toys, "Test");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _requestService.CreateRequestAsync(dto, child.UserId));
    }

    [Fact]
    public async Task CreateRequest_SendsSignalRNotification()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var dto = new CreateTransactionRequestDto(
            child.Id, 20m, TransactionCategory.Snacks, "Ice cream");

        // Act
        await _requestService.CreateRequestAsync(dto, child.UserId);

        // Assert
        _mockHubContext.Verify(
            h => h.Clients.Group($"family-{child.FamilyId}")
                .SendAsync("NewTransactionRequest", It.IsAny<Guid>(), child.Id, 20m, default),
            Times.Once);
    }

    [Fact]
    public async Task CreateRequest_WithAutoApprovalRule_AutoApproves()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        await CreateApprovalRule(child.FamilyId, maxAmount: 10m, category: TransactionCategory.Snacks);

        var dto = new CreateTransactionRequestDto(
            child.Id, 5m, TransactionCategory.Snacks, "Candy bar");

        // Act
        var request = await _requestService.CreateRequestAsync(dto, child.UserId);

        // Assert
        var updated = await _requestService.GetRequestAsync(request.Id);
        updated.Status.Should().Be(RequestStatus.Approved);
        updated.IsAutoApproved.Should().BeTrue();
        updated.TransactionId.Should().NotBeNull();
    }

    // Approve Request Tests (5)
    [Fact]
    public async Task ApproveRequest_ValidRequest_CreatesTransaction()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var request = await CreateRequest(child.Id, 30m, "New game");

        // Act
        var approved = await _requestService.ApproveRequestAsync(
            request.Id, "Good choice!", _parentUserId);

        // Assert
        approved.Status.Should().Be(RequestStatus.Approved);
        approved.ReviewedAt.Should().NotBeNull();
        approved.ReviewedById.Should().Be(_parentUserId);
        approved.ReviewNotes.Should().Be("Good choice!");
        approved.TransactionId.Should().NotBeNull();

        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentBalance.Should().Be(70m); // 100 - 30
        updatedChild.PendingRequestsCount.Should().Be(0);

        var transaction = await _context.Transactions.FindAsync(approved.TransactionId);
        transaction.Should().NotBeNull();
        transaction!.Amount.Should().Be(30m);
        transaction.Type.Should().Be(TransactionType.Debit);
    }

    [Fact]
    public async Task ApproveRequest_AlreadyApproved_ThrowsException()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var request = await CreateRequest(child.Id, 20m, "Test");
        await _requestService.ApproveRequestAsync(request.Id, "OK", _parentUserId);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _requestService.ApproveRequestAsync(request.Id, "Again", _parentUserId));
    }

    [Fact]
    public async Task ApproveRequest_InsufficientBalanceAtApproval_ThrowsException()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var request = await CreateRequest(child.Id, 90m, "Expensive item");

        // Child spends money elsewhere
        child.CurrentBalance = 50m;
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InsufficientFundsException>(
            () => _requestService.ApproveRequestAsync(request.Id, "OK", _parentUserId));
    }

    [Fact]
    public async Task ApproveRequest_UnauthorizedParent_ThrowsException()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var request = await CreateRequest(child.Id, 20m, "Test");
        var otherParentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _requestService.ApproveRequestAsync(request.Id, "OK", otherParentId));
    }

    [Fact]
    public async Task ApproveRequest_SendsSignalRNotification()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var request = await CreateRequest(child.Id, 15m, "Book");

        // Act
        await _requestService.ApproveRequestAsync(request.Id, "Great choice!", _parentUserId);

        // Assert
        _mockHubContext.Verify(
            h => h.Clients.Group($"family-{child.FamilyId}")
                .SendAsync("RequestApproved", request.Id, child.Id, default),
            Times.Once);
    }

    // Reject Request Tests (3)
    [Fact]
    public async Task RejectRequest_WithNotes_UpdatesStatus()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var request = await CreateRequest(child.Id, 50m, "Expensive video game");

        // Act
        var rejected = await _requestService.RejectRequestAsync(
            request.Id,
            "Too expensive. Save more first.",
            _parentUserId);

        // Assert
        rejected.Status.Should().Be(RequestStatus.Rejected);
        rejected.ReviewedAt.Should().NotBeNull();
        rejected.ReviewedById.Should().Be(_parentUserId);
        rejected.ReviewNotes.Should().Be("Too expensive. Save more first.");
        rejected.TransactionId.Should().BeNull();

        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentBalance.Should().Be(100m); // No change
        updatedChild.PendingRequestsCount.Should().Be(0);
    }

    [Fact]
    public async Task RejectRequest_WithoutNotes_ThrowsException()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var request = await CreateRequest(child.Id, 20m, "Test");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _requestService.RejectRequestAsync(request.Id, "", _parentUserId));
    }

    [Fact]
    public async Task RejectRequest_SendsSignalRNotification()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var request = await CreateRequest(child.Id, 30m, "Item");
        var notes = "Not appropriate";

        // Act
        await _requestService.RejectRequestAsync(request.Id, notes, _parentUserId);

        // Assert
        _mockHubContext.Verify(
            h => h.Clients.Group($"family-{child.FamilyId}")
                .SendAsync("RequestRejected", request.Id, child.Id, notes, default),
            Times.Once);
    }

    // Cancel Request Tests (2)
    [Fact]
    public async Task CancelRequest_ByChild_UpdatesStatus()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var request = await CreateRequest(child.Id, 25m, "Changed my mind");

        // Act
        var cancelled = await _requestService.CancelRequestAsync(request.Id, child.UserId);

        // Assert
        cancelled.Status.Should().Be(RequestStatus.Cancelled);

        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.PendingRequestsCount.Should().Be(0);
    }

    [Fact]
    public async Task CancelRequest_ByWrongChild_ThrowsException()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var request = await CreateRequest(child.Id, 20m, "Test");
        var otherChildUserId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _requestService.CancelRequestAsync(request.Id, otherChildUserId));
    }

    // Auto-Approval Tests (5)
    [Fact]
    public async Task CheckAutoApproval_MatchingRule_AutoApproves()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var rule = await CreateApprovalRule(
            child.FamilyId,
            maxAmount: 15m,
            category: TransactionCategory.Snacks);

        var request = await CreateRequest(child.Id, 10m, "Snack", TransactionCategory.Snacks);

        // Act
        var autoApproved = await _requestService.CheckAutoApprovalAsync(request.Id);

        // Assert
        autoApproved.Should().BeTrue();

        var updated = await _requestService.GetRequestAsync(request.Id);
        updated.Status.Should().Be(RequestStatus.Approved);
        updated.IsAutoApproved.Should().BeTrue();
        updated.TransactionId.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckAutoApproval_ExceedsMaxAmount_DoesNotApprove()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        await CreateApprovalRule(child.FamilyId, maxAmount: 10m);

        var request = await CreateRequest(child.Id, 15m, "Too expensive");

        // Act
        var autoApproved = await _requestService.CheckAutoApprovalAsync(request.Id);

        // Assert
        autoApproved.Should().BeFalse();

        var updated = await _requestService.GetRequestAsync(request.Id);
        updated.Status.Should().Be(RequestStatus.Pending);
    }

    [Fact]
    public async Task CheckAutoApproval_ExceedsMaxPerDay_DoesNotApprove()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        await CreateApprovalRule(child.FamilyId, maxAmount: 10m, maxPerDay: 2);

        // Create and auto-approve 2 requests today
        var req1 = await CreateRequest(child.Id, 5m, "First");
        await _requestService.CheckAutoApprovalAsync(req1.Id);
        var req2 = await CreateRequest(child.Id, 5m, "Second");
        await _requestService.CheckAutoApprovalAsync(req2.Id);

        // Third request
        var req3 = await CreateRequest(child.Id, 5m, "Third");

        // Act
        var autoApproved = await _requestService.CheckAutoApprovalAsync(req3.Id);

        // Assert
        autoApproved.Should().BeFalse();
        var updated = await _requestService.GetRequestAsync(req3.Id);
        updated.Status.Should().Be(RequestStatus.Pending);
    }

    [Fact]
    public async Task CheckAutoApproval_ExceedsMaxDailyTotal_DoesNotApprove()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        await CreateApprovalRule(
            child.FamilyId,
            maxAmount: 20m,
            maxPerDay: 10,
            maxDailyTotal: 30m);

        // Auto-approve $25 today
        var req1 = await CreateRequest(child.Id, 15m, "First");
        await _requestService.CheckAutoApprovalAsync(req1.Id);
        var req2 = await CreateRequest(child.Id, 10m, "Second");
        await _requestService.CheckAutoApprovalAsync(req2.Id);

        // Request would exceed daily total
        var req3 = await CreateRequest(child.Id, 10m, "Third");

        // Act
        var autoApproved = await _requestService.CheckAutoApprovalAsync(req3.Id);

        // Assert
        autoApproved.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAutoApproval_WrongDayOfWeek_DoesNotApprove()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var currentDay = (int)DateTime.UtcNow.DayOfWeek;
        var differentDay = (currentDay + 1) % 7;

        await CreateApprovalRule(
            child.FamilyId,
            maxAmount: 20m,
            daysOfWeek: new[] { differentDay });

        var request = await CreateRequest(child.Id, 10m, "Test");

        // Act
        var autoApproved = await _requestService.CheckAutoApprovalAsync(request.Id);

        // Assert
        autoApproved.Should().BeFalse();
    }

    // Get Requests Tests (2)
    [Fact]
    public async Task GetPendingRequestsForFamily_ReturnsOnlyPending()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var req1 = await CreateRequest(child.Id, 10m, "Pending 1");
        var req2 = await CreateRequest(child.Id, 15m, "Pending 2");
        var req3 = await CreateRequest(child.Id, 20m, "Will be approved");
        await _requestService.ApproveRequestAsync(req3.Id, "OK", _parentUserId);

        // Act
        var pending = await _requestService.GetPendingRequestsForFamilyAsync(child.FamilyId!.Value);

        // Assert
        pending.Should().HaveCount(2);
        pending.Should().Contain(r => r.Id == req1.Id);
        pending.Should().Contain(r => r.Id == req2.Id);
        pending.Should().NotContain(r => r.Id == req3.Id);
    }

    [Fact]
    public async Task GetPendingRequestsForFamily_SortsByPriorityAndDate()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var normalReq = await CreateRequest(
            child.Id, 10m, "Normal", priority: RequestPriority.Normal);

        await Task.Delay(100);

        var urgentReq = await CreateRequest(
            child.Id, 15m, "Urgent", priority: RequestPriority.Urgent);

        // Act
        var pending = await _requestService.GetPendingRequestsForFamilyAsync(child.FamilyId!.Value);

        // Assert
        pending.Should().HaveCount(2);
        pending[0].Id.Should().Be(urgentReq.Id); // Urgent first
        pending[1].Id.Should().Be(normalReq.Id);
    }

    // Statistics Tests (3)
    [Fact]
    public async Task GetRequestStatistics_CalculatesCorrectly()
    {
        // Arrange
        var child = await CreateChild(balance: 200m);

        // Create various requests
        var req1 = await CreateRequest(child.Id, 20m, "Approved 1");
        await _requestService.ApproveRequestAsync(req1.Id, "OK", _parentUserId);

        var req2 = await CreateRequest(child.Id, 30m, "Approved 2");
        await _requestService.ApproveRequestAsync(req2.Id, "OK", _parentUserId);

        var req3 = await CreateRequest(child.Id, 50m, "Rejected");
        await _requestService.RejectRequestAsync(req3.Id, "Too much", _parentUserId);

        var req4 = await CreateRequest(child.Id, 10m, "Pending");

        // Act
        var stats = await _requestService.GetRequestStatisticsAsync(child.Id);

        // Assert
        stats.TotalRequests.Should().Be(4);
        stats.Approved.Should().Be(2);
        stats.Rejected.Should().Be(1);
        stats.Pending.Should().Be(1);
        stats.TotalApprovedAmount.Should().Be(50m);
        stats.TotalRejectedAmount.Should().Be(50m);
        stats.ApprovalRate.Should().BeApproximately(66.67, 0.1); // 2/3
    }

    [Fact]
    public async Task GetRequestStatistics_CalculatesAverageResponseTime()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var request = await CreateRequest(child.Id, 20m, "Test");

        // Wait 2 hours (simulated)
        var twoHoursLater = DateTime.UtcNow.AddHours(2);
        request.ReviewedAt = twoHoursLater;
        await _context.SaveChangesAsync();

        await _requestService.ApproveRequestAsync(request.Id, "OK", _parentUserId);

        // Act
        var stats = await _requestService.GetRequestStatisticsAsync(child.Id);

        // Assert
        stats.AverageResponseTimeHours.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetRequestStatistics_TracksAutoApprovals()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        await CreateApprovalRule(child.FamilyId, maxAmount: 10m);

        var req1 = await CreateRequest(child.Id, 5m, "Auto 1");
        await _requestService.CheckAutoApprovalAsync(req1.Id);

        var req2 = await CreateRequest(child.Id, 8m, "Auto 2");
        await _requestService.CheckAutoApprovalAsync(req2.Id);

        var req3 = await CreateRequest(child.Id, 15m, "Manual");
        await _requestService.ApproveRequestAsync(req3.Id, "OK", _parentUserId);

        // Act
        var stats = await _requestService.GetRequestStatisticsAsync(child.Id);

        // Assert
        stats.AutoApprovedCount.Should().Be(2);
        stats.Approved.Should().Be(3);
    }
}
```

---

## Phase 3: API Controllers

### 3.1 TransactionRequestsController

```csharp
[ApiController]
[Route("api/v1/transaction-requests")]
[Authorize]
public class TransactionRequestsController : ControllerBase
{
    private readonly ITransactionRequestService _requestService;
    private readonly ICurrentUserService _currentUserService;

    public TransactionRequestsController(
        ITransactionRequestService requestService,
        ICurrentUserService currentUserService)
    {
        _requestService = requestService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Create new transaction request (child)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Child")]
    [ProducesResponseType(typeof(TransactionRequest), StatusCodes.Status201Created)]
    public async Task<ActionResult<TransactionRequest>> CreateRequest(CreateTransactionRequestDto dto)
    {
        var userId = _currentUserService.GetUserId();
        var request = await _requestService.CreateRequestAsync(dto, userId);
        return CreatedAtAction(nameof(GetRequest), new { id = request.Id }, request);
    }

    /// <summary>
    /// Get transaction request by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TransactionRequest), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionRequest>> GetRequest(Guid id)
    {
        var request = await _requestService.GetRequestAsync(id);
        return Ok(request);
    }

    /// <summary>
    /// Get all pending requests for family (parent)
    /// </summary>
    [HttpGet("family/pending")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(typeof(List<TransactionRequest>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TransactionRequest>>> GetPendingRequests()
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var requests = await _requestService.GetPendingRequestsForFamilyAsync(user.FamilyId!.Value);
        return Ok(requests);
    }

    /// <summary>
    /// Get requests for specific child
    /// </summary>
    [HttpGet("child/{childId}")]
    [ProducesResponseType(typeof(List<TransactionRequest>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TransactionRequest>>> GetRequestsForChild(
        Guid childId,
        [FromQuery] RequestStatus? status = null)
    {
        var requests = await _requestService.GetRequestsForChildAsync(childId, status);
        return Ok(requests);
    }

    /// <summary>
    /// Approve transaction request (parent)
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(typeof(TransactionRequest), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionRequest>> ApproveRequest(
        Guid id,
        [FromBody] ReviewRequest request)
    {
        var userId = _currentUserService.GetUserId();
        var approved = await _requestService.ApproveRequestAsync(id, request.ReviewNotes, userId);
        return Ok(approved);
    }

    /// <summary>
    /// Reject transaction request (parent)
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(typeof(TransactionRequest), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionRequest>> RejectRequest(
        Guid id,
        [FromBody] ReviewRequest request)
    {
        var userId = _currentUserService.GetUserId();
        var rejected = await _requestService.RejectRequestAsync(id, request.ReviewNotes!, userId);
        return Ok(rejected);
    }

    /// <summary>
    /// Cancel transaction request (child)
    /// </summary>
    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Child")]
    [ProducesResponseType(typeof(TransactionRequest), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionRequest>> CancelRequest(Guid id)
    {
        var userId = _currentUserService.GetUserId();
        var cancelled = await _requestService.CancelRequestAsync(id, userId);
        return Ok(cancelled);
    }

    /// <summary>
    /// Get request statistics
    /// </summary>
    [HttpGet("child/{childId}/statistics")]
    [ProducesResponseType(typeof(RequestStatistics), StatusCodes.Status200OK)]
    public async Task<ActionResult<RequestStatistics>> GetStatistics(Guid childId)
    {
        var stats = await _requestService.GetRequestStatisticsAsync(childId);
        return Ok(stats);
    }
}

public record ReviewRequest(string? ReviewNotes);
```

### 3.2 ApprovalRulesController

```csharp
[ApiController]
[Route("api/v1/approval-rules")]
[Authorize(Roles = "Parent")]
public class ApprovalRulesController : ControllerBase
{
    private readonly ITransactionRequestService _requestService;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Create approval rule
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApprovalRule), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApprovalRule>> CreateRule(CreateApprovalRuleDto dto)
    {
        var userId = _currentUserService.GetUserId();
        var rule = await _requestService.CreateApprovalRuleAsync(dto, userId);
        return CreatedAtAction(nameof(GetRule), new { id = rule.Id }, rule);
    }

    /// <summary>
    /// Get approval rule by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApprovalRule), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApprovalRule>> GetRule(Guid id)
    {
        // Implementation
        return Ok();
    }

    /// <summary>
    /// Get all approval rules for family
    /// </summary>
    [HttpGet("family/{familyId}")]
    [ProducesResponseType(typeof(List<ApprovalRule>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ApprovalRule>>> GetRulesForFamily(Guid familyId)
    {
        var rules = await _requestService.GetApprovalRulesAsync(familyId);
        return Ok(rules);
    }

    /// <summary>
    /// Update approval rule
    /// </summary>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(ApprovalRule), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApprovalRule>> UpdateRule(
        Guid id,
        UpdateApprovalRuleDto dto)
    {
        var userId = _currentUserService.GetUserId();
        var rule = await _requestService.UpdateApprovalRuleAsync(id, dto, userId);
        return Ok(rule);
    }

    /// <summary>
    /// Delete approval rule
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> DeleteRule(Guid id)
    {
        var userId = _currentUserService.GetUserId();
        await _requestService.DeleteApprovalRuleAsync(id, userId);
        return NoContent();
    }
}
```

---

## Phase 4: Blazor UI Components

### 4.1 CreateRequestForm Component

```razor
@page "/request-spending/{ChildId:guid}"
@inject ITransactionRequestService RequestService
@inject NavigationManager Navigation

<h3>Request Spending Approval</h3>

<EditForm Model="@formModel" OnValidSubmit="@HandleSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary />

    <div class="mb-3">
        <label>What do you want to buy?</label>
        <InputText @bind-Value="formModel.Description" class="form-control" />
    </div>

    <div class="row">
        <div class="col-md-6 mb-3">
            <label>Amount</label>
            <div class="input-group">
                <span class="input-group-text">$</span>
                <InputNumber @bind-Value="formModel.Amount" class="form-control" />
            </div>
        </div>

        <div class="col-md-6 mb-3">
            <label>Category</label>
            <InputSelect @bind-Value="formModel.Category" class="form-select">
                @foreach (var category in GetSpendingCategories())
                {
                    <option value="@category">@category.ToString()</option>
                }
            </InputSelect>
        </div>
    </div>

    <div class="mb-3">
        <label>Store/Merchant (optional)</label>
        <InputText @bind-Value="formModel.Merchant" class="form-control" />
    </div>

    <div class="mb-3">
        <label>Why do you want this?</label>
        <InputTextArea @bind-Value="formModel.Reason" class="form-control" rows="3" />
        <small class="text-muted">
            Explaining your reason helps parents understand your request
        </small>
    </div>

    <div class="mb-3">
        <div class="form-check">
            <InputCheckbox @bind-Value="formModel.IsUrgent" class="form-check-input" id="urgent" />
            <label class="form-check-label" for="urgent">
                This is urgent (faster review)
            </label>
        </div>
    </div>

    <div class="alert alert-info">
        <strong>Your current balance:</strong> @CurrentBalance.ToString("C")
    </div>

    <button type="submit" class="btn btn-primary" disabled="@submitting">
        @if (submitting)
        {
            <span class="spinner-border spinner-border-sm me-2"></span>
        }
        Submit Request
    </button>
    <button type="button" class="btn btn-secondary" @onclick="Cancel">
        Cancel
    </button>
</EditForm>

@code {
    [Parameter] public Guid ChildId { get; set; }

    private CreateRequestFormModel formModel = new();
    private decimal CurrentBalance = 0;
    private bool submitting = false;

    private class CreateRequestFormModel
    {
        [Required, StringLength(200)]
        public string Description { get; set; } = "";

        [Required, Range(0.01, 10000)]
        public decimal Amount { get; set; }

        [Required]
        public TransactionCategory Category { get; set; } = TransactionCategory.OtherSpending;

        public string? Merchant { get; set; }
        public string? Reason { get; set; }
        public bool IsUrgent { get; set; }
    }

    private async Task HandleSubmit()
    {
        submitting = true;

        try
        {
            var dto = new CreateTransactionRequestDto(
                ChildId,
                formModel.Amount,
                formModel.Category,
                formModel.Description,
                formModel.Merchant,
                formModel.Reason,
                formModel.IsUrgent ? RequestPriority.Urgent : RequestPriority.Normal);

            var request = await RequestService.CreateRequestAsync(dto, Guid.Empty);

            Navigation.NavigateTo($"/requests/{ChildId}");
        }
        finally
        {
            submitting = false;
        }
    }

    private void Cancel()
    {
        Navigation.NavigateTo($"/dashboard");
    }

    private List<TransactionCategory> GetSpendingCategories()
    {
        return new List<TransactionCategory>
        {
            TransactionCategory.Toys,
            TransactionCategory.Games,
            TransactionCategory.Books,
            TransactionCategory.Clothes,
            TransactionCategory.Snacks,
            TransactionCategory.Candy,
            TransactionCategory.Electronics,
            TransactionCategory.Entertainment,
            TransactionCategory.Sports,
            TransactionCategory.OtherSpending
        };
    }
}
```

### 4.2 PendingRequestsList Component (Parent View)

```razor
@page "/requests/pending"
@inject ITransactionRequestService RequestService
@attribute [Authorize(Roles = "Parent")]

<div class="pending-requests">
    <h3>Pending Approval Requests</h3>

    @if (Loading)
    {
        <div class="spinner-border"></div>
    }
    else if (Requests.Any())
    {
        <div class="alert alert-info">
            <strong>@Requests.Count</strong> request(s) awaiting your review
        </div>

        @foreach (var request in Requests)
        {
            <div class="card mb-3 @(request.Priority == RequestPriority.Urgent ? "border-warning" : "")">
                <div class="card-body">
                    <div class="d-flex justify-content-between align-items-start">
                        <div class="flex-grow-1">
                            @if (request.Priority == RequestPriority.Urgent)
                            {
                                <span class="badge bg-warning text-dark">Urgent</span>
                            }
                            <h5>@request.Child.FirstName wants to buy:</h5>
                            <p class="lead">@request.Description</p>

                            <div class="request-details">
                                <div><strong>Amount:</strong> @request.Amount.ToString("C")</div>
                                <div><strong>Category:</strong> @request.Category.ToString()</div>
                                @if (!string.IsNullOrEmpty(request.Merchant))
                                {
                                    <div><strong>Store:</strong> @request.Merchant</div>
                                }
                                <div><strong>Requested:</strong> @request.RequestedAt.ToString("MMM dd, yyyy h:mm tt")</div>
                            </div>

                            @if (!string.IsNullOrEmpty(request.Reason))
                            {
                                <div class="alert alert-light mt-2">
                                    <strong>Why:</strong> @request.Reason
                                </div>
                            }

                            <div class="mt-2">
                                <small class="text-muted">
                                    Child's balance: @request.Child.CurrentBalance.ToString("C")
                                    @if (request.Child.CurrentBalance < request.Amount)
                                    {
                                        <span class="text-danger">(Insufficient!)</span>
                                    }
                                </small>
                            </div>
                        </div>

                        <div class="request-actions">
                            <button class="btn btn-success mb-2" @onclick="() => ShowApprovalDialog(request)">
                                Approve
                            </button>
                            <button class="btn btn-danger" @onclick="() => ShowRejectionDialog(request)">
                                Reject
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        }
    }
    else
    {
        <div class="alert alert-success">
            No pending requests. All caught up!
        </div>
    }
</div>

@code {
    private List<TransactionRequest> Requests = new();
    private bool Loading = true;

    protected override async Task OnInitializedAsync()
    {
        await RefreshRequests();
    }

    private async Task RefreshRequests()
    {
        Loading = true;
        // Load pending requests
        Loading = false;
    }

    private void ShowApprovalDialog(TransactionRequest request)
    {
        // Show modal dialog
    }

    private void ShowRejectionDialog(TransactionRequest request)
    {
        // Show modal dialog
    }
}
```

### 4.3 MyRequestsList Component (Child View)

```razor
@page "/my-requests"
@inject ITransactionRequestService RequestService
@attribute [Authorize(Roles = "Child")]

<div class="my-requests">
    <div class="d-flex justify-content-between align-items-center mb-4">
        <h3>My Spending Requests</h3>
        <button class="btn btn-primary" @onclick="CreateNewRequest">
            New Request
        </button>
    </div>

    @if (PendingRequests.Any())
    {
        <h4>Pending (@PendingRequests.Count)</h4>
        @foreach (var request in PendingRequests)
        {
            <RequestCard Request="@request" OnCancel="@RefreshRequests" />
        }
    }

    @if (ReviewedRequests.Any())
    {
        <h4 class="mt-4">Recent Decisions</h4>
        @foreach (var request in ReviewedRequests)
        {
            <RequestCard Request="@request" />
        }
    }
</div>

@code {
    [Parameter] public Guid ChildId { get; set; }

    private List<TransactionRequest> PendingRequests = new();
    private List<TransactionRequest> ReviewedRequests = new();

    private async Task RefreshRequests()
    {
        PendingRequests = await RequestService.GetRequestsForChildAsync(
            ChildId, RequestStatus.Pending);

        ReviewedRequests = await RequestService.GetRequestsForChildAsync(
            ChildId, null);
        ReviewedRequests = ReviewedRequests
            .Where(r => r.Status != RequestStatus.Pending)
            .OrderByDescending(r => r.ReviewedAt ?? r.RequestedAt)
            .Take(10)
            .ToList();
    }

    private void CreateNewRequest()
    {
        // Navigate to create form
    }
}
```

---

## Success Metrics

- All 25 tests passing
- Children can create spending requests
- Parents receive real-time notifications
- Approval/rejection workflow functional
- Auto-approval rules working correctly
- Request history tracked
- Statistics calculated accurately
- SignalR notifications delivered

---

## Future Enhancements

1. **Request Templates**: Save frequent purchases as templates
2. **Split Requests**: Request from multiple children for shared item
3. **Photo Attachments**: Attach product photos to requests
4. **Price Comparison**: API integration to check prices
5. **Negotiation**: Back-and-forth messaging on requests
6. **Scheduled Approvals**: Auto-approve at specific times
7. **Allowance Reserve**: Hold funds for pending requests

---

**Total Implementation Time**: 3-4 weeks following TDD methodology
