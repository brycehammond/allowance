using AllowanceTracker.DTOs;
using AllowanceTracker.Models;

namespace AllowanceTracker.Services;

public interface ISavingsGoalService
{
    // Goal CRUD
    Task<SavingsGoalDto> CreateGoalAsync(CreateSavingsGoalDto dto, Guid userId);
    Task<SavingsGoalDto?> GetGoalByIdAsync(Guid goalId, Guid userId);
    Task<List<SavingsGoalDto>> GetChildGoalsAsync(Guid childId, GoalStatus? status, bool includeCompleted, Guid userId);
    Task<SavingsGoalDto> UpdateGoalAsync(Guid goalId, UpdateSavingsGoalDto dto, Guid userId);
    Task CancelGoalAsync(Guid goalId, Guid userId);
    Task<SavingsGoalDto> PauseGoalAsync(Guid goalId, Guid userId);
    Task<SavingsGoalDto> ResumeGoalAsync(Guid goalId, Guid userId);

    // Contributions
    Task<GoalProgressEventDto> ContributeAsync(Guid goalId, ContributeToGoalDto dto, Guid userId);
    Task<ContributionDto> WithdrawAsync(Guid goalId, WithdrawFromGoalDto dto, Guid userId);
    Task<List<ContributionDto>> GetContributionsAsync(Guid goalId, ContributionType? type, DateTime? startDate, DateTime? endDate);
    Task<SavingsGoalDto> MarkAsPurchasedAsync(Guid goalId, MarkGoalPurchasedDto dto, Guid userId);

    // Auto-transfer (called by AllowanceService)
    Task ProcessAutoTransfersAsync(Guid childId, decimal allowanceAmount);

    // Matching
    Task<MatchingRuleDto> CreateMatchingRuleAsync(Guid goalId, CreateMatchingRuleDto dto, Guid parentId);
    Task<MatchingRuleDto?> GetMatchingRuleAsync(Guid goalId);
    Task<MatchingRuleDto> UpdateMatchingRuleAsync(Guid goalId, UpdateMatchingRuleDto dto, Guid parentId);
    Task RemoveMatchingRuleAsync(Guid goalId, Guid parentId);

    // Challenges
    Task<GoalChallengeDto> CreateChallengeAsync(Guid goalId, CreateGoalChallengeDto dto, Guid parentId);
    Task<GoalChallengeDto?> GetActiveChallengeAsync(Guid goalId);
    Task CancelChallengeAsync(Guid goalId, Guid parentId);
    Task<List<GoalChallengeDto>> GetChildChallengesAsync(Guid childId, Guid userId);

    // Background job
    Task CheckExpiredChallengesAsync();
}
