namespace AllowanceTracker.DTOs;

public record FamilyMembersDto(
    Guid FamilyId,
    string FamilyName,
    List<FamilyMemberDto> Members);
