using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AllowanceTracker.Tests.Services;

public class SavingsGoalServiceTests : IDisposable
{
    private readonly AllowanceContext _context;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly ISavingsGoalService _savingsGoalService;
    private readonly Guid _currentUserId;
    private readonly Guid _parentUserId;

    public SavingsGoalServiceTests()
    {
        var options = new DbContextOptionsBuilder<AllowanceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new AllowanceContext(options);

        _currentUserId = Guid.NewGuid();
        _parentUserId = Guid.NewGuid();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockCurrentUser.Setup(x => x.UserId).Returns(_currentUserId);

        _savingsGoalService = new SavingsGoalService(_context, _mockCurrentUser.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Helper Methods

    private async Task<(Child child, ApplicationUser parentUser)> CreateTestChild(
        decimal balance = 100m,
        decimal weeklyAllowance = 10m)
    {
        var family = new Family { Id = Guid.NewGuid(), Name = "Test Family" };
        _context.Families.Add(family);

        var parentUser = new ApplicationUser
        {
            Id = _parentUserId,
            Email = "parent@test.com",
            UserName = "parent@test.com",
            FirstName = "Test",
            LastName = "Parent",
            Role = UserRole.Parent,
            FamilyId = family.Id
        };
        _context.Users.Add(parentUser);

        var childUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "child@test.com",
            UserName = "child@test.com",
            FirstName = "Test",
            LastName = "Child",
            Role = UserRole.Child,
            FamilyId = family.Id
        };
        _context.Users.Add(childUser);

        var child = new Child
        {
            Id = Guid.NewGuid(),
            UserId = childUser.Id,
            FamilyId = family.Id,
            CurrentBalance = balance,
            WeeklyAllowance = weeklyAllowance
        };
        _context.Children.Add(child);

        await _context.SaveChangesAsync();

        // Reload to get navigation properties
        await _context.Entry(child).Reference(c => c.User).LoadAsync();

        return (child, parentUser);
    }

    private async Task<SavingsGoal> CreateTestGoal(
        Guid childId,
        decimal targetAmount = 100m,
        decimal currentAmount = 0m,
        GoalStatus status = GoalStatus.Active,
        bool createMilestones = true)
    {
        var goal = new SavingsGoal
        {
            Id = Guid.NewGuid(),
            ChildId = childId,
            Name = "Test Goal",
            Description = "A test savings goal",
            TargetAmount = targetAmount,
            CurrentAmount = currentAmount,
            Category = GoalCategory.Toy,
            Status = status,
            Priority = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.SavingsGoals.Add(goal);

        if (createMilestones)
        {
            var milestonePercentages = new[] { 25, 50, 75, 100 };
            foreach (var percent in milestonePercentages)
            {
                var milestone = new GoalMilestone
                {
                    Id = Guid.NewGuid(),
                    GoalId = goal.Id,
                    PercentComplete = percent,
                    TargetAmount = targetAmount * percent / 100,
                    IsAchieved = false,
                    CelebrationMessage = $"You've reached {percent}%!"
                };
                _context.GoalMilestones.Add(milestone);
            }
        }

        await _context.SaveChangesAsync();
        return goal;
    }

    #endregion

    #region Goal CRUD Tests

    [Fact]
    public async Task CreateGoalAsync_WithValidData_CreatesGoalAndMilestones()
    {
        // Arrange
        var (child, _) = await CreateTestChild();
        var dto = new CreateSavingsGoalDto(
            ChildId: child.Id,
            Name: "New Bike",
            Description: "A cool new bike",
            TargetAmount: 200m,
            ImageUrl: null,
            ProductUrl: null,
            Category: GoalCategory.Toy,
            TargetDate: DateTime.UtcNow.AddMonths(3),
            Priority: 1,
            AutoTransferAmount: 0,
            AutoTransferType: AutoTransferType.None
        );

        // Act
        var result = await _savingsGoalService.CreateGoalAsync(dto, _parentUserId);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Bike");
        result.TargetAmount.Should().Be(200m);
        result.CurrentAmount.Should().Be(0m);
        result.Status.Should().Be(GoalStatus.Active);
        result.Milestones.Should().HaveCount(4);
        result.Milestones.Select(m => m.PercentComplete).Should().BeEquivalentTo(new[] { 25, 50, 75, 100 });
    }

    [Fact]
    public async Task CreateGoalAsync_WithAutoTransfer_SetsCorrectConfig()
    {
        // Arrange
        var (child, _) = await CreateTestChild();
        var dto = new CreateSavingsGoalDto(
            ChildId: child.Id,
            Name: "Savings Goal",
            Description: null,
            TargetAmount: 100m,
            ImageUrl: null,
            ProductUrl: null,
            Category: GoalCategory.Savings,
            TargetDate: null,
            Priority: 1,
            AutoTransferAmount: 5m,
            AutoTransferType: AutoTransferType.FixedAmount
        );

        // Act
        var result = await _savingsGoalService.CreateGoalAsync(dto, _parentUserId);

        // Assert
        result.AutoTransferAmount.Should().Be(5m);
        result.AutoTransferType.Should().Be(AutoTransferType.FixedAmount);
    }

    [Fact]
    public async Task GetGoalByIdAsync_ReturnsGoalWithDetails()
    {
        // Arrange
        var (child, _) = await CreateTestChild();
        var goal = await CreateTestGoal(child.Id, targetAmount: 100m, currentAmount: 25m);

        // Act
        var result = await _savingsGoalService.GetGoalByIdAsync(goal.Id, _parentUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(goal.Id);
        result.Name.Should().Be("Test Goal");
        result.ProgressPercentage.Should().Be(25);
        result.Milestones.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetGoalByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _savingsGoalService.GetGoalByIdAsync(nonExistentId, _parentUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetChildGoalsAsync_ReturnsAllActiveGoals()
    {
        // Arrange
        var (child, _) = await CreateTestChild();
        await CreateTestGoal(child.Id, targetAmount: 100m);
        await CreateTestGoal(child.Id, targetAmount: 200m);
        await CreateTestGoal(child.Id, targetAmount: 50m, status: GoalStatus.Completed);

        // Act
        var result = await _savingsGoalService.GetChildGoalsAsync(child.Id, null, includeCompleted: false, _parentUserId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetChildGoalsAsync_IncludesCompletedGoals_WhenRequested()
    {
        // Arrange
        var (child, _) = await CreateTestChild();
        await CreateTestGoal(child.Id, targetAmount: 100m);
        await CreateTestGoal(child.Id, targetAmount: 50m, status: GoalStatus.Completed);

        // Act
        var result = await _savingsGoalService.GetChildGoalsAsync(child.Id, null, includeCompleted: true, _parentUserId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateGoalAsync_UpdatesGoalDetails()
    {
        // Arrange
        var (child, _) = await CreateTestChild();
        var goal = await CreateTestGoal(child.Id);
        var updateDto = new UpdateSavingsGoalDto(
            Name: "Updated Name",
            Description: "Updated description",
            TargetAmount: 150m,
            ImageUrl: null,
            ProductUrl: null,
            Category: GoalCategory.Electronics,
            TargetDate: null,
            Priority: 2,
            AutoTransferAmount: null,
            AutoTransferType: null
        );

        // Act
        var result = await _savingsGoalService.UpdateGoalAsync(goal.Id, updateDto, _parentUserId);

        // Assert
        result.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated description");
        result.TargetAmount.Should().Be(150m);
        result.Category.Should().Be(GoalCategory.Electronics);
        result.Priority.Should().Be(2);
    }

    [Fact]
    public async Task CancelGoalAsync_ReturnsFundsToBalance()
    {
        // Arrange
        var (child, _) = await CreateTestChild(balance: 50m);
        var goal = await CreateTestGoal(child.Id, targetAmount: 100m, currentAmount: 25m);

        // Act
        await _savingsGoalService.CancelGoalAsync(goal.Id, _parentUserId);

        // Assert
        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentBalance.Should().Be(75m); // 50 + 25 returned

        var updatedGoal = await _context.SavingsGoals.FindAsync(goal.Id);
        updatedGoal!.Status.Should().Be(GoalStatus.Cancelled);
    }

    [Fact]
    public async Task PauseGoalAsync_SetsStatusToPaused()
    {
        // Arrange
        var (child, _) = await CreateTestChild();
        var goal = await CreateTestGoal(child.Id);

        // Act
        var result = await _savingsGoalService.PauseGoalAsync(goal.Id, _parentUserId);

        // Assert
        result.Status.Should().Be(GoalStatus.Paused);
    }

    [Fact]
    public async Task ResumeGoalAsync_SetsStatusToActive()
    {
        // Arrange
        var (child, _) = await CreateTestChild();
        var goal = await CreateTestGoal(child.Id, status: GoalStatus.Paused);

        // Act
        var result = await _savingsGoalService.ResumeGoalAsync(goal.Id, _parentUserId);

        // Assert
        result.Status.Should().Be(GoalStatus.Active);
    }

    #endregion

    #region Contribution Tests

    [Fact]
    public async Task ContributeAsync_DeductsFromChildBalance()
    {
        // Arrange
        var (child, _) = await CreateTestChild(balance: 100m);
        var goal = await CreateTestGoal(child.Id, targetAmount: 100m);
        var dto = new ContributeToGoalDto(Amount: 25m, Description: "Weekly contribution");

        // Act
        var result = await _savingsGoalService.ContributeAsync(goal.Id, dto, _parentUserId);

        // Assert
        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentBalance.Should().Be(75m); // 100 - 25
    }

    [Fact]
    public async Task ContributeAsync_CreatesContributionRecord()
    {
        // Arrange
        var (child, _) = await CreateTestChild(balance: 100m);
        var goal = await CreateTestGoal(child.Id, targetAmount: 100m);
        var dto = new ContributeToGoalDto(Amount: 25m, Description: "Weekly contribution");

        // Act
        var result = await _savingsGoalService.ContributeAsync(goal.Id, dto, _parentUserId);

        // Assert
        var contributions = await _context.SavingsContributions.Where(c => c.GoalId == goal.Id).ToListAsync();
        contributions.Should().HaveCount(1);
        contributions[0].Amount.Should().Be(25m);
        contributions[0].Type.Should().Be(ContributionType.ChildDeposit);
    }

    [Fact]
    public async Task ContributeAsync_UpdatesGoalCurrentAmount()
    {
        // Arrange
        var (child, _) = await CreateTestChild(balance: 100m);
        var goal = await CreateTestGoal(child.Id, targetAmount: 100m);
        var dto = new ContributeToGoalDto(Amount: 25m, Description: null);

        // Act
        var result = await _savingsGoalService.ContributeAsync(goal.Id, dto, _parentUserId);

        // Assert
        result.NewAmount.Should().Be(25m);

        var updatedGoal = await _context.SavingsGoals.FindAsync(goal.Id);
        updatedGoal!.CurrentAmount.Should().Be(25m);
    }

    [Fact]
    public async Task ContributeAsync_AppliesParentMatching_RatioMatch()
    {
        // Arrange
        var (child, parentUser) = await CreateTestChild(balance: 100m);
        var goal = await CreateTestGoal(child.Id, targetAmount: 100m);

        // Create matching rule: $1 for every $2 saved (ratio = 0.5)
        var matchingRule = new ParentMatchingRule
        {
            Id = Guid.NewGuid(),
            GoalId = goal.Id,
            CreatedByParentId = parentUser.Id,
            Type = MatchingType.RatioMatch,
            MatchRatio = 0.5m,
            MaxMatchAmount = 50m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.ParentMatchingRules.Add(matchingRule);
        await _context.SaveChangesAsync();

        var dto = new ContributeToGoalDto(Amount: 20m, Description: null);

        // Act
        var result = await _savingsGoalService.ContributeAsync(goal.Id, dto, _parentUserId);

        // Assert
        result.MatchAmountAdded.Should().Be(10m); // 20 * 0.5 = 10
        result.NewAmount.Should().Be(30m); // 20 + 10 matched
    }

    [Fact]
    public async Task ContributeAsync_AppliesParentMatching_PercentageMatch()
    {
        // Arrange
        var (child, parentUser) = await CreateTestChild(balance: 100m);
        var goal = await CreateTestGoal(child.Id, targetAmount: 100m);

        // Create matching rule: Match 50% of each deposit
        var matchingRule = new ParentMatchingRule
        {
            Id = Guid.NewGuid(),
            GoalId = goal.Id,
            CreatedByParentId = parentUser.Id,
            Type = MatchingType.PercentageMatch,
            MatchRatio = 50m, // 50%
            MaxMatchAmount = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.ParentMatchingRules.Add(matchingRule);
        await _context.SaveChangesAsync();

        var dto = new ContributeToGoalDto(Amount: 20m, Description: null);

        // Act
        var result = await _savingsGoalService.ContributeAsync(goal.Id, dto, _parentUserId);

        // Assert
        result.MatchAmountAdded.Should().Be(10m); // 20 * 50% = 10
    }

    [Fact]
    public async Task ContributeAsync_DoesNotExceedMatchingCap()
    {
        // Arrange
        var (child, parentUser) = await CreateTestChild(balance: 200m);
        var goal = await CreateTestGoal(child.Id, targetAmount: 200m);

        // Create matching rule with $15 cap
        var matchingRule = new ParentMatchingRule
        {
            Id = Guid.NewGuid(),
            GoalId = goal.Id,
            CreatedByParentId = parentUser.Id,
            Type = MatchingType.RatioMatch,
            MatchRatio = 1m, // 1:1 match
            MaxMatchAmount = 15m,
            TotalMatchedAmount = 10m, // Already matched $10
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.ParentMatchingRules.Add(matchingRule);
        await _context.SaveChangesAsync();

        var dto = new ContributeToGoalDto(Amount: 20m, Description: null);

        // Act
        var result = await _savingsGoalService.ContributeAsync(goal.Id, dto, _parentUserId);

        // Assert
        result.MatchAmountAdded.Should().Be(5m); // Only $5 remaining in cap
    }

    [Fact]
    public async Task ContributeAsync_AchievesMilestone_At25Percent()
    {
        // Arrange
        var (child, _) = await CreateTestChild(balance: 100m);
        var goal = await CreateTestGoal(child.Id, targetAmount: 100m);
        var dto = new ContributeToGoalDto(Amount: 25m, Description: null);

        // Act
        var result = await _savingsGoalService.ContributeAsync(goal.Id, dto, _parentUserId);

        // Assert
        result.MilestoneReached.Should().NotBeNull();
        result.MilestoneReached!.PercentComplete.Should().Be(25);
        result.MilestoneReached.IsAchieved.Should().BeTrue();
    }

    [Fact]
    public async Task ContributeAsync_AchievesMilestone_At50Percent()
    {
        // Arrange
        var (child, _) = await CreateTestChild(balance: 100m);
        var goal = await CreateTestGoal(child.Id, targetAmount: 100m, currentAmount: 25m);

        // Mark the 25% milestone as achieved
        var milestone25 = await _context.GoalMilestones.FirstAsync(m => m.GoalId == goal.Id && m.PercentComplete == 25);
        milestone25.IsAchieved = true;
        milestone25.AchievedAt = DateTime.UtcNow.AddDays(-1);
        await _context.SaveChangesAsync();

        var dto = new ContributeToGoalDto(Amount: 25m, Description: null); // Takes to 50%

        // Act
        var result = await _savingsGoalService.ContributeAsync(goal.Id, dto, _parentUserId);

        // Assert
        result.MilestoneReached.Should().NotBeNull();
        result.MilestoneReached!.PercentComplete.Should().Be(50);
    }

    [Fact]
    public async Task ContributeAsync_CompletesGoal_At100Percent()
    {
        // Arrange
        var (child, _) = await CreateTestChild(balance: 100m);
        var goal = await CreateTestGoal(child.Id, targetAmount: 100m, currentAmount: 75m);

        // Mark earlier milestones as achieved
        var milestones = await _context.GoalMilestones.Where(m => m.GoalId == goal.Id && m.PercentComplete < 100).ToListAsync();
        foreach (var m in milestones)
        {
            m.IsAchieved = true;
            m.AchievedAt = DateTime.UtcNow.AddDays(-1);
        }
        await _context.SaveChangesAsync();

        var dto = new ContributeToGoalDto(Amount: 25m, Description: null); // Takes to 100%

        // Act
        var result = await _savingsGoalService.ContributeAsync(goal.Id, dto, _parentUserId);

        // Assert
        result.IsCompleted.Should().BeTrue();
        result.MilestoneReached.Should().NotBeNull();
        result.MilestoneReached!.PercentComplete.Should().Be(100);

        var updatedGoal = await _context.SavingsGoals.FindAsync(goal.Id);
        updatedGoal!.Status.Should().Be(GoalStatus.Completed);
        updatedGoal.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ContributeAsync_ThrowsWhenInsufficientBalance()
    {
        // Arrange
        var (child, _) = await CreateTestChild(balance: 10m);
        var goal = await CreateTestGoal(child.Id, targetAmount: 100m);
        var dto = new ContributeToGoalDto(Amount: 25m, Description: null);

        // Act
        var act = () => _savingsGoalService.ContributeAsync(goal.Id, dto, _parentUserId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Insufficient*");
    }

    [Fact]
    public async Task WithdrawAsync_ReturnsToChildBalance()
    {
        // Arrange
        var (child, _) = await CreateTestChild(balance: 50m);
        var goal = await CreateTestGoal(child.Id, targetAmount: 100m, currentAmount: 30m);
        var dto = new WithdrawFromGoalDto(Amount: 10m, Reason: "Need for emergency");

        // Act
        var result = await _savingsGoalService.WithdrawAsync(goal.Id, dto, _parentUserId);

        // Assert
        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentBalance.Should().Be(60m); // 50 + 10

        var updatedGoal = await _context.SavingsGoals.FindAsync(goal.Id);
        updatedGoal!.CurrentAmount.Should().Be(20m); // 30 - 10

        result.Type.Should().Be(ContributionType.Withdrawal);
        result.Amount.Should().Be(-10m); // Negative for withdrawal
    }

    [Fact]
    public async Task MarkAsPurchasedAsync_SetsStatusAndDeductsAmount()
    {
        // Arrange
        var (child, _) = await CreateTestChild(balance: 0m);
        var goal = await CreateTestGoal(child.Id, targetAmount: 100m, currentAmount: 100m, status: GoalStatus.Completed);
        var dto = new MarkGoalPurchasedDto(Notes: "Bought at store");

        // Act
        var result = await _savingsGoalService.MarkAsPurchasedAsync(goal.Id, dto, _parentUserId);

        // Assert
        result.Status.Should().Be(GoalStatus.Purchased);
        result.PurchasedAt.Should().NotBeNull();
    }

    #endregion

    #region Matching Rule Tests

    [Fact]
    public async Task CreateMatchingRuleAsync_CreatesRule()
    {
        // Arrange
        var (child, parentUser) = await CreateTestChild();
        var goal = await CreateTestGoal(child.Id);
        var dto = new CreateMatchingRuleDto(
            Type: MatchingType.RatioMatch,
            MatchRatio: 0.5m,
            MaxMatchAmount: 25m,
            ExpiresAt: null
        );

        // Act
        var result = await _savingsGoalService.CreateMatchingRuleAsync(goal.Id, dto, parentUser.Id);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(MatchingType.RatioMatch);
        result.MatchRatio.Should().Be(0.5m);
        result.MaxMatchAmount.Should().Be(25m);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateMatchingRuleAsync_ThrowsWhenRuleExists()
    {
        // Arrange
        var (child, parentUser) = await CreateTestChild();
        var goal = await CreateTestGoal(child.Id);

        var existingRule = new ParentMatchingRule
        {
            Id = Guid.NewGuid(),
            GoalId = goal.Id,
            CreatedByParentId = parentUser.Id,
            Type = MatchingType.RatioMatch,
            MatchRatio = 0.5m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.ParentMatchingRules.Add(existingRule);
        await _context.SaveChangesAsync();

        var dto = new CreateMatchingRuleDto(
            Type: MatchingType.RatioMatch,
            MatchRatio: 1m,
            MaxMatchAmount: null,
            ExpiresAt: null
        );

        // Act
        var act = () => _savingsGoalService.CreateMatchingRuleAsync(goal.Id, dto, parentUser.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already has*matching rule*");
    }

    [Fact]
    public async Task GetMatchingRuleAsync_ReturnsRule()
    {
        // Arrange
        var (child, parentUser) = await CreateTestChild();
        var goal = await CreateTestGoal(child.Id);

        var rule = new ParentMatchingRule
        {
            Id = Guid.NewGuid(),
            GoalId = goal.Id,
            CreatedByParentId = parentUser.Id,
            Type = MatchingType.RatioMatch,
            MatchRatio = 0.5m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.ParentMatchingRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        var result = await _savingsGoalService.GetMatchingRuleAsync(goal.Id);

        // Assert
        result.Should().NotBeNull();
        result!.MatchRatio.Should().Be(0.5m);
    }

    [Fact]
    public async Task UpdateMatchingRuleAsync_UpdatesRule()
    {
        // Arrange
        var (child, parentUser) = await CreateTestChild();
        var goal = await CreateTestGoal(child.Id);

        var rule = new ParentMatchingRule
        {
            Id = Guid.NewGuid(),
            GoalId = goal.Id,
            CreatedByParentId = parentUser.Id,
            Type = MatchingType.RatioMatch,
            MatchRatio = 0.5m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.ParentMatchingRules.Add(rule);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateMatchingRuleDto(
            MatchRatio: 1m,
            MaxMatchAmount: 100m,
            IsActive: true,
            ExpiresAt: null
        );

        // Act
        var result = await _savingsGoalService.UpdateMatchingRuleAsync(goal.Id, updateDto, parentUser.Id);

        // Assert
        result.MatchRatio.Should().Be(1m);
        result.MaxMatchAmount.Should().Be(100m);
    }

    [Fact]
    public async Task RemoveMatchingRuleAsync_DeletesRule()
    {
        // Arrange
        var (child, parentUser) = await CreateTestChild();
        var goal = await CreateTestGoal(child.Id);

        var rule = new ParentMatchingRule
        {
            Id = Guid.NewGuid(),
            GoalId = goal.Id,
            CreatedByParentId = parentUser.Id,
            Type = MatchingType.RatioMatch,
            MatchRatio = 0.5m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.ParentMatchingRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        await _savingsGoalService.RemoveMatchingRuleAsync(goal.Id, parentUser.Id);

        // Assert
        var deletedRule = await _context.ParentMatchingRules.FindAsync(rule.Id);
        deletedRule.Should().BeNull();
    }

    #endregion

    #region Challenge Tests

    [Fact]
    public async Task CreateChallengeAsync_CreatesActiveChallenge()
    {
        // Arrange
        var (child, parentUser) = await CreateTestChild();
        var goal = await CreateTestGoal(child.Id, targetAmount: 100m);
        var dto = new CreateGoalChallengeDto(
            TargetAmount: 50m,
            EndDate: DateTime.UtcNow.AddDays(30),
            BonusAmount: 10m,
            Description: "Save $50 in 30 days!"
        );

        // Act
        var result = await _savingsGoalService.CreateChallengeAsync(goal.Id, dto, parentUser.Id);

        // Assert
        result.Should().NotBeNull();
        result.TargetAmount.Should().Be(50m);
        result.BonusAmount.Should().Be(10m);
        result.Status.Should().Be(ChallengeStatus.Active);
    }

    [Fact]
    public async Task CreateChallengeAsync_ThrowsWhenActiveExists()
    {
        // Arrange
        var (child, parentUser) = await CreateTestChild();
        var goal = await CreateTestGoal(child.Id);

        var existingChallenge = new GoalChallenge
        {
            Id = Guid.NewGuid(),
            GoalId = goal.Id,
            CreatedByParentId = parentUser.Id,
            TargetAmount = 50m,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            BonusAmount = 10m,
            Status = ChallengeStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.GoalChallenges.Add(existingChallenge);
        await _context.SaveChangesAsync();

        var dto = new CreateGoalChallengeDto(
            TargetAmount: 75m,
            EndDate: DateTime.UtcNow.AddDays(14),
            BonusAmount: 15m,
            Description: null
        );

        // Act
        var act = () => _savingsGoalService.CreateChallengeAsync(goal.Id, dto, parentUser.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already has an active challenge*");
    }

    [Fact]
    public async Task ContributeAsync_CompletesChallenge_AwardsBonusOnCompletion()
    {
        // Arrange
        var (child, parentUser) = await CreateTestChild(balance: 100m);
        var goal = await CreateTestGoal(child.Id, targetAmount: 100m, currentAmount: 40m);

        var challenge = new GoalChallenge
        {
            Id = Guid.NewGuid(),
            GoalId = goal.Id,
            CreatedByParentId = parentUser.Id,
            TargetAmount = 50m,
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(7),
            BonusAmount = 10m,
            Status = ChallengeStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        };
        _context.GoalChallenges.Add(challenge);
        await _context.SaveChangesAsync();

        var dto = new ContributeToGoalDto(Amount: 15m, Description: null); // Takes to 55m, exceeding 50m target

        // Act
        var result = await _savingsGoalService.ContributeAsync(goal.Id, dto, _parentUserId);

        // Assert
        // 40 + 15 = 55, plus 10 bonus = 65
        result.NewAmount.Should().Be(65m);

        var updatedChallenge = await _context.GoalChallenges.FindAsync(challenge.Id);
        updatedChallenge!.Status.Should().Be(ChallengeStatus.Completed);
        updatedChallenge.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckExpiredChallengesAsync_MarksExpiredAsFailed()
    {
        // Arrange
        var (child, parentUser) = await CreateTestChild();
        var goal = await CreateTestGoal(child.Id, targetAmount: 100m, currentAmount: 30m);

        var expiredChallenge = new GoalChallenge
        {
            Id = Guid.NewGuid(),
            GoalId = goal.Id,
            CreatedByParentId = parentUser.Id,
            TargetAmount = 50m,
            StartDate = DateTime.UtcNow.AddDays(-14),
            EndDate = DateTime.UtcNow.AddDays(-1), // Expired yesterday
            BonusAmount = 10m,
            Status = ChallengeStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-14)
        };
        _context.GoalChallenges.Add(expiredChallenge);
        await _context.SaveChangesAsync();

        // Act
        await _savingsGoalService.CheckExpiredChallengesAsync();

        // Assert
        var updatedChallenge = await _context.GoalChallenges.FindAsync(expiredChallenge.Id);
        updatedChallenge!.Status.Should().Be(ChallengeStatus.Failed);
    }

    [Fact]
    public async Task GetActiveChallengeAsync_ReturnsCurrentChallenge()
    {
        // Arrange
        var (child, parentUser) = await CreateTestChild();
        var goal = await CreateTestGoal(child.Id, targetAmount: 100m, currentAmount: 25m);

        var challenge = new GoalChallenge
        {
            Id = Guid.NewGuid(),
            GoalId = goal.Id,
            CreatedByParentId = parentUser.Id,
            TargetAmount = 50m,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            BonusAmount = 10m,
            Status = ChallengeStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.GoalChallenges.Add(challenge);
        await _context.SaveChangesAsync();

        // Act
        var result = await _savingsGoalService.GetActiveChallengeAsync(goal.Id);

        // Assert
        result.Should().NotBeNull();
        result!.TargetAmount.Should().Be(50m);
        result.CurrentProgress.Should().Be(25m);
    }

    [Fact]
    public async Task CancelChallengeAsync_CancelsActiveChallenge()
    {
        // Arrange
        var (child, parentUser) = await CreateTestChild();
        var goal = await CreateTestGoal(child.Id);

        var challenge = new GoalChallenge
        {
            Id = Guid.NewGuid(),
            GoalId = goal.Id,
            CreatedByParentId = parentUser.Id,
            TargetAmount = 50m,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            BonusAmount = 10m,
            Status = ChallengeStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.GoalChallenges.Add(challenge);
        await _context.SaveChangesAsync();

        // Act
        await _savingsGoalService.CancelChallengeAsync(goal.Id, parentUser.Id);

        // Assert
        var updatedChallenge = await _context.GoalChallenges.FindAsync(challenge.Id);
        updatedChallenge!.Status.Should().Be(ChallengeStatus.Cancelled);
    }

    [Fact]
    public async Task GetChildChallengesAsync_ReturnsAllChallenges()
    {
        // Arrange
        var (child, parentUser) = await CreateTestChild();
        var goal1 = await CreateTestGoal(child.Id, targetAmount: 100m);
        var goal2 = await CreateTestGoal(child.Id, targetAmount: 200m);

        var challenge1 = new GoalChallenge
        {
            Id = Guid.NewGuid(),
            GoalId = goal1.Id,
            CreatedByParentId = parentUser.Id,
            TargetAmount = 50m,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            BonusAmount = 10m,
            Status = ChallengeStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        var challenge2 = new GoalChallenge
        {
            Id = Guid.NewGuid(),
            GoalId = goal2.Id,
            CreatedByParentId = parentUser.Id,
            TargetAmount = 100m,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(-1),
            BonusAmount = 20m,
            Status = ChallengeStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
        _context.GoalChallenges.AddRange(challenge1, challenge2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _savingsGoalService.GetChildChallengesAsync(child.Id, _parentUserId);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region Auto-Transfer Tests

    [Fact]
    public async Task ProcessAutoTransfersAsync_TransfersFixedAmount()
    {
        // Arrange
        var (child, _) = await CreateTestChild(balance: 100m);
        var goal = await CreateTestGoal(child.Id, targetAmount: 100m, createMilestones: false);
        goal.AutoTransferType = AutoTransferType.FixedAmount;
        goal.AutoTransferAmount = 5m;
        await _context.SaveChangesAsync();

        // Act
        await _savingsGoalService.ProcessAutoTransfersAsync(child.Id, 20m);

        // Assert
        var updatedGoal = await _context.SavingsGoals.FindAsync(goal.Id);
        updatedGoal!.CurrentAmount.Should().Be(5m);

        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentBalance.Should().Be(95m); // 100 - 5
    }

    [Fact]
    public async Task ProcessAutoTransfersAsync_TransfersPercentage()
    {
        // Arrange
        var (child, _) = await CreateTestChild(balance: 100m);
        var goal = await CreateTestGoal(child.Id, targetAmount: 100m, createMilestones: false);
        goal.AutoTransferType = AutoTransferType.Percentage;
        goal.AutoTransferAmount = 25m; // 25%
        await _context.SaveChangesAsync();

        // Act
        await _savingsGoalService.ProcessAutoTransfersAsync(child.Id, 20m);

        // Assert
        var updatedGoal = await _context.SavingsGoals.FindAsync(goal.Id);
        updatedGoal!.CurrentAmount.Should().Be(5m); // 25% of 20 = 5

        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentBalance.Should().Be(95m);
    }

    [Fact]
    public async Task ProcessAutoTransfersAsync_SkipsPausedGoals()
    {
        // Arrange
        var (child, _) = await CreateTestChild(balance: 100m);
        var goal = await CreateTestGoal(child.Id, targetAmount: 100m, status: GoalStatus.Paused, createMilestones: false);
        goal.AutoTransferType = AutoTransferType.FixedAmount;
        goal.AutoTransferAmount = 5m;
        await _context.SaveChangesAsync();

        // Act
        await _savingsGoalService.ProcessAutoTransfersAsync(child.Id, 20m);

        // Assert
        var updatedGoal = await _context.SavingsGoals.FindAsync(goal.Id);
        updatedGoal!.CurrentAmount.Should().Be(0m); // No transfer

        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentBalance.Should().Be(100m); // No change
    }

    [Fact]
    public async Task ProcessAutoTransfersAsync_SkipsCompletedGoals()
    {
        // Arrange
        var (child, _) = await CreateTestChild(balance: 100m);
        var goal = await CreateTestGoal(child.Id, targetAmount: 100m, currentAmount: 100m, status: GoalStatus.Completed, createMilestones: false);
        goal.AutoTransferType = AutoTransferType.FixedAmount;
        goal.AutoTransferAmount = 5m;
        await _context.SaveChangesAsync();

        // Act
        await _savingsGoalService.ProcessAutoTransfersAsync(child.Id, 20m);

        // Assert
        var updatedGoal = await _context.SavingsGoals.FindAsync(goal.Id);
        updatedGoal!.CurrentAmount.Should().Be(100m); // No change

        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentBalance.Should().Be(100m); // No change
    }

    [Fact]
    public async Task ProcessAutoTransfersAsync_RespectsGoalPriority()
    {
        // Arrange
        var (child, _) = await CreateTestChild(balance: 10m); // Limited balance
        var goal1 = await CreateTestGoal(child.Id, targetAmount: 100m, createMilestones: false);
        goal1.Priority = 1;
        goal1.AutoTransferType = AutoTransferType.FixedAmount;
        goal1.AutoTransferAmount = 8m;

        var goal2 = await CreateTestGoal(child.Id, targetAmount: 100m, createMilestones: false);
        goal2.Priority = 2;
        goal2.AutoTransferType = AutoTransferType.FixedAmount;
        goal2.AutoTransferAmount = 8m;

        await _context.SaveChangesAsync();

        // Act
        await _savingsGoalService.ProcessAutoTransfersAsync(child.Id, 20m);

        // Assert
        var updatedGoal1 = await _context.SavingsGoals.FindAsync(goal1.Id);
        updatedGoal1!.CurrentAmount.Should().Be(8m); // Priority 1 gets funded first

        var updatedGoal2 = await _context.SavingsGoals.FindAsync(goal2.Id);
        updatedGoal2!.CurrentAmount.Should().Be(2m); // Only $2 remaining

        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentBalance.Should().Be(0m);
    }

    #endregion
}
