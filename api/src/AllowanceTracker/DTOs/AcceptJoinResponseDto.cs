namespace AllowanceTracker.DTOs;

/// <summary>
/// Response after accepting a family join request
/// </summary>
public record AcceptJoinResponseDto(
    Guid FamilyId,
    string FamilyName,
    string Message);
