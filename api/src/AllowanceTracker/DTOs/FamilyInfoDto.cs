namespace AllowanceTracker.DTOs;

public record FamilyInfoDto(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    Guid OwnerId,
    string OwnerName,
    int MemberCount,
    int ChildrenCount);
