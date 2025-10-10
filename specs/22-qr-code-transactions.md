# QR Code Transactions - Quick Payment System

## Overview
QR code-based payment system enabling parents to quickly add money to children's accounts by scanning a unique QR code. Each child has a personal QR code that can be scanned using a mobile device camera, eliminating the need to navigate through menus for simple transactions.

## Core Philosophy: Test-First Development
**Every feature starts with a failing test**. Follow strict TDD methodology for all QR code functionality.

## Technology Stack

### Core Dependencies
```xml
<ItemGroup>
  <!-- QR Code Generation -->
  <PackageReference Include="QRCoder" Version="1.4.3" />

  <!-- Image Processing -->
  <PackageReference Include="SixLabors.ImageSharp" Version="3.1.0" />
</ItemGroup>
```

## Database Schema

### QRCode Model
```csharp
public class QRCode
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string ImageDataUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastScannedAt { get; set; }
    public int ScanCount { get; set; } = 0;

    // Navigation properties
    public virtual Child Child { get; set; } = null!;
}
```

### DbContext Configuration
```csharp
// Add to AllowanceContext.cs

public DbSet<QRCode> QRCodes { get; set; }

protected override void OnModelCreating(ModelBuilder builder)
{
    // ... existing configuration ...

    // QRCode configuration
    builder.Entity<QRCode>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Token).IsRequired().HasMaxLength(100);
        entity.Property(e => e.ImageDataUrl).IsRequired();

        entity.HasOne(e => e.Child)
              .WithMany()
              .HasForeignKey(e => e.ChildId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(e => e.Token).IsUnique();
        entity.HasIndex(e => e.ChildId);
        entity.HasIndex(e => e.IsActive);
    });
}
```

## Service Interfaces

### IQRCodeService Interface
```csharp
public interface IQRCodeService
{
    // QR Code Generation
    Task<QRCode> GenerateQRCodeForChildAsync(Guid childId);
    Task<QRCode> RegenerateQRCodeAsync(Guid childId);
    Task<byte[]> GetQRCodeImageAsync(Guid childId, int pixelsPerModule = 10);

    // QR Code Validation
    Task<QRCodeValidationResult> ValidateTokenAsync(string token);
    Task<Child?> GetChildFromTokenAsync(string token);

    // QR Code Management
    Task<QRCode?> GetActiveQRCodeAsync(Guid childId);
    Task DeactivateQRCodeAsync(Guid childId);
    Task UpdateScanStatisticsAsync(string token);
}

public class QRCodeValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? ChildId { get; set; }
    public string? ChildName { get; set; }
}
```

## Service Implementation

### QRCodeService Implementation
```csharp
public class QRCodeService : IQRCodeService
{
    private readonly AllowanceContext _context;
    private readonly ILogger<QRCodeService> _logger;

    public QRCodeService(
        AllowanceContext context,
        ILogger<QRCodeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<QRCode> GenerateQRCodeForChildAsync(Guid childId)
    {
        var child = await _context.Children
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == childId);

        if (child == null)
            throw new InvalidOperationException("Child not found");

        // Check if active QR code exists
        var existingCode = await GetActiveQRCodeAsync(childId);
        if (existingCode != null)
            return existingCode;

        // Generate unique token
        var token = GenerateSecureToken();

        // Create QR code data (JSON payload)
        var qrData = new
        {
            Type = "AllowancePayment",
            ChildId = childId,
            Token = token,
            Timestamp = DateTime.UtcNow
        };

        var qrDataJson = System.Text.Json.JsonSerializer.Serialize(qrData);

        // Generate QR code image
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(qrDataJson, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(10);

        // Convert to data URL
        var base64Image = Convert.ToBase64String(qrCodeImage);
        var imageDataUrl = $"data:image/png;base64,{base64Image}";

        // Save to database
        var qrCodeEntity = new QRCode
        {
            ChildId = childId,
            Token = token,
            ImageDataUrl = imageDataUrl,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.QRCodes.Add(qrCodeEntity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("QR code generated for child {ChildId}", childId);

        return qrCodeEntity;
    }

    public async Task<QRCode> RegenerateQRCodeAsync(Guid childId)
    {
        // Deactivate existing QR codes
        await DeactivateQRCodeAsync(childId);

        // Generate new QR code
        return await GenerateQRCodeForChildAsync(childId);
    }

    public async Task<byte[]> GetQRCodeImageAsync(Guid childId, int pixelsPerModule = 10)
    {
        var qrCode = await GetActiveQRCodeAsync(childId);

        if (qrCode == null)
            throw new InvalidOperationException("No active QR code found");

        // Extract base64 image from data URL
        var base64Data = qrCode.ImageDataUrl.Split(',')[1];
        return Convert.FromBase64String(base64Data);
    }

    public async Task<QRCodeValidationResult> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new QRCodeValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token is required"
            };
        }

        var qrCode = await _context.QRCodes
            .Include(q => q.Child)
            .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(q => q.Token == token);

        if (qrCode == null)
        {
            return new QRCodeValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid QR code"
            };
        }

        if (!qrCode.IsActive)
        {
            return new QRCodeValidationResult
            {
                IsValid = false,
                ErrorMessage = "QR code has been deactivated"
            };
        }

        if (qrCode.ExpiresAt.HasValue && qrCode.ExpiresAt.Value < DateTime.UtcNow)
        {
            return new QRCodeValidationResult
            {
                IsValid = false,
                ErrorMessage = "QR code has expired"
            };
        }

        return new QRCodeValidationResult
        {
            IsValid = true,
            ChildId = qrCode.ChildId,
            ChildName = qrCode.Child.User.FullName
        };
    }

    public async Task<Child?> GetChildFromTokenAsync(string token)
    {
        var qrCode = await _context.QRCodes
            .Include(q => q.Child)
            .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(q => q.Token == token && q.IsActive);

        if (qrCode == null)
            return null;

        if (qrCode.ExpiresAt.HasValue && qrCode.ExpiresAt.Value < DateTime.UtcNow)
            return null;

        return qrCode.Child;
    }

    public async Task<QRCode?> GetActiveQRCodeAsync(Guid childId)
    {
        return await _context.QRCodes
            .Where(q => q.ChildId == childId && q.IsActive)
            .OrderByDescending(q => q.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task DeactivateQRCodeAsync(Guid childId)
    {
        var activeCodes = await _context.QRCodes
            .Where(q => q.ChildId == childId && q.IsActive)
            .ToListAsync();

        foreach (var code in activeCodes)
        {
            code.IsActive = false;
        }

        if (activeCodes.Any())
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deactivated {Count} QR codes for child {ChildId}", activeCodes.Count, childId);
        }
    }

    public async Task UpdateScanStatisticsAsync(string token)
    {
        var qrCode = await _context.QRCodes
            .FirstOrDefaultAsync(q => q.Token == token);

        if (qrCode != null)
        {
            qrCode.LastScannedAt = DateTime.UtcNow;
            qrCode.ScanCount++;
            await _context.SaveChangesAsync();
        }
    }

    private string GenerateSecureToken()
    {
        // Generate cryptographically secure random token
        var randomBytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")
            .Substring(0, 32);
    }
}
```

## DTOs

```csharp
public record QRCodeDto(
    Guid Id,
    Guid ChildId,
    string ChildName,
    string ImageDataUrl,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    int ScanCount,
    DateTime? LastScannedAt
);

public record CreateQRPaymentDto(
    string Token,
    decimal Amount,
    string Description
);

public record QRPaymentResponseDto(
    Guid TransactionId,
    Guid ChildId,
    string ChildName,
    decimal Amount,
    decimal NewBalance,
    DateTime Timestamp
);
```

## API Controllers

### QRCodeController
```csharp
[ApiController]
[Route("api/v1/qr-codes")]
[Authorize]
public class QRCodeController : ControllerBase
{
    private readonly IQRCodeService _qrCodeService;
    private readonly ITransactionService _transactionService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<QRCodeController> _logger;

    [HttpPost("generate/{childId}")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<QRCodeDto>> GenerateQRCode(Guid childId)
    {
        // Verify parent owns this child
        var child = await _context.Children
            .FirstOrDefaultAsync(c => c.Id == childId && c.FamilyId == _currentUser.FamilyId);

        if (child == null)
            return NotFound("Child not found");

        var qrCode = await _qrCodeService.GenerateQRCodeForChildAsync(childId);

        var dto = new QRCodeDto(
            qrCode.Id,
            qrCode.ChildId,
            child.User.FullName,
            qrCode.ImageDataUrl,
            qrCode.IsActive,
            qrCode.CreatedAt,
            qrCode.ExpiresAt,
            qrCode.ScanCount,
            qrCode.LastScannedAt
        );

        return Ok(dto);
    }

    [HttpPost("regenerate/{childId}")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<QRCodeDto>> RegenerateQRCode(Guid childId)
    {
        var qrCode = await _qrCodeService.RegenerateQRCodeAsync(childId);

        var child = await _context.Children
            .Include(c => c.User)
            .FirstAsync(c => c.Id == childId);

        var dto = new QRCodeDto(
            qrCode.Id,
            qrCode.ChildId,
            child.User.FullName,
            qrCode.ImageDataUrl,
            qrCode.IsActive,
            qrCode.CreatedAt,
            qrCode.ExpiresAt,
            qrCode.ScanCount,
            qrCode.LastScannedAt
        );

        return Ok(dto);
    }

    [HttpGet("child/{childId}")]
    [Authorize]
    public async Task<ActionResult<QRCodeDto>> GetQRCode(Guid childId)
    {
        var qrCode = await _qrCodeService.GetActiveQRCodeAsync(childId);

        if (qrCode == null)
            return NotFound("No active QR code found");

        var child = await _context.Children
            .Include(c => c.User)
            .FirstAsync(c => c.Id == childId);

        var dto = new QRCodeDto(
            qrCode.Id,
            qrCode.ChildId,
            child.User.FullName,
            qrCode.ImageDataUrl,
            qrCode.IsActive,
            qrCode.CreatedAt,
            qrCode.ExpiresAt,
            qrCode.ScanCount,
            qrCode.LastScannedAt
        );

        return Ok(dto);
    }

    [HttpGet("child/{childId}/image")]
    [Authorize]
    public async Task<IActionResult> GetQRCodeImage(Guid childId)
    {
        try
        {
            var imageBytes = await _qrCodeService.GetQRCodeImageAsync(childId);
            return File(imageBytes, "image/png");
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("validate")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<QRCodeValidationResult>> ValidateToken([FromBody] string token)
    {
        var result = await _qrCodeService.ValidateTokenAsync(token);
        return Ok(result);
    }

    [HttpPost("pay")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<QRPaymentResponseDto>> ProcessQRPayment([FromBody] CreateQRPaymentDto dto)
    {
        // Validate token
        var validation = await _qrCodeService.ValidateTokenAsync(dto.Token);
        if (!validation.IsValid)
            return BadRequest(validation.ErrorMessage);

        // Update scan statistics
        await _qrCodeService.UpdateScanStatisticsAsync(dto.Token);

        // Create transaction
        var transactionDto = new CreateTransactionDto(
            validation.ChildId!.Value,
            dto.Amount,
            TransactionType.Credit,
            dto.Description
        );

        var transaction = await _transactionService.CreateTransactionAsync(transactionDto);

        var child = await _context.Children
            .Include(c => c.User)
            .FirstAsync(c => c.Id == validation.ChildId.Value);

        _logger.LogInformation("QR payment processed: {Amount:C} to child {ChildId}", dto.Amount, validation.ChildId);

        var response = new QRPaymentResponseDto(
            transaction.Id,
            child.Id,
            child.User.FullName,
            transaction.Amount,
            transaction.BalanceAfter,
            transaction.CreatedAt
        );

        return Ok(response);
    }

    [HttpDelete("{childId}")]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> DeactivateQRCode(Guid childId)
    {
        await _qrCodeService.DeactivateQRCodeAsync(childId);
        return NoContent();
    }
}
```

## Blazor Components

### QRCodeDisplay.razor
```razor
@inject IQRCodeService QRCodeService

<div class="qr-code-display">
    @if (qrCode == null)
    {
        <div class="text-center p-4">
            <button class="btn btn-primary" @onclick="GenerateQRCode">
                Generate QR Code
            </button>
        </div>
    }
    else
    {
        <div class="card">
            <div class="card-header">
                <h4>@ChildName's Payment QR Code</h4>
            </div>
            <div class="card-body text-center">
                <img src="@qrCode.ImageDataUrl" alt="QR Code" class="qr-code-image" />

                <div class="mt-3">
                    <p class="text-muted">Scan this code to quickly add money</p>
                    <p><small>Created: @qrCode.CreatedAt.ToString("MMM dd, yyyy")</small></p>
                    <p><small>Scanned: @qrCode.ScanCount times</small></p>
                </div>

                <div class="mt-3">
                    <button class="btn btn-sm btn-outline-primary me-2" @onclick="DownloadQRCode">
                        <i class="bi bi-download"></i> Download
                    </button>
                    <button class="btn btn-sm btn-outline-warning me-2" @onclick="RegenerateQRCode">
                        <i class="bi bi-arrow-clockwise"></i> Regenerate
                    </button>
                    <button class="btn btn-sm btn-outline-danger" @onclick="DeactivateQRCode">
                        <i class="bi bi-x-circle"></i> Deactivate
                    </button>
                </div>
            </div>
        </div>
    }
</div>

@code {
    [Parameter] public Guid ChildId { get; set; }
    [Parameter] public string ChildName { get; set; } = string.Empty;

    private QRCodeDto? qrCode;

    protected override async Task OnInitializedAsync()
    {
        await LoadQRCode();
    }

    private async Task LoadQRCode()
    {
        try
        {
            // Load from API
            qrCode = null; // Replace with actual API call
        }
        catch
        {
            // No QR code exists yet
            qrCode = null;
        }
    }

    private async Task GenerateQRCode()
    {
        // Call API to generate
        await LoadQRCode();
    }

    private async Task RegenerateQRCode()
    {
        if (await JSRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to regenerate? The old QR code will stop working."))
        {
            // Call API to regenerate
            await LoadQRCode();
        }
    }

    private async Task DeactivateQRCode()
    {
        if (await JSRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to deactivate this QR code?"))
        {
            // Call API to deactivate
            qrCode = null;
        }
    }

    private async Task DownloadQRCode()
    {
        // Download image
        var url = $"/api/v1/qr-codes/child/{ChildId}/image";
        await JSRuntime.InvokeVoidAsync("downloadFile", url, $"qr-code-{ChildName}.png");
    }
}
```

### QRScanner.razor
```razor
@page "/scan-qr"
@attribute [Authorize(Roles = "Parent")]
@inject IQRCodeService QRCodeService
@inject ITransactionService TransactionService
@inject NavigationManager Navigation

<h2>Scan QR Code</h2>

@if (!cameraPermissionGranted)
{
    <div class="alert alert-info">
        <p>Camera permission is required to scan QR codes.</p>
        <button class="btn btn-primary" @onclick="RequestCameraPermission">
            Enable Camera
        </button>
    </div>
}
else if (validationResult == null)
{
    <div class="scanner-container">
        <div class="camera-view" @ref="videoElement">
            <video id="qr-video" autoplay playsinline></video>
        </div>
        <div class="scanner-overlay">
            <div class="scanner-frame"></div>
        </div>
        <p class="text-center mt-3">Position the QR code within the frame</p>
    </div>
}
else if (validationResult.IsValid && !paymentCompleted)
{
    <div class="payment-form">
        <div class="alert alert-success">
            <h4><i class="bi bi-check-circle"></i> QR Code Scanned</h4>
            <p>Ready to add money to <strong>@validationResult.ChildName</strong>'s account</p>
        </div>

        <EditForm Model="@paymentDto" OnValidSubmit="@ProcessPayment">
            <DataAnnotationsValidator />
            <ValidationSummary />

            <div class="mb-3">
                <label class="form-label">Amount</label>
                <InputNumber class="form-control form-control-lg" @bind-Value="paymentDto.Amount" />
            </div>

            <div class="mb-3">
                <label class="form-label">Description</label>
                <InputText class="form-control" @bind-Value="paymentDto.Description" />
            </div>

            <div class="d-grid gap-2">
                <button type="submit" class="btn btn-primary btn-lg" disabled="@isProcessing">
                    @if (isProcessing)
                    {
                        <span class="spinner-border spinner-border-sm me-2"></span>
                        <span>Processing...</span>
                    }
                    else
                    {
                        <span>Add @paymentDto.Amount.ToString("C") to Account</span>
                    }
                </button>
                <button type="button" class="btn btn-outline-secondary" @onclick="ResetScanner">
                    Cancel
                </button>
            </div>
        </EditForm>
    </div>
}
else if (paymentCompleted && paymentResponse != null)
{
    <div class="payment-success">
        <div class="alert alert-success">
            <h3><i class="bi bi-check-circle-fill"></i> Payment Successful!</h3>
            <p><strong>@paymentResponse.Amount.ToString("C")</strong> added to @paymentResponse.ChildName's account</p>
            <p>New Balance: <strong>@paymentResponse.NewBalance.ToString("C")</strong></p>
        </div>

        <div class="d-grid gap-2">
            <button class="btn btn-primary" @onclick="ResetScanner">
                Scan Another QR Code
            </button>
            <button class="btn btn-outline-primary" @onclick="GoToDashboard">
                Go to Dashboard
            </button>
        </div>
    </div>
}
else
{
    <div class="alert alert-danger">
        <h4><i class="bi bi-exclamation-triangle"></i> Invalid QR Code</h4>
        <p>@validationResult?.ErrorMessage</p>
        <button class="btn btn-primary" @onclick="ResetScanner">
            Try Again
        </button>
    </div>
}

@code {
    private ElementReference videoElement;
    private bool cameraPermissionGranted = false;
    private QRCodeValidationResult? validationResult;
    private CreateQRPaymentDto paymentDto = new("", 10m, "QR Code Payment");
    private bool isProcessing = false;
    private bool paymentCompleted = false;
    private QRPaymentResponseDto? paymentResponse;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Initialize QR scanner using JavaScript interop
            await JSRuntime.InvokeVoidAsync("initializeQRScanner", DotNetObjectReference.Create(this));
        }
    }

    [JSInvokable]
    public async Task OnQRCodeScanned(string qrData)
    {
        try
        {
            // Parse QR code data
            var data = System.Text.Json.JsonSerializer.Deserialize<QRCodeData>(qrData);
            if (data?.Token == null)
            {
                validationResult = new QRCodeValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid QR code format"
                };
                return;
            }

            // Validate token
            validationResult = await QRCodeService.ValidateTokenAsync(data.Token);
            paymentDto = new CreateQRPaymentDto(data.Token, 10m, "QR Code Payment");

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            validationResult = new QRCodeValidationResult
            {
                IsValid = false,
                ErrorMessage = "Error processing QR code"
            };
        }
    }

    private async Task RequestCameraPermission()
    {
        cameraPermissionGranted = await JSRuntime.InvokeAsync<bool>("requestCameraPermission");
    }

    private async Task ProcessPayment()
    {
        isProcessing = true;
        try
        {
            // Call API to process payment
            // paymentResponse = await ...
            paymentCompleted = true;
        }
        catch (Exception ex)
        {
            // Handle error
        }
        finally
        {
            isProcessing = false;
        }
    }

    private void ResetScanner()
    {
        validationResult = null;
        paymentCompleted = false;
        paymentResponse = null;
        paymentDto = new CreateQRPaymentDto("", 10m, "QR Code Payment");
    }

    private void GoToDashboard()
    {
        Navigation.NavigateTo("/dashboard");
    }

    private class QRCodeData
    {
        public string? Type { get; set; }
        public Guid ChildId { get; set; }
        public string? Token { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
```

### JavaScript for QR Scanning (wwwroot/js/qr-scanner.js)
```javascript
let qrScanner = null;

async function initializeQRScanner(dotNetHelper) {
    try {
        const video = document.getElementById('qr-video');
        const stream = await navigator.mediaDevices.getUserMedia({
            video: { facingMode: 'environment' }
        });

        video.srcObject = stream;

        // Use a QR code scanning library like jsQR
        qrScanner = setInterval(() => {
            const canvas = document.createElement('canvas');
            const context = canvas.getContext('2d');

            if (video.readyState === video.HAVE_ENOUGH_DATA) {
                canvas.width = video.videoWidth;
                canvas.height = video.videoHeight;
                context.drawImage(video, 0, 0, canvas.width, canvas.height);

                const imageData = context.getImageData(0, 0, canvas.width, canvas.height);
                const code = jsQR(imageData.data, imageData.width, imageData.height);

                if (code) {
                    clearInterval(qrScanner);
                    dotNetHelper.invokeMethodAsync('OnQRCodeScanned', code.data);
                }
            }
        }, 100);

        return true;
    } catch (error) {
        console.error('Camera error:', error);
        return false;
    }
}

async function requestCameraPermission() {
    try {
        await navigator.mediaDevices.getUserMedia({ video: true });
        return true;
    } catch {
        return false;
    }
}

function stopQRScanner() {
    if (qrScanner) {
        clearInterval(qrScanner);
    }
}

function downloadFile(url, filename) {
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
}
```

## Test Cases (12 Tests Total)

### QRCodeService Tests
```csharp
public class QRCodeServiceTests
{
    [Fact]
    public async Task GenerateQRCodeForChild_CreatesNewQRCode()
    {
        // Arrange
        var service = CreateService();
        var childId = Guid.NewGuid();

        // Act
        var qrCode = await service.GenerateQRCodeForChildAsync(childId);

        // Assert
        qrCode.Should().NotBeNull();
        qrCode.Token.Should().NotBeNullOrEmpty();
        qrCode.ImageDataUrl.Should().StartWith("data:image/png;base64,");
        qrCode.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateQRCodeForChild_ReturnsExistingIfActive()
    {
        // Arrange
        var service = CreateService();
        var childId = Guid.NewGuid();
        var firstQRCode = await service.GenerateQRCodeForChildAsync(childId);

        // Act
        var secondQRCode = await service.GenerateQRCodeForChildAsync(childId);

        // Assert
        secondQRCode.Id.Should().Be(firstQRCode.Id);
        secondQRCode.Token.Should().Be(firstQRCode.Token);
    }

    [Fact]
    public async Task RegenerateQRCode_DeactivatesOldCode()
    {
        // Arrange
        var service = CreateService();
        var childId = Guid.NewGuid();
        var oldQRCode = await service.GenerateQRCodeForChildAsync(childId);

        // Act
        var newQRCode = await service.RegenerateQRCodeAsync(childId);

        // Assert
        newQRCode.Id.Should().NotBe(oldQRCode.Id);
        newQRCode.Token.Should().NotBe(oldQRCode.Token);

        var oldCodeFromDb = await _context.QRCodes.FindAsync(oldQRCode.Id);
        oldCodeFromDb!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateToken_ValidToken_ReturnsSuccess()
    {
        // Arrange
        var service = CreateService();
        var child = await CreateTestChild();
        var qrCode = await service.GenerateQRCodeForChildAsync(child.Id);

        // Act
        var result = await service.ValidateTokenAsync(qrCode.Token);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ChildId.Should().Be(child.Id);
        result.ChildName.Should().Be(child.User.FullName);
    }

    [Fact]
    public async Task ValidateToken_InvalidToken_ReturnsError()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.ValidateTokenAsync("invalid-token");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid QR code");
    }

    [Fact]
    public async Task ValidateToken_DeactivatedCode_ReturnsError()
    {
        // Arrange
        var service = CreateService();
        var child = await CreateTestChild();
        var qrCode = await service.GenerateQRCodeForChildAsync(child.Id);
        await service.DeactivateQRCodeAsync(child.Id);

        // Act
        var result = await service.ValidateTokenAsync(qrCode.Token);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("deactivated");
    }

    [Fact]
    public async Task ValidateToken_ExpiredCode_ReturnsError()
    {
        // Arrange
        var service = CreateService();
        var qrCode = await CreateExpiredQRCode();

        // Act
        var result = await service.ValidateTokenAsync(qrCode.Token);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("expired");
    }

    [Fact]
    public async Task GetChildFromToken_ValidToken_ReturnsChild()
    {
        // Arrange
        var service = CreateService();
        var child = await CreateTestChild();
        var qrCode = await service.GenerateQRCodeForChildAsync(child.Id);

        // Act
        var result = await service.GetChildFromTokenAsync(qrCode.Token);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(child.Id);
    }

    [Fact]
    public async Task UpdateScanStatistics_IncrementsCount()
    {
        // Arrange
        var service = CreateService();
        var child = await CreateTestChild();
        var qrCode = await service.GenerateQRCodeForChildAsync(child.Id);

        // Act
        await service.UpdateScanStatisticsAsync(qrCode.Token);
        await service.UpdateScanStatisticsAsync(qrCode.Token);

        // Assert
        var updatedCode = await service.GetActiveQRCodeAsync(child.Id);
        updatedCode!.ScanCount.Should().Be(2);
        updatedCode.LastScannedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeactivateQRCode_DeactivatesAllActiveCodes()
    {
        // Arrange
        var service = CreateService();
        var childId = Guid.NewGuid();
        await service.GenerateQRCodeForChildAsync(childId);

        // Act
        await service.DeactivateQRCodeAsync(childId);

        // Assert
        var activeCode = await service.GetActiveQRCodeAsync(childId);
        activeCode.Should().BeNull();
    }
}

public class QRCodeControllerTests
{
    [Fact]
    public async Task ProcessQRPayment_ValidToken_CreatesTransaction()
    {
        // Arrange
        var controller = CreateController();
        var child = await CreateTestChild();
        var qrCode = await GenerateQRCode(child.Id);
        var dto = new CreateQRPaymentDto(qrCode.Token, 25m, "Test payment");

        // Act
        var result = await controller.ProcessQRPayment(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var response = (result as OkObjectResult)!.Value as QRPaymentResponseDto;
        response!.Amount.Should().Be(25m);
        response.ChildId.Should().Be(child.Id);
    }

    [Fact]
    public async Task ProcessQRPayment_InvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var dto = new CreateQRPaymentDto("invalid-token", 25m, "Test");

        // Act
        var result = await controller.ProcessQRPayment(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
```

## Security Considerations

### Token Security
- Tokens are cryptographically random (32 characters)
- One token per child at a time
- Optional expiration dates
- Can be manually deactivated
- Scan statistics for monitoring

### Best Practices
```csharp
// Add rate limiting to prevent abuse
[RateLimit(10, TimeWindow = 60)] // 10 scans per minute
public async Task<IActionResult> ProcessQRPayment([FromBody] CreateQRPaymentDto dto)
{
    // ...
}

// Add maximum transaction amount for QR payments
if (dto.Amount > 100m)
{
    return BadRequest("QR code payments are limited to $100");
}

// Log all QR payment attempts
_logger.LogInformation("QR payment attempt: {Token} for {Amount:C}", dto.Token, dto.Amount);
```

## Success Metrics

### Performance Targets
- QR code generation: < 500ms
- QR code validation: < 100ms
- Payment processing: < 200ms
- Image loading: < 100ms

### Quality Metrics
- 12 tests passing (100% critical path coverage)
- QR codes scannable from 6+ inches away
- Token uniqueness guaranteed
- Zero unauthorized payments

## Configuration

### appsettings.json
```json
{
  "QRCode": {
    "DefaultPixelsPerModule": 10,
    "DefaultExpirationDays": null,
    "MaxTransactionAmount": 100.00
  }
}
```
