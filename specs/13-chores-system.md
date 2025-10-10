# Chores & Tasks System Specification

## Overview

The Chores system enables parents to create tasks for children to earn money beyond their regular allowance. This teaches work ethic, responsibility, and the connection between effort and rewards.

## Goals

1. **Earn Through Effort**: Children learn money comes from work
2. **Approval Workflow**: Parents verify completion before payment
3. **Recurring Tasks**: Automate daily/weekly chores
4. **Photo Proof**: Optional photo upload for verification
5. **Performance Tracking**: Statistics on completion rates
6. **TDD Approach**: 45+ comprehensive tests

---

## Phase 1: Database Schema

### 1.1 Chore Model

```csharp
namespace AllowanceTracker.Models;

public class Chore
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Reward amount paid upon approval
    /// </summary>
    public decimal RewardAmount { get; set; }

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    public Guid FamilyId { get; set; }
    public virtual Family Family { get; set; } = null!;

    public ChoreStatus Status { get; set; } = ChoreStatus.Assigned;

    /// <summary>
    /// When child marked as completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Photo proof URL (optional)
    /// </summary>
    public string? ProofPhotoUrl { get; set; }

    /// <summary>
    /// When parent approved/rejected
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    public Guid? ReviewedById { get; set; }
    public virtual ApplicationUser? ReviewedBy { get; set; }

    /// <summary>
    /// Parent's review comments
    /// </summary>
    public string? ReviewNotes { get; set; }

    /// <summary>
    /// Transaction created upon approval
    /// </summary>
    public Guid? TransactionId { get; set; }
    public virtual Transaction? Transaction { get; set; }

    /// <summary>
    /// Due date for completion (optional)
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Link to recurring chore template
    /// </summary>
    public Guid? ChoreTemplateId { get; set; }
    public virtual ChoreTemplate? ChoreTemplate { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid CreatedById { get; set; }
    public virtual ApplicationUser CreatedBy { get; set; } = null!;
}

public enum ChoreStatus
{
    Assigned = 1,        // Created by parent
    InProgress = 2,      // Child started working
    Completed = 3,       // Child marked done, awaiting approval
    Approved = 4,        // Parent approved, payment issued
    Rejected = 5,        // Parent rejected, no payment
    Expired = 6          // Past due date without completion
}
```

### 1.2 ChoreTemplate Model

```csharp
namespace AllowanceTracker.Models;

/// <summary>
/// Recurring chore templates that auto-generate chore instances
/// </summary>
public class ChoreTemplate
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal RewardAmount { get; set; }

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    public Guid FamilyId { get; set; }
    public virtual Family Family { get; set; } = null!;

    public RecurrencePattern RecurrencePattern { get; set; }

    /// <summary>
    /// Days of week (for Weekly pattern): 0=Sunday, 6=Saturday
    /// </summary>
    public string? DaysOfWeek { get; set; } // JSON: [1,3,5] for Mon/Wed/Fri

    /// <summary>
    /// Day of month (for Monthly pattern): 1-31
    /// </summary>
    public int? DayOfMonth { get; set; }

    /// <summary>
    /// Last date a chore was generated from this template
    /// </summary>
    public DateTime? LastGeneratedDate { get; set; }

    /// <summary>
    /// Is template active?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Require photo proof?
    /// </summary>
    public bool RequirePhoto { get; set; } = false;

    /// <summary>
    /// Auto-approve after X hours if no review
    /// </summary>
    public int? AutoApproveHours { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid CreatedById { get; set; }
    public virtual ApplicationUser CreatedBy { get; set; } = null!;

    public virtual ICollection<Chore> Chores { get; set; } = new List<Chore>();
}

public enum RecurrencePattern
{
    OneTime = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3
}
```

### 1.3 Database Migration

```bash
dotnet ef migrations add AddChoresSystem
```

---

## Phase 2: Service Layer (TDD)

### 2.1 IChoreService Interface

```csharp
namespace AllowanceTracker.Services;

public interface IChoreService
{
    // Chore Management
    Task<Chore> CreateChoreAsync(CreateChoreDto dto, Guid currentUserId);
    Task<Chore> GetChoreAsync(Guid choreId);
    Task<List<Chore>> GetChoresForChildAsync(Guid childId, ChoreStatus? status = null);
    Task<List<Chore>> GetPendingApprovalsAsync(Guid familyId);
    Task UpdateChoreAsync(Guid choreId, UpdateChoreDto dto, Guid currentUserId);
    Task DeleteChoreAsync(Guid choreId, Guid currentUserId);

    // Child Actions
    Task<Chore> StartChoreAsync(Guid choreId, Guid childUserId);
    Task<Chore> CompleteChoreAsync(Guid choreId, string? proofPhotoUrl, Guid childUserId);

    // Parent Actions
    Task<Chore> ApproveChoreAsync(Guid choreId, string? reviewNotes, Guid parentUserId);
    Task<Chore> RejectChoreAsync(Guid choreId, string reviewNotes, Guid parentUserId);

    // Statistics
    Task<ChoreStatistics> GetStatisticsAsync(Guid childId, DateTime? startDate = null);

    // Templates
    Task<ChoreTemplate> CreateTemplateAsync(CreateChoreTemplateDto dto, Guid currentUserId);
    Task<List<ChoreTemplate>> GetTemplatesAsync(Guid familyId);
    Task<ChoreTemplate> UpdateTemplateAsync(Guid templateId, UpdateChoreTemplateDto dto);
    Task DeleteTemplateAsync(Guid templateId, Guid currentUserId);

    // Background Job
    Task GenerateRecurringChoresAsync();
}
```

### 2.2 DTOs

```csharp
namespace AllowanceTracker.DTOs;

public record CreateChoreDto(
    Guid ChildId,
    string Title,
    string Description,
    decimal RewardAmount,
    DateTime? DueDate = null,
    bool RequirePhoto = false);

public record UpdateChoreDto(
    string? Title = null,
    string? Description = null,
    decimal? RewardAmount = null,
    DateTime? DueDate = null);

public record CreateChoreTemplateDto(
    Guid ChildId,
    string Title,
    string Description,
    decimal RewardAmount,
    RecurrencePattern RecurrencePattern,
    int[]? DaysOfWeek = null,
    int? DayOfMonth = null,
    bool RequirePhoto = false,
    int? AutoApproveHours = null);

public record UpdateChoreTemplateDto(
    string? Title = null,
    string? Description = null,
    decimal? RewardAmount = null,
    bool? IsActive = null);

public record ChoreStatistics(
    int TotalAssigned,
    int Completed,
    int Approved,
    int Rejected,
    decimal TotalEarned,
    double CompletionRate,
    double ApprovalRate,
    double AverageCompletionHours);
```

### 2.3 Key Service Methods

```csharp
public class ChoreService : IChoreService
{
    private readonly AllowanceContext _context;
    private readonly ITransactionService _transactionService;
    private readonly IHubContext<FamilyHub>? _hubContext;

    public async Task<Chore> ApproveChoreAsync(Guid choreId, string? reviewNotes, Guid parentUserId)
    {
        var chore = await _context.Chores
            .Include(c => c.Child)
            .FirstOrDefaultAsync(c => c.Id == choreId)
            ?? throw new NotFoundException("Chore not found");

        if (chore.Status != ChoreStatus.Completed)
            throw new InvalidOperationException("Can only approve completed chores");

        // Create transaction for payment
        var transactionDto = new CreateTransactionDto(
            chore.ChildId,
            chore.RewardAmount,
            TransactionType.Credit,
            TransactionCategory.Chores,
            $"Chore completed: {chore.Title}");

        var transaction = await _transactionService.CreateTransactionAsync(transactionDto);

        // Update chore
        chore.Status = ChoreStatus.Approved;
        chore.ReviewedAt = DateTime.UtcNow;
        chore.ReviewedById = parentUserId;
        chore.ReviewNotes = reviewNotes;
        chore.TransactionId = transaction.Id;

        await _context.SaveChangesAsync();

        // Notify via SignalR
        await _hubContext?.Clients
            .Group($"family-{chore.FamilyId}")
            .SendAsync("ChoreApproved", choreId, chore.ChildId, chore.RewardAmount);

        return chore;
    }

    public async Task GenerateRecurringChoresAsync()
    {
        var now = DateTime.UtcNow;
        var templates = await _context.ChoreTemplates
            .Where(t => t.IsActive)
            .ToListAsync();

        foreach (var template in templates)
        {
            if (ShouldGenerateChore(template, now))
            {
                var chore = new Chore
                {
                    Id = Guid.NewGuid(),
                    Title = template.Title,
                    Description = template.Description,
                    RewardAmount = template.RewardAmount,
                    ChildId = template.ChildId,
                    FamilyId = template.FamilyId,
                    Status = ChoreStatus.Assigned,
                    ChoreTemplateId = template.Id,
                    CreatedAt = now,
                    CreatedById = template.CreatedById,
                    DueDate = CalculateDueDate(template, now)
                };

                _context.Chores.Add(chore);
                template.LastGeneratedDate = now.Date;
            }
        }

        await _context.SaveChangesAsync();
    }

    private bool ShouldGenerateChore(ChoreTemplate template, DateTime now)
    {
        if (template.LastGeneratedDate?.Date == now.Date)
            return false; // Already generated today

        return template.RecurrencePattern switch
        {
            RecurrencePattern.Daily => true,
            RecurrencePattern.Weekly => CheckWeeklyPattern(template, now),
            RecurrencePattern.Monthly => now.Day == template.DayOfMonth,
            _ => false
        };
    }

    private bool CheckWeeklyPattern(ChoreTemplate template, DateTime now)
    {
        if (string.IsNullOrEmpty(template.DaysOfWeek))
            return false;

        var days = JsonSerializer.Deserialize<int[]>(template.DaysOfWeek);
        return days?.Contains((int)now.DayOfWeek) ?? false;
    }
}
```

### 2.4 Test Cases (20 Service Tests)

```csharp
public class ChoreServiceTests
{
    // Create chore tests
    [Fact] CreateChore_ValidData_CreatesSuccessfully
    [Fact] CreateChore_WithDueDate_SetsDueDateCorrectly
    [Fact] CreateChore_InvalidChildId_ThrowsNotFoundException

    // Complete chore tests
    [Fact] CompleteChore_ValidChore_UpdatesStatus
    [Fact] CompleteChore_WithProofPhoto_StoresPhotoUrl
    [Fact] CompleteChore_AlreadyCompleted_ThrowsException

    // Approve chore tests
    [Fact] ApproveChore_CompletedChore_CreatesTransaction
    [Fact] ApproveChore_UpdatesBalanceCorrectly
    [Fact] ApproveChore_NotCompleted_ThrowsException
    [Fact] ApproveChore_SendsSignalRNotification

    // Reject chore tests
    [Fact] RejectChore_WithNotes_UpdatesStatusAndNotes
    [Fact] RejectChore_DoesNotCreateTransaction

    // Statistics tests
    [Fact] GetStatistics_CalculatesCompletionRateCorrectly
    [Fact] GetStatistics_CalculatesTotalEarnedCorrectly

    // Template tests
    [Fact] CreateTemplate_DailyRecurrence_CreatesSuccessfully
    [Fact] CreateTemplate_WeeklyWithDays_StoresCorrectly

    // Recurring generation tests
    [Fact] GenerateRecurringChores_DailyTemplate_CreatesChoreDaily
    [Fact] GenerateRecurringChores_WeeklyTemplate_CreatesOnCorrectDays
    [Fact] GenerateRecurringChores_MonthlyTemplate_CreatesOnCorrectDay
    [Fact] GenerateRecurringChores_DoesNotDuplicateSameDay
}
```

---

## Phase 3: API Controllers

### 3.1 ChoresController

```csharp
[ApiController]
[Route("api/v1/chores")]
[Authorize]
public class ChoresController : ControllerBase
{
    private readonly IChoreService _choreService;
    private readonly ICurrentUserService _currentUserService;

    [HttpPost]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<Chore>> CreateChore(CreateChoreDto dto)
    {
        var userId = _currentUserService.GetUserId();
        var chore = await _choreService.CreateChoreAsync(dto, userId);
        return CreatedAtAction(nameof(GetChore), new { id = chore.Id }, chore);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Chore>> GetChore(Guid id)
    {
        var chore = await _choreService.GetChoreAsync(id);
        return Ok(chore);
    }

    [HttpGet("child/{childId}")]
    public async Task<ActionResult<List<Chore>>> GetChoresForChild(
        Guid childId,
        [FromQuery] ChoreStatus? status = null)
    {
        var chores = await _choreService.GetChoresForChildAsync(childId, status);
        return Ok(chores);
    }

    [HttpGet("family/pending")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<List<Chore>>> GetPendingApprovals()
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var chores = await _choreService.GetPendingApprovalsAsync(user.FamilyId.Value);
        return Ok(chores);
    }

    [HttpPost("{id}/complete")]
    public async Task<ActionResult<Chore>> CompleteChore(
        Guid id,
        [FromBody] CompleteChoreRequest request)
    {
        var userId = _currentUserService.GetUserId();
        var chore = await _choreService.CompleteChoreAsync(id, request.ProofPhotoUrl, userId);
        return Ok(chore);
    }

    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<Chore>> ApproveChore(
        Guid id,
        [FromBody] ReviewChoreRequest request)
    {
        var userId = _currentUserService.GetUserId();
        var chore = await _choreService.ApproveChoreAsync(id, request.ReviewNotes, userId);
        return Ok(chore);
    }

    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<Chore>> RejectChore(
        Guid id,
        [FromBody] ReviewChoreRequest request)
    {
        var userId = _currentUserService.GetUserId();
        var chore = await _choreService.RejectChoreAsync(id, request.ReviewNotes, userId);
        return Ok(chore);
    }

    [HttpGet("child/{childId}/statistics")]
    public async Task<ActionResult<ChoreStatistics>> GetStatistics(Guid childId)
    {
        var stats = await _choreService.GetStatisticsAsync(childId);
        return Ok(stats);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult> DeleteChore(Guid id)
    {
        var userId = _currentUserService.GetUserId();
        await _choreService.DeleteChoreAsync(id, userId);
        return NoContent();
    }
}

public record CompleteChoreRequest(string? ProofPhotoUrl);
public record ReviewChoreRequest(string? ReviewNotes);
```

---

## Phase 4: Blazor Components

### 4.1 ChoresList Component

```razor
@inject IChoreService ChoreService

<div class="chores-list">
    <div class="d-flex justify-content-between align-items-center mb-3">
        <h5>Chores</h5>
        @if (IsParent)
        {
            <button class="btn btn-sm btn-primary" @onclick="OnAddChore">
                <span class="oi oi-plus"></span> Add Chore
            </button>
        }
    </div>

    @if (Loading)
    {
        <div class="spinner-border"></div>
    }
    else if (Chores.Any())
    {
        @foreach (var group in GroupedChores)
        {
            <div class="chore-group mb-4">
                <h6 class="text-muted">@group.Key</h6>
                @foreach (var chore in group.Value)
                {
                    <ChoreCard Chore="@chore"
                              IsParent="@IsParent"
                              OnStatusChanged="@RefreshChores" />
                }
            </div>
        }
    }
    else
    {
        <div class="alert alert-info">
            <p>No chores assigned yet.</p>
            @if (IsParent)
            {
                <button class="btn btn-sm btn-primary" @onclick="OnAddChore">
                    Create First Chore
                </button>
            }
        </div>
    }
</div>

@code {
    [Parameter] public Guid ChildId { get; set; }
    [Parameter] public bool IsParent { get; set; }
    [Parameter] public EventCallback OnAddChore { get; set; }

    private List<Chore> Chores = new();
    private bool Loading = true;

    private Dictionary<string, List<Chore>> GroupedChores =>
        Chores.GroupBy(c => c.Status.ToString())
              .ToDictionary(g => g.Key, g => g.ToList());

    protected override async Task OnInitializedAsync()
    {
        await RefreshChores();
    }

    private async Task RefreshChores()
    {
        Loading = true;
        Chores = await ChoreService.GetChoresForChildAsync(ChildId);
        Loading = false;
        StateHasChanged();
    }
}
```

### 4.2 ChoreCard Component

```razor
<div class="card chore-card mb-2 @GetStatusClass()">
    <div class="card-body">
        <div class="d-flex justify-content-between align-items-start">
            <div class="flex-grow-1">
                <h6 class="mb-1">
                    @GetStatusIcon() @Chore.Title
                </h6>
                <p class="text-muted small mb-2">@Chore.Description</p>
                <div class="chore-meta">
                    <span class="badge bg-success">@Chore.RewardAmount.ToString("C")</span>
                    @if (Chore.DueDate.HasValue)
                    {
                        <span class="badge @(IsPastDue() ? "bg-danger" : "bg-secondary")">
                            Due: @Chore.DueDate.Value.ToString("MMM dd")
                        </span>
                    }
                </div>
            </div>

            <div class="chore-actions">
                @if (Chore.Status == ChoreStatus.Assigned && !IsParent)
                {
                    <button class="btn btn-sm btn-primary" @onclick="OnStartChore">
                        Start
                    </button>
                }
                else if (Chore.Status == ChoreStatus.InProgress && !IsParent)
                {
                    <button class="btn btn-sm btn-success" @onclick="OnCompleteChore">
                        Mark Done
                    </button>
                }
                else if (Chore.Status == ChoreStatus.Completed && IsParent)
                {
                    <div class="btn-group-vertical">
                        <button class="btn btn-sm btn-success" @onclick="OnApproveChore">
                            ✓ Approve
                        </button>
                        <button class="btn btn-sm btn-danger" @onclick="OnRejectChore">
                            ✗ Reject
                        </button>
                    </div>
                }
            </div>
        </div>

        @if (Chore.ProofPhotoUrl != null)
        {
            <div class="mt-2">
                <img src="@Chore.ProofPhotoUrl" class="img-thumbnail" style="max-width: 200px;" />
            </div>
        }

        @if (Chore.ReviewNotes != null)
        {
            <div class="alert alert-info mt-2 mb-0">
                <small><strong>Review:</strong> @Chore.ReviewNotes</small>
            </div>
        }
    </div>
</div>

@code {
    [Parameter] public Chore Chore { get; set; } = null!;
    [Parameter] public bool IsParent { get; set; }
    [Parameter] public EventCallback OnStatusChanged { get; set; }

    private string GetStatusClass() => Chore.Status switch
    {
        ChoreStatus.Approved => "border-success",
        ChoreStatus.Rejected => "border-danger",
        ChoreStatus.Completed => "border-warning",
        _ => ""
    };

    private string GetStatusIcon() => Chore.Status switch
    {
        ChoreStatus.Assigned => "📋",
        ChoreStatus.InProgress => "⏳",
        ChoreStatus.Completed => "✅",
        ChoreStatus.Approved => "💰",
        ChoreStatus.Rejected => "❌",
        _ => ""
    };

    private bool IsPastDue() =>
        Chore.DueDate.HasValue && Chore.DueDate.Value < DateTime.Now;

    private async Task OnStartChore()
    {
        // Call service to start chore
        await OnStatusChanged.InvokeAsync();
    }

    private async Task OnCompleteChore()
    {
        // Call service to complete chore
        await OnStatusChanged.InvokeAsync();
    }

    private async Task OnApproveChore()
    {
        // Call service to approve chore
        await OnStatusChanged.InvokeAsync();
    }

    private async Task OnRejectChore()
    {
        // Call service to reject chore
        await OnStatusChanged.InvokeAsync();
    }
}
```

### 4.3 Component Tests (20 Tests)

```csharp
public class ChoresListTests : TestContext
{
    [Fact] ChoresList_RendersEmptyState_WithNoChores
    [Fact] ChoresList_DisplaysChores_GroupedByStatus
    [Fact] ChoresList_ShowsAddButton_ForParents
    [Fact] ChoresList_HidesAddButton_ForChildren

    // Additional 16 tests...
}
```

---

## Phase 5: Background Job

### 5.1 RecurringChoresJob

```csharp
public class RecurringChoresJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RecurringChoresJob> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RecurringChoresJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var choreService = scope.ServiceProvider.GetRequiredService<IChoreService>();

                await choreService.GenerateRecurringChoresAsync();
                _logger.LogInformation("Generated recurring chores successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating recurring chores");
            }

            // Run once per day at midnight
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
```

---

## Success Metrics

- ✅ 45 tests passing (20 service + 20 component + 5 integration)
- ✅ Chores created and assigned successfully
- ✅ Approval workflow functional
- ✅ Recurring chores generated daily
- ✅ Real-time notifications via SignalR
- ✅ Photo proof uploads working

---

## Future Enhancements

1. **Chore Templates Library**: Pre-made chore ideas by age
2. **Chore Difficulty Levels**: Easy/Medium/Hard with varying rewards
3. **Chore Chains**: Complete A before B unlocks
4. **Team Chores**: Multiple children work together
5. **Chore Marketplace**: Children bid on available chores

---

**Total Implementation Time**: 4-6 weeks following TDD
