# Co-Parent Management Specification

## Overview

This specification defines the co-parent management feature, which allows families to have multiple parents managing children's allowances together. The feature introduces an account owner concept and a complete invitation workflow for adding co-parents.

## Goals

- Allow multiple parents to manage a family's allowances
- Establish clear ownership with one designated account owner
- Provide secure invitation workflow for adding co-parents
- Enable owner-only actions (removing co-parents, transferring ownership)
- Support both new user registration and existing user joining via invitations

## User Stories

### As an Account Owner, I want to:
- Invite co-parents to join my family by email
- View all pending and past invitations
- Cancel pending invitations
- Remove co-parents from my family
- Transfer ownership to another parent

### As a Co-Parent, I want to:
- Accept an invitation to join a family
- Invite other co-parents (same permissions as owner for invitations)
- Manage children's allowances equally with other parents
- Leave a family voluntarily

### As an Invited User, I want to:
- Receive an email invitation with a secure link
- Accept the invitation and join the family
- Register a new account if I don't have one

## Data Model Changes

### Family Model (Modified)

```csharp
public class Family : IHasCreatedAt
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // NEW: Account owner reference
    public Guid OwnerId { get; set; }

    // Navigation properties
    public virtual ApplicationUser Owner { get; set; } = null!;
    public virtual ICollection<ApplicationUser> Members { get; set; } = new List<ApplicationUser>();
    public virtual ICollection<Child> Children { get; set; } = new List<Child>();
    public virtual ICollection<ParentInvite> Invitations { get; set; } = new List<ParentInvite>();
}
```

### ParentInvite Model (Already Exists - No Changes Needed)

The existing `ParentInvite` model already has all required fields:
- `Id`, `InvitedEmail`, `FirstName`, `LastName`
- `FamilyId`, `InvitedById`, `Token`
- `IsExistingUser`, `ExistingUserId`
- `Status` (Pending, Accepted, Expired, Cancelled)
- `ExpiresAt`, `CreatedAt`, `AcceptedAt`

### Migration Strategy for Existing Families

Add a migration that:
1. Adds `OwnerId` column to `Families` table (non-nullable after population)
2. Populates `OwnerId` with the first parent who joined each family (earliest `ApplicationUser.Id` where `Role = Parent` and `FamilyId` matches)
3. Adds foreign key constraint

```csharp
// Migration pseudo-code
migrationBuilder.AddColumn<Guid>(
    name: "OwnerId",
    table: "Families",
    nullable: true);

// SQL to populate based on earliest parent
migrationBuilder.Sql(@"
    UPDATE Families
    SET OwnerId = (
        SELECT TOP 1 Id
        FROM AspNetUsers
        WHERE FamilyId = Families.Id
          AND Role = 0  -- Parent
        ORDER BY (SELECT NULL)  -- First inserted
    )
");

migrationBuilder.AlterColumn<Guid>(
    name: "OwnerId",
    table: "Families",
    nullable: false);

migrationBuilder.AddForeignKey(
    name: "FK_Families_AspNetUsers_OwnerId",
    table: "Families",
    column: "OwnerId",
    principalTable: "AspNetUsers",
    principalColumn: "Id",
    onDelete: ReferentialAction.Restrict);
```

## API Specification

### DTOs

```csharp
// Request DTOs
public record CreateParentInviteDto(
    string Email,
    string FirstName,
    string LastName);

public record AcceptInviteDto(
    string Token,
    string? Password);  // Required only if new user

public record TransferOwnershipDto(
    Guid NewOwnerId);

// Response DTOs
public record ParentInviteDto(
    Guid Id,
    string InvitedEmail,
    string FirstName,
    string LastName,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? AcceptedAt,
    string InvitedByName);

public record InviteDetailsDto(
    Guid Id,
    string FamilyName,
    string InvitedByName,
    string InvitedEmail,
    string FirstName,
    string LastName,
    bool IsExistingUser,
    bool IsExpired,
    DateTime ExpiresAt);

public record FamilyMemberDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsOwner,
    DateTime JoinedAt);  // Based on user creation or invite acceptance

public record FamilyInfoDto(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    Guid OwnerId,
    string OwnerName,
    int MemberCount,
    int ChildrenCount);
```

### API Endpoints

#### Invitation Management

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/v1/families/current/invitations` | Parent | List all invitations for the family |
| POST | `/api/v1/families/current/invitations` | Parent | Create a new invitation |
| DELETE | `/api/v1/families/current/invitations/{id}` | Parent | Cancel a pending invitation |
| GET | `/api/v1/invitations/{token}` | Anonymous | Get invitation details by token |
| POST | `/api/v1/invitations/{token}/accept` | Anonymous | Accept an invitation |

#### Co-Parent Management (Owner Only)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| DELETE | `/api/v1/families/current/members/{userId}` | Owner | Remove a co-parent from family |
| POST | `/api/v1/families/current/transfer-ownership` | Owner | Transfer ownership to another parent |

#### Self-Service

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/v1/families/current/leave` | Co-Parent | Leave the family (non-owners only) |

### Endpoint Details

#### POST `/api/v1/families/current/invitations`

Creates a new co-parent invitation and sends an email.

**Request:**
```json
{
  "email": "coparent@example.com",
  "firstName": "Jane",
  "lastName": "Doe"
}
```

**Response (201 Created):**
```json
{
  "id": "guid",
  "invitedEmail": "coparent@example.com",
  "firstName": "Jane",
  "lastName": "Doe",
  "status": "Pending",
  "createdAt": "2024-01-15T10:00:00Z",
  "expiresAt": "2024-01-22T10:00:00Z",
  "acceptedAt": null,
  "invitedByName": "John Smith"
}
```

**Validation:**
- Email must be valid format
- Cannot invite someone already in the family
- Cannot invite someone with a pending invitation to this family
- Cannot invite children (users with Role = Child)

#### GET `/api/v1/invitations/{token}`

Gets invitation details for display on accept page (anonymous access).

**Response:**
```json
{
  "id": "guid",
  "familyName": "Smith Family",
  "invitedByName": "John Smith",
  "invitedEmail": "coparent@example.com",
  "firstName": "Jane",
  "lastName": "Doe",
  "isExistingUser": false,
  "isExpired": false,
  "expiresAt": "2024-01-22T10:00:00Z"
}
```

#### POST `/api/v1/invitations/{token}/accept`

Accepts an invitation. Behavior differs based on user status:

**For New Users:**
```json
{
  "token": "secure-token",
  "password": "SecurePassword123!"
}
```
- Creates new ApplicationUser with Role = Parent
- Assigns to family
- Marks invitation as Accepted

**For Existing Users:**
```json
{
  "token": "secure-token"
}
```
- User must be authenticated
- User must not already be in a family
- Assigns user to family
- Marks invitation as Accepted

**Response (200 OK):**
```json
{
  "userId": "guid",
  "email": "coparent@example.com",
  "firstName": "Jane",
  "lastName": "Doe",
  "role": "Parent",
  "familyId": "guid",
  "familyName": "Smith Family",
  "token": "jwt-token",
  "expiresAt": "2024-01-16T10:00:00Z"
}
```

#### DELETE `/api/v1/families/current/members/{userId}`

Removes a co-parent from the family. Owner only.

**Validation:**
- Caller must be family owner
- Cannot remove yourself (use transfer ownership first)
- Cannot remove children via this endpoint
- Target must be a parent in the caller's family

**Response (204 No Content)**

**Side Effects:**
- Sets user's `FamilyId` to null
- User loses access to all family data

#### POST `/api/v1/families/current/transfer-ownership`

Transfers family ownership to another parent.

**Request:**
```json
{
  "newOwnerId": "guid"
}
```

**Validation:**
- Caller must be current owner
- New owner must be a parent in the same family
- Cannot transfer to a child

**Response (200 OK):**
```json
{
  "id": "guid",
  "name": "Smith Family",
  "ownerId": "new-owner-guid",
  "ownerName": "Jane Doe"
}
```

#### POST `/api/v1/families/current/leave`

Allows a co-parent to voluntarily leave a family.

**Validation:**
- Caller cannot be the family owner
- Caller must be a parent

**Response (204 No Content)**

## Service Layer

### IParentInviteService

```csharp
public interface IParentInviteService
{
    Task<ParentInviteDto> CreateInviteAsync(CreateParentInviteDto dto, Guid familyId, Guid invitedById);
    Task<List<ParentInviteDto>> GetFamilyInvitesAsync(Guid familyId);
    Task<InviteDetailsDto?> GetInviteByTokenAsync(string token);
    Task CancelInviteAsync(Guid inviteId, Guid familyId);
    Task<AuthResponseDto> AcceptInviteAsync(string token, AcceptInviteDto dto, Guid? currentUserId);
    Task ExpireOldInvitesAsync();  // Called by background job
}
```

### IFamilyService (Extended)

```csharp
public interface IFamilyService
{
    // Existing methods...

    // New methods
    Task<bool> IsOwnerAsync(Guid userId, Guid familyId);
    Task RemoveParentAsync(Guid parentId, Guid familyId, Guid requestingUserId);
    Task TransferOwnershipAsync(Guid newOwnerId, Guid familyId, Guid currentOwnerId);
    Task LeaveFamily Async(Guid userId);
}
```

## Authorization

### Policy Definitions

```csharp
// Add to Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("FamilyOwner", policy =>
        policy.RequireRole("Parent")
              .AddRequirements(new FamilyOwnerRequirement()));
});

// Custom authorization handler
public class FamilyOwnerHandler : AuthorizationHandler<FamilyOwnerRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FamilyOwnerRequirement requirement)
    {
        var userId = context.User.GetUserId();
        var familyId = context.User.GetFamilyId();

        if (await _familyService.IsOwnerAsync(userId, familyId))
        {
            context.Succeed(requirement);
        }
    }
}
```

### Controller Authorization

```csharp
[HttpDelete("members/{userId}")]
[Authorize(Policy = "FamilyOwner")]
public async Task<IActionResult> RemoveParent(Guid userId) { ... }

[HttpPost("transfer-ownership")]
[Authorize(Policy = "FamilyOwner")]
public async Task<IActionResult> TransferOwnership(TransferOwnershipDto dto) { ... }
```

## Email Notifications

### Invitation Email

**Subject:** "You've been invited to join {FamilyName} on Allowance Tracker"

**Content:**
- Inviter's name
- Family name
- Expiration date (7 days)
- Accept invitation link: `{BaseUrl}/invitations/{token}`

### Implementation

Use existing email service pattern with a new template:

```csharp
public interface IEmailService
{
    Task SendParentInviteAsync(string toEmail, string toName, string familyName,
        string inviterName, string acceptUrl, DateTime expiresAt);
}
```

## Background Jobs

### Invitation Expiration Job

Run daily to mark expired invitations:

```csharp
public class InvitationExpirationJob : IHostedService
{
    public async Task ExecuteAsync()
    {
        await _inviteService.ExpireOldInvitesAsync();
    }
}

// In ParentInviteService
public async Task ExpireOldInvitesAsync()
{
    var expiredInvites = await _context.ParentInvites
        .Where(i => i.Status == InviteStatus.Pending && i.ExpiresAt < DateTime.UtcNow)
        .ToListAsync();

    foreach (var invite in expiredInvites)
    {
        invite.Status = InviteStatus.Expired;
    }

    await _context.SaveChangesAsync();
}
```

## Testing Strategy

### Unit Tests

1. **ParentInviteService Tests**
   - CreateInviteAsync generates secure token
   - CreateInviteAsync sets correct expiration (7 days)
   - Cannot invite existing family member
   - Cannot invite user with pending invite
   - AcceptInviteAsync creates user for new user scenario
   - AcceptInviteAsync assigns existing user to family
   - Expired invites cannot be accepted
   - Cancelled invites cannot be accepted

2. **FamilyService Tests**
   - IsOwnerAsync returns true for owner
   - IsOwnerAsync returns false for co-parent
   - RemoveParentAsync removes user from family
   - RemoveParentAsync fails if not owner
   - RemoveParentAsync fails for self-removal
   - TransferOwnershipAsync updates family owner
   - LeaveFamilyAsync removes user from family
   - LeaveFamilyAsync fails for owner

### Integration Tests

1. **Invitation Flow**
   - Full flow: create invite -> accept as new user -> verify family membership
   - Full flow: create invite -> accept as existing user -> verify family membership
   - Expired invite returns appropriate error
   - Cancelled invite returns appropriate error

2. **Co-Parent Management**
   - Owner can remove co-parent
   - Co-parent cannot remove other parents
   - Owner can transfer ownership
   - New owner has correct permissions after transfer

## Security Considerations

1. **Token Security**
   - Invitation tokens are cryptographically secure random strings (32 bytes, base64)
   - Tokens are single-use (marked as Accepted after use)
   - Tokens expire after 7 days

2. **Authorization**
   - Owner-only actions enforced at both controller and service level
   - Family-scoped access prevents cross-family operations

3. **Email Validation**
   - Invitations sent only to valid email addresses
   - No disclosure of whether email exists in system

4. **Rate Limiting**
   - Consider limiting invitation creation (e.g., 10 per day per family)

## UI Considerations (Frontend)

### Family Settings Page

- Display family members with owner badge
- "Invite Co-Parent" button opens invite modal
- List pending invitations with cancel option
- Owner sees "Remove" button next to co-parents
- Owner sees "Transfer Ownership" option

### Invitation Accept Page

- Accessible via `/invitations/{token}`
- Shows family name and inviter
- For existing users: "Join Family" button
- For new users: Registration form with password field
- Error states for expired/cancelled invites

## Implementation Phases

### Phase 1: Data Model (1-2 days)
1. Add `OwnerId` to Family model
2. Create migration with data population
3. Update AllowanceContext relationships
4. Update existing DTOs

### Phase 2: Invitation Service (2-3 days)
1. Create IParentInviteService interface
2. Implement ParentInviteService with TDD
3. Add invitation creation with token generation
4. Add invitation acceptance logic
5. Add expiration job

### Phase 3: Family Management Service (1-2 days)
1. Add IsOwnerAsync method
2. Add RemoveParentAsync method
3. Add TransferOwnershipAsync method
4. Add LeaveFamilyAsync method

### Phase 4: API Controllers (2-3 days)
1. Create invitation endpoints
2. Add owner-only endpoints with authorization
3. Update family info endpoint to include owner
4. Integration tests

### Phase 5: Email Integration (1 day)
1. Create invitation email template
2. Integrate with existing email service
3. Test email delivery

## Design Decisions

1. **Removal Notification**: Yes - send an email to notify co-parents when they are removed from a family
2. **Expired Invitations**: Create new invitations only - no resending expired ones (they remain in history with Expired status)
3. **Owner Demotion**: Not allowed - owner must transfer ownership before leaving
4. **Invitation History**: Retained indefinitely for audit purposes
