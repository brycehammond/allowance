namespace AllowanceTracker.Services;

public interface ICurrentUserService
{
    Guid UserId { get; }
    string Email { get; }
    bool IsParent { get; }
    Guid? FamilyId { get; }
    Guid? ChildId { get; }
}
