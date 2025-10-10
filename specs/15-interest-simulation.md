# Interest Simulation & Compound Interest Specification

## Overview

This specification introduces a simulated interest system that teaches children about compound interest, the power of saving, and financial growth. Parents can configure interest rates, and the system automatically calculates and pays interest on savings accounts at regular intervals.

## Goals

1. **Teach Compound Interest**: Show how money grows over time
2. **Parent-Configurable Rates**: Parents set realistic interest rates (5-10% annually)
3. **Automated Payments**: Weekly or monthly interest payments via background job
4. **Visual Learning**: Charts showing compound interest growth over time
5. **What-If Calculator**: Project future balance based on savings habits
6. **TDD Approach**: 20 comprehensive tests

## Technology Stack

- **Backend**: ASP.NET Core 8.0 with Entity Framework Core
- **Database**: PostgreSQL with decimal precision
- **Testing**: xUnit, FluentAssertions, Moq
- **Charts**: ApexCharts for compound interest visualization
- **Background Jobs**: IHostedService for interest payments
- **UI**: Blazor Server with real-time updates

---

## Phase 1: Database Schema

### 1.1 SavingsAccount Model

```csharp
namespace AllowanceTracker.Models;

/// <summary>
/// Savings account with interest accrual
/// </summary>
public class SavingsAccount
{
    public Guid Id { get; set; }

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    /// <summary>
    /// Current balance earning interest
    /// </summary>
    public decimal Balance { get; set; } = 0;

    /// <summary>
    /// Annual interest rate (e.g., 5.0 for 5%)
    /// </summary>
    public decimal AnnualInterestRate { get; set; } = 5.0m;

    /// <summary>
    /// How often interest is paid
    /// </summary>
    public InterestFrequency Frequency { get; set; } = InterestFrequency.Weekly;

    /// <summary>
    /// Last date interest was calculated and paid
    /// </summary>
    public DateTime? LastInterestDate { get; set; }

    /// <summary>
    /// Next scheduled interest payment date
    /// </summary>
    public DateTime? NextInterestDate { get; set; }

    /// <summary>
    /// Total interest earned (lifetime)
    /// </summary>
    public decimal TotalInterestEarned { get; set; } = 0;

    /// <summary>
    /// Is account active?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Minimum balance required to earn interest
    /// </summary>
    public decimal MinimumBalance { get; set; } = 0;

    /// <summary>
    /// Account opened date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid CreatedById { get; set; }
    public virtual ApplicationUser CreatedBy { get; set; } = null!;

    // Navigation properties
    public virtual ICollection<InterestTransaction> InterestTransactions { get; set; } = new List<InterestTransaction>();
}

public enum InterestFrequency
{
    Weekly = 1,
    BiWeekly = 2,
    Monthly = 3
}
```

### 1.2 InterestTransaction Model

```csharp
namespace AllowanceTracker.Models;

/// <summary>
/// Record of interest payment
/// </summary>
public class InterestTransaction
{
    public Guid Id { get; set; }

    public Guid SavingsAccountId { get; set; }
    public virtual SavingsAccount SavingsAccount { get; set; } = null!;

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    /// <summary>
    /// Interest amount paid
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Balance before interest
    /// </summary>
    public decimal BalanceBefore { get; set; }

    /// <summary>
    /// Balance after interest
    /// </summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>
    /// Interest rate at time of calculation
    /// </summary>
    public decimal InterestRate { get; set; }

    /// <summary>
    /// Period this interest covers
    /// </summary>
    public DateTime PeriodStartDate { get; set; }

    public DateTime PeriodEndDate { get; set; }

    /// <summary>
    /// When interest was paid
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Linked transaction in main ledger
    /// </summary>
    public Guid? TransactionId { get; set; }
    public virtual Transaction? Transaction { get; set; }
}
```

### 1.3 Update Child Model

```csharp
namespace AllowanceTracker.Models;

public class Child
{
    // ... existing properties ...

    /// <summary>
    /// Savings account (one per child)
    /// </summary>
    public virtual SavingsAccount? SavingsAccount { get; set; }
}
```

### 1.4 Database Migration

```bash
dotnet ef migrations add AddInterestSimulation
```

```csharp
public partial class AddInterestSimulation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create SavingsAccounts table
        migrationBuilder.CreateTable(
            name: "SavingsAccounts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                Balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                AnnualInterestRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                Frequency = table.Column<int>(type: "integer", nullable: false),
                LastInterestDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                NextInterestDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                TotalInterestEarned = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                MinimumBalance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SavingsAccounts", x => x.Id);
                table.ForeignKey(
                    name: "FK_SavingsAccounts_Children_ChildId",
                    column: x => x.ChildId,
                    principalTable: "Children",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SavingsAccounts_AspNetUsers_CreatedById",
                    column: x => x.CreatedById,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        // Create InterestTransactions table
        migrationBuilder.CreateTable(
            name: "InterestTransactions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SavingsAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                BalanceBefore = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                BalanceAfter = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                InterestRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                PeriodStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                PeriodEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                TransactionId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InterestTransactions", x => x.Id);
                table.ForeignKey(
                    name: "FK_InterestTransactions_SavingsAccounts_SavingsAccountId",
                    column: x => x.SavingsAccountId,
                    principalTable: "SavingsAccounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_InterestTransactions_Children_ChildId",
                    column: x => x.ChildId,
                    principalTable: "Children",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_InterestTransactions_Transactions_TransactionId",
                    column: x => x.TransactionId,
                    principalTable: "Transactions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // Create indexes
        migrationBuilder.CreateIndex(
            name: "IX_SavingsAccounts_ChildId",
            table: "SavingsAccounts",
            column: "ChildId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SavingsAccounts_NextInterestDate",
            table: "SavingsAccounts",
            column: "NextInterestDate");

        migrationBuilder.CreateIndex(
            name: "IX_InterestTransactions_SavingsAccountId",
            table: "InterestTransactions",
            column: "SavingsAccountId");

        migrationBuilder.CreateIndex(
            name: "IX_InterestTransactions_ChildId",
            table: "InterestTransactions",
            column: "ChildId");

        migrationBuilder.CreateIndex(
            name: "IX_InterestTransactions_CreatedAt",
            table: "InterestTransactions",
            column: "CreatedAt");
    }
}
```

---

## Phase 2: Service Layer (TDD)

### 2.1 IInterestService Interface

```csharp
namespace AllowanceTracker.Services;

public interface IInterestService
{
    // Account Management
    Task<SavingsAccount> CreateSavingsAccountAsync(CreateSavingsAccountDto dto, Guid currentUserId);
    Task<SavingsAccount> GetSavingsAccountAsync(Guid accountId);
    Task<SavingsAccount?> GetSavingsAccountByChildIdAsync(Guid childId);
    Task<SavingsAccount> UpdateAccountSettingsAsync(Guid accountId, UpdateSavingsAccountDto dto, Guid currentUserId);

    // Deposits & Withdrawals
    Task<SavingsAccount> DepositAsync(Guid accountId, decimal amount, Guid userId);
    Task<SavingsAccount> WithdrawAsync(Guid accountId, decimal amount, Guid userId);

    // Interest Calculation
    Task<decimal> CalculateInterestAsync(Guid accountId);
    Task<InterestTransaction> PayInterestAsync(Guid accountId, Guid systemUserId);
    Task ProcessAllInterestPaymentsAsync();

    // Interest History
    Task<List<InterestTransaction>> GetInterestHistoryAsync(Guid accountId, int months = 12);
    Task<InterestStatistics> GetInterestStatisticsAsync(Guid childId);

    // Projections
    Task<CompoundInterestProjection> ProjectFutureBalanceAsync(
        decimal currentBalance,
        decimal annualRate,
        decimal monthlyDeposit,
        int months);

    Task<CompoundInterestProjection> ProjectAccountBalanceAsync(Guid accountId, int months);

    // Utilities
    DateTime CalculateNextInterestDate(DateTime lastDate, InterestFrequency frequency);
    decimal CalculatePeriodRate(decimal annualRate, InterestFrequency frequency);
}
```

### 2.2 Data Transfer Objects

```csharp
namespace AllowanceTracker.DTOs;

public record CreateSavingsAccountDto(
    Guid ChildId,
    decimal InitialBalance,
    decimal AnnualInterestRate = 5.0m,
    InterestFrequency Frequency = InterestFrequency.Weekly,
    decimal MinimumBalance = 0);

public record UpdateSavingsAccountDto(
    decimal? AnnualInterestRate = null,
    InterestFrequency? Frequency = null,
    decimal? MinimumBalance = null,
    bool? IsActive = null);

public record InterestStatistics(
    decimal TotalInterestEarned,
    decimal AverageMonthlyInterest,
    int PaymentCount,
    DateTime? LastPaymentDate,
    decimal CurrentBalance,
    decimal EffectiveAnnualRate);

public record CompoundInterestProjection(
    List<ProjectionDataPoint> DataPoints,
    decimal FinalBalance,
    decimal TotalDeposits,
    decimal TotalInterest,
    decimal EffectiveAnnualYield);

public record ProjectionDataPoint(
    int Month,
    decimal Balance,
    decimal InterestEarned,
    decimal CumulativeInterest);
```

### 2.3 InterestService Implementation

```csharp
namespace AllowanceTracker.Services;

public class InterestService : IInterestService
{
    private readonly AllowanceContext _context;
    private readonly ITransactionService _transactionService;
    private readonly IHubContext<FamilyHub>? _hubContext;
    private readonly ILogger<InterestService> _logger;

    public InterestService(
        AllowanceContext context,
        ITransactionService transactionService,
        IHubContext<FamilyHub>? hubContext,
        ILogger<InterestService> logger)
    {
        _context = context;
        _transactionService = transactionService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<SavingsAccount> CreateSavingsAccountAsync(
        CreateSavingsAccountDto dto,
        Guid currentUserId)
    {
        // Validate child exists
        var child = await _context.Children.FindAsync(dto.ChildId)
            ?? throw new NotFoundException("Child not found");

        // Check if account already exists
        var existingAccount = await _context.SavingsAccounts
            .FirstOrDefaultAsync(a => a.ChildId == dto.ChildId);

        if (existingAccount != null)
            throw new InvalidOperationException("Savings account already exists for this child");

        // Validate interest rate
        if (dto.AnnualInterestRate < 0 || dto.AnnualInterestRate > 100)
            throw new ValidationException("Interest rate must be between 0 and 100");

        var now = DateTime.UtcNow;
        var account = new SavingsAccount
        {
            Id = Guid.NewGuid(),
            ChildId = dto.ChildId,
            Balance = dto.InitialBalance,
            AnnualInterestRate = dto.AnnualInterestRate,
            Frequency = dto.Frequency,
            MinimumBalance = dto.MinimumBalance,
            IsActive = true,
            LastInterestDate = now,
            NextInterestDate = CalculateNextInterestDate(now, dto.Frequency),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedById = currentUserId
        };

        _context.SavingsAccounts.Add(account);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created savings account {AccountId} for child {ChildId} with {Rate}% interest",
            account.Id, dto.ChildId, dto.AnnualInterestRate);

        return account;
    }

    public async Task<SavingsAccount> DepositAsync(Guid accountId, decimal amount, Guid userId)
    {
        if (amount <= 0)
            throw new ValidationException("Deposit amount must be greater than zero");

        var account = await _context.SavingsAccounts
            .Include(a => a.Child)
            .FirstOrDefaultAsync(a => a.Id == accountId)
            ?? throw new NotFoundException("Savings account not found");

        if (!account.IsActive)
            throw new InvalidOperationException("Account is not active");

        // Check if child has sufficient balance
        if (account.Child.CurrentBalance < amount)
            throw new InsufficientFundsException($"Insufficient balance. Available: {account.Child.CurrentBalance:C}");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Deduct from child's balance
            account.Child.CurrentBalance -= amount;

            // Add to savings account
            account.Balance += amount;
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Deposited {Amount} to savings account {AccountId}", amount, accountId);

            return account;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<SavingsAccount> WithdrawAsync(Guid accountId, decimal amount, Guid userId)
    {
        if (amount <= 0)
            throw new ValidationException("Withdrawal amount must be greater than zero");

        var account = await _context.SavingsAccounts
            .Include(a => a.Child)
            .FirstOrDefaultAsync(a => a.Id == accountId)
            ?? throw new NotFoundException("Savings account not found");

        if (account.Balance < amount)
            throw new InsufficientFundsException($"Insufficient savings balance. Available: {account.Balance:C}");

        // Check minimum balance requirement
        if (account.Balance - amount < account.MinimumBalance)
            throw new InvalidOperationException(
                $"Withdrawal would bring balance below minimum of {account.MinimumBalance:C}");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Remove from savings account
            account.Balance -= amount;
            account.UpdatedAt = DateTime.UtcNow;

            // Add back to child's balance
            account.Child.CurrentBalance += amount;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Withdrew {Amount} from savings account {AccountId}", amount, accountId);

            return account;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<decimal> CalculateInterestAsync(Guid accountId)
    {
        var account = await _context.SavingsAccounts.FindAsync(accountId)
            ?? throw new NotFoundException("Savings account not found");

        if (account.Balance < account.MinimumBalance)
            return 0;

        // Calculate period rate
        var periodRate = CalculatePeriodRate(account.AnnualInterestRate, account.Frequency);

        // Simple interest: balance * rate
        var interest = Math.Round(account.Balance * (periodRate / 100), 2);

        return interest;
    }

    public async Task<InterestTransaction> PayInterestAsync(Guid accountId, Guid systemUserId)
    {
        var account = await _context.SavingsAccounts
            .Include(a => a.Child)
            .FirstOrDefaultAsync(a => a.Id == accountId)
            ?? throw new NotFoundException("Savings account not found");

        if (!account.IsActive)
            throw new InvalidOperationException("Account is not active");

        var interest = await CalculateInterestAsync(accountId);

        if (interest <= 0)
        {
            _logger.LogInformation("No interest to pay for account {AccountId}", accountId);

            // Still update next interest date
            account.LastInterestDate = DateTime.UtcNow;
            account.NextInterestDate = CalculateNextInterestDate(DateTime.UtcNow, account.Frequency);
            await _context.SaveChangesAsync();

            return null!; // Return null when no interest paid
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;
            var balanceBefore = account.Balance;

            // Add interest to account
            account.Balance += interest;
            account.TotalInterestEarned += interest;

            // Also add to child's main balance
            account.Child.CurrentBalance += interest;

            // Update interest dates
            account.LastInterestDate = now;
            account.NextInterestDate = CalculateNextInterestDate(now, account.Frequency);
            account.UpdatedAt = now;

            // Create interest transaction record
            var interestTx = new InterestTransaction
            {
                Id = Guid.NewGuid(),
                SavingsAccountId = accountId,
                ChildId = account.ChildId,
                Amount = interest,
                BalanceBefore = balanceBefore,
                BalanceAfter = account.Balance,
                InterestRate = account.AnnualInterestRate,
                PeriodStartDate = account.LastInterestDate ?? now.AddDays(-7),
                PeriodEndDate = now,
                CreatedAt = now
            };

            _context.InterestTransactions.Add(interestTx);

            // Create main transaction for audit trail
            var mainTx = await _transactionService.CreateTransactionAsync(new CreateTransactionDto(
                account.ChildId,
                interest,
                TransactionType.Credit,
                TransactionCategory.Investment,
                $"Interest payment ({account.AnnualInterestRate}% APR)"));

            interestTx.TransactionId = mainTx.Id;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Send notification
            await _hubContext?.Clients
                .Group($"family-{account.Child.FamilyId}")
                .SendAsync("InterestPaid", accountId, interest);

            _logger.LogInformation(
                "Paid {Interest} interest to account {AccountId}. New balance: {Balance}",
                interest, accountId, account.Balance);

            return interestTx;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ProcessAllInterestPaymentsAsync()
    {
        var now = DateTime.UtcNow;

        var accountsDue = await _context.SavingsAccounts
            .Where(a => a.IsActive &&
                       a.NextInterestDate.HasValue &&
                       a.NextInterestDate.Value <= now)
            .ToListAsync();

        _logger.LogInformation("Processing interest for {Count} accounts", accountsDue.Count);

        foreach (var account in accountsDue)
        {
            try
            {
                await PayInterestAsync(account.Id, Guid.Empty); // System user
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error paying interest for account {AccountId}", account.Id);
            }
        }
    }

    public decimal CalculatePeriodRate(decimal annualRate, InterestFrequency frequency)
    {
        return frequency switch
        {
            InterestFrequency.Weekly => annualRate / 52,
            InterestFrequency.BiWeekly => annualRate / 26,
            InterestFrequency.Monthly => annualRate / 12,
            _ => throw new ArgumentException("Invalid frequency")
        };
    }

    public DateTime CalculateNextInterestDate(DateTime lastDate, InterestFrequency frequency)
    {
        return frequency switch
        {
            InterestFrequency.Weekly => lastDate.AddDays(7),
            InterestFrequency.BiWeekly => lastDate.AddDays(14),
            InterestFrequency.Monthly => lastDate.AddMonths(1),
            _ => throw new ArgumentException("Invalid frequency")
        };
    }

    public async Task<CompoundInterestProjection> ProjectFutureBalanceAsync(
        decimal currentBalance,
        decimal annualRate,
        decimal monthlyDeposit,
        int months)
    {
        var dataPoints = new List<ProjectionDataPoint>();
        var balance = currentBalance;
        var totalDeposits = currentBalance;
        var cumulativeInterest = 0m;

        // Monthly compounding
        var monthlyRate = annualRate / 12 / 100;

        for (int month = 1; month <= months; month++)
        {
            // Add monthly deposit
            balance += monthlyDeposit;
            totalDeposits += monthlyDeposit;

            // Calculate interest
            var interestEarned = Math.Round(balance * monthlyRate, 2);
            balance += interestEarned;
            cumulativeInterest += interestEarned;

            dataPoints.Add(new ProjectionDataPoint(
                month,
                balance,
                interestEarned,
                cumulativeInterest));
        }

        var effectiveYield = months >= 12 && totalDeposits > 0
            ? (cumulativeInterest / totalDeposits) * 100
            : 0;

        return new CompoundInterestProjection(
            dataPoints,
            balance,
            totalDeposits,
            cumulativeInterest,
            Math.Round(effectiveYield, 2));
    }

    public async Task<CompoundInterestProjection> ProjectAccountBalanceAsync(Guid accountId, int months)
    {
        var account = await _context.SavingsAccounts
            .Include(a => a.Child)
            .FirstOrDefaultAsync(a => a.Id == accountId)
            ?? throw new NotFoundException("Savings account not found");

        // Estimate monthly deposit based on weekly allowance
        var monthlyDeposit = account.Child.WeeklyAllowance * 4;

        return await ProjectFutureBalanceAsync(
            account.Balance,
            account.AnnualInterestRate,
            monthlyDeposit,
            months);
    }

    public async Task<InterestStatistics> GetInterestStatisticsAsync(Guid childId)
    {
        var account = await _context.SavingsAccounts
            .FirstOrDefaultAsync(a => a.ChildId == childId);

        if (account == null)
            return new InterestStatistics(0, 0, 0, null, 0, 0);

        var interestHistory = await _context.InterestTransactions
            .Where(t => t.SavingsAccountId == account.Id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var avgMonthlyInterest = interestHistory.Any()
            ? interestHistory.Average(t => t.Amount)
            : 0;

        var effectiveRate = account.Balance > 0 && account.TotalInterestEarned > 0
            ? (account.TotalInterestEarned / account.Balance) * 100
            : account.AnnualInterestRate;

        return new InterestStatistics(
            account.TotalInterestEarned,
            avgMonthlyInterest,
            interestHistory.Count,
            interestHistory.FirstOrDefault()?.CreatedAt,
            account.Balance,
            Math.Round(effectiveRate, 2));
    }
}
```

### 2.4 Test Cases (20 Tests)

```csharp
namespace AllowanceTracker.Tests.Services;

public class InterestServiceTests
{
    // Create Account Tests (3)
    [Fact]
    public async Task CreateSavingsAccount_ValidData_CreatesSuccessfully()
    {
        // Arrange
        var child = await CreateChild();
        var dto = new CreateSavingsAccountDto(
            child.Id,
            InitialBalance: 100m,
            AnnualInterestRate: 5.0m,
            Frequency: InterestFrequency.Weekly);

        // Act
        var account = await _interestService.CreateSavingsAccountAsync(dto, _parentUserId);

        // Assert
        account.Should().NotBeNull();
        account.ChildId.Should().Be(child.Id);
        account.Balance.Should().Be(100m);
        account.AnnualInterestRate.Should().Be(5.0m);
        account.Frequency.Should().Be(InterestFrequency.Weekly);
        account.IsActive.Should().BeTrue();
        account.NextInterestDate.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateSavingsAccount_DuplicateAccount_ThrowsException()
    {
        // Arrange
        var child = await CreateChild();
        var dto = new CreateSavingsAccountDto(child.Id, 100m);
        await _interestService.CreateSavingsAccountAsync(dto, _parentUserId);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _interestService.CreateSavingsAccountAsync(dto, _parentUserId));
    }

    [Fact]
    public async Task CreateSavingsAccount_InvalidInterestRate_ThrowsException()
    {
        // Arrange
        var child = await CreateChild();
        var dto = new CreateSavingsAccountDto(child.Id, 100m, AnnualInterestRate: 150m);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _interestService.CreateSavingsAccountAsync(dto, _parentUserId));
    }

    // Deposit/Withdraw Tests (4)
    [Fact]
    public async Task Deposit_ValidAmount_IncreasesAccountBalance()
    {
        // Arrange
        var child = await CreateChild(balance: 200m);
        var account = await CreateSavingsAccount(child.Id, initialBalance: 50m);

        // Act
        var updated = await _interestService.DepositAsync(account.Id, 75m, _parentUserId);

        // Assert
        updated.Balance.Should().Be(125m); // 50 + 75

        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentBalance.Should().Be(125m); // 200 - 75
    }

    [Fact]
    public async Task Deposit_InsufficientBalance_ThrowsException()
    {
        // Arrange
        var child = await CreateChild(balance: 50m);
        var account = await CreateSavingsAccount(child.Id, initialBalance: 10m);

        // Act & Assert
        await Assert.ThrowsAsync<InsufficientFundsException>(
            () => _interestService.DepositAsync(account.Id, 100m, _parentUserId));
    }

    [Fact]
    public async Task Withdraw_ValidAmount_DecreasesAccountBalance()
    {
        // Arrange
        var child = await CreateChild(balance: 50m);
        var account = await CreateSavingsAccount(child.Id, initialBalance: 100m);

        // Act
        var updated = await _interestService.WithdrawAsync(account.Id, 30m, _parentUserId);

        // Assert
        updated.Balance.Should().Be(70m); // 100 - 30

        var updatedChild = await _context.Children.FindAsync(child.Id);
        updatedChild!.CurrentBalance.Should().Be(80m); // 50 + 30
    }

    [Fact]
    public async Task Withdraw_BelowMinimumBalance_ThrowsException()
    {
        // Arrange
        var child = await CreateChild(balance: 50m);
        var account = await CreateSavingsAccount(
            child.Id, initialBalance: 100m, minimumBalance: 50m);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _interestService.WithdrawAsync(account.Id, 60m, _parentUserId));
    }

    // Interest Calculation Tests (5)
    [Fact]
    public async Task CalculateInterest_Weekly_CalculatesCorrectly()
    {
        // Arrange
        var child = await CreateChild();
        var account = await CreateSavingsAccount(
            child.Id,
            initialBalance: 100m,
            annualRate: 5.2m,
            frequency: InterestFrequency.Weekly);

        // Act
        var interest = await _interestService.CalculateInterestAsync(account.Id);

        // Assert
        // 5.2% annual / 52 weeks = 0.1% weekly = $0.10 on $100
        interest.Should().BeApproximately(0.10m, 0.01m);
    }

    [Fact]
    public async Task CalculateInterest_Monthly_CalculatesCorrectly()
    {
        // Arrange
        var child = await CreateChild();
        var account = await CreateSavingsAccount(
            child.Id,
            initialBalance: 120m,
            annualRate: 6.0m,
            frequency: InterestFrequency.Monthly);

        // Act
        var interest = await _interestService.CalculateInterestAsync(account.Id);

        // Assert
        // 6% annual / 12 months = 0.5% monthly = $0.60 on $120
        interest.Should().Be(0.60m);
    }

    [Fact]
    public async Task CalculateInterest_BelowMinimumBalance_ReturnsZero()
    {
        // Arrange
        var child = await CreateChild();
        var account = await CreateSavingsAccount(
            child.Id,
            initialBalance: 5m,
            annualRate: 10.0m,
            minimumBalance: 10m);

        // Act
        var interest = await _interestService.CalculateInterestAsync(account.Id);

        // Assert
        interest.Should().Be(0);
    }

    [Fact]
    public async Task PayInterest_ValidAccount_AddsInterestToBalance()
    {
        // Arrange
        var child = await CreateChild(balance: 50m);
        var account = await CreateSavingsAccount(
            child.Id,
            initialBalance: 100m,
            annualRate: 5.2m,
            frequency: InterestFrequency.Weekly);

        // Act
        var interestTx = await _interestService.PayInterestAsync(account.Id, Guid.Empty);

        // Assert
        interestTx.Should().NotBeNull();
        interestTx.Amount.Should().BeApproximately(0.10m, 0.01m);
        interestTx.BalanceBefore.Should().Be(100m);
        interestTx.BalanceAfter.Should().BeGreaterThan(100m);

        var updatedAccount = await _interestService.GetSavingsAccountAsync(account.Id);
        updatedAccount.Balance.Should().BeGreaterThan(100m);
        updatedAccount.TotalInterestEarned.Should().BeGreaterThan(0);
        updatedAccount.LastInterestDate.Should().NotBeNull();

        // Check main transaction created
        var mainTx = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == interestTx.TransactionId);
        mainTx.Should().NotBeNull();
        mainTx!.Type.Should().Be(TransactionType.Credit);
    }

    [Fact]
    public async Task PayInterest_BelowMinimum_DoesNotPayButUpdatesDate()
    {
        // Arrange
        var child = await CreateChild();
        var account = await CreateSavingsAccount(
            child.Id,
            initialBalance: 5m,
            annualRate: 10m,
            minimumBalance: 10m);

        var beforeDate = account.LastInterestDate;

        // Act
        var interestTx = await _interestService.PayInterestAsync(account.Id, Guid.Empty);

        // Assert
        interestTx.Should().BeNull();

        var updatedAccount = await _interestService.GetSavingsAccountAsync(account.Id);
        updatedAccount.Balance.Should().Be(5m); // No change
        updatedAccount.TotalInterestEarned.Should().Be(0);
        updatedAccount.LastInterestDate.Should().BeAfter(beforeDate!.Value);
    }

    // Period Rate Calculation Tests (3)
    [Fact]
    public void CalculatePeriodRate_Weekly_ReturnsCorrectRate()
    {
        // Act
        var rate = _interestService.CalculatePeriodRate(5.2m, InterestFrequency.Weekly);

        // Assert
        rate.Should().BeApproximately(0.1m, 0.01m); // 5.2 / 52
    }

    [Fact]
    public void CalculatePeriodRate_BiWeekly_ReturnsCorrectRate()
    {
        // Act
        var rate = _interestService.CalculatePeriodRate(5.2m, InterestFrequency.BiWeekly);

        // Assert
        rate.Should().BeApproximately(0.2m, 0.01m); // 5.2 / 26
    }

    [Fact]
    public void CalculatePeriodRate_Monthly_ReturnsCorrectRate()
    {
        // Act
        var rate = _interestService.CalculatePeriodRate(6.0m, InterestFrequency.Monthly);

        // Assert
        rate.Should().Be(0.5m); // 6.0 / 12
    }

    // Projection Tests (3)
    [Fact]
    public async Task ProjectFutureBalance_NoDeposits_CompoundsCorrectly()
    {
        // Act - $100 at 12% for 12 months, no deposits
        var projection = await _interestService.ProjectFutureBalanceAsync(
            currentBalance: 100m,
            annualRate: 12m,
            monthlyDeposit: 0m,
            months: 12);

        // Assert - Should be approximately $112.68 (compound interest)
        projection.FinalBalance.Should().BeApproximately(112.68m, 0.5m);
        projection.TotalDeposits.Should().Be(100m);
        projection.TotalInterest.Should().BeApproximately(12.68m, 0.5m);
        projection.DataPoints.Should().HaveCount(12);
    }

    [Fact]
    public async Task ProjectFutureBalance_WithDeposits_IncludesDepositsInCompounding()
    {
        // Act - $100 starting, $50/month deposit, 6% for 12 months
        var projection = await _interestService.ProjectFutureBalanceAsync(
            currentBalance: 100m,
            annualRate: 6m,
            monthlyDeposit: 50m,
            months: 12);

        // Assert
        projection.FinalBalance.Should().BeGreaterThan(100m + (50m * 12)); // More than deposits alone
        projection.TotalDeposits.Should().Be(100m + (50m * 12)); // 700
        projection.TotalInterest.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ProjectAccountBalance_UsesAccountSettings()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 10m);
        var account = await CreateSavingsAccount(
            child.Id,
            initialBalance: 50m,
            annualRate: 5m);

        // Act
        var projection = await _interestService.ProjectAccountBalanceAsync(account.Id, 6);

        // Assert
        projection.DataPoints.Should().HaveCount(6);
        projection.FinalBalance.Should().BeGreaterThan(50m);
    }

    // Statistics Tests (2)
    [Fact]
    public async Task GetInterestStatistics_NoAccount_ReturnsZeros()
    {
        // Arrange
        var child = await CreateChild();

        // Act
        var stats = await _interestService.GetInterestStatisticsAsync(child.Id);

        // Assert
        stats.TotalInterestEarned.Should().Be(0);
        stats.PaymentCount.Should().Be(0);
        stats.CurrentBalance.Should().Be(0);
    }

    [Fact]
    public async Task GetInterestStatistics_WithHistory_CalculatesCorrectly()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        var account = await CreateSavingsAccount(child.Id, initialBalance: 100m, annualRate: 5.2m);

        // Pay interest 3 times
        await _interestService.PayInterestAsync(account.Id, Guid.Empty);
        await _interestService.PayInterestAsync(account.Id, Guid.Empty);
        await _interestService.PayInterestAsync(account.Id, Guid.Empty);

        // Act
        var stats = await _interestService.GetInterestStatisticsAsync(child.Id);

        // Assert
        stats.TotalInterestEarned.Should().BeGreaterThan(0);
        stats.PaymentCount.Should().Be(3);
        stats.LastPaymentDate.Should().NotBeNull();
        stats.CurrentBalance.Should().BeGreaterThan(100m);
    }
}
```

---

## Phase 3: Background Job

### 3.1 InterestPaymentJob

```csharp
namespace AllowanceTracker.BackgroundJobs;

public class InterestPaymentJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InterestPaymentJob> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour

    public InterestPaymentJob(
        IServiceProvider serviceProvider,
        ILogger<InterestPaymentJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("InterestPaymentJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var interestService = scope.ServiceProvider.GetRequiredService<IInterestService>();

                await interestService.ProcessAllInterestPaymentsAsync();
                _logger.LogInformation("Processed interest payments successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing interest payments");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("InterestPaymentJob stopped");
    }
}
```

### 3.2 Register in Program.cs

```csharp
// Add to services
builder.Services.AddHostedService<InterestPaymentJob>();
builder.Services.AddScoped<IInterestService, InterestService>();
```

---

## Phase 4: API Controllers

### 4.1 InterestController

```csharp
[ApiController]
[Route("api/v1/interest")]
[Authorize]
public class InterestController : ControllerBase
{
    private readonly IInterestService _interestService;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Create savings account
    /// </summary>
    [HttpPost("accounts")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<SavingsAccount>> CreateAccount(CreateSavingsAccountDto dto)
    {
        var userId = _currentUserService.GetUserId();
        var account = await _interestService.CreateSavingsAccountAsync(dto, userId);
        return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, account);
    }

    /// <summary>
    /// Get savings account
    /// </summary>
    [HttpGet("accounts/{id}")]
    public async Task<ActionResult<SavingsAccount>> GetAccount(Guid id)
    {
        var account = await _interestService.GetSavingsAccountAsync(id);
        return Ok(account);
    }

    /// <summary>
    /// Get savings account by child ID
    /// </summary>
    [HttpGet("accounts/child/{childId}")]
    public async Task<ActionResult<SavingsAccount>> GetAccountByChild(Guid childId)
    {
        var account = await _interestService.GetSavingsAccountByChildIdAsync(childId);
        if (account == null)
            return NotFound();
        return Ok(account);
    }

    /// <summary>
    /// Update account settings
    /// </summary>
    [HttpPatch("accounts/{id}")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<SavingsAccount>> UpdateAccount(
        Guid id,
        UpdateSavingsAccountDto dto)
    {
        var userId = _currentUserService.GetUserId();
        var account = await _interestService.UpdateAccountSettingsAsync(id, dto, userId);
        return Ok(account);
    }

    /// <summary>
    /// Deposit to savings account
    /// </summary>
    [HttpPost("accounts/{id}/deposit")]
    public async Task<ActionResult<SavingsAccount>> Deposit(
        Guid id,
        [FromBody] decimal amount)
    {
        var userId = _currentUserService.GetUserId();
        var account = await _interestService.DepositAsync(id, amount, userId);
        return Ok(account);
    }

    /// <summary>
    /// Withdraw from savings account
    /// </summary>
    [HttpPost("accounts/{id}/withdraw")]
    public async Task<ActionResult<SavingsAccount>> Withdraw(
        Guid id,
        [FromBody] decimal amount)
    {
        var userId = _currentUserService.GetUserId();
        var account = await _interestService.WithdrawAsync(id, amount, userId);
        return Ok(account);
    }

    /// <summary>
    /// Calculate interest (preview)
    /// </summary>
    [HttpGet("accounts/{id}/calculate")]
    public async Task<ActionResult<decimal>> CalculateInterest(Guid id)
    {
        var interest = await _interestService.CalculateInterestAsync(id);
        return Ok(new { Interest = interest });
    }

    /// <summary>
    /// Get interest history
    /// </summary>
    [HttpGet("accounts/{id}/history")]
    public async Task<ActionResult<List<InterestTransaction>>> GetHistory(
        Guid id,
        [FromQuery] int months = 12)
    {
        var history = await _interestService.GetInterestHistoryAsync(id, months);
        return Ok(history);
    }

    /// <summary>
    /// Get interest statistics
    /// </summary>
    [HttpGet("statistics/{childId}")]
    public async Task<ActionResult<InterestStatistics>> GetStatistics(Guid childId)
    {
        var stats = await _interestService.GetInterestStatisticsAsync(childId);
        return Ok(stats);
    }

    /// <summary>
    /// Project future balance
    /// </summary>
    [HttpPost("projection")]
    public async Task<ActionResult<CompoundInterestProjection>> ProjectBalance(
        [FromBody] ProjectionRequest request)
    {
        var projection = await _interestService.ProjectFutureBalanceAsync(
            request.CurrentBalance,
            request.AnnualRate,
            request.MonthlyDeposit,
            request.Months);
        return Ok(projection);
    }

    /// <summary>
    /// Project account balance
    /// </summary>
    [HttpGet("accounts/{id}/projection")]
    public async Task<ActionResult<CompoundInterestProjection>> ProjectAccountBalance(
        Guid id,
        [FromQuery] int months = 12)
    {
        var projection = await _interestService.ProjectAccountBalanceAsync(id, months);
        return Ok(projection);
    }
}

public record ProjectionRequest(
    decimal CurrentBalance,
    decimal AnnualRate,
    decimal MonthlyDeposit,
    int Months);
```

---

## Phase 5: Blazor UI Components

### 5.1 InterestDashboard Component

```razor
@page "/interest/{ChildId:guid}"
@inject IInterestService InterestService

<div class="interest-dashboard">
    <h3>Interest & Compound Growth</h3>

    @if (Account != null)
    {
        <!-- Account Summary -->
        <div class="row mb-4">
            <div class="col-md-3">
                <div class="stat-card">
                    <div class="stat-value">@Account.Balance.ToString("C")</div>
                    <div class="stat-label">Savings Balance</div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="stat-card">
                    <div class="stat-value">@Account.AnnualInterestRate%</div>
                    <div class="stat-label">Interest Rate (APR)</div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="stat-card">
                    <div class="stat-value">@Account.TotalInterestEarned.ToString("C")</div>
                    <div class="stat-label">Total Interest Earned</div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="stat-card">
                    <div class="stat-value">@Account.NextInterestDate?.ToString("MMM dd")</div>
                    <div class="stat-label">Next Payment</div>
                </div>
            </div>
        </div>

        <!-- What-If Calculator -->
        <div class="card mb-4">
            <div class="card-header">
                <h5>What-If Calculator</h5>
            </div>
            <div class="card-body">
                <WhatIfCalculator Account="@Account" />
            </div>
        </div>

        <!-- Compound Interest Chart -->
        <div class="card mb-4">
            <div class="card-header">
                <h5>Projected Growth</h5>
            </div>
            <div class="card-body">
                <CompoundInterestChart AccountId="@Account.Id" />
            </div>
        </div>

        <!-- Interest History -->
        <div class="card">
            <div class="card-header">
                <h5>Interest Payment History</h5>
            </div>
            <div class="card-body">
                <InterestHistoryTable AccountId="@Account.Id" />
            </div>
        </div>
    }
    else
    {
        <div class="alert alert-info">
            <h5>No Savings Account</h5>
            <p>Create a savings account to start earning interest!</p>
            <button class="btn btn-primary" @onclick="CreateAccount">
                Create Savings Account
            </button>
        </div>
    }
</div>

@code {
    [Parameter] public Guid ChildId { get; set; }

    private SavingsAccount? Account;

    protected override async Task OnInitializedAsync()
    {
        Account = await InterestService.GetSavingsAccountByChildIdAsync(ChildId);
    }

    private void CreateAccount()
    {
        // Navigate to create form
    }
}
```

### 5.2 WhatIfCalculator Component

```razor
<div class="what-if-calculator">
    <div class="row">
        <div class="col-md-4">
            <label>Starting Balance</label>
            <div class="input-group">
                <span class="input-group-text">$</span>
                <input type="number" class="form-control" @bind="startingBalance" />
            </div>
        </div>
        <div class="col-md-4">
            <label>Monthly Deposit</label>
            <div class="input-group">
                <span class="input-group-text">$</span>
                <input type="number" class="form-control" @bind="monthlyDeposit" />
            </div>
        </div>
        <div class="col-md-4">
            <label>Time Period (months)</label>
            <input type="number" class="form-control" @bind="months" min="1" max="120" />
        </div>
    </div>

    <button class="btn btn-primary mt-3" @onclick="Calculate">
        Calculate
    </button>

    @if (projection != null)
    {
        <div class="results mt-4">
            <div class="row">
                <div class="col-md-4">
                    <div class="result-card">
                        <div class="result-label">Final Balance</div>
                        <div class="result-value text-success">
                            @projection.FinalBalance.ToString("C")
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="result-card">
                        <div class="result-label">Total Deposits</div>
                        <div class="result-value">
                            @projection.TotalDeposits.ToString("C")
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="result-card">
                        <div class="result-label">Interest Earned</div>
                        <div class="result-value text-primary">
                            @projection.TotalInterest.ToString("C")
                        </div>
                    </div>
                </div>
            </div>
        </div>
    }
</div>

@code {
    [Parameter] public SavingsAccount Account { get; set; } = null!;
    [Inject] private IInterestService InterestService { get; set; } = null!;

    private decimal startingBalance;
    private decimal monthlyDeposit = 20m;
    private int months = 12;
    private CompoundInterestProjection? projection;

    protected override void OnParametersSet()
    {
        startingBalance = Account.Balance;
    }

    private async Task Calculate()
    {
        projection = await InterestService.ProjectFutureBalanceAsync(
            startingBalance,
            Account.AnnualInterestRate,
            monthlyDeposit,
            months);
    }
}
```

---

## Success Metrics

- ✅ All 20 tests passing
- ✅ Savings accounts created with configurable rates
- ✅ Interest calculated accurately using period rates
- ✅ Interest payments automated via background job
- ✅ Compound interest projections accurate
- ✅ What-if calculator functional
- ✅ Interest history tracked
- ✅ Real-time notifications on interest payments

---

## Future Enhancements

1. **Tiered Interest Rates**: Higher rates for higher balances
2. **Bonus Interest**: Extra interest for maintaining balance
3. **Interest Boosts**: Temporary rate increases for achievements
4. **Savings Challenges**: Earn bonus interest for saving goals
5. **APY Calculator**: Show Annual Percentage Yield
6. **Tax Simulation**: Teach about interest taxation
7. **Multiple Accounts**: Different accounts for different goals

---

**Total Implementation Time**: 2-3 weeks following TDD methodology
