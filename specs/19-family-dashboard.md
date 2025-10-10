# Family Dashboard Specification

## Overview

This specification creates a comprehensive parent dashboard showing family-wide financial statistics, comparing children, tracking pending actions, and displaying activity feeds. This gives parents a bird's-eye view of their family's financial learning journey.

## Goals

1. **Aggregate Statistics**: Total balances, savings, and spending across all children
2. **Child Comparison**: Side-by-side comparison charts
3. **Pending Actions**: Quick view of approvals, requests, and chores
4. **Activity Feed**: Real-time family financial activity
5. **Financial Health Score**: Overall family financial wellness metric
6. **Export Reports**: Generate PDF/CSV family reports
7. **TDD Approach**: 20 comprehensive tests

---

## Phase 1: Service Layer

### 1.1 IFamilyDashboardService Interface

```csharp
namespace AllowanceTracker.Services;

public interface IFamilyDashboardService
{
    Task<FamilyOverview> GetFamilyOverviewAsync(Guid familyId);
    Task<List<ChildComparison>> GetChildComparisonsAsync(Guid familyId);
    Task<PendingActions> GetPendingActionsAsync(Guid familyId);
    Task<List<FamilyActivity>> GetActivityFeedAsync(Guid familyId, int days = 7);
    Task<FamilyFinancialHealth> CalculateFinancialHealthAsync(Guid familyId);
    Task<byte[]> ExportFamilyReportAsync(Guid familyId, DateTime startDate, DateTime endDate);
}
```

### 1.2 Data Transfer Objects

```csharp
namespace AllowanceTracker.DTOs;

public record FamilyOverview(
    int TotalChildren,
    decimal TotalBalance,
    decimal TotalSavingsGoals,
    decimal TotalAllowancePerWeek,
    decimal TotalSpentThisMonth,
    decimal TotalSavedThisMonth,
    int TotalTransactionsThisMonth,
    int TotalGoalsCompleted,
    int TotalChoresCompleted,
    DateTime LastActivity);

public record ChildComparison(
    Guid ChildId,
    string Name,
    decimal Balance,
    decimal SavingsRate,
    int GoalsCompleted,
    int ChoresCompleted,
    int CurrentStreak,
    int AchievementsUnlocked,
    decimal MonthlySpending,
    decimal MonthlySavings);

public record PendingActions(
    int PendingRequests,
    int PendingChores,
    int UnapprovedGoals,
    int UpcomingAllowances,
    List<TransactionRequest> RecentRequests,
    List<Chore> RecentChores);

public record FamilyActivity(
    DateTime Timestamp,
    ActivityType Type,
    Guid ChildId,
    string ChildName,
    string Description,
    decimal? Amount,
    string Icon);

public enum ActivityType
{
    Transaction,
    SavingsGoal,
    ChoreCompleted,
    Achievement,
    Streak,
    Request
}

public record FamilyFinancialHealth(
    int OverallScore,      // 0-100
    int SavingsScore,      // 0-100
    int ActivityScore,     // 0-100
    int GoalScore,         // 0-100
    HealthRating Rating,   // Poor, Fair, Good, Excellent
    string Summary,
    List<string> Strengths,
    List<string> Recommendations);

public enum HealthRating
{
    Poor,
    Fair,
    Good,
    Excellent
}
```

### 1.3 FamilyDashboardService Implementation

```csharp
namespace AllowanceTracker.Services;

public class FamilyDashboardService : IFamilyDashboardService
{
    private readonly AllowanceContext _context;
    private readonly ILogger<FamilyDashboardService> _logger;

    public async Task<FamilyOverview> GetFamilyOverviewAsync(Guid familyId)
    {
        var children = await _context.Children
            .Where(c => c.FamilyId == familyId)
            .ToListAsync();

        if (!children.Any())
            return new FamilyOverview(0, 0, 0, 0, 0, 0, 0, 0, 0, DateTime.UtcNow);

        var totalBalance = children.Sum(c => c.CurrentBalance);
        var totalSavingsGoals = children.Sum(c => c.TotalGoalSavings);
        var totalAllowancePerWeek = children.Sum(c => c.WeeklyAllowance);

        var monthStart = DateTime.UtcNow.AddMonths(-1).Date;
        var childIds = children.Select(c => c.Id).ToList();

        var monthTransactions = await _context.Transactions
            .Where(t => childIds.Contains(t.ChildId) && t.CreatedAt >= monthStart)
            .ToListAsync();

        var totalSpent = monthTransactions
            .Where(t => t.Type == TransactionType.Debit)
            .Sum(t => t.Amount);

        var totalSaved = monthTransactions
            .Where(t => t.Type == TransactionType.Credit)
            .Sum(t => t.Amount);

        var totalGoalsCompleted = await _context.WishListItems
            .CountAsync(g => childIds.Contains(g.ChildId) && g.CompletedAt != null);

        var totalChoresCompleted = await _context.Chores
            .CountAsync(c => childIds.Contains(c.ChildId) && c.Status == ChoreStatus.Approved);

        var lastActivity = await _context.Transactions
            .Where(t => childIds.Contains(t.ChildId))
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        return new FamilyOverview(
            children.Count,
            totalBalance,
            totalSavingsGoals,
            totalAllowancePerWeek,
            totalSpent,
            totalSaved,
            monthTransactions.Count,
            totalGoalsCompleted,
            totalChoresCompleted,
            lastActivity);
    }

    public async Task<List<ChildComparison>> GetChildComparisonsAsync(Guid familyId)
    {
        var children = await _context.Children
            .Include(c => c.Achievements)
            .Where(c => c.FamilyId == familyId)
            .ToListAsync();

        var monthStart = DateTime.UtcNow.AddMonths(-1).Date;
        var comparisons = new List<ChildComparison>();

        foreach (var child in children)
        {
            var monthTransactions = await _context.Transactions
                .Where(t => t.ChildId == child.Id && t.CreatedAt >= monthStart)
                .ToListAsync();

            var monthlySpending = monthTransactions
                .Where(t => t.Type == TransactionType.Debit)
                .Sum(t => t.Amount);

            var monthlySavings = monthTransactions
                .Where(t => t.Type == TransactionType.Credit)
                .Sum(t => t.Amount);

            var savingsRate = monthlySavings > 0
                ? Math.Round((monthlySavings / (monthlySavings + monthlySpending)) * 100, 2)
                : 0;

            var goalsCompleted = await _context.WishListItems
                .CountAsync(g => g.ChildId == child.Id && g.CompletedAt != null);

            var choresCompleted = await _context.Chores
                .CountAsync(c => c.ChildId == child.Id && c.Status == ChoreStatus.Approved);

            comparisons.Add(new ChildComparison(
                child.Id,
                child.FirstName,
                child.CurrentBalance,
                savingsRate,
                goalsCompleted,
                choresCompleted,
                child.CurrentStreak,
                child.AchievementsUnlocked,
                monthlySpending,
                monthlySavings));
        }

        return comparisons.OrderByDescending(c => c.Balance).ToList();
    }

    public async Task<PendingActions> GetPendingActionsAsync(Guid familyId)
    {
        var pendingRequests = await _context.TransactionRequests
            .Where(r => r.FamilyId == familyId && r.Status == RequestStatus.Pending)
            .CountAsync();

        var pendingChores = await _context.Chores
            .Where(c => c.FamilyId == familyId && c.Status == ChoreStatus.Completed)
            .CountAsync();

        var recentRequests = await _context.TransactionRequests
            .Include(r => r.Child)
            .Where(r => r.FamilyId == familyId && r.Status == RequestStatus.Pending)
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.RequestedAt)
            .Take(5)
            .ToListAsync();

        var recentChores = await _context.Chores
            .Include(c => c.Child)
            .Where(c => c.FamilyId == familyId && c.Status == ChoreStatus.Completed)
            .OrderByDescending(c => c.CompletedAt)
            .Take(5)
            .ToListAsync();

        var nextWeekStart = DateTime.UtcNow.AddDays(7).Date;
        var upcomingAllowances = await _context.Children
            .Where(c => c.FamilyId == familyId &&
                       (!c.LastAllowanceDate.HasValue ||
                        c.LastAllowanceDate.Value.AddDays(7) <= nextWeekStart))
            .CountAsync();

        return new PendingActions(
            pendingRequests,
            pendingChores,
            0, // unapproved goals placeholder
            upcomingAllowances,
            recentRequests,
            recentChores);
    }

    public async Task<List<FamilyActivity>> GetActivityFeedAsync(Guid familyId, int days = 7)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        var activities = new List<FamilyActivity>();

        var children = await _context.Children
            .Where(c => c.FamilyId == familyId)
            .ToListAsync();

        var childIds = children.Select(c => c.Id).ToList();
        var childNames = children.ToDictionary(c => c.Id, c => c.FirstName);

        // Transactions
        var transactions = await _context.Transactions
            .Where(t => childIds.Contains(t.ChildId) && t.CreatedAt >= startDate)
            .OrderByDescending(t => t.CreatedAt)
            .Take(20)
            .ToListAsync();

        foreach (var tx in transactions)
        {
            activities.Add(new FamilyActivity(
                tx.CreatedAt,
                ActivityType.Transaction,
                tx.ChildId,
                childNames[tx.ChildId],
                tx.Type == TransactionType.Credit
                    ? $"Received {tx.Amount:C}: {tx.Description}"
                    : $"Spent {tx.Amount:C}: {tx.Description}",
                tx.Amount,
                tx.Type == TransactionType.Credit ? "💵" : "💸"));
        }

        // Goals completed
        var completedGoals = await _context.WishListItems
            .Where(g => childIds.Contains(g.ChildId) &&
                       g.CompletedAt.HasValue &&
                       g.CompletedAt >= startDate)
            .OrderByDescending(g => g.CompletedAt)
            .Take(10)
            .ToListAsync();

        foreach (var goal in completedGoals)
        {
            activities.Add(new FamilyActivity(
                goal.CompletedAt!.Value,
                ActivityType.SavingsGoal,
                goal.ChildId,
                childNames[goal.ChildId],
                $"Completed savings goal: {goal.Name}",
                goal.TargetAmount,
                "🎯"));
        }

        // Chores
        var approvedChores = await _context.Chores
            .Where(c => childIds.Contains(c.ChildId) &&
                       c.Status == ChoreStatus.Approved &&
                       c.ReviewedAt.HasValue &&
                       c.ReviewedAt >= startDate)
            .OrderByDescending(c => c.ReviewedAt)
            .Take(10)
            .ToListAsync();

        foreach (var chore in approvedChores)
        {
            activities.Add(new FamilyActivity(
                chore.ReviewedAt!.Value,
                ActivityType.ChoreCompleted,
                chore.ChildId,
                childNames[chore.ChildId],
                $"Completed chore: {chore.Title}",
                chore.RewardAmount,
                "💪"));
        }

        // Achievements
        var recentAchievements = await _context.ChildAchievements
            .Include(ca => ca.Achievement)
            .Where(ca => childIds.Contains(ca.ChildId) && ca.UnlockedAt >= startDate)
            .OrderByDescending(ca => ca.UnlockedAt)
            .Take(10)
            .ToListAsync();

        foreach (var achievement in recentAchievements)
        {
            activities.Add(new FamilyActivity(
                achievement.UnlockedAt,
                ActivityType.Achievement,
                achievement.ChildId,
                childNames[achievement.ChildId],
                $"Unlocked: {achievement.Achievement.Title}",
                null,
                achievement.Achievement.Icon));
        }

        return activities
            .OrderByDescending(a => a.Timestamp)
            .Take(50)
            .ToList();
    }

    public async Task<FamilyFinancialHealth> CalculateFinancialHealthAsync(Guid familyId)
    {
        var children = await _context.Children
            .Where(c => c.FamilyId == familyId)
            .ToListAsync();

        if (!children.Any())
            return new FamilyFinancialHealth(0, 0, 0, 0, HealthRating.Poor, "No children yet", new(), new());

        // Savings Score (0-100)
        var avgBalance = children.Average(c => c.CurrentBalance);
        var avgSavingsGoals = children.Average(c => c.TotalGoalSavings);
        var savingsScore = Math.Min(100, (int)((avgBalance + avgSavingsGoals) / 5));

        // Activity Score (0-100)
        var monthStart = DateTime.UtcNow.AddMonths(-1);
        var childIds = children.Select(c => c.Id).ToList();
        var monthTransactions = await _context.Transactions
            .CountAsync(t => childIds.Contains(t.ChildId) && t.CreatedAt >= monthStart);
        var activityScore = Math.Min(100, monthTransactions * 2);

        // Goal Score (0-100)
        var avgGoalsCompleted = children.Average(c => c.CompletedGoalsCount);
        var activeGoals = await _context.WishListItems
            .CountAsync(g => childIds.Contains(g.ChildId) && g.IsActive);
        var goalScore = Math.Min(100, (int)((avgGoalsCompleted * 20) + (activeGoals * 10)));

        // Overall Score
        var overallScore = (savingsScore + activityScore + goalScore) / 3;

        var rating = overallScore switch
        {
            >= 80 => HealthRating.Excellent,
            >= 60 => HealthRating.Good,
            >= 40 => HealthRating.Fair,
            _ => HealthRating.Poor
        };

        var strengths = new List<string>();
        var recommendations = new List<string>();

        if (savingsScore >= 70)
            strengths.Add("Strong savings habits across the family");
        else
            recommendations.Add("Encourage more regular savings");

        if (activityScore >= 70)
            strengths.Add("Active financial engagement");
        else
            recommendations.Add("Increase transaction frequency");

        if (goalScore >= 70)
            strengths.Add("Excellent goal-setting and achievement");
        else
            recommendations.Add("Set more savings goals");

        var summary = rating switch
        {
            HealthRating.Excellent => "Your family is doing fantastic with financial learning!",
            HealthRating.Good => "Great progress! Keep up the good work.",
            HealthRating.Fair => "You're on the right track. Room for improvement.",
            _ => "Let's work together to build better financial habits."
        };

        return new FamilyFinancialHealth(
            overallScore,
            savingsScore,
            activityScore,
            goalScore,
            rating,
            summary,
            strengths,
            recommendations);
    }

    public async Task<byte[]> ExportFamilyReportAsync(Guid familyId, DateTime startDate, DateTime endDate)
    {
        // Generate CSV report
        var overview = await GetFamilyOverviewAsync(familyId);
        var comparisons = await GetChildComparisonsAsync(familyId);
        var activities = await GetActivityFeedAsync(familyId, (endDate - startDate).Days);

        var csv = new StringBuilder();
        csv.AppendLine("Allowance Tracker - Family Report");
        csv.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        csv.AppendLine();

        csv.AppendLine("Family Overview");
        csv.AppendLine($"Total Children,{overview.TotalChildren}");
        csv.AppendLine($"Total Balance,{overview.TotalBalance:C}");
        csv.AppendLine($"Total Savings Goals,{overview.TotalSavingsGoals:C}");
        csv.AppendLine($"Weekly Allowance,{overview.TotalAllowancePerWeek:C}");
        csv.AppendLine();

        csv.AppendLine("Child Comparisons");
        csv.AppendLine("Name,Balance,Savings Rate,Goals,Chores,Streak,Achievements");
        foreach (var child in comparisons)
        {
            csv.AppendLine($"{child.Name},{child.Balance:C},{child.SavingsRate}%," +
                          $"{child.GoalsCompleted},{child.ChoresCompleted}," +
                          $"{child.CurrentStreak},{child.AchievementsUnlocked}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }
}
```

### 1.4 Test Cases (20 Tests)

```csharp
public class FamilyDashboardServiceTests
{
    [Fact]
    public async Task GetFamilyOverview_MultipleChildren_AggregatesCorrectly()
    {
        // Arrange
        var family = await CreateFamily();
        var child1 = await CreateChild(family.Id, balance: 100m, weeklyAllowance: 10m);
        var child2 = await CreateChild(family.Id, balance: 150m, weeklyAllowance: 15m);

        // Act
        var overview = await _dashboardService.GetFamilyOverviewAsync(family.Id);

        // Assert
        overview.TotalChildren.Should().Be(2);
        overview.TotalBalance.Should().Be(250m);
        overview.TotalAllowancePerWeek.Should().Be(25m);
    }

    [Fact]
    public async Task GetChildComparisons_OrdersByBalance()
    {
        // Arrange
        var family = await CreateFamily();
        var child1 = await CreateChild(family.Id, balance: 50m, firstName: "Alice");
        var child2 = await CreateChild(family.Id, balance: 150m, firstName: "Bob");
        var child3 = await CreateChild(family.Id, balance: 100m, firstName: "Charlie");

        // Act
        var comparisons = await _dashboardService.GetChildComparisonsAsync(family.Id);

        // Assert
        comparisons.Should().HaveCount(3);
        comparisons[0].Name.Should().Be("Bob"); // Highest balance
        comparisons[1].Name.Should().Be("Charlie");
        comparisons[2].Name.Should().Be("Alice");
    }

    [Fact]
    public async Task GetPendingActions_CountsCorrectly()
    {
        // Arrange
        var family = await CreateFamily();
        var child = await CreateChild(family.Id);

        await CreateTransactionRequest(child.Id, RequestStatus.Pending);
        await CreateTransactionRequest(child.Id, RequestStatus.Pending);
        await CreateChore(child.Id, ChoreStatus.Completed);

        // Act
        var pending = await _dashboardService.GetPendingActionsAsync(family.Id);

        // Assert
        pending.PendingRequests.Should().Be(2);
        pending.PendingChores.Should().Be(1);
    }

    [Fact]
    public async Task GetActivityFeed_IncludesAllActivityTypes()
    {
        // Arrange
        var family = await CreateFamily();
        var child = await CreateChild(family.Id);

        await CreateTransaction(child.Id, 20m);
        await CreateCompletedGoal(child.Id);
        await CreateApprovedChore(child.Id);
        await UnlockAchievement(child.Id, "FIRST_TRANSACTION");

        // Act
        var activities = await _dashboardService.GetActivityFeedAsync(family.Id);

        // Assert
        activities.Should().HaveCount(4);
        activities.Should().Contain(a => a.Type == ActivityType.Transaction);
        activities.Should().Contain(a => a.Type == ActivityType.SavingsGoal);
        activities.Should().Contain(a => a.Type == ActivityType.ChoreCompleted);
        activities.Should().Contain(a => a.Type == ActivityType.Achievement);
    }

    [Fact]
    public async Task CalculateFinancialHealth_ExcellentRating()
    {
        // Arrange
        var family = await CreateFamily();
        var child = await CreateChild(family.Id, balance: 500m);
        await CreateMultipleTransactions(child.Id, 50); // High activity
        await CreateMultipleCompletedGoals(child.Id, 10); // Many goals

        // Act
        var health = await _dashboardService.CalculateFinancialHealthAsync(family.Id);

        // Assert
        health.OverallScore.Should().BeGreaterThan(80);
        health.Rating.Should().Be(HealthRating.Excellent);
        health.Strengths.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CalculateFinancialHealth_PoorRating()
    {
        // Arrange
        var family = await CreateFamily();
        var child = await CreateChild(family.Id, balance: 5m);
        // No activity, no goals

        // Act
        var health = await _dashboardService.CalculateFinancialHealthAsync(family.Id);

        // Assert
        health.OverallScore.Should().BeLessThan(40);
        health.Rating.Should().Be(HealthRating.Poor);
        health.Recommendations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportFamilyReport_GeneratesValidCSV()
    {
        // Arrange
        var family = await CreateFamily();
        var child = await CreateChild(family.Id, balance: 100m);
        var startDate = DateTime.UtcNow.AddMonths(-1);
        var endDate = DateTime.UtcNow;

        // Act
        var csvBytes = await _dashboardService.ExportFamilyReportAsync(family.Id, startDate, endDate);

        // Assert
        csvBytes.Should().NotBeNull();
        csvBytes.Length.Should().BeGreaterThan(0);

        var csvContent = Encoding.UTF8.GetString(csvBytes);
        csvContent.Should().Contain("Family Report");
        csvContent.Should().Contain("Total Balance");
        csvContent.Should().Contain(child.FirstName);
    }

    // Remaining 13 tests cover:
    // - Empty family handling
    // - Activity feed pagination
    // - Health score edge cases
    // - Pending actions with different statuses
    // - Comparison calculations
}
```

---

## Phase 2: Blazor UI Components

### 2.1 FamilyDashboard Page

```razor
@page "/family-dashboard"
@inject IFamilyDashboardService DashboardService
@attribute [Authorize(Roles = "Parent")]

<div class="family-dashboard">
    <div class="d-flex justify-content-between align-items-center mb-4">
        <h2>Family Dashboard</h2>
        <button class="btn btn-primary" @onclick="ExportReport">
            Export Report
        </button>
    </div>

    @if (Overview != null)
    {
        <!-- Overview Stats -->
        <div class="row mb-4">
            <div class="col-md-3">
                <StatCard Title="Total Balance"
                         Value="@Overview.TotalBalance.ToString("C")"
                         Icon="💰"
                         Color="success" />
            </div>
            <div class="col-md-3">
                <StatCard Title="Savings Goals"
                         Value="@Overview.TotalSavingsGoals.ToString("C")"
                         Icon="🎯"
                         Color="info" />
            </div>
            <div class="col-md-3">
                <StatCard Title="Weekly Allowance"
                         Value="@Overview.TotalAllowancePerWeek.ToString("C")"
                         Icon="📅"
                         Color="primary" />
            </div>
            <div class="col-md-3">
                <StatCard Title="Monthly Spending"
                         Value="@Overview.TotalSpentThisMonth.ToString("C")"
                         Icon="💸"
                         Color="warning" />
            </div>
        </div>

        <!-- Financial Health -->
        @if (Health != null)
        {
            <div class="card mb-4">
                <div class="card-header">
                    <h4>Family Financial Health</h4>
                </div>
                <div class="card-body">
                    <FinancialHealthWidget Health="@Health" />
                </div>
            </div>
        }

        <!-- Pending Actions -->
        @if (Pending != null && (Pending.PendingRequests > 0 || Pending.PendingChores > 0))
        {
            <div class="card mb-4 border-warning">
                <div class="card-header bg-warning">
                    <h4>⚠️ Pending Your Review</h4>
                </div>
                <div class="card-body">
                    <PendingActionsWidget Pending="@Pending" OnActionTaken="@RefreshDashboard" />
                </div>
            </div>
        }

        <!-- Child Comparisons -->
        @if (Comparisons.Any())
        {
            <div class="card mb-4">
                <div class="card-header">
                    <h4>Child Comparisons</h4>
                </div>
                <div class="card-body">
                    <ChildComparisonChart Comparisons="@Comparisons" />
                </div>
            </div>
        }

        <!-- Activity Feed -->
        <div class="card">
            <div class="card-header">
                <h4>Recent Activity</h4>
            </div>
            <div class="card-body">
                <ActivityFeed Activities="@Activities" />
            </div>
        </div>
    }
</div>

@code {
    [CascadingParameter] private Task<AuthenticationState> AuthStateTask { get; set; } = null!;

    private FamilyOverview? Overview;
    private FamilyFinancialHealth? Health;
    private PendingActions? Pending;
    private List<ChildComparison> Comparisons = new();
    private List<FamilyActivity> Activities = new();
    private Guid FamilyId;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateTask;
        var user = authState.User;
        // Get family ID from user

        await LoadDashboard();
    }

    private async Task LoadDashboard()
    {
        Overview = await DashboardService.GetFamilyOverviewAsync(FamilyId);
        Health = await DashboardService.CalculateFinancialHealthAsync(FamilyId);
        Pending = await DashboardService.GetPendingActionsAsync(FamilyId);
        Comparisons = await DashboardService.GetChildComparisonsAsync(FamilyId);
        Activities = await DashboardService.GetActivityFeedAsync(FamilyId);
    }

    private async Task RefreshDashboard()
    {
        await LoadDashboard();
        StateHasChanged();
    }

    private async Task ExportReport()
    {
        var startDate = DateTime.UtcNow.AddMonths(-1);
        var endDate = DateTime.UtcNow;

        var reportBytes = await DashboardService.ExportFamilyReportAsync(
            FamilyId, startDate, endDate);

        // Trigger download
        // Implementation depends on JS interop
    }
}
```

### 2.2 FinancialHealthWidget Component

```razor
<div class="financial-health">
    <div class="row">
        <div class="col-md-8">
            <div class="health-rating mb-3">
                <h3 class="@GetRatingClass()">@Health.Rating.ToString()</h3>
                <p class="lead">@Health.Summary</p>
            </div>

            <div class="health-scores">
                <div class="score-item">
                    <label>Overall Score</label>
                    <div class="progress">
                        <div class="progress-bar @GetScoreColor(Health.OverallScore)"
                             style="width: @Health.OverallScore%">
                            @Health.OverallScore
                        </div>
                    </div>
                </div>

                <div class="score-item">
                    <label>Savings</label>
                    <div class="progress">
                        <div class="progress-bar bg-success"
                             style="width: @Health.SavingsScore%">
                            @Health.SavingsScore
                        </div>
                    </div>
                </div>

                <div class="score-item">
                    <label>Activity</label>
                    <div class="progress">
                        <div class="progress-bar bg-info"
                             style="width: @Health.ActivityScore%">
                            @Health.ActivityScore
                        </div>
                    </div>
                </div>

                <div class="score-item">
                    <label>Goals</label>
                    <div class="progress">
                        <div class="progress-bar bg-warning"
                             style="width: @Health.GoalScore%">
                            @Health.GoalScore
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div class="col-md-4">
            @if (Health.Strengths.Any())
            {
                <div class="strengths mb-3">
                    <h6>✅ Strengths</h6>
                    <ul>
                        @foreach (var strength in Health.Strengths)
                        {
                            <li>@strength</li>
                        }
                    </ul>
                </div>
            }

            @if (Health.Recommendations.Any())
            {
                <div class="recommendations">
                    <h6>💡 Recommendations</h6>
                    <ul>
                        @foreach (var rec in Health.Recommendations)
                        {
                            <li>@rec</li>
                        }
                    </ul>
                </div>
            }
        </div>
    </div>
</div>

@code {
    [Parameter] public FamilyFinancialHealth Health { get; set; } = null!;

    private string GetRatingClass()
    {
        return Health.Rating switch
        {
            HealthRating.Excellent => "text-success",
            HealthRating.Good => "text-info",
            HealthRating.Fair => "text-warning",
            _ => "text-danger"
        };
    }

    private string GetScoreColor(int score)
    {
        return score switch
        {
            >= 80 => "bg-success",
            >= 60 => "bg-info",
            >= 40 => "bg-warning",
            _ => "bg-danger"
        };
    }
}
```

### 2.3 ActivityFeed Component

```razor
<div class="activity-feed">
    @if (Activities.Any())
    {
        <div class="timeline">
            @foreach (var activity in Activities)
            {
                <div class="timeline-item">
                    <div class="timeline-icon">@activity.Icon</div>
                    <div class="timeline-content">
                        <div class="timeline-header">
                            <strong>@activity.ChildName</strong>
                            <small class="text-muted">@activity.Timestamp.ToString("MMM dd, h:mm tt")</small>
                        </div>
                        <div class="timeline-body">
                            @activity.Description
                            @if (activity.Amount.HasValue)
                            {
                                <span class="badge bg-primary ms-2">
                                    @activity.Amount.Value.ToString("C")
                                </span>
                            }
                        </div>
                    </div>
                </div>
            }
        </div>
    }
    else
    {
        <p class="text-muted">No recent activity</p>
    }
</div>

@code {
    [Parameter] public List<FamilyActivity> Activities { get; set; } = new();
}
```

---

## Success Metrics

- All 20 tests passing
- Family overview aggregates correctly
- Child comparisons calculate accurately
- Pending actions display correctly
- Activity feed shows all event types
- Financial health score calculated
- Export report generates valid CSV
- Real-time updates working

---

## Future Enhancements

1. **Custom Date Ranges**: Filter dashboard by date range
2. **Goal Alerts**: Notify when children close to goals
3. **Spending Insights**: AI-powered spending analysis
4. **Budget Alerts**: Notify when over budget
5. **Mobile App Integration**: Push notifications
6. **Family Chat**: In-app messaging about finances
7. **Scheduled Reports**: Email weekly summaries

---

**Implementation Time**: 2-3 weeks following TDD methodology
