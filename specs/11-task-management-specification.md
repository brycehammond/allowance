# Task Management System Specification

## Overview

The Task Management system allows parents to assign chores and tasks to children with associated monetary rewards. Children complete tasks and request approval, parents review and approve, triggering automatic payment.

---

## Database Schema

### Task Model

```csharp
public class Task
{
    public Guid Id { get; set; }

    // Relationships
    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    public Guid CreatedById { get; set; }
    public virtual ApplicationUser CreatedBy { get; set; } = null!;

    // Task Details
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal RewardAmount { get; set; }

    // Status
    public TaskStatus Status { get; set; } = TaskStatus.Active;

    // Recurrence
    public bool IsRecurring { get; set; }
    public RecurrenceType? RecurrenceType { get; set; }
    public DayOfWeek? RecurrenceDay { get; set; } // For weekly tasks
    public int? RecurrenceDayOfMonth { get; set; } // For monthly tasks

    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }

    // Navigation
    public virtual ICollection<TaskCompletion> Completions { get; set; } = new List<TaskCompletion>();
}

public enum TaskStatus
{
    Active,      // Can be completed
    Archived     // No longer active
}

public enum RecurrenceType
{
    Daily,
    Weekly,
    Monthly
}
```

### TaskCompletion Model

```csharp
public class TaskCompletion
{
    public Guid Id { get; set; }

    // Relationships
    public Guid TaskId { get; set; }
    public virtual Task Task { get; set; } = null!;

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    // Completion Details
    public DateTime CompletedAt { get; set; }
    public string? Notes { get; set; }
    public string? PhotoUrl { get; set; } // Optional proof photo

    // Approval
    public CompletionStatus Status { get; set; } = CompletionStatus.PendingApproval;
    public Guid? ApprovedById { get; set; }
    public virtual ApplicationUser? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }

    // Payment
    public Guid? TransactionId { get; set; }
    public virtual Transaction? Transaction { get; set; }
}

public enum CompletionStatus
{
    PendingApproval,  // Child completed, waiting for parent
    Approved,         // Parent approved, payment created
    Rejected          // Parent rejected
}
```

---

## DTOs

### Request DTOs

```csharp
// Create a task
public record CreateTaskDto(
    Guid ChildId,
    string Title,
    string? Description,
    decimal RewardAmount,
    bool IsRecurring,
    RecurrenceType? RecurrenceType,
    DayOfWeek? RecurrenceDay,
    int? RecurrenceDayOfMonth
);

// Update a task
public record UpdateTaskDto(
    string Title,
    string? Description,
    decimal RewardAmount,
    bool IsRecurring,
    RecurrenceType? RecurrenceType,
    DayOfWeek? RecurrenceDay,
    int? RecurrenceDayOfMonth
);

// Mark task as complete
public record CompleteTaskDto(
    string? Notes,
    string? PhotoUrl
);

// Approve/reject completion
public record ReviewCompletionDto(
    bool IsApproved,
    string? RejectionReason
);
```

### Response DTOs

```csharp
public record TaskDto(
    Guid Id,
    Guid ChildId,
    string ChildName,
    string Title,
    string? Description,
    decimal RewardAmount,
    TaskStatus Status,
    bool IsRecurring,
    RecurrenceType? RecurrenceType,
    string RecurrenceDisplay,
    DateTime CreatedAt,
    Guid CreatedById,
    string CreatedByName,
    int TotalCompletions,
    int PendingApprovals,
    DateTime? LastCompletedAt
);

public record TaskCompletionDto(
    Guid Id,
    Guid TaskId,
    string TaskTitle,
    decimal RewardAmount,
    Guid ChildId,
    string ChildName,
    DateTime CompletedAt,
    string? Notes,
    string? PhotoUrl,
    CompletionStatus Status,
    Guid? ApprovedById,
    string? ApprovedByName,
    DateTime? ApprovedAt,
    string? RejectionReason,
    Guid? TransactionId
);

public record TaskStatisticsDto(
    int TotalTasks,
    int ActiveTasks,
    int ArchivedTasks,
    int TotalCompletions,
    int PendingApprovals,
    decimal TotalEarned,
    decimal PendingEarnings,
    double CompletionRate
);
```

---

## API Endpoints

### Task Management

#### GET /api/v1/tasks
Get all tasks (filtered by family)

**Authorization**: Parent or Child
**Response**: `List<TaskDto>`

**Query Parameters**:
- `childId` (optional) - Filter by specific child
- `status` (optional) - Filter by status (Active, Archived)
- `isRecurring` (optional) - Filter recurring tasks

---

#### GET /api/v1/tasks/{id}
Get task by ID

**Authorization**: Parent or assigned Child
**Response**: `TaskDto`

---

#### POST /api/v1/tasks
Create new task

**Authorization**: Parent only
**Request Body**: `CreateTaskDto`
**Response**: `TaskDto`

**Validation**:
- Title: Required, 1-100 characters
- RewardAmount: > 0, <= 1000
- ChildId: Must exist and be in same family
- If IsRecurring: RecurrenceType required
- If Weekly: RecurrenceDay required
- If Monthly: RecurrenceDayOfMonth required (1-28)

---

#### PUT /api/v1/tasks/{id}
Update task

**Authorization**: Parent only
**Request Body**: `UpdateTaskDto`
**Response**: `TaskDto`

**Notes**:
- Cannot update if task has pending approvals
- Updating recurring settings doesn't affect past completions

---

#### DELETE /api/v1/tasks/{id}
Archive task (soft delete)

**Authorization**: Parent only
**Response**: 204 No Content

**Business Rules**:
- Sets Status = Archived
- Sets ArchivedAt = DateTime.UtcNow
- Cannot be completed once archived
- Completions remain visible in history

---

### Task Completions

#### GET /api/v1/tasks/{taskId}/completions
Get completions for a task

**Authorization**: Parent or assigned Child
**Response**: `List<TaskCompletionDto>`

**Query Parameters**:
- `status` (optional) - Filter by status
- `startDate` (optional) - From date
- `endDate` (optional) - To date

---

#### POST /api/v1/tasks/{taskId}/complete
Mark task as complete

**Authorization**: Assigned Child only
**Request Body**: `CompleteTaskDto`
**Response**: `TaskCompletionDto`

**Business Rules**:
- Task must be Active
- Child must be assigned to task
- Creates TaskCompletion with PendingApproval status
- Optional: Upload photo proof

---

#### GET /api/v1/completions/pending
Get all pending approvals

**Authorization**: Parent only
**Response**: `List<TaskCompletionDto>`

**Notes**:
- Returns all completions with PendingApproval status
- Filtered by family
- Sorted by CompletedAt (oldest first)

---

#### PUT /api/v1/completions/{id}/review
Approve or reject completion

**Authorization**: Parent only
**Request Body**: `ReviewCompletionDto`
**Response**: `TaskCompletionDto`

**Business Rules - Approval**:
- Sets Status = Approved
- Sets ApprovedById = CurrentUserId
- Sets ApprovedAt = DateTime.UtcNow
- Creates Transaction (Credit) for RewardAmount
- Links Transaction to TaskCompletion

**Business Rules - Rejection**:
- Sets Status = Rejected
- Requires RejectionReason
- No transaction created
- Child can re-complete task

---

#### GET /api/v1/children/{childId}/tasks/statistics
Get task statistics for child

**Authorization**: Parent or assigned Child
**Response**: `TaskStatisticsDto`

---

## Service Layer

### ITaskService

```csharp
public interface ITaskService
{
    // Task Management
    Task<TaskDto> CreateTaskAsync(CreateTaskDto dto, Guid createdById);
    Task<TaskDto> UpdateTaskAsync(Guid taskId, UpdateTaskDto dto, Guid userId);
    Task<TaskDto> GetTaskByIdAsync(Guid taskId, Guid userId);
    Task<List<TaskDto>> GetTasksAsync(Guid? childId, TaskStatus? status, bool? isRecurring, Guid userId);
    Task ArchiveTaskAsync(Guid taskId, Guid userId);

    // Task Completions
    Task<TaskCompletionDto> CompleteTaskAsync(Guid taskId, CompleteTaskDto dto, Guid childId);
    Task<List<TaskCompletionDto>> GetTaskCompletionsAsync(Guid taskId, CompletionStatus? status, DateTime? startDate, DateTime? endDate, Guid userId);
    Task<List<TaskCompletionDto>> GetPendingApprovalsAsync(Guid userId);
    Task<TaskCompletionDto> ReviewCompletionAsync(Guid completionId, ReviewCompletionDto dto, Guid userId);

    // Statistics
    Task<TaskStatisticsDto> GetTaskStatisticsAsync(Guid childId, Guid userId);
}
```

---

## Business Rules

### Task Creation
1. Only parents can create tasks
2. RewardAmount must be > 0 and <= $1000
3. Child must be in same family as parent
4. Recurring tasks must have valid recurrence settings

### Task Completion
1. Only assigned child can complete task
2. Task must be Active (not Archived)
3. Can complete recurring tasks multiple times
4. Optional: Upload photo proof

### Approval Process
1. Only parents can approve/reject
2. Must be in same family as child
3. Approval creates automatic payment transaction
4. Rejection requires reason
5. Rejected tasks can be re-completed

### Recurring Tasks
1. **Daily**: Can be completed once per day
2. **Weekly**: Can be completed once per week (on specified day)
3. **Monthly**: Can be completed once per month (on specified day)
4. Recurrence validation happens at completion time

### Payment
1. Payment only occurs on approval
2. Creates Credit transaction with child
3. Links transaction to completion for audit trail
4. Uses child's current balance as starting point

---

## Security & Authorization

### Parent Permissions
- Create, update, archive tasks
- Approve/reject completions
- View all tasks and completions in family

### Child Permissions
- View assigned tasks
- Complete own tasks
- View own completions
- Cannot create/edit/delete tasks
- Cannot approve own completions

### Family Isolation
- All operations scoped to user's family
- Cannot view/modify tasks from other families

---

## Database Indexes

```csharp
// Task indexes
modelBuilder.Entity<Task>()
    .HasIndex(t => t.ChildId);

modelBuilder.Entity<Task>()
    .HasIndex(t => t.Status);

modelBuilder.Entity<Task>()
    .HasIndex(t => t.CreatedById);

// TaskCompletion indexes
modelBuilder.Entity<TaskCompletion>()
    .HasIndex(tc => tc.TaskId);

modelBuilder.Entity<TaskCompletion>()
    .HasIndex(tc => tc.ChildId);

modelBuilder.Entity<TaskCompletion>()
    .HasIndex(tc => tc.Status);

modelBuilder.Entity<TaskCompletion>()
    .HasIndex(tc => tc.CompletedAt);
```

---

## Example Workflows

### Workflow 1: One-Time Task
1. Parent creates task: "Clean your room" - $5
2. Child sees task in task list
3. Child completes task, adds note: "All done!"
4. Parent sees pending approval
5. Parent approves
6. System creates $5 credit transaction
7. Child's balance increases by $5

### Workflow 2: Weekly Recurring Task
1. Parent creates recurring task: "Take out trash" - $2/week (every Monday)
2. Child completes task on Monday
3. Parent approves
4. Child gets $2
5. Next Monday, task is available again
6. Process repeats

### Workflow 3: Rejection Flow
1. Child completes task: "Mow the lawn" - $10
2. Parent reviews and rejects: "Missed the backyard"
3. Child sees rejection reason
4. Child completes task again properly
5. Parent approves
6. Child gets $10

---

## Error Handling

### Common Errors

**400 Bad Request**:
- Invalid reward amount
- Invalid recurrence settings
- Missing required fields

**403 Forbidden**:
- Child trying to create task
- Child trying to approve completion
- User accessing different family's tasks

**404 Not Found**:
- Task doesn't exist
- Completion doesn't exist
- Child not found

**409 Conflict**:
- Trying to complete archived task
- Task already completed (for recurring with restrictions)
- Completion already reviewed

---

## Future Enhancements

### Phase 2 Features
1. **Task Templates**: Pre-defined common chores
2. **Task Categories**: Organize tasks by type
3. **Photo Upload**: Direct photo capture in app
4. **Completion Streak**: Track consecutive completions
5. **Bonus Multipliers**: Extra rewards for streaks
6. **Task Reminders**: Notify when tasks are due
7. **Completion Deadlines**: Time-limited tasks
8. **Partial Completion**: Support for multi-step tasks

### Phase 3 Features
1. **Task Assignment Rotation**: Auto-assign between siblings
2. **Quality Ratings**: Parents rate completion quality
3. **Task Difficulty Levels**: Easy/Medium/Hard with different rewards
4. **Seasonal Tasks**: Holiday-specific chores
5. **Achievement Integration**: Unlock badges for task completion

---

## Testing Strategy

### Unit Tests (Service Layer)
- Task creation validation
- Update authorization checks
- Completion creation
- Approval/rejection logic
- Payment transaction creation
- Recurring task validation

### Integration Tests (API Layer)
- Full workflow: create → complete → approve → payment
- Authorization for different user roles
- Family isolation verification
- Error scenarios

### Test Coverage Target: >90%

---

## Success Metrics

### User Engagement
- Number of tasks created per family
- Task completion rate
- Average time to approval
- Parent approval rate

### Financial Impact
- Total earnings through tasks
- Average reward amount
- Tasks vs. allowance ratio

### System Health
- API response times (<200ms)
- Approval workflow time
- Photo upload success rate

---

## Implementation Checklist

### Backend (.NET)
- [ ] Create Task model
- [ ] Create TaskCompletion model
- [ ] Add database migration
- [ ] Write TaskService tests (TDD)
- [ ] Implement TaskService
- [ ] Write TaskController tests
- [ ] Implement TaskController
- [ ] Update AllowanceContext
- [ ] Register services in DI

### Frontend (React)
- [ ] Create task list view
- [ ] Create task form
- [ ] Create completion view
- [ ] Create approval queue
- [ ] Add pending approval badge
- [ ] Task statistics dashboard

### iOS (SwiftUI)
- [ ] Create Task and TaskCompletion models
- [ ] Add TaskService
- [ ] Create TaskListView
- [ ] Create TaskDetailView
- [ ] Create CompletionView
- [ ] Create ApprovalQueueView
- [ ] Add camera integration for photos

---

This specification provides a complete foundation for implementing the Task Management system following TDD principles and maintaining consistency with the existing AllowanceTracker architecture.
