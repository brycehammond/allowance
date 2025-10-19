namespace AllowanceTracker.DTOs;

public record FamilyInfoDto(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    int MemberCount,
    int ChildrenCount);
