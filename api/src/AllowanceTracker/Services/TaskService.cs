using AllowanceTracker.Data;
using AllowanceTracker.DTOs.Tasks;
using AllowanceTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Services;

public class TaskService : ITaskService
{
    private readonly AllowanceContext _context;
    private readonly INotificationService? _notificationService;
    private readonly IAchievementService? _achievementService;

    public TaskService(
        AllowanceContext context,
        INotificationService? notificationService = null,
        IAchievementService? achievementService = null)
    {
        _context = context;
        _notificationService = notificationService;
        _achievementService = achievementService;
    }

    public async Task<TaskDto> CreateTaskAsync(CreateTaskDto dto, Guid createdById)
    {
        // Verify child exists
        var child = await _context.Children
            .Include(c => c.User)
            .Include(c => c.Family)
            .FirstOrDefaultAsync(c => c.Id == dto.ChildId);

        if (child == null)
        {
            throw new InvalidOperationException("Child not found");
        }

        // Verify creator is in same family
        var creator = await _context.Users.FindAsync(createdById);
        if (creator == null || creator.FamilyId != child.FamilyId)
        {
            throw new UnauthorizedAccessException("Cannot create task for child in different family");
        }

        var task = new ChoreTask
        {
            Id = Guid.NewGuid(),
            ChildId = dto.ChildId,
            Title = dto.Title,
            Description = dto.Description,
            RewardAmount = dto.RewardAmount,
            Status = ChoreTaskStatus.Active,
            IsRecurring = dto.IsRecurring,
            RecurrenceType = dto.RecurrenceType,
            RecurrenceDay = dto.RecurrenceDay,
            RecurrenceDayOfMonth = dto.RecurrenceDayOfMonth,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Send notification to the child about the new task
        if (_notificationService != null)
        {
            try
            {
                var rewardText = dto.RewardAmount > 0 ? $" Worth {dto.RewardAmount:C}!" : "";
                await _notificationService.SendNotificationAsync(
                    child.UserId,
                    NotificationType.TaskAssigned,
                    "New Task Assigned!",
                    $"You have a new task: {dto.Title}.{rewardText}",
                    data: new { taskId = task.Id, title = dto.Title, rewardAmount = dto.RewardAmount },
                    relatedEntityId: task.Id,
                    relatedEntityType: "Task");
            }
            catch
            {
                // Don't fail task creation if notification fails
            }
        }

        return await MapToDto(task, child, creator);
    }

    public async Task<TaskDto> UpdateTaskAsync(Guid taskId, UpdateTaskDto dto, Guid userId)
    {
        var task = await _context.Tasks
            .Include(t => t.Child)
                .ThenInclude(c => c.User)
            .Include(t => t.CreatedBy)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }

        // Verify user is in same family
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.FamilyId != task.Child.FamilyId)
        {
            throw new UnauthorizedAccessException("Cannot update task for different family");
        }

        // Check for pending approvals
        var hasPendingApprovals = await _context.TaskCompletions
            .AnyAsync(tc => tc.TaskId == taskId && tc.Status == CompletionStatus.PendingApproval);

        if (hasPendingApprovals)
        {
            throw new InvalidOperationException("Cannot update task with pending approvals");
        }

        task.Title = dto.Title;
        task.Description = dto.Description;
        task.RewardAmount = dto.RewardAmount;
        task.IsRecurring = dto.IsRecurring;
        task.RecurrenceType = dto.RecurrenceType;
        task.RecurrenceDay = dto.RecurrenceDay;
        task.RecurrenceDayOfMonth = dto.RecurrenceDayOfMonth;

        await _context.SaveChangesAsync();

        return await MapToDto(task, task.Child, task.CreatedBy);
    }

    public async Task<TaskDto> GetTaskByIdAsync(Guid taskId, Guid userId)
    {
        var task = await _context.Tasks
            .Include(t => t.Child)
                .ThenInclude(c => c.User)
            .Include(t => t.CreatedBy)
            .Include(t => t.Completions)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }

        // Verify user is in same family
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.FamilyId != task.Child.FamilyId)
        {
            throw new UnauthorizedAccessException("Cannot access task from different family");
        }

        return await MapToDto(task, task.Child, task.CreatedBy);
    }

    public async Task<List<TaskDto>> GetTasksAsync(Guid? childId, ChoreTaskStatus? status, bool? isRecurring, Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.FamilyId == null)
        {
            throw new UnauthorizedAccessException("User not found or not in a family");
        }

        var query = _context.Tasks
            .Include(t => t.Child)
                .ThenInclude(c => c.User)
            .Include(t => t.CreatedBy)
            .Include(t => t.Completions)
            .Where(t => t.Child.FamilyId == user.FamilyId);

        if (childId.HasValue)
        {
            query = query.Where(t => t.ChildId == childId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (isRecurring.HasValue)
        {
            query = query.Where(t => t.IsRecurring == isRecurring.Value);
        }

        var tasks = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

        var taskDtos = new List<TaskDto>();
        foreach (var task in tasks)
        {
            taskDtos.Add(await MapToDto(task, task.Child, task.CreatedBy));
        }

        return taskDtos;
    }

    public async Task ArchiveTaskAsync(Guid taskId, Guid userId)
    {
        var task = await _context.Tasks
            .Include(t => t.Child)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }

        // Verify user is in same family
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.FamilyId != task.Child.FamilyId)
        {
            throw new UnauthorizedAccessException("Cannot archive task from different family");
        }

        task.Status = ChoreTaskStatus.Archived;
        task.ArchivedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<TaskCompletionDto> CompleteTaskAsync(Guid taskId, CompleteTaskDto dto, Guid childUserId)
    {
        var task = await _context.Tasks
            .Include(t => t.Child)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }

        if (task.Status == ChoreTaskStatus.Archived)
        {
            throw new InvalidOperationException("Cannot complete archived task");
        }

        // Verify child is assigned to this task
        if (task.Child.UserId != childUserId)
        {
            throw new UnauthorizedAccessException("Can only complete tasks assigned to you");
        }

        var completion = new TaskCompletion
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            ChildId = task.ChildId,
            CompletedAt = DateTime.UtcNow,
            Notes = dto.Notes,
            PhotoUrl = dto.PhotoUrl,
            Status = CompletionStatus.PendingApproval
        };

        _context.TaskCompletions.Add(completion);
        await _context.SaveChangesAsync();

        // Send notification to parents about pending approval
        if (_notificationService != null)
        {
            try
            {
                var childName = task.Child.User.FirstName;
                await _notificationService.SendFamilyNotificationAsync(
                    task.Child.FamilyId,
                    NotificationType.TaskCompletionPendingApproval,
                    "Task Awaiting Approval",
                    $"{childName} completed \"{task.Title}\" and is waiting for approval.",
                    excludeUserId: task.Child.UserId);
            }
            catch
            {
                // Don't fail completion if notification fails
            }
        }

        return await MapCompletionToDto(completion, task, task.Child);
    }

    public async Task<List<TaskCompletionDto>> GetTaskCompletionsAsync(
        Guid taskId,
        CompletionStatus? status,
        DateTime? startDate,
        DateTime? endDate,
        Guid userId)
    {
        var task = await _context.Tasks
            .Include(t => t.Child)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }

        // Verify user is in same family
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.FamilyId != task.Child.FamilyId)
        {
            throw new UnauthorizedAccessException("Cannot access completions from different family");
        }

        var query = _context.TaskCompletions
            .Include(tc => tc.Task)
            .Include(tc => tc.Child)
                .ThenInclude(c => c.User)
            .Include(tc => tc.ApprovedBy)
            .Where(tc => tc.TaskId == taskId);

        if (status.HasValue)
        {
            query = query.Where(tc => tc.Status == status.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(tc => tc.CompletedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(tc => tc.CompletedAt <= endDate.Value);
        }

        var completions = await query.OrderByDescending(tc => tc.CompletedAt).ToListAsync();

        var completionDtos = new List<TaskCompletionDto>();
        foreach (var completion in completions)
        {
            completionDtos.Add(await MapCompletionToDto(completion, completion.Task, completion.Child));
        }

        return completionDtos;
    }

    public async Task<List<TaskCompletionDto>> GetPendingApprovalsAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.FamilyId == null)
        {
            throw new UnauthorizedAccessException("User not found or not in a family");
        }

        var completions = await _context.TaskCompletions
            .Include(tc => tc.Task)
            .Include(tc => tc.Child)
                .ThenInclude(c => c.User)
            .Include(tc => tc.ApprovedBy)
            .Where(tc => tc.Child.FamilyId == user.FamilyId && tc.Status == CompletionStatus.PendingApproval)
            .OrderBy(tc => tc.CompletedAt) // Oldest first
            .ToListAsync();

        var completionDtos = new List<TaskCompletionDto>();
        foreach (var completion in completions)
        {
            completionDtos.Add(await MapCompletionToDto(completion, completion.Task, completion.Child));
        }

        return completionDtos;
    }

    public async Task<TaskCompletionDto> ReviewCompletionAsync(Guid completionId, ReviewCompletionDto dto, Guid userId)
    {
        var completion = await _context.TaskCompletions
            .Include(tc => tc.Task)
            .Include(tc => tc.Child)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(tc => tc.Id == completionId);

        if (completion == null)
        {
            throw new InvalidOperationException("Completion not found");
        }

        if (completion.Status != CompletionStatus.PendingApproval)
        {
            throw new InvalidOperationException("Completion already reviewed");
        }

        // Verify user is in same family
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.FamilyId != completion.Child.FamilyId)
        {
            throw new UnauthorizedAccessException("Cannot review completion from different family");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (dto.IsApproved)
            {
                // Approve and create payment
                completion.Status = CompletionStatus.Approved;
                completion.ApprovedById = userId;
                completion.ApprovedAt = DateTime.UtcNow;

                // Create transaction for payment
                var child = completion.Child;
                var paymentTransaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    ChildId = completion.ChildId,
                    Amount = completion.Task.RewardAmount,
                    Type = TransactionType.Credit,
                    Category = TransactionCategory.Task,
                    Description = $"Task completed: {completion.Task.Title}",
                    BalanceAfter = child.CurrentBalance + completion.Task.RewardAmount,
                    CreatedById = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Transactions.Add(paymentTransaction);
                child.CurrentBalance = paymentTransaction.BalanceAfter;
                completion.TransactionId = paymentTransaction.Id;
            }
            else
            {
                // Reject
                completion.Status = CompletionStatus.Rejected;
                completion.RejectionReason = dto.RejectionReason;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Send notification to the child about the review result
            if (_notificationService != null)
            {
                try
                {
                    if (dto.IsApproved)
                    {
                        await _notificationService.SendNotificationAsync(
                            completion.Child.UserId,
                            NotificationType.TaskApproved,
                            "Task Approved!",
                            $"Great job! Your task \"{completion.Task.Title}\" was approved. You earned {completion.Task.RewardAmount:C}!",
                            data: new { taskId = completion.TaskId, completionId = completion.Id, rewardAmount = completion.Task.RewardAmount },
                            relatedEntityId: completion.Id,
                            relatedEntityType: "TaskCompletion");
                    }
                    else
                    {
                        var reasonText = string.IsNullOrEmpty(dto.RejectionReason) ? "" : $" Reason: {dto.RejectionReason}";
                        await _notificationService.SendNotificationAsync(
                            completion.Child.UserId,
                            NotificationType.TaskRejected,
                            "Task Needs Redo",
                            $"Your task \"{completion.Task.Title}\" needs to be redone.{reasonText}",
                            data: new { taskId = completion.TaskId, completionId = completion.Id, rejectionReason = dto.RejectionReason },
                            relatedEntityId: completion.Id,
                            relatedEntityType: "TaskCompletion");
                    }
                }
                catch
                {
                    // Don't fail the review if notification fails
                }
            }

            // Check for badge unlocks after task approval
            if (dto.IsApproved && _achievementService != null)
            {
                try
                {
                    await _achievementService.CheckAndUnlockBadgesAsync(
                        completion.ChildId,
                        BadgeTrigger.TaskApproved,
                        new { TaskId = completion.TaskId, RewardAmount = completion.Task.RewardAmount });
                }
                catch
                {
                    // Don't fail the review if badge check fails
                }
            }

            return await MapCompletionToDto(completion, completion.Task, completion.Child);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<TaskStatisticsDto> GetTaskStatisticsAsync(Guid childId, Guid userId)
    {
        var child = await _context.Children
            .Include(c => c.Tasks)
                .ThenInclude(t => t.Completions)
            .FirstOrDefaultAsync(c => c.Id == childId);

        if (child == null)
        {
            throw new InvalidOperationException("Child not found");
        }

        // Verify user is in same family
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.FamilyId != child.FamilyId)
        {
            throw new UnauthorizedAccessException("Cannot access statistics from different family");
        }

        var totalTasks = child.Tasks.Count;
        var activeTasks = child.Tasks.Count(t => t.Status == ChoreTaskStatus.Active);
        var archivedTasks = child.Tasks.Count(t => t.Status == ChoreTaskStatus.Archived);

        var allCompletions = child.Tasks.SelectMany(t => t.Completions).ToList();
        var totalCompletions = allCompletions.Count;
        var pendingApprovals = allCompletions.Count(c => c.Status == CompletionStatus.PendingApproval);
        var approvedCompletions = allCompletions.Where(c => c.Status == CompletionStatus.Approved).ToList();

        var totalEarned = approvedCompletions.Sum(c => child.Tasks.First(t => t.Id == c.TaskId).RewardAmount);
        var pendingEarnings = allCompletions
            .Where(c => c.Status == CompletionStatus.PendingApproval)
            .Sum(c => child.Tasks.First(t => t.Id == c.TaskId).RewardAmount);

        var completionRate = totalCompletions > 0
            ? (double)approvedCompletions.Count / totalCompletions * 100
            : 0.0;

        return new TaskStatisticsDto(
            TotalTasks: totalTasks,
            ActiveTasks: activeTasks,
            ArchivedTasks: archivedTasks,
            TotalCompletions: totalCompletions,
            PendingApprovals: pendingApprovals,
            TotalEarned: totalEarned,
            PendingEarnings: pendingEarnings,
            CompletionRate: Math.Round(completionRate, 2)
        );
    }

    private async Task<TaskDto> MapToDto(ChoreTask task, Child child, ApplicationUser creator)
    {
        var totalCompletions = task.Completions?.Count ?? 0;
        var pendingApprovals = task.Completions?.Count(c => c.Status == CompletionStatus.PendingApproval) ?? 0;
        var lastCompletedAt = task.Completions?
            .OrderByDescending(c => c.CompletedAt)
            .FirstOrDefault()?.CompletedAt;

        return new TaskDto(
            Id: task.Id,
            ChildId: task.ChildId,
            ChildName: child.User.FullName,
            Title: task.Title,
            Description: task.Description,
            RewardAmount: task.RewardAmount,
            Status: task.Status,
            IsRecurring: task.IsRecurring,
            RecurrenceType: task.RecurrenceType,
            RecurrenceDisplay: task.RecurrenceDisplay,
            CreatedAt: task.CreatedAt,
            CreatedById: task.CreatedById,
            CreatedByName: creator.FullName,
            TotalCompletions: totalCompletions,
            PendingApprovals: pendingApprovals,
            LastCompletedAt: lastCompletedAt
        );
    }

    private async Task<TaskCompletionDto> MapCompletionToDto(TaskCompletion completion, ChoreTask task, Child child)
    {
        var approvedByName = completion.ApprovedById.HasValue
            ? (await _context.Users.FindAsync(completion.ApprovedById.Value))?.FullName
            : null;

        return new TaskCompletionDto(
            Id: completion.Id,
            TaskId: completion.TaskId,
            TaskTitle: task.Title,
            RewardAmount: task.RewardAmount,
            ChildId: completion.ChildId,
            ChildName: child.User.FullName,
            CompletedAt: completion.CompletedAt,
            Notes: completion.Notes,
            PhotoUrl: completion.PhotoUrl,
            Status: completion.Status,
            ApprovedById: completion.ApprovedById,
            ApprovedByName: approvedByName,
            ApprovedAt: completion.ApprovedAt,
            RejectionReason: completion.RejectionReason,
            TransactionId: completion.TransactionId
        );
    }

    public async Task<int> GenerateRecurringTasksAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var generatedCount = 0;

        // Get all active recurring tasks
        var recurringTasks = await _context.Tasks
            .Include(t => t.Completions)
            .Where(t => t.IsRecurring && t.Status == ChoreTaskStatus.Active)
            .ToListAsync();

        foreach (var task in recurringTasks)
        {
            // Check if we should generate a task instance today
            if (!ShouldGenerateToday(task, now))
            {
                continue;
            }

            // Check if already has a completion for today
            var hasCompletionToday = task.Completions.Any(c =>
                c.CompletedAt.Date == today);

            if (hasCompletionToday)
            {
                continue;
            }

            // Create a new pending completion for today
            var completion = new TaskCompletion
            {
                Id = Guid.NewGuid(),
                TaskId = task.Id,
                ChildId = task.ChildId,
                CompletedAt = now,
                Status = CompletionStatus.PendingApproval,
                Notes = $"Auto-generated recurring task for {today:yyyy-MM-dd}"
            };

            _context.TaskCompletions.Add(completion);
            generatedCount++;
        }

        if (generatedCount > 0)
        {
            await _context.SaveChangesAsync();
        }

        return generatedCount;
    }

    private bool ShouldGenerateToday(ChoreTask task, DateTime now)
    {
        if (task.RecurrenceType == null)
        {
            return false;
        }

        return task.RecurrenceType switch
        {
            RecurrenceType.Daily => true,
            RecurrenceType.Weekly => task.RecurrenceDay == now.DayOfWeek,
            RecurrenceType.Monthly => task.RecurrenceDayOfMonth == now.Day,
            _ => false
        };
    }
}
