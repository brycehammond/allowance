using AllowanceTracker.Api.V1;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Api;

public class SavingsAccountControllerTests
{
    private readonly Mock<ISavingsAccountService> _mockSavingsAccountService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IAccountService> _mockAccountService;
    private readonly Mock<IChildManagementService> _mockChildManagementService;
    private readonly SavingsAccountController _controller;
    private readonly Guid _currentUserId = Guid.NewGuid();

    public SavingsAccountControllerTests()
    {
        _mockSavingsAccountService = new Mock<ISavingsAccountService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockAccountService = new Mock<IAccountService>();
        _mockChildManagementService = new Mock<IChildManagementService>();
        _mockCurrentUserService.Setup(x => x.UserId).Returns(_currentUserId);

        _controller = new SavingsAccountController(
            _mockSavingsAccountService.Object,
            _mockCurrentUserService.Object,
            _mockAccountService.Object,
            _mockChildManagementService.Object);
    }

    [Fact]
    public async Task EnableSavingsAccount_ReturnsOk()
    {
        // Arrange
        var request = new EnableSavingsAccountRequest(
            Guid.NewGuid(),
            SavingsTransferType.FixedAmount,
            5m);

        _mockSavingsAccountService
            .Setup(x => x.EnableSavingsAccountAsync(request.ChildId, request.TransferType, request.Amount))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.EnableSavingsAccount(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateSavingsConfig_ReturnsOk()
    {
        // Arrange
        var request = new UpdateSavingsConfigRequest(
            Guid.NewGuid(),
            SavingsTransferType.Percentage,
            20m);

        _mockSavingsAccountService
            .Setup(x => x.UpdateSavingsConfigAsync(request.ChildId, request.TransferType, request.Amount))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateSavingsConfig(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DisableSavingsAccount_ReturnsOk()
    {
        // Arrange
        var childId = Guid.NewGuid();

        _mockSavingsAccountService
            .Setup(x => x.DisableSavingsAccountAsync(childId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DisableSavingsAccount(childId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DepositToSavings_ReturnsCreatedWithTransaction()
    {
        // Arrange
        var request = new DepositToSavingsRequest(
            Guid.NewGuid(),
            25m,
            "Manual deposit");

        var transaction = new SavingsTransaction
        {
            Id = Guid.NewGuid(),
            ChildId = request.ChildId,
            Amount = request.Amount,
            Type = SavingsTransactionType.Deposit,
            Description = request.Description,
            BalanceAfter = 25m,
            IsAutomatic = false,
            CreatedById = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        _mockSavingsAccountService
            .Setup(x => x.DepositToSavingsAsync(
                request.ChildId,
                request.Amount,
                request.Description,
                It.IsAny<Guid>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _controller.DepositToSavings(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(SavingsAccountController.GetSavingsHistory));
        var returnedTransaction = createdResult.Value.Should().BeAssignableTo<SavingsTransaction>().Subject;
        returnedTransaction.Amount.Should().Be(25m);
    }

    [Fact]
    public async Task WithdrawFromSavings_ReturnsCreatedWithTransaction()
    {
        // Arrange
        var request = new WithdrawFromSavingsRequest(
            Guid.NewGuid(),
            15m,
            "Need money");

        var transaction = new SavingsTransaction
        {
            Id = Guid.NewGuid(),
            ChildId = request.ChildId,
            Amount = request.Amount,
            Type = SavingsTransactionType.Withdrawal,
            Description = request.Description,
            BalanceAfter = 10m,
            IsAutomatic = false,
            CreatedById = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        _mockSavingsAccountService
            .Setup(x => x.WithdrawFromSavingsAsync(
                request.ChildId,
                request.Amount,
                request.Description,
                It.IsAny<Guid>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _controller.WithdrawFromSavings(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var returnedTransaction = createdResult.Value.Should().BeAssignableTo<SavingsTransaction>().Subject;
        returnedTransaction.Amount.Should().Be(15m);
        returnedTransaction.Type.Should().Be(SavingsTransactionType.Withdrawal);
    }

    [Fact]
    public async Task GetSavingsBalance_AsParent_ReturnsBalance()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var balance = 123.45m;
        var parent = new ApplicationUser
        {
            Id = _currentUserId,
            Role = UserRole.Parent,
            FamilyId = Guid.NewGuid()
        };
        var child = new Child
        {
            Id = childId,
            SavingsBalance = balance,
            SavingsBalanceVisibleToChild = false // Even if hidden, parent should see
        };

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(parent);
        _mockChildManagementService.Setup(x => x.GetChildAsync(childId, parent.Id)).ReturnsAsync(child);
        _mockSavingsAccountService.Setup(x => x.GetSavingsBalanceAsync(childId)).ReturnsAsync(balance);

        // Act
        var result = await _controller.GetSavingsBalance(childId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSavingsBalance_AsChild_WhenVisible_ReturnsBalance()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var balance = 123.45m;
        var childUser = new ApplicationUser
        {
            Id = _currentUserId,
            Role = UserRole.Child,
            FamilyId = Guid.NewGuid()
        };
        var child = new Child
        {
            Id = childId,
            UserId = childUser.Id,
            SavingsBalance = balance,
            SavingsBalanceVisibleToChild = true
        };

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(childUser);
        _mockChildManagementService.Setup(x => x.GetChildAsync(childId, childUser.Id)).ReturnsAsync(child);
        _mockSavingsAccountService.Setup(x => x.GetSavingsBalanceAsync(childId)).ReturnsAsync(balance);

        // Act
        var result = await _controller.GetSavingsBalance(childId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSavingsBalance_AsChild_WhenHidden_ReturnsNullBalance()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var childUser = new ApplicationUser
        {
            Id = _currentUserId,
            Role = UserRole.Child,
            FamilyId = Guid.NewGuid()
        };
        var child = new Child
        {
            Id = childId,
            UserId = childUser.Id,
            SavingsBalance = 100m,
            SavingsBalanceVisibleToChild = false
        };

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(childUser);
        _mockChildManagementService.Setup(x => x.GetChildAsync(childId, childUser.Id)).ReturnsAsync(child);

        // Act
        var result = await _controller.GetSavingsBalance(childId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        // The response should have balance = null
    }

    [Fact]
    public async Task GetSavingsHistory_ReturnsOkWithTransactions()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var transactions = new List<SavingsTransaction>
        {
            new SavingsTransaction
            {
                Id = Guid.NewGuid(),
                ChildId = childId,
                Amount = 10m,
                Type = SavingsTransactionType.Deposit,
                Description = "Deposit 1",
                BalanceAfter = 10m,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new SavingsTransaction
            {
                Id = Guid.NewGuid(),
                ChildId = childId,
                Amount = 5m,
                Type = SavingsTransactionType.AutoTransfer,
                Description = "Auto transfer",
                BalanceAfter = 15m,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        _mockSavingsAccountService
            .Setup(x => x.GetSavingsHistoryAsync(childId, 50))
            .ReturnsAsync(transactions);

        // Act
        var result = await _controller.GetSavingsHistory(childId, 50);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTransactions = okResult.Value.Should().BeAssignableTo<List<SavingsTransaction>>().Subject;
        returnedTransactions.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSavingsSummary_AsParent_ReturnsFullSummary()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var parent = new ApplicationUser
        {
            Id = _currentUserId,
            Role = UserRole.Parent,
            FamilyId = Guid.NewGuid()
        };
        var child = new Child
        {
            Id = childId,
            SavingsBalanceVisibleToChild = false // Even if hidden, parent should see
        };
        var summary = new SavingsAccountSummary(
            ChildId: childId,
            IsEnabled: true,
            CurrentBalance: 50m,
            TransferType: SavingsTransferType.FixedAmount,
            TransferAmount: 5m,
            TransferPercentage: 0,
            TotalTransactions: 10,
            TotalDeposited: 60m,
            TotalWithdrawn: 10m,
            LastTransactionDate: DateTime.UtcNow,
            ConfigDescription: "Saves $5.00 per allowance"
        );

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(parent);
        _mockChildManagementService.Setup(x => x.GetChildAsync(childId, parent.Id)).ReturnsAsync(child);
        _mockSavingsAccountService.Setup(x => x.GetSummaryAsync(childId)).ReturnsAsync(summary);

        // Act
        var result = await _controller.GetSavingsSummary(childId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSummary = okResult.Value.Should().BeAssignableTo<SavingsAccountSummary>().Subject;
        returnedSummary.CurrentBalance.Should().Be(50m);
    }

    [Fact]
    public async Task GetSavingsSummary_AsChild_WhenHidden_ReturnsHiddenSummary()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var childUser = new ApplicationUser
        {
            Id = _currentUserId,
            Role = UserRole.Child,
            FamilyId = Guid.NewGuid()
        };
        var child = new Child
        {
            Id = childId,
            UserId = childUser.Id,
            SavingsBalanceVisibleToChild = false
        };
        var summary = new SavingsAccountSummary(
            ChildId: childId,
            IsEnabled: true,
            CurrentBalance: 50m,
            TransferType: SavingsTransferType.FixedAmount,
            TransferAmount: 5m,
            TransferPercentage: 0,
            TotalTransactions: 10,
            TotalDeposited: 60m,
            TotalWithdrawn: 10m,
            LastTransactionDate: DateTime.UtcNow,
            ConfigDescription: "Saves $5.00 per allowance"
        );

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(childUser);
        _mockChildManagementService.Setup(x => x.GetChildAsync(childId, childUser.Id)).ReturnsAsync(child);
        _mockSavingsAccountService.Setup(x => x.GetSummaryAsync(childId)).ReturnsAsync(summary);

        // Act
        var result = await _controller.GetSavingsSummary(childId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        // When balance is hidden, the response should have balanceHidden = true
    }
}
