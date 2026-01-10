# Extended Family Gifting Specification

## Overview

The Extended Family Gifting system allows grandparents, aunts, uncles, and other relatives to send monetary gifts to children without creating accounts. Parents generate secure, shareable gift links with customizable privacy settings. Recipients submit gifts through a guest portal, parents approve them, and children are prompted to send thank-you notes.

Key features:
- Secure guest gifting portal (no account required)
- Customizable privacy controls (what relatives can see)
- Gift occasions (birthday, holiday, achievement)
- Parent approval workflow for gifts
- Thank-you note system with prompts
- Gift history and reporting

---

## Database Schema

### GiftLink Model

```csharp
public class GiftLink
{
    public Guid Id { get; set; }

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    public Guid CreatedByParentId { get; set; }
    public virtual ApplicationUser CreatedByParent { get; set; } = null!;

    // Link configuration
    public string Token { get; set; } = string.Empty;  // Secure URL-safe token
    public string ChildDisplayName { get; set; } = string.Empty;  // What relatives see
    public string? WelcomeMessage { get; set; }
    public string? ChildPhotoUrl { get; set; }  // Optional photo for portal

    // Privacy settings
    public GiftLinkVisibility Visibility { get; set; } = GiftLinkVisibility.NameOnly;
    public bool ShowSavingsGoals { get; set; } = false;
    public bool ShowWishList { get; set; } = false;

    // Limits
    public decimal? MinGiftAmount { get; set; }
    public decimal? MaxGiftAmount { get; set; }

    // Expiration
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Usage tracking
    public int UsageCount { get; set; } = 0;
    public int? MaxUses { get; set; }  // null = unlimited

    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }

    // Navigation
    public virtual ICollection<Gift> Gifts { get; set; } = new List<Gift>();
}

public enum GiftLinkVisibility
{
    NameOnly = 1,           // Just child's display name
    NameAndPhoto = 2,       // Name + profile photo
    NameAndGoals = 3,       // Name + active savings goals
    Full = 4                // Name + goals + wish list
}
```

### Gift Model

```csharp
public class Gift
{
    public Guid Id { get; set; }

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    public Guid? GiftLinkId { get; set; }
    public virtual GiftLink? GiftLink { get; set; }

    // Gift details
    public decimal Amount { get; set; }
    public GiftOccasion Occasion { get; set; }
    public string? Message { get; set; }

    // Giver information
    public string GiverName { get; set; } = string.Empty;
    public string? GiverEmail { get; set; }  // For thank-you notifications
    public string? GiverRelationship { get; set; }  // "Grandma", "Uncle", etc.

    // Allocation (optional)
    public Guid? AllocateToGoalId { get; set; }
    public virtual SavingsGoal? AllocateToGoal { get; set; }
    public decimal? AllocateToSavingsPercent { get; set; }

    // Status
    public GiftStatus Status { get; set; } = GiftStatus.Pending;
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedById { get; set; }
    public virtual ApplicationUser? ApprovedBy { get; set; }
    public string? RejectionReason { get; set; }

    // Transaction link (created on approval)
    public Guid? TransactionId { get; set; }
    public virtual Transaction? Transaction { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; }
    public string? IpAddress { get; set; }  // For fraud detection

    // Navigation
    public virtual ThankYouNote? ThankYouNote { get; set; }
}

public enum GiftOccasion
{
    Birthday = 1,
    Holiday = 2,
    Christmas = 3,
    Hanukkah = 4,
    Easter = 5,
    ReportCard = 6,
    Graduation = 7,
    Achievement = 8,
    JustBecause = 9,
    Other = 99
}

public enum GiftStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4,
    Expired = 5
}
```

### ThankYouNote Model

```csharp
public class ThankYouNote
{
    public Guid Id { get; set; }

    public Guid GiftId { get; set; }
    public virtual Gift Gift { get; set; } = null!;

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    // Note content
    public string Message { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }  // Optional photo attachment
    public string? DrawingUrl { get; set; }  // Optional drawing

    // Delivery
    public bool IsSent { get; set; } = false;
    public DateTime? SentAt { get; set; }
    public string? SentToEmail { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

---

## DTOs

### Request DTOs

```csharp
// Gift Link management
public record CreateGiftLinkDto(
    Guid ChildId,
    string ChildDisplayName,
    string? WelcomeMessage,
    string? ChildPhotoUrl,
    GiftLinkVisibility Visibility,
    bool ShowSavingsGoals,
    bool ShowWishList,
    decimal? MinGiftAmount,
    decimal? MaxGiftAmount,
    DateTime? ExpiresAt,
    int? MaxUses
);

public record UpdateGiftLinkDto(
    string? ChildDisplayName,
    string? WelcomeMessage,
    string? ChildPhotoUrl,
    GiftLinkVisibility? Visibility,
    bool? ShowSavingsGoals,
    bool? ShowWishList,
    decimal? MinGiftAmount,
    decimal? MaxGiftAmount,
    DateTime? ExpiresAt,
    int? MaxUses,
    bool? IsActive
);

// Guest gift submission
public record SubmitGiftDto(
    decimal Amount,
    GiftOccasion Occasion,
    string? Message,
    string GiverName,
    string? GiverEmail,
    string? GiverRelationship,
    Guid? AllocateToGoalId,
    decimal? AllocateToSavingsPercent
);

// Gift approval
public record ApproveGiftDto(
    Guid? AllocateToGoalId,
    decimal? AllocateToSavingsPercent
);

public record RejectGiftDto(
    string Reason
);

// Thank you notes
public record CreateThankYouNoteDto(
    string Message,
    string? ImageUrl,
    string? DrawingUrl
);

public record UpdateThankYouNoteDto(
    string? Message,
    string? ImageUrl,
    string? DrawingUrl
);

public record SendThankYouNoteDto(
    string? OverrideEmail  // Optional override of giver's email
);
```

### Response DTOs

```csharp
public record GiftLinkDto(
    Guid Id,
    Guid ChildId,
    string ChildName,
    string Token,
    string ShareableUrl,
    string ChildDisplayName,
    string? WelcomeMessage,
    string? ChildPhotoUrl,
    GiftLinkVisibility Visibility,
    string VisibilityDescription,
    bool ShowSavingsGoals,
    bool ShowWishList,
    decimal? MinGiftAmount,
    decimal? MaxGiftAmount,
    DateTime? ExpiresAt,
    bool IsActive,
    int UsageCount,
    int? MaxUses,
    int? RemainingUses,
    DateTime CreatedAt,
    DateTime? LastUsedAt
);

// Guest portal data (no auth required)
public record GiftPortalDataDto(
    string ChildDisplayName,
    string? ChildPhotoUrl,
    string? WelcomeMessage,
    decimal? MinGiftAmount,
    decimal? MaxGiftAmount,
    List<GiftOccasionDto> Occasions,
    List<SavingsGoalSummaryDto>? SavingsGoals,  // If visibility allows
    List<WishListItemSummaryDto>? WishListItems  // If visibility allows
);

public record GiftOccasionDto(
    GiftOccasion Value,
    string Name,
    string IconName
);

public record SavingsGoalSummaryDto(
    Guid Id,
    string Name,
    decimal TargetAmount,
    decimal CurrentAmount,
    double ProgressPercentage,
    string? ImageUrl
);

public record WishListItemSummaryDto(
    Guid Id,
    string Name,
    decimal Price,
    string? ImageUrl
);

public record GiftDto(
    Guid Id,
    Guid ChildId,
    string ChildName,
    decimal Amount,
    GiftOccasion Occasion,
    string OccasionName,
    string? Message,
    string GiverName,
    string? GiverEmail,
    string? GiverRelationship,
    Guid? AllocateToGoalId,
    string? AllocateToGoalName,
    decimal? AllocateToSavingsPercent,
    GiftStatus Status,
    string StatusName,
    DateTime? ApprovedAt,
    string? ApprovedByName,
    string? RejectionReason,
    bool HasThankYouNote,
    bool ThankYouNoteSent,
    DateTime CreatedAt
);

public record GiftSubmissionResultDto(
    Guid GiftId,
    string Message,
    bool RequiresApproval
);

public record ThankYouNoteDto(
    Guid Id,
    Guid GiftId,
    string GiverName,
    decimal GiftAmount,
    GiftOccasion Occasion,
    string Message,
    string? ImageUrl,
    string? DrawingUrl,
    bool IsSent,
    DateTime? SentAt,
    DateTime CreatedAt
);

public record PendingThankYouDto(
    Guid GiftId,
    string GiverName,
    decimal Amount,
    GiftOccasion Occasion,
    string? GiverRelationship,
    DateTime ReceivedAt,
    int DaysSinceReceived
);
```

---

## API Endpoints

### Gift Links (Parent)

#### POST /api/v1/gift-links
Create a gift link

**Authorization**: Parent only
**Request Body**: `CreateGiftLinkDto`
**Response**: `GiftLinkDto`

---

#### GET /api/v1/gift-links
Get all gift links for family

**Authorization**: Parent only
**Response**: `List<GiftLinkDto>`

---

#### GET /api/v1/gift-links/{id}
Get gift link details

**Authorization**: Parent only
**Response**: `GiftLinkDto`

---

#### PUT /api/v1/gift-links/{id}
Update gift link settings

**Authorization**: Parent only
**Request Body**: `UpdateGiftLinkDto`
**Response**: `GiftLinkDto`

---

#### DELETE /api/v1/gift-links/{id}
Deactivate gift link

**Authorization**: Parent only
**Response**: 204 No Content

---

#### POST /api/v1/gift-links/{id}/regenerate
Generate new token (invalidates old links)

**Authorization**: Parent only
**Response**: `GiftLinkDto`

---

### Guest Portal (Public)

#### GET /api/v1/gifts/portal/{token}
Get gift portal data (public)

**Authorization**: None (uses token)
**Response**: `GiftPortalDataDto`

**Validation**:
- Token must be valid and active
- Link must not be expired
- Link must not exceed max uses

---

#### POST /api/v1/gifts/portal/{token}/submit
Submit a gift (public)

**Authorization**: None (uses token)
**Request Body**: `SubmitGiftDto`
**Response**: `GiftSubmissionResultDto`

**Business Rules**:
- Validates amount against min/max limits
- Creates pending gift for parent approval
- Increments link usage count
- Sends notification to parent

---

### Gift Approval (Parent)

#### GET /api/v1/gifts/pending
Get pending gifts for approval

**Authorization**: Parent only
**Response**: `List<GiftDto>`

---

#### POST /api/v1/gifts/{id}/approve
Approve a gift

**Authorization**: Parent only
**Request Body**: `ApproveGiftDto`
**Response**: `GiftDto`

**Business Rules**:
- Creates transaction crediting child
- Optionally allocates to savings/goal
- Sends notification to child
- Sends confirmation to giver (if email provided)

---

#### POST /api/v1/gifts/{id}/reject
Reject a gift

**Authorization**: Parent only
**Request Body**: `RejectGiftDto`
**Response**: `GiftDto`

---

### Gifts

#### GET /api/v1/children/{childId}/gifts
Get child's gift history

**Authorization**: Parent or self
**Response**: `List<GiftDto>`

**Query Parameters**:
- `status` (optional) - Filter by status
- `startDate` / `endDate` (optional) - Date range

---

#### GET /api/v1/gifts/{id}
Get gift details

**Authorization**: Family member
**Response**: `GiftDto`

---

### Thank You Notes

#### GET /api/v1/children/{childId}/thank-you/pending
Get gifts awaiting thank you notes

**Authorization**: Parent or self
**Response**: `List<PendingThankYouDto>`

---

#### POST /api/v1/gifts/{giftId}/thank-you
Create thank you note

**Authorization**: Child (self) only
**Request Body**: `CreateThankYouNoteDto`
**Response**: `ThankYouNoteDto`

---

#### PUT /api/v1/gifts/{giftId}/thank-you
Update thank you note

**Authorization**: Child (self) only
**Request Body**: `UpdateThankYouNoteDto`
**Response**: `ThankYouNoteDto`

---

#### GET /api/v1/gifts/{giftId}/thank-you
Get thank you note

**Authorization**: Family member
**Response**: `ThankYouNoteDto`

---

#### POST /api/v1/gifts/{giftId}/thank-you/send
Send thank you note to giver

**Authorization**: Parent or self
**Request Body**: `SendThankYouNoteDto`
**Response**: `ThankYouNoteDto`

**Business Rules**:
- Requires giver email (from gift or override)
- Sends formatted email with child's message
- Marks note as sent
- Cannot resend once sent

---

## Service Layer

### IGiftLinkService

```csharp
public interface IGiftLinkService
{
    Task<GiftLinkDto> CreateLinkAsync(CreateGiftLinkDto dto, Guid parentId);
    Task<List<GiftLinkDto>> GetFamilyLinksAsync(Guid parentId);
    Task<GiftLinkDto> GetLinkByIdAsync(Guid linkId, Guid parentId);
    Task<GiftLinkDto> UpdateLinkAsync(Guid linkId, UpdateGiftLinkDto dto, Guid parentId);
    Task DeactivateLinkAsync(Guid linkId, Guid parentId);
    Task<GiftLinkDto> RegenerateTokenAsync(Guid linkId, Guid parentId);

    // Token validation
    Task<GiftLink?> ValidateTokenAsync(string token);
    string GenerateSecureToken();
}
```

### IGiftService

```csharp
public interface IGiftService
{
    // Portal (public)
    Task<GiftPortalDataDto> GetPortalDataAsync(string token);
    Task<GiftSubmissionResultDto> SubmitGiftAsync(string token, SubmitGiftDto dto, string? ipAddress);

    // Approval
    Task<List<GiftDto>> GetPendingGiftsAsync(Guid parentId);
    Task<GiftDto> ApproveGiftAsync(Guid giftId, ApproveGiftDto dto, Guid parentId);
    Task<GiftDto> RejectGiftAsync(Guid giftId, RejectGiftDto dto, Guid parentId);

    // History
    Task<List<GiftDto>> GetChildGiftsAsync(Guid childId, GiftStatus? status, DateTime? startDate, DateTime? endDate, Guid userId);
    Task<GiftDto> GetGiftByIdAsync(Guid giftId, Guid userId);

    // Background
    Task ExpireOldPendingGiftsAsync(int daysOld = 30);
}
```

### IThankYouNoteService

```csharp
public interface IThankYouNoteService
{
    Task<List<PendingThankYouDto>> GetPendingThankYousAsync(Guid childId, Guid userId);
    Task<ThankYouNoteDto> CreateNoteAsync(Guid giftId, CreateThankYouNoteDto dto, Guid childId);
    Task<ThankYouNoteDto> UpdateNoteAsync(Guid giftId, UpdateThankYouNoteDto dto, Guid childId);
    Task<ThankYouNoteDto?> GetNoteAsync(Guid giftId, Guid userId);
    Task<ThankYouNoteDto> SendNoteAsync(Guid giftId, SendThankYouNoteDto dto, Guid userId);
}
```

---

## Gift Approval Flow

```csharp
public async Task<GiftDto> ApproveGiftAsync(Guid giftId, ApproveGiftDto dto, Guid parentId)
{
    var gift = await _context.Gifts
        .Include(g => g.Child)
        .Include(g => g.GiftLink)
        .FirstOrDefaultAsync(g => g.Id == giftId);

    if (gift == null)
        throw new NotFoundException("Gift not found");

    if (gift.Status != GiftStatus.Pending)
        throw new InvalidOperationException("Gift is not pending");

    // Verify parent is in same family
    await VerifyParentAccessAsync(parentId, gift.ChildId);

    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // Create transaction
        var trans = new Transaction
        {
            ChildId = gift.ChildId,
            Amount = gift.Amount,
            Type = TransactionType.Credit,
            Category = "Gift",
            Description = $"Gift from {gift.GiverName}" +
                (gift.Occasion != GiftOccasion.JustBecause ? $" ({GetOccasionName(gift.Occasion)})" : ""),
            CreatedById = parentId,
            CreatedAt = DateTime.UtcNow
        };

        // Update child's balance
        gift.Child.CurrentBalance += gift.Amount;
        trans.BalanceAfter = gift.Child.CurrentBalance;

        _context.Transactions.Add(trans);

        // Handle allocation
        decimal remainingAmount = gift.Amount;

        if (dto.AllocateToGoalId.HasValue)
        {
            var goal = await _context.SavingsGoals.FindAsync(dto.AllocateToGoalId);
            if (goal != null)
            {
                var allocateAmount = gift.Amount;
                goal.CurrentAmount += allocateAmount;

                var contribution = new SavingsContribution
                {
                    GoalId = goal.Id,
                    ChildId = gift.ChildId,
                    Amount = allocateAmount,
                    Type = ContributionType.ExternalGift,
                    GoalBalanceAfter = goal.CurrentAmount,
                    Description = $"Gift from {gift.GiverName}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.SavingsContributions.Add(contribution);

                gift.AllocateToGoalId = goal.Id;
            }
        }
        else if (dto.AllocateToSavingsPercent.HasValue && dto.AllocateToSavingsPercent > 0)
        {
            var savingsAmount = gift.Amount * (dto.AllocateToSavingsPercent.Value / 100);
            gift.Child.SavingsBalance = (gift.Child.SavingsBalance ?? 0) + savingsAmount;
            gift.AllocateToSavingsPercent = dto.AllocateToSavingsPercent;
        }

        // Update gift status
        gift.Status = GiftStatus.Approved;
        gift.ApprovedAt = DateTime.UtcNow;
        gift.ApprovedById = parentId;
        gift.TransactionId = trans.Id;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        // Send notifications
        await _notificationService.SendNotificationAsync(
            gift.Child.UserId,
            NotificationType.GiftReceived,
            "Gift Received!",
            $"{gift.GiverName} sent you ${gift.Amount:F2}!",
            new { GiftId = gift.Id, Amount = gift.Amount }
        );

        // Send confirmation to giver if email provided
        if (!string.IsNullOrEmpty(gift.GiverEmail))
        {
            await _emailService.SendGiftConfirmationAsync(
                gift.GiverEmail,
                gift.GiverName,
                gift.Child.Name,
                gift.Amount
            );
        }

        return MapToDto(gift);
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

---

## iOS Implementation

### Models

```swift
import Foundation

struct GiftLink: Codable, Identifiable {
    let id: UUID
    let childId: UUID
    let childName: String
    let token: String
    let shareableUrl: String
    let childDisplayName: String
    let welcomeMessage: String?
    let childPhotoUrl: String?
    let visibility: GiftLinkVisibility
    let visibilityDescription: String
    let showSavingsGoals: Bool
    let showWishList: Bool
    let minGiftAmount: Decimal?
    let maxGiftAmount: Decimal?
    let expiresAt: Date?
    let isActive: Bool
    let usageCount: Int
    let maxUses: Int?
    let remainingUses: Int?
    let createdAt: Date
    let lastUsedAt: Date?
}

struct Gift: Codable, Identifiable {
    let id: UUID
    let childId: UUID
    let childName: String
    let amount: Decimal
    let occasion: GiftOccasion
    let occasionName: String
    let message: String?
    let giverName: String
    let giverEmail: String?
    let giverRelationship: String?
    let allocateToGoalId: UUID?
    let allocateToGoalName: String?
    let allocateToSavingsPercent: Decimal?
    let status: GiftStatus
    let statusName: String
    let approvedAt: Date?
    let approvedByName: String?
    let rejectionReason: String?
    let hasThankYouNote: Bool
    let thankYouNoteSent: Bool
    let createdAt: Date
}

struct ThankYouNote: Codable, Identifiable {
    let id: UUID
    let giftId: UUID
    let giverName: String
    let giftAmount: Decimal
    let occasion: GiftOccasion
    let message: String
    let imageUrl: String?
    let drawingUrl: String?
    let isSent: Bool
    let sentAt: Date?
    let createdAt: Date
}

struct PendingThankYou: Codable, Identifiable {
    var id: UUID { giftId }
    let giftId: UUID
    let giverName: String
    let amount: Decimal
    let occasion: GiftOccasion
    let giverRelationship: String?
    let receivedAt: Date
    let daysSinceReceived: Int
}

enum GiftLinkVisibility: Int, Codable {
    case nameOnly = 1
    case nameAndPhoto = 2
    case nameAndGoals = 3
    case full = 4

    var description: String {
        switch self {
        case .nameOnly: return "Name only"
        case .nameAndPhoto: return "Name and photo"
        case .nameAndGoals: return "Name and savings goals"
        case .full: return "Full (goals and wish list)"
        }
    }
}

enum GiftOccasion: Int, Codable, CaseIterable {
    case birthday = 1
    case holiday = 2
    case christmas = 3
    case hanukkah = 4
    case easter = 5
    case reportCard = 6
    case graduation = 7
    case achievement = 8
    case justBecause = 9
    case other = 99

    var displayName: String {
        switch self {
        case .birthday: return "Birthday"
        case .holiday: return "Holiday"
        case .christmas: return "Christmas"
        case .hanukkah: return "Hanukkah"
        case .easter: return "Easter"
        case .reportCard: return "Report Card"
        case .graduation: return "Graduation"
        case .achievement: return "Achievement"
        case .justBecause: return "Just Because"
        case .other: return "Other"
        }
    }

    var iconName: String {
        switch self {
        case .birthday: return "birthday.cake"
        case .holiday, .christmas, .hanukkah, .easter: return "gift"
        case .reportCard: return "doc.text"
        case .graduation: return "graduationcap"
        case .achievement: return "star"
        case .justBecause: return "heart"
        case .other: return "sparkles"
        }
    }
}

enum GiftStatus: Int, Codable {
    case pending = 1
    case approved = 2
    case rejected = 3
    case cancelled = 4
    case expired = 5
}
```

### ViewModels

```swift
import Foundation

@Observable
@MainActor
final class GiftingViewModel {
    var giftLinks: [GiftLink] = []
    var pendingGifts: [Gift] = []
    var childGifts: [Gift] = []
    var pendingThankYous: [PendingThankYou] = []

    var isLoading = false
    var errorMessage: String?

    private let apiService: APIServiceProtocol

    init(apiService: APIServiceProtocol = APIService()) {
        self.apiService = apiService
    }

    // Gift Links
    func loadGiftLinks() async {
        isLoading = true
        do {
            giftLinks = try await apiService.get(endpoint: "/api/v1/gift-links")
        } catch {
            errorMessage = error.localizedDescription
        }
        isLoading = false
    }

    func createGiftLink(_ request: CreateGiftLinkRequest) async -> GiftLink? {
        do {
            let link: GiftLink = try await apiService.post(
                endpoint: "/api/v1/gift-links",
                body: request
            )
            await loadGiftLinks()
            return link
        } catch {
            errorMessage = error.localizedDescription
            return nil
        }
    }

    func shareLink(_ link: GiftLink) {
        let url = URL(string: link.shareableUrl)!
        let activityVC = UIActivityViewController(
            activityItems: [url],
            applicationActivities: nil
        )
        // Present activity view controller
    }

    // Pending gifts
    func loadPendingGifts() async {
        do {
            pendingGifts = try await apiService.get(endpoint: "/api/v1/gifts/pending")
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func approveGift(_ gift: Gift, allocateToGoalId: UUID? = nil, savingsPercent: Decimal? = nil) async {
        do {
            let _: Gift = try await apiService.post(
                endpoint: "/api/v1/gifts/\(gift.id)/approve",
                body: ApproveGiftRequest(
                    allocateToGoalId: allocateToGoalId,
                    allocateToSavingsPercent: savingsPercent
                )
            )
            await loadPendingGifts()
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func rejectGift(_ gift: Gift, reason: String) async {
        do {
            let _: Gift = try await apiService.post(
                endpoint: "/api/v1/gifts/\(gift.id)/reject",
                body: RejectGiftRequest(reason: reason)
            )
            await loadPendingGifts()
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    // Child gifts
    func loadChildGifts(childId: UUID) async {
        do {
            childGifts = try await apiService.get(
                endpoint: "/api/v1/children/\(childId)/gifts"
            )
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    // Thank you notes
    func loadPendingThankYous(childId: UUID) async {
        do {
            pendingThankYous = try await apiService.get(
                endpoint: "/api/v1/children/\(childId)/thank-you/pending"
            )
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func sendThankYouNote(giftId: UUID, message: String, imageUrl: String?) async {
        do {
            // Create note
            let _: ThankYouNote = try await apiService.post(
                endpoint: "/api/v1/gifts/\(giftId)/thank-you",
                body: CreateThankYouRequest(message: message, imageUrl: imageUrl, drawingUrl: nil)
            )

            // Send it
            let _: ThankYouNote = try await apiService.post(
                endpoint: "/api/v1/gifts/\(giftId)/thank-you/send",
                body: SendThankYouRequest(overrideEmail: nil)
            )
        } catch {
            errorMessage = error.localizedDescription
        }
    }
}

struct CreateGiftLinkRequest: Codable {
    let childId: UUID
    let childDisplayName: String
    let welcomeMessage: String?
    let childPhotoUrl: String?
    let visibility: GiftLinkVisibility
    let showSavingsGoals: Bool
    let showWishList: Bool
    let minGiftAmount: Decimal?
    let maxGiftAmount: Decimal?
    let expiresAt: Date?
    let maxUses: Int?
}

struct ApproveGiftRequest: Codable {
    let allocateToGoalId: UUID?
    let allocateToSavingsPercent: Decimal?
}

struct RejectGiftRequest: Codable {
    let reason: String
}

struct CreateThankYouRequest: Codable {
    let message: String
    let imageUrl: String?
    let drawingUrl: String?
}

struct SendThankYouRequest: Codable {
    let overrideEmail: String?
}
```

### Views

```swift
import SwiftUI

struct GiftLinksView: View {
    @State private var viewModel = GiftingViewModel()
    @State private var showingCreateLink = false

    var body: some View {
        NavigationStack {
            List {
                if viewModel.giftLinks.isEmpty {
                    ContentUnavailableView(
                        "No Gift Links",
                        systemImage: "gift",
                        description: Text("Create a gift link to share with family members.")
                    )
                } else {
                    ForEach(viewModel.giftLinks) { link in
                        GiftLinkRow(link: link, onShare: {
                            viewModel.shareLink(link)
                        })
                    }
                }
            }
            .navigationTitle("Gift Links")
            .toolbar {
                ToolbarItem(placement: .primaryAction) {
                    Button(action: { showingCreateLink = true }) {
                        Image(systemName: "plus")
                    }
                }
            }
            .sheet(isPresented: $showingCreateLink) {
                CreateGiftLinkView(viewModel: viewModel)
            }
            .refreshable {
                await viewModel.loadGiftLinks()
            }
        }
        .task {
            await viewModel.loadGiftLinks()
        }
    }
}

struct GiftLinkRow: View {
    let link: GiftLink
    let onShare: () -> Void

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack {
                Text(link.childDisplayName)
                    .font(.headline)

                Spacer()

                if !link.isActive {
                    Text("Inactive")
                        .font(.caption)
                        .padding(.horizontal, 8)
                        .padding(.vertical, 2)
                        .background(Color.red.opacity(0.2))
                        .foregroundStyle(.red)
                        .cornerRadius(4)
                }
            }

            Text(link.visibilityDescription)
                .font(.caption)
                .foregroundStyle(.secondary)

            HStack {
                Label("\(link.usageCount) uses", systemImage: "person.2")
                    .font(.caption)

                if let remaining = link.remainingUses {
                    Text("(\(remaining) left)")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                Spacer()

                Button(action: onShare) {
                    Label("Share", systemImage: "square.and.arrow.up")
                        .font(.caption)
                }
                .buttonStyle(.bordered)
            }
        }
        .padding(.vertical, 4)
    }
}

struct PendingGiftsView: View {
    @Bindable var viewModel: GiftingViewModel
    @State private var selectedGift: Gift?
    @State private var showingApproval = false

    var body: some View {
        List {
            if viewModel.pendingGifts.isEmpty {
                ContentUnavailableView(
                    "No Pending Gifts",
                    systemImage: "checkmark.circle",
                    description: Text("All gifts have been reviewed.")
                )
            } else {
                ForEach(viewModel.pendingGifts) { gift in
                    PendingGiftRow(gift: gift)
                        .onTapGesture {
                            selectedGift = gift
                            showingApproval = true
                        }
                }
            }
        }
        .navigationTitle("Pending Gifts")
        .sheet(isPresented: $showingApproval) {
            if let gift = selectedGift {
                GiftApprovalSheet(gift: gift, viewModel: viewModel)
            }
        }
        .refreshable {
            await viewModel.loadPendingGifts()
        }
    }
}

struct PendingGiftRow: View {
    let gift: Gift

    var body: some View {
        HStack {
            VStack(alignment: .leading, spacing: 4) {
                Text(gift.giverName)
                    .font(.headline)

                if let relationship = gift.giverRelationship {
                    Text(relationship)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                HStack {
                    Image(systemName: gift.occasion.iconName)
                    Text(gift.occasionName)
                }
                .font(.caption)
                .foregroundStyle(.secondary)
            }

            Spacer()

            VStack(alignment: .trailing) {
                Text("$\(gift.amount, specifier: "%.2f")")
                    .font(.title2)
                    .fontWeight(.bold)
                    .foregroundStyle(.green)

                Text("for \(gift.childName)")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
        .padding(.vertical, 4)
    }
}

struct ThankYouNotesView: View {
    let childId: UUID
    @State private var viewModel = GiftingViewModel()
    @State private var selectedGift: PendingThankYou?
    @State private var showingCompose = false

    var body: some View {
        List {
            if viewModel.pendingThankYous.isEmpty {
                ContentUnavailableView(
                    "All Caught Up!",
                    systemImage: "heart.fill",
                    description: Text("You've thanked everyone for their gifts.")
                )
            } else {
                Section {
                    ForEach(viewModel.pendingThankYous) { pending in
                        ThankYouRow(pending: pending)
                            .onTapGesture {
                                selectedGift = pending
                                showingCompose = true
                            }
                    }
                } footer: {
                    Text("Tap to write a thank you note")
                }
            }
        }
        .navigationTitle("Thank You Notes")
        .sheet(isPresented: $showingCompose) {
            if let pending = selectedGift {
                ComposeThankYouView(pending: pending, viewModel: viewModel)
            }
        }
        .task {
            await viewModel.loadPendingThankYous(childId: childId)
        }
    }
}

struct ThankYouRow: View {
    let pending: PendingThankYou

    var body: some View {
        HStack {
            VStack(alignment: .leading, spacing: 4) {
                Text(pending.giverName)
                    .font(.headline)

                Text("$\(pending.amount, specifier: "%.2f") for \(pending.occasion.displayName)")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
            }

            Spacer()

            VStack(alignment: .trailing) {
                if pending.daysSinceReceived > 7 {
                    Text("\(pending.daysSinceReceived) days ago")
                        .font(.caption)
                        .foregroundStyle(.orange)
                } else {
                    Text("\(pending.daysSinceReceived)d ago")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                Image(systemName: "envelope.badge")
                    .foregroundStyle(.blue)
            }
        }
    }
}

struct ComposeThankYouView: View {
    let pending: PendingThankYou
    @Bindable var viewModel: GiftingViewModel
    @State private var message = ""
    @Environment(\.dismiss) private var dismiss

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    VStack(alignment: .leading, spacing: 8) {
                        Text("To: \(pending.giverName)")
                            .font(.headline)

                        Text("For: $\(pending.amount, specifier: "%.2f") \(pending.occasion.displayName) gift")
                            .font(.subheadline)
                            .foregroundStyle(.secondary)
                    }
                }

                Section("Your Message") {
                    TextEditor(text: $message)
                        .frame(minHeight: 150)
                }

                Section {
                    Text("Suggested: \"Thank you so much for the \(pending.occasion.displayName.lowercased()) gift! I really appreciate it.\"")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                        .onTapGesture {
                            message = "Thank you so much for the \(pending.occasion.displayName.lowercased()) gift! I really appreciate it."
                        }
                }
            }
            .navigationTitle("Thank You Note")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") { dismiss() }
                }
                ToolbarItem(placement: .primaryAction) {
                    Button("Send") {
                        Task {
                            await viewModel.sendThankYouNote(
                                giftId: pending.giftId,
                                message: message,
                                imageUrl: nil
                            )
                            dismiss()
                        }
                    }
                    .disabled(message.isEmpty)
                }
            }
        }
    }
}
```

---

## Guest Portal Web Page

The guest portal is a simple, mobile-friendly web page for gift submission:

```tsx
// pages/gift/[token].tsx
import { useState, useEffect } from 'react';
import { useRouter } from 'next/router';

interface PortalData {
  childDisplayName: string;
  childPhotoUrl?: string;
  welcomeMessage?: string;
  minGiftAmount?: number;
  maxGiftAmount?: number;
  occasions: Array<{ value: number; name: string; iconName: string }>;
  savingsGoals?: Array<{ id: string; name: string; progressPercentage: number }>;
  wishListItems?: Array<{ id: string; name: string; price: number }>;
}

export default function GiftPortal() {
  const router = useRouter();
  const { token } = router.query;

  const [portalData, setPortalData] = useState<PortalData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [submitted, setSubmitted] = useState(false);

  const [amount, setAmount] = useState('');
  const [occasion, setOccasion] = useState(9); // Just Because
  const [message, setMessage] = useState('');
  const [giverName, setGiverName] = useState('');
  const [giverEmail, setGiverEmail] = useState('');
  const [giverRelationship, setGiverRelationship] = useState('');

  useEffect(() => {
    if (token) {
      fetchPortalData();
    }
  }, [token]);

  const fetchPortalData = async () => {
    try {
      const res = await fetch(`/api/v1/gifts/portal/${token}`);
      if (!res.ok) throw new Error('Invalid or expired link');
      const data = await res.json();
      setPortalData(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const submitGift = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      const res = await fetch(`/api/v1/gifts/portal/${token}/submit`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          amount: parseFloat(amount),
          occasion,
          message,
          giverName,
          giverEmail,
          giverRelationship,
        }),
      });

      if (!res.ok) throw new Error('Failed to submit gift');
      setSubmitted(true);
    } catch (err) {
      setError(err.message);
    }
  };

  if (loading) return <div className="loading">Loading...</div>;
  if (error) return <div className="error">{error}</div>;
  if (!portalData) return null;

  if (submitted) {
    return (
      <div className="success-page">
        <h1>Thank You!</h1>
        <p>Your gift has been submitted and is awaiting approval.</p>
        <p>{portalData.childDisplayName} will be so happy!</p>
      </div>
    );
  }

  return (
    <div className="gift-portal">
      <header>
        {portalData.childPhotoUrl && (
          <img src={portalData.childPhotoUrl} alt="" className="child-photo" />
        )}
        <h1>Send a Gift to {portalData.childDisplayName}</h1>
        {portalData.welcomeMessage && (
          <p className="welcome">{portalData.welcomeMessage}</p>
        )}
      </header>

      <form onSubmit={submitGift}>
        <div className="form-group">
          <label>Gift Amount</label>
          <input
            type="number"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            min={portalData.minGiftAmount || 1}
            max={portalData.maxGiftAmount || 1000}
            required
          />
        </div>

        <div className="form-group">
          <label>Occasion</label>
          <select value={occasion} onChange={(e) => setOccasion(Number(e.target.value))}>
            {portalData.occasions.map((occ) => (
              <option key={occ.value} value={occ.value}>{occ.name}</option>
            ))}
          </select>
        </div>

        <div className="form-group">
          <label>Your Name</label>
          <input
            type="text"
            value={giverName}
            onChange={(e) => setGiverName(e.target.value)}
            required
          />
        </div>

        <div className="form-group">
          <label>Your Email (for thank you note)</label>
          <input
            type="email"
            value={giverEmail}
            onChange={(e) => setGiverEmail(e.target.value)}
          />
        </div>

        <div className="form-group">
          <label>Relationship (e.g., Grandma, Uncle)</label>
          <input
            type="text"
            value={giverRelationship}
            onChange={(e) => setGiverRelationship(e.target.value)}
          />
        </div>

        <div className="form-group">
          <label>Message (optional)</label>
          <textarea
            value={message}
            onChange={(e) => setMessage(e.target.value)}
            rows={3}
          />
        </div>

        <button type="submit" className="submit-btn">
          Send Gift
        </button>
      </form>
    </div>
  );
}
```

---

## Testing Strategy

### Unit Tests - 35 tests

```csharp
public class GiftLinkServiceTests
{
    [Fact]
    public async Task CreateLink_GeneratesSecureToken() { }

    [Fact]
    public async Task ValidateToken_ReturnsNullForExpired() { }

    [Fact]
    public async Task ValidateToken_ReturnsNullForMaxUses() { }
}

public class GiftServiceTests
{
    [Fact]
    public async Task SubmitGift_ValidatesAmountLimits() { }

    [Fact]
    public async Task ApproveGift_CreatesTransaction() { }

    [Fact]
    public async Task ApproveGift_AllocatesToGoal() { }

    [Fact]
    public async Task ApproveGift_SendsNotifications() { }
}

public class ThankYouNoteServiceTests
{
    [Fact]
    public async Task SendNote_SendsEmail() { }

    [Fact]
    public async Task SendNote_MarksAsSent() { }

    [Fact]
    public async Task SendNote_FailsWithoutEmail() { }
}
```

---

## Implementation Phases

### Phase 1: Database & Models (2 days)
- [ ] Create GiftLink, Gift, ThankYouNote models
- [ ] Add database migration
- [ ] Implement token generation

### Phase 2: Gift Link Service (2 days)
- [ ] Implement IGiftLinkService
- [ ] Token validation logic

### Phase 3: Gift Service (3 days)
- [ ] Implement portal data endpoint
- [ ] Implement gift submission
- [ ] Implement approval workflow

### Phase 4: Thank You Notes (2 days)
- [ ] Implement IThankYouNoteService
- [ ] Email sending integration

### Phase 5: API Controllers (2 days)
- [ ] Implement controllers
- [ ] Test public/private endpoints

### Phase 6: Guest Portal (2 days)
- [ ] Create web portal page
- [ ] Style for mobile

### Phase 7: iOS Implementation (3 days)
- [ ] Gift links management
- [ ] Pending gifts approval
- [ ] Thank you note composition

---

## Success Criteria

- [ ] Gift links generate unique, secure tokens
- [ ] Guest portal works without authentication
- [ ] Parents can approve/reject gifts
- [ ] Thank you notes send via email
- [ ] Privacy controls respected
- [ ] >90% test coverage

---

This specification provides a complete extended family gifting system.
