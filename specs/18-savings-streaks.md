# Savings Streaks Specification

## Overview

This specification tracks consecutive weeks of positive net savings (more deposits than withdrawals), rewarding children for consistent savings habits with streak bonuses and visual motivation.

## Goals

1. **Streak Tracking**: Count consecutive weeks of positive savings
2. **Visual Motivation**: Fire emoji streak counter and progress bars
3. **Streak Bonuses**: Reward milestones (10, 20, 50 weeks) with bonus payments
4. **Broken Streak Alerts**: Notify when streak is at risk or broken
5. **Achievement Integration**: Trigger streak achievements
6. **TDD Approach**: 15 comprehensive tests

---

## Phase 1: Update Child Model

```csharp
namespace AllowanceTracker.Models;

public class Child
{
    // ... existing properties ...

    /// <summary>
    /// Current consecutive weeks of positive savings
    /// </summary>
    public int CurrentStreak { get; set; } = 0;

    /// <summary>
    /// Longest streak ever achieved
    /// </summary>
    public int LongestStreak { get; set; } = 0;

    /// <summary>
    /// Last date streak was calculated/updated
    /// </summary>
    public DateTime? LastStreakDate { get; set; }

    /// <summary>
    /// Total bonus earned from streaks (lifetime)
    /// </summary>
    public decimal TotalStreakBonuses { get; set; } = 0;
}
```

### Database Migration

```bash
dotnet ef migrations add AddSavingsStreaks
```

---

## Phase 2: Service Layer

### 2.1 IStreakService Interface

```csharp
namespace AllowanceTracker.Services;

public interface IStreakService
{
    Task<StreakInfo> GetStreakInfoAsync(Guid childId);
    Task<bool> UpdateWeeklyStreakAsync(Guid childId);
    Task ProcessAllStreaksAsync(); // Background job
    Task<StreakStatistics> GetStreakStatisticsAsync(Guid childId);
    decimal CalculateStreakBonus(int streakWeeks);
}
```

### 2.2 DTOs

```csharp
public record StreakInfo(
    int CurrentStreak,
    int LongestStreak,
    DateTime? LastUpdated,
    decimal TotalBonuses,
    bool IsActive,
    int DaysUntilRisk,
    decimal NextBonusAt,
    string StreakEmoji);

public record StreakStatistics(
    int CurrentStreak,
    int LongestStreak,
    int TotalWeeksTracked,
    decimal TotalBonusesEarned,
    int TimesStreakBroken,
    double AverageStreakLength);
```

### 2.3 StreakService Implementation

```csharp
public class StreakService : IStreakService
{
    private readonly AllowanceContext _context;
    private readonly ITransactionService _transactionService;
    private readonly IAchievementService _achievementService;
    private readonly IHubContext<FamilyHub> _hubContext;

    public async Task<bool> UpdateWeeklyStreakAsync(Guid childId)
    {
        var child = await _context.Children.FindAsync(childId)
            ?? throw new NotFoundException("Child not found");

        var now = DateTime.UtcNow;
        var weekStart = now.AddDays(-7).Date;

        // Calculate net savings for the week
        var weeklyTransactions = await _context.Transactions
            .Where(t => t.ChildId == childId && t.CreatedAt >= weekStart)
            .ToListAsync();

        var deposits = weeklyTransactions
            .Where(t => t.Type == TransactionType.Credit)
            .Sum(t => t.Amount);

        var withdrawals = weeklyTransactions
            .Where(t => t.Type == TransactionType.Debit)
            .Sum(t => t.Amount);

        var netSavings = deposits - withdrawals;

        // Check savings goals deposits
        var goalDeposits = await _context.SavingsGoalTransactions
            .Where(t => t.ChildId == childId &&
                       t.CreatedAt >= weekStart &&
                       t.Type == SavingsTransactionType.Deposit)
            .SumAsync(t => t.Amount);

        netSavings += goalDeposits;

        if (netSavings > 0)
        {
            // Positive savings - increase streak
            child.CurrentStreak++;

            if (child.CurrentStreak > child.LongestStreak)
            {
                child.LongestStreak = child.CurrentStreak;
            }

            // Check for milestone bonuses
            await CheckStreakMilestoneAsync(child);

            // Trigger achievements
            await _achievementService.OnStreakUpdatedAsync(childId, child.CurrentStreak);

            child.LastStreakDate = now;
            await _context.SaveChangesAsync();

            // Notify
            await _hubContext.Clients
                .Group($"family-{child.FamilyId}")
                .SendAsync("StreakUpdated", childId, child.CurrentStreak);

            return true;
        }
        else
        {
            // No savings or negative - check if streak should break
            if (child.LastStreakDate.HasValue &&
                (now - child.LastStreakDate.Value).Days > 14) // 2 weeks grace
            {
                var oldStreak = child.CurrentStreak;
                child.CurrentStreak = 0;
                child.LastStreakDate = now;

                await _context.SaveChangesAsync();

                // Notify streak broken
                await _hubContext.Clients
                    .Group($"family-{child.FamilyId}")
                    .SendAsync("StreakBroken", childId, oldStreak);

                return false;
            }
        }

        return false;
    }

    private async Task CheckStreakMilestoneAsync(Child child)
    {
        var milestones = new[] { 10, 20, 50, 100 };
        var bonusPercentages = new Dictionary<int, int>
        {
            { 10, 10 },  // 10% bonus
            { 20, 15 },  // 15% bonus
            { 50, 25 },  // 25% bonus
            { 100, 50 }  // 50% bonus!
        };

        foreach (var milestone in milestones)
        {
            if (child.CurrentStreak == milestone)
            {
                var bonusPercent = bonusPercentages[milestone];
                var bonusAmount = (child.CurrentBalance * bonusPercent) / 100;

                // Create bonus transaction
                var dto = new CreateTransactionDto(
                    child.Id,
                    bonusAmount,
                    TransactionType.Credit,
                    TransactionCategory.BonusReward,
                    $"{milestone}-week streak bonus! ({bonusPercent}%)");

                await _transactionService.CreateTransactionAsync(dto);

                child.TotalStreakBonuses += bonusAmount;

                // Send celebration notification
                await _hubContext.Clients
                    .Group($"family-{child.FamilyId}")
                    .SendAsync("StreakMilestone", child.Id, milestone, bonusAmount);

                break;
            }
        }
    }

    public decimal CalculateStreakBonus(int streakWeeks)
    {
        return streakWeeks switch
        {
            >= 100 => 0.50m, // 50%
            >= 50 => 0.25m,  // 25%
            >= 20 => 0.15m,  // 15%
            >= 10 => 0.10m,  // 10%
            _ => 0m
        };
    }

    public async Task<StreakInfo> GetStreakInfoAsync(Guid childId)
    {
        var child = await _context.Children.FindAsync(childId)
            ?? throw new NotFoundException("Child not found");

        var daysSinceUpdate = child.LastStreakDate.HasValue
            ? (DateTime.UtcNow - child.LastStreakDate.Value).Days
            : 0;

        var daysUntilRisk = Math.Max(0, 14 - daysSinceUpdate);
        var isActive = daysUntilRisk > 0;

        var nextMilestone = new[] { 10, 20, 50, 100 }
            .FirstOrDefault(m => m > child.CurrentStreak);

        return new StreakInfo(
            child.CurrentStreak,
            child.LongestStreak,
            child.LastStreakDate,
            child.TotalStreakBonuses,
            isActive,
            daysUntilRisk,
            nextMilestone,
            GetStreakEmoji(child.CurrentStreak));
    }

    private string GetStreakEmoji(int streak)
    {
        return streak switch
        {
            >= 50 => "🔥🔥🔥",
            >= 20 => "🔥🔥",
            >= 10 => "🔥",
            >= 3 => "🌟",
            >= 1 => "✨",
            _ => "💤"
        };
    }

    public async Task ProcessAllStreaksAsync()
    {
        var children = await _context.Children
            .Where(c => c.IsActive)
            .ToListAsync();

        foreach (var child in children)
        {
            try
            {
                await UpdateWeeklyStreakAsync(child.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating streak for child {ChildId}", child.Id);
            }
        }
    }
}
```

### 2.4 Test Cases (15 Tests)

```csharp
public class StreakServiceTests
{
    [Fact]
    public async Task UpdateStreak_PositiveSavings_IncreasesStreak()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        await CreateCreditTransaction(child.Id, 20m); // Deposit

        // Act
        var updated = await _streakService.UpdateWeeklyStreakAsync(child.Id);

        // Assert
        updated.Should().BeTrue();
        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentStreak.Should().Be(1);
        updatedChild.LongestStreak.Should().Be(1);
    }

    [Fact]
    public async Task UpdateStreak_NegativeSavings_DoesNotIncreaseStreak()
    {
        // Arrange
        var child = await CreateChild(balance: 100m, currentStreak: 5);
        await CreateDebitTransaction(child.Id, 50m); // More spending than earning

        // Act
        var updated = await _streakService.UpdateWeeklyStreakAsync(child.Id);

        // Assert
        updated.Should().BeFalse();
        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentStreak.Should().Be(5); // Unchanged
    }

    [Fact]
    public async Task UpdateStreak_ReachesMilestone_PaysBonusAndUpdatesLongest()
    {
        // Arrange
        var child = await CreateChild(balance: 100m, currentStreak: 9);
        await CreateCreditTransaction(child.Id, 10m);

        // Act
        await _streakService.UpdateWeeklyStreakAsync(child.Id);

        // Assert
        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentStreak.Should().Be(10);
        updatedChild.LongestStreak.Should().Be(10);
        updatedChild.TotalStreakBonuses.Should().BeGreaterThan(0);

        // Check bonus transaction created
        var bonus = await _context.Transactions
            .FirstOrDefaultAsync(t => t.ChildId == child.Id &&
                                     t.Category == TransactionCategory.BonusReward);
        bonus.Should().NotBeNull();
        bonus!.Description.Should().Contain("10-week streak");
    }

    [Fact]
    public async Task UpdateStreak_NoActivityFor2Weeks_BreaksStreak()
    {
        // Arrange
        var child = await CreateChild(balance: 100m, currentStreak: 15);
        child.LastStreakDate = DateTime.UtcNow.AddDays(-15); // 15 days ago
        await _context.SaveChangesAsync();

        // Act
        await _streakService.UpdateWeeklyStreakAsync(child.Id);

        // Assert
        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentStreak.Should().Be(0);
        updatedChild.LongestStreak.Should().Be(15); // Preserved
    }

    [Fact]
    public async Task UpdateStreak_IncludesSavingsGoalDeposits()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var goal = await CreateSavingsGoal(child.Id, targetAmount: 100m);
        await _savingsGoalService.DepositToGoalAsync(goal.Id, 25m, "Save", _userId);

        // Act
        var updated = await _streakService.UpdateWeeklyStreakAsync(child.Id);

        // Assert
        updated.Should().BeTrue();
        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentStreak.Should().Be(1);
    }

    [Fact]
    public async Task GetStreakInfo_ReturnsCorrectDaysUntilRisk()
    {
        // Arrange
        var child = await CreateChild(currentStreak: 10);
        child.LastStreakDate = DateTime.UtcNow.AddDays(-10);
        await _context.SaveChangesAsync();

        // Act
        var info = await _streakService.GetStreakInfoAsync(child.Id);

        // Assert
        info.DaysUntilRisk.Should().Be(4); // 14 - 10 = 4 days left
        info.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetStreakInfo_ReturnsCorrectEmoji()
    {
        // Arrange
        var child = await CreateChild(currentStreak: 25);

        // Act
        var info = await _streakService.GetStreakInfoAsync(child.Id);

        // Assert
        info.StreakEmoji.Should().Be("🔥🔥"); // 20+ weeks
    }

    [Fact]
    public async Task CalculateStreakBonus_ReturnsCorrectPercentage()
    {
        // Act & Assert
        _streakService.CalculateStreakBonus(5).Should().Be(0m);
        _streakService.CalculateStreakBonus(10).Should().Be(0.10m);
        _streakService.CalculateStreakBonus(20).Should().Be(0.15m);
        _streakService.CalculateStreakBonus(50).Should().Be(0.25m);
        _streakService.CalculateStreakBonus(100).Should().Be(0.50m);
    }

    // Remaining 7 tests cover:
    // - Statistics calculation
    // - Milestone bonuses for each level
    // - Achievement triggers
    // - SignalR notifications
}
```

---

## Phase 3: Blazor UI

### StreakWidget Component

```razor
@inject IStreakService StreakService

<div class="streak-widget">
    @if (StreakInfo != null)
    {
        <div class="streak-display">
            <div class="streak-icon">@StreakInfo.StreakEmoji</div>
            <div class="streak-count">
                <h2>@StreakInfo.CurrentStreak</h2>
                <p>Week Streak</p>
            </div>
        </div>

        @if (StreakInfo.IsActive)
        {
            <div class="progress mb-2">
                <div class="progress-bar bg-success"
                     style="width: @((14 - StreakInfo.DaysUntilRisk) * 100 / 14)%">
                </div>
            </div>
            <small class="text-muted">
                @StreakInfo.DaysUntilRisk days until streak at risk
            </small>
        }
        else
        {
            <div class="alert alert-warning">
                Streak at risk! Save money this week to keep it going.
            </div>
        }

        <div class="streak-stats mt-3">
            <div><strong>Longest:</strong> @StreakInfo.LongestStreak weeks</div>
            <div><strong>Bonuses:</strong> @StreakInfo.TotalBonuses.ToString("C")</div>
            @if (StreakInfo.NextBonusAt > 0)
            {
                <div><strong>Next bonus:</strong> @StreakInfo.NextBonusAt weeks</div>
            }
        </div>
    }
</div>

@code {
    [Parameter] public Guid ChildId { get; set; }

    private StreakInfo? StreakInfo;

    protected override async Task OnInitializedAsync()
    {
        StreakInfo = await StreakService.GetStreakInfoAsync(ChildId);
    }
}
```

---

## Success Metrics

- All 15 tests passing
- Streak tracking accurate
- Bonuses paid at milestones
- Visual feedback clear and motivating
- Achievements triggered
- Broken streak notifications sent

---

**Implementation Time**: 1-2 weeks following TDD
