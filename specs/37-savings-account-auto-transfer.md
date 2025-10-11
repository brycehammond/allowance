# Savings Account with Auto-Transfer Specification

## Overview

This specification adds an optional general-purpose savings account feature that teaches children the "pay yourself first" financial principle. Unlike spec 14 (Savings Goals & Milestones) which is goal-specific with milestones, this is a simple, general savings account that automatically diverts money when weekly allowance is paid.

## Goals

1. **Pay Yourself First**: Teach children to save automatically before spending
2. **Simple Savings**: General savings account separate from main balance
3. **Flexible Configuration**: Fixed dollar amount OR percentage-based transfers
4. **Parental Control**: Parents configure transfer rules, children see results
5. **Transaction History**: Full audit trail of all savings movements
6. **TDD Approach**: 38 comprehensive tests across all layers

## Technology Stack

- **Backend**: ASP.NET Core 8.0 with Entity Framework Core
- **Database**: PostgreSQL with decimal precision for money
- **Testing**: xUnit, FluentAssertions, Moq
- **UI**: Blazor Server with real-time SignalR updates
- **API**: RESTful endpoints for mobile apps

---

## Phase 1: Database Schema

### 1.1 Update Child Model

Add savings account properties to existing `Child` model:

```csharp
namespace AllowanceTracker.Models;

public class Child : IHasCreatedAt
{
    // ... existing properties ...
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid FamilyId { get; set; }
    public decimal WeeklyAllowance { get; set; } = 0;
    public decimal CurrentBalance { get; set; } = 0;
    public DateTime? LastAllowanceDate { get; set; }

    /// <summary>
    /// Is savings account feature enabled for this child?
    /// </summary>
    public bool SavingsAccountEnabled { get; set; } = false;

    /// <summary>
    /// Current balance in savings account
    /// </summary>
    public decimal SavingsBalance { get; set; } = 0;

    /// <summary>
    /// Transfer type: None, FixedAmount, or Percentage
    /// </summary>
    public SavingsTransferType SavingsTransferType { get; set; } = SavingsTransferType.None;

    /// <summary>
    /// Fixed dollar amount to transfer (if SavingsTransferType == FixedAmount)
    /// Example: 5.00 means transfer $5 per allowance
    /// </summary>
    public decimal SavingsTransferAmount { get; set; } = 0;

    /// <summary>
    /// Percentage of allowance to transfer (if SavingsTransferType == Percentage)
    /// Example: 20 means transfer 20% of each allowance
    /// Range: 0-100
    /// </summary>
    public int SavingsTransferPercentage { get; set; } = 0;

    // Navigation properties
    public virtual ICollection<SavingsTransaction> SavingsTransactions { get; set; } = new List<SavingsTransaction>();
}
```

### 1.2 SavingsTransferType Enum

```csharp
namespace AllowanceTracker.Models;

/// <summary>
/// Defines how savings transfers are calculated
/// </summary>
public enum SavingsTransferType
{
    /// <summary>
    /// No automatic transfer configured
    /// </summary>
    None = 0,

    /// <summary>
    /// Transfer a fixed dollar amount
    /// Example: Always transfer $5.00 per allowance
    /// </summary>
    FixedAmount = 1,

    /// <summary>
    /// Transfer a percentage of the allowance
    /// Example: Transfer 20% of each allowance
    /// </summary>
    Percentage = 2
}
```

### 1.3 SavingsTransaction Model

```csharp
namespace AllowanceTracker.Models;

/// <summary>
/// Tracks all deposits and withdrawals in savings account
/// Provides complete audit trail
/// </summary>
public class SavingsTransaction
{
    public Guid Id { get; set; }

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    /// <summary>
    /// Amount transferred (positive for deposits, negative for withdrawals)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Type of savings transaction
    /// </summary>
    public SavingsTransactionType Type { get; set; }

    /// <summary>
    /// Description of the transaction
    /// Example: "Auto-transfer from allowance", "Manual deposit", "Withdrawal for purchase"
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Balance in savings account after this transaction
    /// </summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>
    /// Was this an automatic transfer? (vs manual deposit/withdrawal)
    /// </summary>
    public bool IsAutomatic { get; set; } = false;

    /// <summary>
    /// Reference to the allowance transaction that triggered this (if auto-transfer)
    /// </summary>
    public Guid? SourceAllowanceTransactionId { get; set; }

    public Guid CreatedById { get; set; }
    public virtual ApplicationUser CreatedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### 1.4 SavingsTransactionType Enum

```csharp
namespace AllowanceTracker.Models;

/// <summary>
/// Types of savings account transactions
/// </summary>
public enum SavingsTransactionType
{
    /// <summary>
    /// Manual deposit to savings
    /// </summary>
    Deposit = 1,

    /// <summary>
    /// Withdrawal from savings
    /// </summary>
    Withdrawal = 2,

    /// <summary>
    /// Automatic transfer from allowance
    /// </summary>
    AutoTransfer = 3,

    /// <summary>
    /// Interest earned (future enhancement)
    /// </summary>
    Interest = 4
}
```

### 1.5 Database Migration

```bash
dotnet ef migrations add AddSavingsAccount
```

```csharp
public partial class AddSavingsAccount : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add columns to Children table
        migrationBuilder.AddColumn<bool>(
            name: "SavingsAccountEnabled",
            table: "Children",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<decimal>(
            name: "SavingsBalance",
            table: "Children",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<int>(
            name: "SavingsTransferType",
            table: "Children",
            type: "integer",
            nullable: false,
            defaultValue: 0); // None

        migrationBuilder.AddColumn<decimal>(
            name: "SavingsTransferAmount",
            table: "Children",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<int>(
            name: "SavingsTransferPercentage",
            table: "Children",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        // Create SavingsTransactions table
        migrationBuilder.CreateTable(
            name: "SavingsTransactions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                BalanceAfter = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                IsAutomatic = table.Column<bool>(type: "boolean", nullable: false),
                SourceAllowanceTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SavingsTransactions", x => x.Id);
                table.ForeignKey(
                    name: "FK_SavingsTransactions_Children_ChildId",
                    column: x => x.ChildId,
                    principalTable: "Children",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SavingsTransactions_AspNetUsers_CreatedById",
                    column: x => x.CreatedById,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        // Add indexes
        migrationBuilder.CreateIndex(
            name: "IX_SavingsTransactions_ChildId",
            table: "SavingsTransactions",
            column: "ChildId");

        migrationBuilder.CreateIndex(
            name: "IX_SavingsTransactions_CreatedAt",
            table: "SavingsTransactions",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_SavingsTransactions_Type",
            table: "SavingsTransactions",
            column: "Type");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SavingsTransactions");

        migrationBuilder.DropColumn(name: "SavingsAccountEnabled", table: "Children");
        migrationBuilder.DropColumn(name: "SavingsBalance", table: "Children");
        migrationBuilder.DropColumn(name: "SavingsTransferType", table: "Children");
        migrationBuilder.DropColumn(name: "SavingsTransferAmount", table: "Children");
        migrationBuilder.DropColumn(name: "SavingsTransferPercentage", table: "Children");
    }
}
```

### 1.6 Update AllowanceContext

```csharp
namespace AllowanceTracker.Data;

public class AllowanceContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    // ... existing DbSets ...

    public DbSet<SavingsTransaction> SavingsTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ... existing configurations ...

        // Configure SavingsTransaction
        builder.Entity<SavingsTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(e => e.BalanceAfter)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(500);

            entity.HasOne(e => e.Child)
                .WithMany(c => c.SavingsTransactions)
                .HasForeignKey(e => e.ChildId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.ChildId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Type);
        });

        // Update Child configuration
        builder.Entity<Child>(entity =>
        {
            // ... existing config ...

            entity.Property(e => e.SavingsBalance)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m);

            entity.Property(e => e.SavingsTransferAmount)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m);

            entity.Property(e => e.SavingsAccountEnabled)
                .HasDefaultValue(false);

            entity.Property(e => e.SavingsTransferType)
                .HasDefaultValue(SavingsTransferType.None);

            entity.Property(e => e.SavingsTransferPercentage)
                .HasDefaultValue(0);
        });
    }
}
```

---

## Phase 2: Service Layer (TDD)

### 2.1 ISavingsAccountService Interface

```csharp
namespace AllowanceTracker.Services;

public interface ISavingsAccountService
{
    // Configuration
    Task EnableSavingsAccountAsync(Guid childId, SavingsTransferType transferType, decimal amount);
    Task DisableSavingsAccountAsync(Guid childId);
    Task UpdateSavingsConfigAsync(Guid childId, SavingsTransferType transferType, decimal amount);

    // Manual Transactions
    Task<SavingsTransaction> DepositToSavingsAsync(Guid childId, decimal amount, string description, Guid userId);
    Task<SavingsTransaction> WithdrawFromSavingsAsync(Guid childId, decimal amount, string description, Guid userId);

    // Automatic Transfer (called by AllowanceService)
    Task ProcessAutomaticTransferAsync(Guid childId, Guid allowanceTransactionId, decimal allowanceAmount);

    // Query
    Task<decimal> GetSavingsBalanceAsync(Guid childId);
    Task<List<SavingsTransaction>> GetSavingsHistoryAsync(Guid childId, int limit = 50);
    Task<SavingsAccountSummary> GetSummaryAsync(Guid childId);

    // Validation & Calculation
    decimal CalculateTransferAmount(decimal allowanceAmount, SavingsTransferType type, decimal configValue);
    bool ValidateSavingsConfig(SavingsTransferType type, decimal amount);
}
```

### 2.2 Data Transfer Objects

```csharp
namespace AllowanceTracker.DTOs;

public record EnableSavingsAccountRequest(
    Guid ChildId,
    SavingsTransferType TransferType,
    decimal Amount); // Fixed amount OR percentage (0-100)

public record UpdateSavingsConfigRequest(
    Guid ChildId,
    SavingsTransferType TransferType,
    decimal Amount);

public record DepositToSavingsRequest(
    Guid ChildId,
    decimal Amount,
    string Description);

public record WithdrawFromSavingsRequest(
    Guid ChildId,
    decimal Amount,
    string Description);

public record SavingsAccountSummary(
    Guid ChildId,
    bool IsEnabled,
    decimal CurrentBalance,
    SavingsTransferType TransferType,
    decimal TransferAmount,
    int TransferPercentage,
    int TotalTransactions,
    decimal TotalDeposited,
    decimal TotalWithdrawn,
    DateTime? LastTransactionDate,
    string ConfigDescription); // Human-readable: "Saves $5.00 per allowance" or "Saves 20% per allowance"
```

### 2.3 Service Tests (15 tests)

**File**: `AllowanceTracker.Tests/Services/SavingsAccountServiceTests.cs`

```csharp
namespace AllowanceTracker.Tests.Services;

public class SavingsAccountServiceTests : IDisposable
{
    private readonly AllowanceContext _context;
    private readonly ISavingsAccountService _savingsAccountService;
    private readonly Guid _parentUserId = Guid.NewGuid();

    public SavingsAccountServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<AllowanceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AllowanceContext(options);
        _savingsAccountService = new SavingsAccountService(_context, null); // No SignalR in tests
    }

    // Configuration Tests (5)
    [Fact]
    public async Task EnableSavingsAccount_FixedAmount_ConfiguresCorrectly()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 100m);

        // Act
        await _savingsAccountService.EnableSavingsAccountAsync(
            child.Id, SavingsTransferType.FixedAmount, 5m);

        // Assert
        var updated = await _context.Children.FindAsync(child.Id);
        updated!.SavingsAccountEnabled.Should().BeTrue();
        updated.SavingsTransferType.Should().Be(SavingsTransferType.FixedAmount);
        updated.SavingsTransferAmount.Should().Be(5m);
        updated.SavingsTransferPercentage.Should().Be(0);
    }

    [Fact]
    public async Task EnableSavingsAccount_Percentage_ConfiguresCorrectly()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 100m);

        // Act
        await _savingsAccountService.EnableSavingsAccountAsync(
            child.Id, SavingsTransferType.Percentage, 20m);

        // Assert
        var updated = await _context.Children.FindAsync(child.Id);
        updated!.SavingsAccountEnabled.Should().BeTrue();
        updated.SavingsTransferType.Should().Be(SavingsTransferType.Percentage);
        updated.SavingsTransferAmount.Should().Be(0);
        updated.SavingsTransferPercentage.Should().Be(20);
    }

    [Fact]
    public async Task DisableSavingsAccount_DisablesButKeepsBalance()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 100m, savingsBalance: 50m);
        await _savingsAccountService.EnableSavingsAccountAsync(
            child.Id, SavingsTransferType.FixedAmount, 5m);

        // Act
        await _savingsAccountService.DisableSavingsAccountAsync(child.Id);

        // Assert
        var updated = await _context.Children.FindAsync(child.Id);
        updated!.SavingsAccountEnabled.Should().BeFalse();
        updated.SavingsBalance.Should().Be(50m); // Balance preserved
        updated.SavingsTransferType.Should().Be(SavingsTransferType.None);
    }

    [Fact]
    public async Task UpdateSavingsConfig_UpdatesSettings()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 100m);
        await _savingsAccountService.EnableSavingsAccountAsync(
            child.Id, SavingsTransferType.FixedAmount, 5m);

        // Act
        await _savingsAccountService.UpdateSavingsConfigAsync(
            child.Id, SavingsTransferType.Percentage, 25m);

        // Assert
        var updated = await _context.Children.FindAsync(child.Id);
        updated!.SavingsTransferType.Should().Be(SavingsTransferType.Percentage);
        updated.SavingsTransferPercentage.Should().Be(25);
        updated.SavingsTransferAmount.Should().Be(0);
    }

    [Fact]
    public async Task EnableSavingsAccount_InvalidPercentage_ThrowsException()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 100m);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _savingsAccountService.EnableSavingsAccountAsync(
                child.Id, SavingsTransferType.Percentage, 150m));
    }

    // Manual Transaction Tests (4)
    [Fact]
    public async Task DepositToSavings_ValidAmount_IncreasesBalance()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 100m, savingsBalance: 0m);
        await _savingsAccountService.EnableSavingsAccountAsync(
            child.Id, SavingsTransferType.FixedAmount, 5m);

        // Act
        var transaction = await _savingsAccountService.DepositToSavingsAsync(
            child.Id, 25m, "Manual deposit", _parentUserId);

        // Assert
        transaction.Should().NotBeNull();
        transaction.Amount.Should().Be(25m);
        transaction.BalanceAfter.Should().Be(25m);
        transaction.Type.Should().Be(SavingsTransactionType.Deposit);
        transaction.IsAutomatic.Should().BeFalse();

        var updated = await _context.Children.FindAsync(child.Id);
        updated!.SavingsBalance.Should().Be(25m);
        updated.CurrentBalance.Should().Be(75m); // 100 - 25
    }

    [Fact]
    public async Task DepositToSavings_InsufficientBalance_ThrowsException()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 10m, savingsBalance: 0m);
        await _savingsAccountService.EnableSavingsAccountAsync(
            child.Id, SavingsTransferType.FixedAmount, 5m);

        // Act & Assert
        await Assert.ThrowsAsync<InsufficientFundsException>(
            () => _savingsAccountService.DepositToSavingsAsync(
                child.Id, 50m, "Too much", _parentUserId));
    }

    [Fact]
    public async Task WithdrawFromSavings_ValidAmount_DecreasesBalance()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 50m, savingsBalance: 40m);
        await _savingsAccountService.EnableSavingsAccountAsync(
            child.Id, SavingsTransferType.FixedAmount, 5m);

        // Act
        var transaction = await _savingsAccountService.WithdrawFromSavingsAsync(
            child.Id, 15m, "Need money", _parentUserId);

        // Assert
        transaction.Amount.Should().Be(15m);
        transaction.BalanceAfter.Should().Be(25m); // 40 - 15
        transaction.Type.Should().Be(SavingsTransactionType.Withdrawal);

        var updated = await _context.Children.FindAsync(child.Id);
        updated!.SavingsBalance.Should().Be(25m);
        updated.CurrentBalance.Should().Be(65m); // 50 + 15
    }

    [Fact]
    public async Task WithdrawFromSavings_ExceedsSavingsBalance_ThrowsException()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 50m, savingsBalance: 10m);
        await _savingsAccountService.EnableSavingsAccountAsync(
            child.Id, SavingsTransferType.FixedAmount, 5m);

        // Act & Assert
        await Assert.ThrowsAsync<InsufficientFundsException>(
            () => _savingsAccountService.WithdrawFromSavingsAsync(
                child.Id, 20m, "Too much", _parentUserId));
    }

    // Automatic Transfer Tests (4)
    [Fact]
    public async Task ProcessAutomaticTransfer_FixedAmount_TransfersCorrectAmount()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 120m, savingsBalance: 0m);
        await _savingsAccountService.EnableSavingsAccountAsync(
            child.Id, SavingsTransferType.FixedAmount, 5m);
        var allowanceTransactionId = Guid.NewGuid();

        // Act
        await _savingsAccountService.ProcessAutomaticTransferAsync(
            child.Id, allowanceTransactionId, 20m);

        // Assert
        var updated = await _context.Children.FindAsync(child.Id);
        updated!.SavingsBalance.Should().Be(5m);
        updated.CurrentBalance.Should().Be(115m); // 120 - 5

        var transactions = await _context.SavingsTransactions
            .Where(t => t.ChildId == child.Id)
            .ToListAsync();
        transactions.Should().HaveCount(1);
        transactions[0].Amount.Should().Be(5m);
        transactions[0].Type.Should().Be(SavingsTransactionType.AutoTransfer);
        transactions[0].IsAutomatic.Should().BeTrue();
        transactions[0].SourceAllowanceTransactionId.Should().Be(allowanceTransactionId);
    }

    [Fact]
    public async Task ProcessAutomaticTransfer_Percentage_CalculatesCorrectly()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 120m, savingsBalance: 0m);
        await _savingsAccountService.EnableSavingsAccountAsync(
            child.Id, SavingsTransferType.Percentage, 25m);
        var allowanceTransactionId = Guid.NewGuid();

        // Act
        await _savingsAccountService.ProcessAutomaticTransferAsync(
            child.Id, allowanceTransactionId, 20m);

        // Assert
        var updated = await _context.Children.FindAsync(child.Id);
        updated!.SavingsBalance.Should().Be(5m); // 25% of 20
        updated.CurrentBalance.Should().Be(115m); // 120 - 5
    }

    [Fact]
    public async Task ProcessAutomaticTransfer_NotEnabled_DoesNothing()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 120m, savingsBalance: 0m);
        // Not enabled
        var allowanceTransactionId = Guid.NewGuid();

        // Act
        await _savingsAccountService.ProcessAutomaticTransferAsync(
            child.Id, allowanceTransactionId, 20m);

        // Assert
        var updated = await _context.Children.FindAsync(child.Id);
        updated!.SavingsBalance.Should().Be(0m); // No transfer
        updated.CurrentBalance.Should().Be(120m); // Unchanged
    }

    [Fact]
    public async Task ProcessAutomaticTransfer_InsufficientBalance_SkipsTransfer()
    {
        // Arrange
        var child = await CreateChild(weeklyAllowance: 20m, balance: 3m, savingsBalance: 0m);
        await _savingsAccountService.EnableSavingsAccountAsync(
            child.Id, SavingsTransferType.FixedAmount, 10m);
        var allowanceTransactionId = Guid.NewGuid();

        // Act
        await _savingsAccountService.ProcessAutomaticTransferAsync(
            child.Id, allowanceTransactionId, 20m);

        // Assert - Transfer skipped due to insufficient balance
        var updated = await _context.Children.FindAsync(child.Id);
        updated!.SavingsBalance.Should().Be(0m);
        updated.CurrentBalance.Should().Be(3m);
    }

    // Calculation & Validation Tests (2)
    [Fact]
    public void CalculateTransferAmount_FixedAmount_ReturnsFixedValue()
    {
        // Act
        var amount = _savingsAccountService.CalculateTransferAmount(
            allowanceAmount: 20m,
            type: SavingsTransferType.FixedAmount,
            configValue: 5m);

        // Assert
        amount.Should().Be(5m);
    }

    [Fact]
    public void CalculateTransferAmount_Percentage_CalculatesCorrectly()
    {
        // Act
        var amount = _savingsAccountService.CalculateTransferAmount(
            allowanceAmount: 25m,
            type: SavingsTransferType.Percentage,
            configValue: 20m);

        // Assert
        amount.Should().Be(5m); // 20% of 25
    }

    // Helper Methods
    private async Task<Child> CreateChild(
        decimal weeklyAllowance = 10m,
        decimal balance = 0m,
        decimal savingsBalance = 0m)
    {
        var family = new Family
        {
            Id = Guid.NewGuid(),
            Name = "Test Family",
            CreatedAt = DateTime.UtcNow
        };
        _context.Families.Add(family);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testchild@test.com",
            Email = "testchild@test.com",
            FamilyId = family.Id
        };
        _context.Users.Add(user);

        var child = new Child
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FamilyId = family.Id,
            WeeklyAllowance = weeklyAllowance,
            CurrentBalance = balance,
            SavingsBalance = savingsBalance,
            CreatedAt = DateTime.UtcNow
        };
        _context.Children.Add(child);

        await _context.SaveChangesAsync();
        return child;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

### 2.4 SavingsAccountService Implementation

**File**: `src/AllowanceTracker/Services/SavingsAccountService.cs`

```csharp
namespace AllowanceTracker.Services;

public class SavingsAccountService : ISavingsAccountService
{
    private readonly AllowanceContext _context;
    private readonly IHubContext<FamilyHub>? _hubContext;
    private readonly ILogger<SavingsAccountService> _logger;

    public SavingsAccountService(
        AllowanceContext context,
        IHubContext<FamilyHub>? hubContext = null,
        ILogger<SavingsAccountService>? logger = null)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger ?? NullLogger<SavingsAccountService>.Instance;
    }

    public async Task EnableSavingsAccountAsync(Guid childId, SavingsTransferType transferType, decimal amount)
    {
        var child = await _context.Children.FindAsync(childId)
            ?? throw new NotFoundException($"Child with ID {childId} not found");

        // Validate configuration
        if (!ValidateSavingsConfig(transferType, amount))
        {
            throw new ValidationException("Invalid savings configuration");
        }

        child.SavingsAccountEnabled = true;
        child.SavingsTransferType = transferType;

        if (transferType == SavingsTransferType.FixedAmount)
        {
            child.SavingsTransferAmount = amount;
            child.SavingsTransferPercentage = 0;
        }
        else if (transferType == SavingsTransferType.Percentage)
        {
            child.SavingsTransferAmount = 0;
            child.SavingsTransferPercentage = (int)amount;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Enabled savings account for child {ChildId} with {Type}: {Amount}",
            childId, transferType, amount);
    }

    public async Task DisableSavingsAccountAsync(Guid childId)
    {
        var child = await _context.Children.FindAsync(childId)
            ?? throw new NotFoundException($"Child with ID {childId} not found");

        child.SavingsAccountEnabled = false;
        child.SavingsTransferType = SavingsTransferType.None;
        child.SavingsTransferAmount = 0;
        child.SavingsTransferPercentage = 0;
        // Note: SavingsBalance is NOT cleared - money stays in savings

        await _context.SaveChangesAsync();

        _logger.LogInformation("Disabled savings account for child {ChildId}", childId);
    }

    public async Task UpdateSavingsConfigAsync(Guid childId, SavingsTransferType transferType, decimal amount)
    {
        var child = await _context.Children.FindAsync(childId)
            ?? throw new NotFoundException($"Child with ID {childId} not found");

        if (!child.SavingsAccountEnabled)
        {
            throw new InvalidOperationException("Savings account is not enabled");
        }

        if (!ValidateSavingsConfig(transferType, amount))
        {
            throw new ValidationException("Invalid savings configuration");
        }

        child.SavingsTransferType = transferType;

        if (transferType == SavingsTransferType.FixedAmount)
        {
            child.SavingsTransferAmount = amount;
            child.SavingsTransferPercentage = 0;
        }
        else if (transferType == SavingsTransferType.Percentage)
        {
            child.SavingsTransferAmount = 0;
            child.SavingsTransferPercentage = (int)amount;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Updated savings config for child {ChildId} to {Type}: {Amount}",
            childId, transferType, amount);
    }

    public async Task<SavingsTransaction> DepositToSavingsAsync(
        Guid childId,
        decimal amount,
        string description,
        Guid userId)
    {
        if (amount <= 0)
        {
            throw new ValidationException("Deposit amount must be greater than zero");
        }

        var child = await _context.Children
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == childId)
            ?? throw new NotFoundException($"Child with ID {childId} not found");

        if (child.CurrentBalance < amount)
        {
            throw new InsufficientFundsException(
                $"Insufficient balance. Available: {child.CurrentBalance:C}, Needed: {amount:C}");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Deduct from main balance
            child.CurrentBalance -= amount;

            // Add to savings
            child.SavingsBalance += amount;

            // Create transaction record
            var savingsTransaction = new SavingsTransaction
            {
                Id = Guid.NewGuid(),
                ChildId = childId,
                Amount = amount,
                Type = SavingsTransactionType.Deposit,
                Description = description,
                BalanceAfter = child.SavingsBalance,
                IsAutomatic = false,
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.SavingsTransactions.Add(savingsTransaction);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Send real-time update
            await NotifySavingsUpdate(child.FamilyId, childId, child.SavingsBalance);

            _logger.LogInformation(
                "Deposited {Amount} to savings for child {ChildId}",
                amount, childId);

            return savingsTransaction;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<SavingsTransaction> WithdrawFromSavingsAsync(
        Guid childId,
        decimal amount,
        string description,
        Guid userId)
    {
        if (amount <= 0)
        {
            throw new ValidationException("Withdrawal amount must be greater than zero");
        }

        var child = await _context.Children
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == childId)
            ?? throw new NotFoundException($"Child with ID {childId} not found");

        if (child.SavingsBalance < amount)
        {
            throw new InsufficientFundsException(
                $"Insufficient savings balance. Available: {child.SavingsBalance:C}, Needed: {amount:C}");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Remove from savings
            child.SavingsBalance -= amount;

            // Add to main balance
            child.CurrentBalance += amount;

            // Create transaction record
            var savingsTransaction = new SavingsTransaction
            {
                Id = Guid.NewGuid(),
                ChildId = childId,
                Amount = amount,
                Type = SavingsTransactionType.Withdrawal,
                Description = description,
                BalanceAfter = child.SavingsBalance,
                IsAutomatic = false,
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.SavingsTransactions.Add(savingsTransaction);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Send real-time update
            await NotifySavingsUpdate(child.FamilyId, childId, child.SavingsBalance);

            _logger.LogInformation(
                "Withdrew {Amount} from savings for child {ChildId}",
                amount, childId);

            return savingsTransaction;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ProcessAutomaticTransferAsync(
        Guid childId,
        Guid allowanceTransactionId,
        decimal allowanceAmount)
    {
        var child = await _context.Children
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == childId)
            ?? throw new NotFoundException($"Child with ID {childId} not found");

        // Skip if not enabled
        if (!child.SavingsAccountEnabled || child.SavingsTransferType == SavingsTransferType.None)
        {
            return;
        }

        // Calculate transfer amount
        var transferAmount = CalculateTransferAmount(
            allowanceAmount,
            child.SavingsTransferType,
            child.SavingsTransferType == SavingsTransferType.FixedAmount
                ? child.SavingsTransferAmount
                : child.SavingsTransferPercentage);

        // Skip if insufficient balance
        if (child.CurrentBalance < transferAmount || transferAmount <= 0)
        {
            _logger.LogWarning(
                "Skipping auto-transfer for child {ChildId}: insufficient balance {Balance} for transfer {Transfer}",
                childId, child.CurrentBalance, transferAmount);
            return;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Deduct from main balance
            child.CurrentBalance -= transferAmount;

            // Add to savings
            child.SavingsBalance += transferAmount;

            // Create transaction record
            var savingsTransaction = new SavingsTransaction
            {
                Id = Guid.NewGuid(),
                ChildId = childId,
                Amount = transferAmount,
                Type = SavingsTransactionType.AutoTransfer,
                Description = $"Auto-transfer from allowance ({GetConfigDescription(child)})",
                BalanceAfter = child.SavingsBalance,
                IsAutomatic = true,
                SourceAllowanceTransactionId = allowanceTransactionId,
                CreatedById = child.UserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.SavingsTransactions.Add(savingsTransaction);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Send real-time update
            await NotifySavingsUpdate(child.FamilyId, childId, child.SavingsBalance);

            _logger.LogInformation(
                "Auto-transferred {Amount} to savings for child {ChildId}",
                transferAmount, childId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<decimal> GetSavingsBalanceAsync(Guid childId)
    {
        var child = await _context.Children.FindAsync(childId)
            ?? throw new NotFoundException($"Child with ID {childId} not found");

        return child.SavingsBalance;
    }

    public async Task<List<SavingsTransaction>> GetSavingsHistoryAsync(Guid childId, int limit = 50)
    {
        return await _context.SavingsTransactions
            .Where(t => t.ChildId == childId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<SavingsAccountSummary> GetSummaryAsync(Guid childId)
    {
        var child = await _context.Children.FindAsync(childId)
            ?? throw new NotFoundException($"Child with ID {childId} not found");

        var transactions = await _context.SavingsTransactions
            .Where(t => t.ChildId == childId)
            .ToListAsync();

        var totalDeposited = transactions
            .Where(t => t.Type == SavingsTransactionType.Deposit || t.Type == SavingsTransactionType.AutoTransfer)
            .Sum(t => t.Amount);

        var totalWithdrawn = transactions
            .Where(t => t.Type == SavingsTransactionType.Withdrawal)
            .Sum(t => t.Amount);

        var lastTransaction = transactions
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefault();

        return new SavingsAccountSummary(
            childId,
            child.SavingsAccountEnabled,
            child.SavingsBalance,
            child.SavingsTransferType,
            child.SavingsTransferAmount,
            child.SavingsTransferPercentage,
            transactions.Count,
            totalDeposited,
            totalWithdrawn,
            lastTransaction?.CreatedAt,
            GetConfigDescription(child));
    }

    public decimal CalculateTransferAmount(
        decimal allowanceAmount,
        SavingsTransferType type,
        decimal configValue)
    {
        return type switch
        {
            SavingsTransferType.FixedAmount => configValue,
            SavingsTransferType.Percentage => Math.Round((allowanceAmount * configValue) / 100, 2),
            _ => 0
        };
    }

    public bool ValidateSavingsConfig(SavingsTransferType type, decimal amount)
    {
        return type switch
        {
            SavingsTransferType.None => true,
            SavingsTransferType.FixedAmount => amount > 0,
            SavingsTransferType.Percentage => amount >= 0 && amount <= 100,
            _ => false
        };
    }

    private string GetConfigDescription(Child child)
    {
        return child.SavingsTransferType switch
        {
            SavingsTransferType.FixedAmount => $"Saves {child.SavingsTransferAmount:C} per allowance",
            SavingsTransferType.Percentage => $"Saves {child.SavingsTransferPercentage}% per allowance",
            _ => "No automatic transfer"
        };
    }

    private async Task NotifySavingsUpdate(Guid familyId, Guid childId, decimal newBalance)
    {
        if (_hubContext != null)
        {
            await _hubContext.Clients
                .Group($"family-{familyId}")
                .SendAsync("SavingsBalanceUpdated", childId, newBalance);
        }
    }
}
```

### 2.5 Integration with AllowanceService

**Update**: `src/AllowanceTracker/Services/AllowanceService.cs`

```csharp
public class AllowanceService : IAllowanceService
{
    private readonly AllowanceContext _context;
    private readonly ITransactionService _transactionService;
    private readonly ISavingsAccountService _savingsAccountService; // NEW
    private readonly IHubContext<FamilyHub>? _hubContext;
    private readonly ILogger<AllowanceService> _logger;

    public AllowanceService(
        AllowanceContext context,
        ITransactionService transactionService,
        ISavingsAccountService savingsAccountService, // NEW
        IHubContext<FamilyHub>? hubContext,
        ILogger<AllowanceService> logger)
    {
        _context = context;
        _transactionService = transactionService;
        _savingsAccountService = savingsAccountService; // NEW
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task PayWeeklyAllowanceAsync(Guid childId)
    {
        var child = await _context.Children
            .Include(c => c.User)
            .Include(c => c.Family)
            .FirstOrDefaultAsync(c => c.Id == childId)
            ?? throw new NotFoundException($"Child with ID {childId} not found");

        // Check if already paid this week
        if (child.LastAllowanceDate.HasValue)
        {
            var daysSinceLastAllowance = (DateTime.UtcNow - child.LastAllowanceDate.Value).TotalDays;
            if (daysSinceLastAllowance < 7)
            {
                throw new InvalidOperationException($"Allowance already paid this week. Next payment in {Math.Ceiling(7 - daysSinceLastAllowance)} days.");
            }
        }

        // Create allowance transaction
        var dto = new CreateTransactionDto(
            childId,
            child.WeeklyAllowance,
            TransactionType.Credit,
            TransactionCategory.Allowance,
            "Weekly allowance");

        var transaction = await _transactionService.CreateTransactionAsync(dto, child.UserId);

        // Update last allowance date
        child.LastAllowanceDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // NEW: Process automatic savings transfer
        await _savingsAccountService.ProcessAutomaticTransferAsync(
            childId, transaction.Id, child.WeeklyAllowance);

        _logger.LogInformation("Paid weekly allowance of {Amount} to child {ChildId}", child.WeeklyAllowance, childId);
    }

    // ... rest of existing code ...
}
```

### 2.6 Integration Tests (3 tests)

**File**: `AllowanceTracker.Tests/Services/AllowanceServiceIntegrationTests.cs`

```csharp
[Fact]
public async Task PayWeeklyAllowance_WithSavingsEnabled_TransfersToSavings()
{
    // Arrange
    var child = await CreateChild(weeklyAllowance: 20m, balance: 50m);
    await _savingsAccountService.EnableSavingsAccountAsync(
        child.Id, SavingsTransferType.FixedAmount, 5m);

    // Act
    await _allowanceService.PayWeeklyAllowanceAsync(child.Id);

    // Assert
    var updated = await _context.Children.FindAsync(child.Id);
    updated!.CurrentBalance.Should().Be(65m); // 50 + 20 - 5
    updated.SavingsBalance.Should().Be(5m);

    var savingsTransactions = await _context.SavingsTransactions
        .Where(t => t.ChildId == child.Id)
        .ToListAsync();
    savingsTransactions.Should().HaveCount(1);
    savingsTransactions[0].Type.Should().Be(SavingsTransactionType.AutoTransfer);
}

[Fact]
public async Task PayWeeklyAllowance_WithPercentageSavings_CalculatesCorrectly()
{
    // Arrange
    var child = await CreateChild(weeklyAllowance: 25m, balance: 100m);
    await _savingsAccountService.EnableSavingsAccountAsync(
        child.Id, SavingsTransferType.Percentage, 20m);

    // Act
    await _allowanceService.PayWeeklyAllowanceAsync(child.Id);

    // Assert
    var updated = await _context.Children.FindAsync(child.Id);
    updated!.CurrentBalance.Should().Be(120m); // 100 + 25 - 5
    updated.SavingsBalance.Should().Be(5m); // 20% of 25
}

[Fact]
public async Task PayWeeklyAllowance_SavingsNotEnabled_NoTransfer()
{
    // Arrange
    var child = await CreateChild(weeklyAllowance: 20m, balance: 50m);
    // Savings not enabled

    // Act
    await _allowanceService.PayWeeklyAllowanceAsync(child.Id);

    // Assert
    var updated = await _context.Children.FindAsync(child.Id);
    updated!.CurrentBalance.Should().Be(70m); // 50 + 20 (no transfer)
    updated.SavingsBalance.Should().Be(0m);
}
```

---

## Phase 3: API Layer (8 Tests)

### 3.1 SavingsAccountController

**File**: `src/AllowanceTracker/Api/V1/SavingsAccountController.cs`

```csharp
namespace AllowanceTracker.Api.V1;

[ApiController]
[Route("api/v1/children/{childId}/savings")]
[Authorize]
public class SavingsAccountController : ControllerBase
{
    private readonly ISavingsAccountService _savingsAccountService;
    private readonly ICurrentUserService _currentUserService;

    public SavingsAccountController(
        ISavingsAccountService savingsAccountService,
        ICurrentUserService currentUserService)
    {
        _savingsAccountService = savingsAccountService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Enable savings account with auto-transfer configuration
    /// </summary>
    [HttpPost("enable")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EnableSavingsAccount(
        Guid childId,
        [FromBody] EnableSavingsAccountRequest request)
    {
        await _savingsAccountService.EnableSavingsAccountAsync(
            childId, request.TransferType, request.Amount);

        return Ok(new { message = "Savings account enabled successfully" });
    }

    /// <summary>
    /// Disable savings account (balance is preserved)
    /// </summary>
    [HttpPost("disable")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DisableSavingsAccount(Guid childId)
    {
        await _savingsAccountService.DisableSavingsAccountAsync(childId);
        return Ok(new { message = "Savings account disabled successfully" });
    }

    /// <summary>
    /// Update savings transfer configuration
    /// </summary>
    [HttpPut("config")]
    [Authorize(Roles = "Parent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateConfig(
        Guid childId,
        [FromBody] UpdateSavingsConfigRequest request)
    {
        await _savingsAccountService.UpdateSavingsConfigAsync(
            childId, request.TransferType, request.Amount);

        return Ok(new { message = "Savings configuration updated successfully" });
    }

    /// <summary>
    /// Manually deposit money to savings
    /// </summary>
    [HttpPost("deposit")]
    [ProducesResponseType(typeof(SavingsTransaction), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SavingsTransaction>> Deposit(
        Guid childId,
        [FromBody] DepositToSavingsRequest request)
    {
        var userId = _currentUserService.GetUserId();
        var transaction = await _savingsAccountService.DepositToSavingsAsync(
            childId, request.Amount, request.Description, userId);

        return Ok(transaction);
    }

    /// <summary>
    /// Withdraw money from savings
    /// </summary>
    [HttpPost("withdraw")]
    [ProducesResponseType(typeof(SavingsTransaction), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SavingsTransaction>> Withdraw(
        Guid childId,
        [FromBody] WithdrawFromSavingsRequest request)
    {
        var userId = _currentUserService.GetUserId();
        var transaction = await _savingsAccountService.WithdrawFromSavingsAsync(
            childId, request.Amount, request.Description, userId);

        return Ok(transaction);
    }

    /// <summary>
    /// Get current savings balance
    /// </summary>
    [HttpGet("balance")]
    [ProducesResponseType(typeof(BalanceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BalanceResponse>> GetBalance(Guid childId)
    {
        var balance = await _savingsAccountService.GetSavingsBalanceAsync(childId);
        return Ok(new BalanceResponse(balance));
    }

    /// <summary>
    /// Get savings transaction history
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(List<SavingsTransaction>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SavingsTransaction>>> GetHistory(
        Guid childId,
        [FromQuery] int limit = 50)
    {
        var history = await _savingsAccountService.GetSavingsHistoryAsync(childId, limit);
        return Ok(history);
    }

    /// <summary>
    /// Get complete savings account summary
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(SavingsAccountSummary), StatusCodes.Status200OK)]
    public async Task<ActionResult<SavingsAccountSummary>> GetSummary(Guid childId)
    {
        var summary = await _savingsAccountService.GetSummaryAsync(childId);
        return Ok(summary);
    }
}

public record BalanceResponse(decimal Balance);
```

### 3.2 Controller Tests (8 tests)

**File**: `AllowanceTracker.Tests/Api/SavingsAccountControllerTests.cs`

```csharp
namespace AllowanceTracker.Tests.Api;

public class SavingsAccountControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SavingsAccountControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task EnableSavingsAccount_ValidRequest_Returns200()
    {
        // Arrange
        var child = await CreateChild();
        var request = new EnableSavingsAccountRequest(
            child.Id, SavingsTransferType.FixedAmount, 5m);
        var token = await GetParentToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/children/{child.Id}/savings/enable", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EnableSavingsAccount_ChildRole_Returns403()
    {
        // Arrange
        var child = await CreateChild();
        var request = new EnableSavingsAccountRequest(
            child.Id, SavingsTransferType.FixedAmount, 5m);
        var token = await GetChildToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/children/{child.Id}/savings/enable", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Deposit_ValidAmount_ReturnsTransaction()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        await EnableSavings(child.Id, SavingsTransferType.FixedAmount, 5m);
        var request = new DepositToSavingsRequest(child.Id, 25m, "Manual deposit");
        var token = await GetParentToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/children/{child.Id}/savings/deposit", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var transaction = await response.Content.ReadFromJsonAsync<SavingsTransaction>();
        transaction.Should().NotBeNull();
        transaction!.Amount.Should().Be(25m);
        transaction.Type.Should().Be(SavingsTransactionType.Deposit);
    }

    [Fact]
    public async Task Withdraw_ValidAmount_ReturnsTransaction()
    {
        // Arrange
        var child = await CreateChild(balance: 50m, savingsBalance: 30m);
        var request = new WithdrawFromSavingsRequest(child.Id, 10m, "Need money");
        var token = await GetParentToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/children/{child.Id}/savings/withdraw", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var transaction = await response.Content.ReadFromJsonAsync<SavingsTransaction>();
        transaction.Should().NotBeNull();
        transaction!.Amount.Should().Be(10m);
        transaction.Type.Should().Be(SavingsTransactionType.Withdrawal);
    }

    [Fact]
    public async Task GetBalance_ReturnsCurrentSavingsBalance()
    {
        // Arrange
        var child = await CreateChild(balance: 100m, savingsBalance: 45m);
        var token = await GetParentToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/v1/children/{child.Id}/savings/balance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BalanceResponse>();
        result.Should().NotBeNull();
        result!.Balance.Should().Be(45m);
    }

    [Fact]
    public async Task GetHistory_ReturnsTransactionList()
    {
        // Arrange
        var child = await CreateChild(balance: 100m);
        await EnableSavings(child.Id, SavingsTransferType.FixedAmount, 5m);
        await DepositToSavings(child.Id, 20m, "First deposit");
        await DepositToSavings(child.Id, 15m, "Second deposit");
        var token = await GetParentToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/v1/children/{child.Id}/savings/history?limit=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await response.Content.ReadFromJsonAsync<List<SavingsTransaction>>();
        history.Should().NotBeNull();
        history!.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSummary_ReturnsCompleteSummary()
    {
        // Arrange
        var child = await CreateChild(balance: 100m, savingsBalance: 50m);
        await EnableSavings(child.Id, SavingsTransferType.Percentage, 20m);
        var token = await GetParentToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/v1/children/{child.Id}/savings/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<SavingsAccountSummary>();
        summary.Should().NotBeNull();
        summary!.IsEnabled.Should().BeTrue();
        summary.CurrentBalance.Should().Be(50m);
        summary.TransferType.Should().Be(SavingsTransferType.Percentage);
        summary.TransferPercentage.Should().Be(20);
    }

    [Fact]
    public async Task DisableSavingsAccount_PreservesBalance()
    {
        // Arrange
        var child = await CreateChild(balance: 100m, savingsBalance: 50m);
        await EnableSavings(child.Id, SavingsTransferType.FixedAmount, 5m);
        var token = await GetParentToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsync(
            $"/api/v1/children/{child.Id}/savings/disable", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify balance preserved
        var balanceResponse = await _client.GetAsync($"/api/v1/children/{child.Id}/savings/balance");
        var balance = await balanceResponse.Content.ReadFromJsonAsync<BalanceResponse>();
        balance!.Balance.Should().Be(50m); // Balance still there
    }

    // Helper methods...
}
```

---

## Phase 4: Blazor UI Components (10 Tests)

### 4.1 SavingsAccountCard Component

**File**: `src/AllowanceTracker/Shared/Components/SavingsAccountCard.razor`

```razor
@inject ISavingsAccountService SavingsAccountService

<div class="card savings-account-card">
    <div class="card-header bg-success text-white">
        <div class="d-flex justify-content-between align-items-center">
            <h5 class="mb-0">
                <span class="oi oi-piggy-bank me-2"></span>
                Savings Account
            </h5>
            @if (Summary?.IsEnabled == true)
            {
                <span class="badge bg-light text-success">Active</span>
            }
        </div>
    </div>

    <div class="card-body">
        @if (Loading)
        {
            <div class="text-center py-3">
                <div class="spinner-border spinner-border-sm" role="status"></div>
            </div>
        }
        else if (Summary != null)
        {
            <div class="savings-balance mb-3">
                <div class="text-muted small">Current Savings</div>
                <h2 class="mb-0 text-success">@Summary.CurrentBalance.ToString("C")</h2>
            </div>

            @if (Summary.IsEnabled)
            {
                <div class="alert alert-info py-2 mb-3">
                    <small>
                        <span class="oi oi-loop-circular me-1"></span>
                        @Summary.ConfigDescription
                    </small>
                </div>
            }

            <div class="row g-2 mb-3">
                <div class="col-6">
                    <div class="stat-box">
                        <div class="stat-label">Total Saved</div>
                        <div class="stat-value">@Summary.TotalDeposited.ToString("C")</div>
                    </div>
                </div>
                <div class="col-6">
                    <div class="stat-box">
                        <div class="stat-label">Transactions</div>
                        <div class="stat-value">@Summary.TotalTransactions</div>
                    </div>
                </div>
            </div>

            <div class="btn-group w-100" role="group">
                <button class="btn btn-success btn-sm" @onclick="OnDepositClick">
                    <span class="oi oi-plus"></span> Deposit
                </button>
                @if (Summary.CurrentBalance > 0)
                {
                    <button class="btn btn-warning btn-sm" @onclick="OnWithdrawClick">
                        <span class="oi oi-minus"></span> Withdraw
                    </button>
                }
                <button class="btn btn-info btn-sm" @onclick="OnHistoryClick">
                    <span class="oi oi-list"></span> History
                </button>
            </div>

            @if (IsParent && !Summary.IsEnabled)
            {
                <button class="btn btn-outline-success btn-sm w-100 mt-2" @onclick="OnEnableClick">
                    <span class="oi oi-cog"></span> Enable Savings
                </button>
            }
            @if (IsParent && Summary.IsEnabled)
            {
                <button class="btn btn-outline-secondary btn-sm w-100 mt-2" @onclick="OnConfigClick">
                    <span class="oi oi-cog"></span> Configure
                </button>
            }
        }
    </div>
</div>

@code {
    [Parameter] public Guid ChildId { get; set; }
    [Parameter] public bool IsParent { get; set; } = false;
    [Parameter] public EventCallback OnDepositRequested { get; set; }
    [Parameter] public EventCallback OnWithdrawRequested { get; set; }
    [Parameter] public EventCallback OnConfigureRequested { get; set; }
    [Parameter] public EventCallback OnHistoryRequested { get; set; }

    private SavingsAccountSummary? Summary;
    private bool Loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadSummary();
    }

    public async Task LoadSummary()
    {
        Loading = true;
        try
        {
            Summary = await SavingsAccountService.GetSummaryAsync(ChildId);
        }
        finally
        {
            Loading = false;
        }
    }

    private async Task OnDepositClick() => await OnDepositRequested.InvokeAsync();
    private async Task OnWithdrawClick() => await OnWithdrawRequested.InvokeAsync();
    private async Task OnHistoryClick() => await OnHistoryRequested.InvokeAsync();
    private async Task OnEnableClick() => await OnConfigureRequested.InvokeAsync();
    private async Task OnConfigClick() => await OnConfigureRequested.InvokeAsync();
}
```

### 4.2 SavingsConfigForm Component

**File**: `src/AllowanceTracker/Shared/Components/SavingsConfigForm.razor`

```razor
@inject ISavingsAccountService SavingsAccountService

<div class="modal" tabindex="-1" style="display: block; background-color: rgba(0,0,0,0.5);">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">Configure Savings Account</h5>
                <button type="button" class="btn-close" @onclick="OnCancel"></button>
            </div>

            <div class="modal-body">
                <EditForm Model="@formModel" OnValidSubmit="@HandleSubmit">
                    <DataAnnotationsValidator />
                    <ValidationSummary />

                    <div class="mb-3">
                        <label class="form-label">Transfer Type</label>
                        <InputSelect @bind-Value="formModel.TransferType" class="form-select">
                            <option value="@SavingsTransferType.None">No Automatic Transfer</option>
                            <option value="@SavingsTransferType.FixedAmount">Fixed Dollar Amount</option>
                            <option value="@SavingsTransferType.Percentage">Percentage of Allowance</option>
                        </InputSelect>
                    </div>

                    @if (formModel.TransferType == SavingsTransferType.FixedAmount)
                    {
                        <div class="mb-3">
                            <label class="form-label">Transfer Amount</label>
                            <div class="input-group">
                                <span class="input-group-text">$</span>
                                <InputNumber @bind-Value="formModel.Amount" class="form-control" step="0.01" />
                            </div>
                            <small class="text-muted">
                                This amount will be transferred to savings every time allowance is paid
                            </small>
                        </div>
                    }
                    else if (formModel.TransferType == SavingsTransferType.Percentage)
                    {
                        <div class="mb-3">
                            <label class="form-label">Transfer Percentage</label>
                            <div class="input-group">
                                <InputNumber @bind-Value="formModel.Percentage" class="form-control" min="0" max="100" />
                                <span class="input-group-text">%</span>
                            </div>
                            <small class="text-muted">
                                This percentage of allowance will be transferred to savings
                            </small>
                        </div>
                    }

                    <div class="d-flex gap-2">
                        <button type="submit" class="btn btn-primary" disabled="@submitting">
                            @if (submitting)
                            {
                                <span class="spinner-border spinner-border-sm me-2"></span>
                            }
                            Save Configuration
                        </button>
                        <button type="button" class="btn btn-secondary" @onclick="OnCancel">
                            Cancel
                        </button>
                    </div>
                </EditForm>
            </div>
        </div>
    </div>
</div>

@code {
    [Parameter] public Guid ChildId { get; set; }
    [Parameter] public EventCallback OnConfigured { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    private ConfigFormModel formModel = new();
    private bool submitting = false;

    private async Task HandleSubmit()
    {
        submitting = true;
        try
        {
            var amount = formModel.TransferType == SavingsTransferType.Percentage
                ? formModel.Percentage
                : formModel.Amount;

            if (!await IsAccountEnabled())
            {
                await SavingsAccountService.EnableSavingsAccountAsync(
                    ChildId, formModel.TransferType, amount);
            }
            else
            {
                await SavingsAccountService.UpdateSavingsConfigAsync(
                    ChildId, formModel.TransferType, amount);
            }

            await OnConfigured.InvokeAsync();
        }
        finally
        {
            submitting = false;
        }
    }

    private async Task<bool> IsAccountEnabled()
    {
        var summary = await SavingsAccountService.GetSummaryAsync(ChildId);
        return summary.IsEnabled;
    }

    private async Task OnCancel()
    {
        await OnCancelled.InvokeAsync();
    }

    private class ConfigFormModel
    {
        public SavingsTransferType TransferType { get; set; } = SavingsTransferType.None;

        [Range(0.01, 10000)]
        public decimal Amount { get; set; } = 5m;

        [Range(0, 100)]
        public int Percentage { get; set; } = 20;
    }
}
```

### 4.3 SavingsHistoryTable Component

**File**: `src/AllowanceTracker/Shared/Components/SavingsHistoryTable.razor`

```razor
@inject ISavingsAccountService SavingsAccountService

<div class="savings-history">
    <h5 class="mb-3">Savings History</h5>

    @if (Loading)
    {
        <div class="text-center py-3">
            <div class="spinner-border" role="status"></div>
        </div>
    }
    else if (!Transactions.Any())
    {
        <div class="alert alert-info">
            No savings transactions yet. Start saving to see your history!
        </div>
    }
    else
    {
        <div class="table-responsive">
            <table class="table table-hover">
                <thead>
                    <tr>
                        <th>Date</th>
                        <th>Type</th>
                        <th>Description</th>
                        <th class="text-end">Amount</th>
                        <th class="text-end">Balance</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var transaction in Transactions)
                    {
                        <tr>
                            <td>@transaction.CreatedAt.ToLocalTime().ToString("MMM dd, yyyy")</td>
                            <td>
                                <span class="badge @GetTypeBadgeClass(transaction.Type)">
                                    @GetTypeDisplay(transaction.Type)
                                </span>
                            </td>
                            <td>
                                @transaction.Description
                                @if (transaction.IsAutomatic)
                                {
                                    <span class="badge bg-secondary ms-1">Auto</span>
                                }
                            </td>
                            <td class="text-end @GetAmountClass(transaction.Type)">
                                @GetAmountDisplay(transaction)
                            </td>
                            <td class="text-end">@transaction.BalanceAfter.ToString("C")</td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    }
</div>

@code {
    [Parameter] public Guid ChildId { get; set; }
    [Parameter] public int Limit { get; set; } = 50;

    private List<SavingsTransaction> Transactions = new();
    private bool Loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadHistory();
    }

    public async Task LoadHistory()
    {
        Loading = true;
        try
        {
            Transactions = await SavingsAccountService.GetSavingsHistoryAsync(ChildId, Limit);
        }
        finally
        {
            Loading = false;
        }
    }

    private string GetTypeBadgeClass(SavingsTransactionType type) => type switch
    {
        SavingsTransactionType.Deposit => "bg-success",
        SavingsTransactionType.Withdrawal => "bg-warning",
        SavingsTransactionType.AutoTransfer => "bg-info",
        SavingsTransactionType.Interest => "bg-primary",
        _ => "bg-secondary"
    };

    private string GetTypeDisplay(SavingsTransactionType type) => type switch
    {
        SavingsTransactionType.Deposit => "Deposit",
        SavingsTransactionType.Withdrawal => "Withdrawal",
        SavingsTransactionType.AutoTransfer => "Auto Transfer",
        SavingsTransactionType.Interest => "Interest",
        _ => type.ToString()
    };

    private string GetAmountClass(SavingsTransactionType type) => type switch
    {
        SavingsTransactionType.Withdrawal => "text-danger",
        _ => "text-success"
    };

    private string GetAmountDisplay(SavingsTransaction transaction)
    {
        var sign = transaction.Type == SavingsTransactionType.Withdrawal ? "-" : "+";
        return $"{sign}{transaction.Amount:C}";
    }
}
```

### 4.4 Blazor Component Tests (10 tests)

**File**: `AllowanceTracker.Tests/Components/SavingsAccountCardTests.cs`

```csharp
using Bunit;
using Xunit;

namespace AllowanceTracker.Tests.Components;

public class SavingsAccountCardTests : TestContext
{
    [Fact]
    public void SavingsAccountCard_DisplaysCurrentBalance()
    {
        // Arrange
        var mockService = new Mock<ISavingsAccountService>();
        mockService.Setup(s => s.GetSummaryAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new SavingsAccountSummary(
                Guid.NewGuid(),
                IsEnabled: true,
                CurrentBalance: 125.50m,
                SavingsTransferType.FixedAmount,
                TransferAmount: 5m,
                TransferPercentage: 0,
                TotalTransactions: 10,
                TotalDeposited: 150m,
                TotalWithdrawn: 24.50m,
                LastTransactionDate: DateTime.UtcNow,
                ConfigDescription: "Saves $5.00 per allowance"));

        Services.AddSingleton(mockService.Object);

        // Act
        var component = RenderComponent<SavingsAccountCard>(parameters => parameters
            .Add(p => p.ChildId, Guid.NewGuid()));

        // Assert
        component.Find(".savings-balance h2").TextContent.Should().Contain("$125.50");
    }

    [Fact]
    public void SavingsAccountCard_ShowsActiveWhenEnabled()
    {
        // Arrange
        var mockService = new Mock<ISavingsAccountService>();
        mockService.Setup(s => s.GetSummaryAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new SavingsAccountSummary(
                Guid.NewGuid(),
                IsEnabled: true,
                CurrentBalance: 100m,
                SavingsTransferType.Percentage,
                TransferAmount: 0,
                TransferPercentage: 20,
                TotalTransactions: 5,
                TotalDeposited: 100m,
                TotalWithdrawn: 0,
                LastTransactionDate: null,
                ConfigDescription: "Saves 20% per allowance"));

        Services.AddSingleton(mockService.Object);

        // Act
        var component = RenderComponent<SavingsAccountCard>(parameters => parameters
            .Add(p => p.ChildId, Guid.NewGuid()));

        // Assert
        component.Find(".badge").TextContent.Should().Contain("Active");
    }

    // ... 8 more component tests ...
}
```

---

## Phase 5: Registration & Deployment

### 5.1 Register Services in Program.cs

```csharp
// Add to Program.cs

builder.Services.AddScoped<ISavingsAccountService, SavingsAccountService>();
```

### 5.2 Run Migration

```bash
dotnet ef migrations add AddSavingsAccount
dotnet ef database update
```

---

## Success Metrics

- ✅ **38 total tests passing** (15 service + 3 integration + 8 API + 10 UI + 2 model)
- ✅ Savings account can be enabled/disabled
- ✅ Fixed amount transfers work correctly
- ✅ Percentage-based transfers calculate accurately
- ✅ Manual deposits reduce main balance, increase savings
- ✅ Withdrawals increase main balance, reduce savings
- ✅ Automatic transfers process on allowance payment
- ✅ Transaction history maintained with full audit trail
- ✅ Real-time SignalR updates notify family of changes
- ✅ API endpoints secured with proper authorization
- ✅ Blazor UI components display savings status
- ✅ Parents can configure, children can view

---

## Business Rules

1. **Configuration**:
   - Only parents can enable/disable/configure savings accounts
   - Children can view their savings but not configure
   - Fixed amount must be > $0
   - Percentage must be 0-100

2. **Transfers**:
   - Automatic transfers only happen on allowance payment
   - If insufficient balance, transfer is skipped (no error)
   - Transfers are atomic (all-or-nothing database transactions)

3. **Manual Transactions**:
   - Deposits require sufficient main balance
   - Withdrawals require sufficient savings balance
   - Both deposits and withdrawals create audit records

4. **Balance Integrity**:
   - Total money = CurrentBalance + SavingsBalance
   - Cannot have negative balances
   - All money movements are tracked

5. **Disabling**:
   - Disabling savings stops auto-transfers
   - Savings balance is preserved
   - Money remains accessible via withdrawal

---

## Future Enhancements

1. **Interest Simulation**: Teach compound interest with virtual interest payments
2. **Savings Goals Integration**: Link savings account to specific goals (spec 14)
3. **Savings Challenges**: Time-limited savings challenges with rewards
4. **Matching Contributions**: Parents match X% of child's savings
5. **Multiple Savings Accounts**: Create separate accounts for different purposes
6. **Savings Milestones**: Celebrate reaching certain savings amounts
7. **Export History**: Download savings transaction history as CSV/PDF
8. **Savings Charts**: Visualize savings growth over time

---

## Differences from Spec 14 (Savings Goals)

| Feature | Spec 37 (This) | Spec 14 (Goals) |
|---------|---------------|-----------------|
| **Purpose** | General savings account | Goal-specific with milestones |
| **Configuration** | Single transfer rule | Per-goal auto-transfer percentages |
| **Complexity** | Simple (one account) | Complex (multiple goals) |
| **Milestones** | No milestones | 25%, 50%, 75%, 100% |
| **Focus** | "Pay yourself first" | "Save for specific items" |
| **UI** | Single card | Multiple goal cards |
| **Database** | 2 tables | 4 tables |
| **Tests** | 38 tests | 30 tests |

---

## Implementation Timeline

**Total Estimated Time**: 8 working days

| Day | Phase | Tasks | Tests |
|-----|-------|-------|-------|
| 1 | Database | Models, enums, migration, DbContext | 2 |
| 2-3 | Service Layer | ISavingsAccountService, implementation | 15 |
| 4 | Integration | Update AllowanceService | 3 |
| 5 | API | Controller, DTOs, endpoints | 8 |
| 6-7 | UI | Components, modals, forms | 10 |
| 8 | Polish | Styling, documentation, testing | - |

---

**Ready to implement following strict TDD methodology: RED → GREEN → REFACTOR**

**Test count by layer:**
- Model Tests: 2
- Service Tests: 15
- Integration Tests: 3
- API Tests: 8
- UI Tests: 10
- **Total: 38 tests**
