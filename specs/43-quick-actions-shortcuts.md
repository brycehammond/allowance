# Quick Actions & Shortcuts Specification

## Overview

The Quick Actions system reduces friction for common operations by providing iOS widgets, Siri Shortcuts, recurring transactions, and quick-add templates. Users can check balances at a glance, create transactions with one tap, and automate regular expenses.

Key features:
- iOS home screen widgets (balance, goals, quick actions)
- Siri Shortcuts for voice commands
- Recurring transactions (subscriptions, regular expenses)
- Quick-add templates for common transactions
- Lock screen widgets (iOS 16+)

---

## Database Schema

### RecurringTransaction Model

```csharp
public class RecurringTransaction
{
    public Guid Id { get; set; }

    public Guid ChildId { get; set; }
    public virtual Child Child { get; set; } = null!;

    // Transaction details
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }

    // Recurrence settings
    public RecurrencePattern Pattern { get; set; }
    public int? DayOfWeek { get; set; }      // 0-6 for weekly
    public int? DayOfMonth { get; set; }     // 1-28 for monthly
    public TimeOnly? TimeOfDay { get; set; }  // When to execute

    // Schedule
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? MaxOccurrences { get; set; }
    public int OccurrenceCount { get; set; } = 0;

    // Execution tracking
    public DateTime? LastExecutedAt { get; set; }
    public DateTime NextExecutionDate { get; set; }

    // Status
    public bool IsActive { get; set; } = true;
    public bool IsPaused { get; set; } = false;
    public bool RequiresApproval { get; set; } = false;

    // Metadata
    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public virtual ICollection<Transaction> GeneratedTransactions { get; set; } = new List<Transaction>();
}

public enum RecurrencePattern
{
    Daily = 1,
    Weekly = 2,
    BiWeekly = 3,
    Monthly = 4,
    FirstOfMonth = 5,
    LastOfMonth = 6
}
```

### TransactionTemplate Model

```csharp
public class TransactionTemplate
{
    public Guid Id { get; set; }

    public Guid? FamilyId { get; set; }          // null = global template
    public virtual Family? Family { get; set; }

    public string Name { get; set; } = string.Empty;
    public string IconName { get; set; } = string.Empty;
    public string? Color { get; set; }           // Hex color for display

    public decimal? DefaultAmount { get; set; }
    public TransactionType Type { get; set; }
    public string? Category { get; set; }
    public string? DescriptionTemplate { get; set; }

    public bool IsGlobal { get; set; } = false;  // System-provided
    public bool IsActive { get; set; } = true;
    public int UsageCount { get; set; } = 0;
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }
}
```

### WidgetConfiguration Model

```csharp
public class WidgetConfiguration
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    public string WidgetType { get; set; } = string.Empty;  // "balance", "goal", "quickActions"
    public string WidgetSize { get; set; } = string.Empty;  // "small", "medium", "large"

    // Configuration (JSON)
    public string Configuration { get; set; } = "{}";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

---

## DTOs

### Request DTOs

```csharp
// Recurring transactions
public record CreateRecurringTransactionDto(
    Guid ChildId,
    string Name,
    decimal Amount,
    TransactionType Type,
    string? Category,
    string? Description,
    RecurrencePattern Pattern,
    int? DayOfWeek,
    int? DayOfMonth,
    TimeOnly? TimeOfDay,
    DateTime StartDate,
    DateTime? EndDate,
    int? MaxOccurrences,
    bool RequiresApproval
);

public record UpdateRecurringTransactionDto(
    string? Name,
    decimal? Amount,
    string? Category,
    string? Description,
    RecurrencePattern? Pattern,
    int? DayOfWeek,
    int? DayOfMonth,
    TimeOnly? TimeOfDay,
    DateTime? EndDate,
    int? MaxOccurrences,
    bool? RequiresApproval
);

// Templates
public record CreateTransactionTemplateDto(
    string Name,
    string IconName,
    string? Color,
    decimal? DefaultAmount,
    TransactionType Type,
    string? Category,
    string? DescriptionTemplate
);

public record UpdateTransactionTemplateDto(
    string? Name,
    string? IconName,
    string? Color,
    decimal? DefaultAmount,
    string? Category,
    string? DescriptionTemplate,
    int? SortOrder
);

public record UseTemplateDto(
    Guid ChildId,
    decimal? Amount,        // Override default amount
    string? Description     // Override description
);

// Widget
public record UpdateWidgetConfigDto(
    string WidgetType,
    string WidgetSize,
    object Configuration
);
```

### Response DTOs

```csharp
public record RecurringTransactionDto(
    Guid Id,
    Guid ChildId,
    string ChildName,
    string Name,
    decimal Amount,
    TransactionType Type,
    string TypeName,
    string? Category,
    string? Description,
    RecurrencePattern Pattern,
    string PatternDescription,
    int? DayOfWeek,
    int? DayOfMonth,
    TimeOnly? TimeOfDay,
    DateTime StartDate,
    DateTime? EndDate,
    int? MaxOccurrences,
    int OccurrenceCount,
    DateTime? LastExecutedAt,
    DateTime NextExecutionDate,
    bool IsActive,
    bool IsPaused,
    bool RequiresApproval,
    DateTime CreatedAt
);

public record TransactionTemplateDto(
    Guid Id,
    string Name,
    string IconName,
    string? Color,
    decimal? DefaultAmount,
    TransactionType Type,
    string TypeName,
    string? Category,
    string? DescriptionTemplate,
    bool IsGlobal,
    int UsageCount,
    int SortOrder
);

// Widget data responses
public record BalanceWidgetDataDto(
    Guid ChildId,
    string ChildName,
    string? AvatarUrl,
    decimal CurrentBalance,
    decimal? SavingsBalance,
    decimal TotalBalance,
    string BalanceChangeToday,      // "+$5.00" or "-$2.50"
    DateTime LastUpdated
);

public record GoalWidgetDataDto(
    Guid GoalId,
    string GoalName,
    string? ImageUrl,
    decimal CurrentAmount,
    decimal TargetAmount,
    double ProgressPercentage,
    decimal RemainingAmount,
    int? DaysRemaining
);

public record QuickActionsWidgetDataDto(
    List<TransactionTemplateDto> Templates,
    decimal CurrentBalance
);

public record MultiChildWidgetDataDto(
    List<ChildBalanceSummaryDto> Children
);

public record ChildBalanceSummaryDto(
    Guid ChildId,
    string Name,
    string? AvatarUrl,
    decimal Balance
);

// Siri responses
public record SiriBalanceResponseDto(
    string SpokenResponse,
    decimal Balance,
    string FormattedBalance
);

public record SiriGoalProgressResponseDto(
    string SpokenResponse,
    string GoalName,
    double ProgressPercentage,
    decimal RemainingAmount
);
```

---

## API Endpoints

### Recurring Transactions

#### POST /api/v1/recurring-transactions
Create recurring transaction

**Authorization**: Parent only
**Request Body**: `CreateRecurringTransactionDto`
**Response**: `RecurringTransactionDto`

---

#### GET /api/v1/recurring-transactions
Get all recurring transactions for family

**Authorization**: Parent
**Response**: `List<RecurringTransactionDto>`

---

#### GET /api/v1/children/{childId}/recurring-transactions
Get child's recurring transactions

**Authorization**: Parent or self
**Response**: `List<RecurringTransactionDto>`

---

#### GET /api/v1/recurring-transactions/{id}
Get recurring transaction details

**Authorization**: Family member
**Response**: `RecurringTransactionDto`

---

#### PUT /api/v1/recurring-transactions/{id}
Update recurring transaction

**Authorization**: Parent only
**Request Body**: `UpdateRecurringTransactionDto`
**Response**: `RecurringTransactionDto`

---

#### DELETE /api/v1/recurring-transactions/{id}
Delete recurring transaction

**Authorization**: Parent only
**Response**: 204 No Content

---

#### POST /api/v1/recurring-transactions/{id}/pause
Pause recurring transaction

**Authorization**: Parent only
**Response**: `RecurringTransactionDto`

---

#### POST /api/v1/recurring-transactions/{id}/resume
Resume recurring transaction

**Authorization**: Parent only
**Response**: `RecurringTransactionDto`

---

#### POST /api/v1/recurring-transactions/{id}/execute
Manually execute recurring transaction now

**Authorization**: Parent only
**Response**: `TransactionDto`

---

### Transaction Templates

#### GET /api/v1/templates
Get all templates (global + family)

**Authorization**: Authenticated
**Response**: `List<TransactionTemplateDto>`

---

#### POST /api/v1/templates
Create custom template

**Authorization**: Parent only
**Request Body**: `CreateTransactionTemplateDto`
**Response**: `TransactionTemplateDto`

---

#### PUT /api/v1/templates/{id}
Update template

**Authorization**: Template creator
**Request Body**: `UpdateTransactionTemplateDto`
**Response**: `TransactionTemplateDto`

---

#### DELETE /api/v1/templates/{id}
Delete custom template

**Authorization**: Template creator
**Response**: 204 No Content

---

#### POST /api/v1/templates/{id}/use
Use template to create transaction

**Authorization**: Parent or self
**Request Body**: `UseTemplateDto`
**Response**: `TransactionDto`

---

### Widget Data

#### GET /api/v1/widgets/balance/{childId}
Get balance widget data

**Authorization**: Parent or self
**Response**: `BalanceWidgetDataDto`

---

#### GET /api/v1/widgets/goal/{goalId}
Get goal progress widget data

**Authorization**: Family member
**Response**: `GoalWidgetDataDto`

---

#### GET /api/v1/widgets/quick-actions/{childId}
Get quick actions widget data

**Authorization**: Parent or self
**Response**: `QuickActionsWidgetDataDto`

---

#### GET /api/v1/widgets/family-balances
Get all children's balances (for parents)

**Authorization**: Parent only
**Response**: `MultiChildWidgetDataDto`

---

### Siri Shortcuts

#### GET /api/v1/siri/balance/{childId}
Get balance for Siri response

**Authorization**: Parent or self
**Response**: `SiriBalanceResponseDto`

---

#### GET /api/v1/siri/goal-progress/{goalId}
Get goal progress for Siri response

**Authorization**: Family member
**Response**: `SiriGoalProgressResponseDto`

---

## Service Layer

### IRecurringTransactionService

```csharp
public interface IRecurringTransactionService
{
    // CRUD
    Task<RecurringTransactionDto> CreateAsync(CreateRecurringTransactionDto dto, Guid userId);
    Task<RecurringTransactionDto> GetByIdAsync(Guid id, Guid userId);
    Task<List<RecurringTransactionDto>> GetByChildAsync(Guid childId, Guid userId);
    Task<List<RecurringTransactionDto>> GetByFamilyAsync(Guid userId);
    Task<RecurringTransactionDto> UpdateAsync(Guid id, UpdateRecurringTransactionDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);

    // Control
    Task<RecurringTransactionDto> PauseAsync(Guid id, Guid userId);
    Task<RecurringTransactionDto> ResumeAsync(Guid id, Guid userId);
    Task<TransactionDto> ExecuteNowAsync(Guid id, Guid userId);

    // Background job methods
    Task ProcessDueRecurringTransactionsAsync();
    DateTime CalculateNextExecutionDate(RecurringTransaction recurring);
}
```

### ITransactionTemplateService

```csharp
public interface ITransactionTemplateService
{
    Task<List<TransactionTemplateDto>> GetTemplatesAsync(Guid userId);
    Task<TransactionTemplateDto> CreateTemplateAsync(CreateTransactionTemplateDto dto, Guid userId);
    Task<TransactionTemplateDto> UpdateTemplateAsync(Guid id, UpdateTransactionTemplateDto dto, Guid userId);
    Task DeleteTemplateAsync(Guid id, Guid userId);
    Task<TransactionDto> UseTemplateAsync(Guid templateId, UseTemplateDto dto, Guid userId);
}
```

### IWidgetDataService

```csharp
public interface IWidgetDataService
{
    Task<BalanceWidgetDataDto> GetBalanceDataAsync(Guid childId, Guid userId);
    Task<GoalWidgetDataDto> GetGoalDataAsync(Guid goalId, Guid userId);
    Task<QuickActionsWidgetDataDto> GetQuickActionsDataAsync(Guid childId, Guid userId);
    Task<MultiChildWidgetDataDto> GetFamilyBalancesAsync(Guid userId);

    // Siri
    Task<SiriBalanceResponseDto> GetSiriBalanceAsync(Guid childId, Guid userId);
    Task<SiriGoalProgressResponseDto> GetSiriGoalProgressAsync(Guid goalId, Guid userId);
}
```

---

## Background Job

### RecurringTransactionJob

```csharp
public class RecurringTransactionJob : IHostedService, IDisposable
{
    private Timer? _timer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecurringTransactionJob> _logger;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Recurring Transaction Job starting");

        // Run every hour
        _timer = new Timer(ProcessRecurring, null, TimeSpan.Zero, TimeSpan.FromHours(1));

        return Task.CompletedTask;
    }

    private async void ProcessRecurring(object? state)
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRecurringTransactionService>();

        try
        {
            await service.ProcessDueRecurringTransactionsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing recurring transactions");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
```

### Processing Logic

```csharp
public async Task ProcessDueRecurringTransactionsAsync()
{
    var now = DateTime.UtcNow;

    var dueTransactions = await _context.RecurringTransactions
        .Include(rt => rt.Child)
        .Where(rt => rt.IsActive && !rt.IsPaused)
        .Where(rt => rt.NextExecutionDate <= now)
        .Where(rt => rt.EndDate == null || rt.EndDate > now)
        .Where(rt => rt.MaxOccurrences == null || rt.OccurrenceCount < rt.MaxOccurrences)
        .ToListAsync();

    foreach (var recurring in dueTransactions)
    {
        try
        {
            await ExecuteRecurringTransactionAsync(recurring);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute recurring transaction {Id}", recurring.Id);
        }
    }
}

private async Task ExecuteRecurringTransactionAsync(RecurringTransaction recurring)
{
    // Check if child has sufficient balance for debits
    if (recurring.Type == TransactionType.Debit)
    {
        if (recurring.Child.CurrentBalance < recurring.Amount)
        {
            _logger.LogWarning("Insufficient balance for recurring {Id}, skipping", recurring.Id);

            // Notify parent
            await _notificationService.SendNotificationAsync(
                recurring.CreatedById,
                NotificationType.BalanceAlert,
                "Recurring Transaction Skipped",
                $"{recurring.Child.Name}'s recurring '{recurring.Name}' was skipped due to insufficient balance"
            );

            return;
        }
    }

    // Create transaction
    var transaction = new Transaction
    {
        ChildId = recurring.ChildId,
        Amount = recurring.Amount,
        Type = recurring.Type,
        Category = recurring.Category,
        Description = recurring.Description ?? recurring.Name,
        CreatedById = recurring.CreatedById,
        CreatedAt = DateTime.UtcNow,
        RecurringTransactionId = recurring.Id
    };

    // Update balance
    if (recurring.Type == TransactionType.Credit)
    {
        recurring.Child.CurrentBalance += recurring.Amount;
    }
    else
    {
        recurring.Child.CurrentBalance -= recurring.Amount;
    }

    transaction.BalanceAfter = recurring.Child.CurrentBalance;

    _context.Transactions.Add(transaction);

    // Update recurring transaction
    recurring.LastExecutedAt = DateTime.UtcNow;
    recurring.OccurrenceCount++;
    recurring.NextExecutionDate = CalculateNextExecutionDate(recurring);

    // Check if completed
    if (recurring.MaxOccurrences.HasValue && recurring.OccurrenceCount >= recurring.MaxOccurrences)
    {
        recurring.IsActive = false;
    }

    if (recurring.EndDate.HasValue && recurring.NextExecutionDate > recurring.EndDate)
    {
        recurring.IsActive = false;
    }

    await _context.SaveChangesAsync();

    _logger.LogInformation("Executed recurring transaction {Id}, occurrence {Count}",
        recurring.Id, recurring.OccurrenceCount);
}

public DateTime CalculateNextExecutionDate(RecurringTransaction recurring)
{
    var baseDate = recurring.LastExecutedAt ?? recurring.StartDate;

    return recurring.Pattern switch
    {
        RecurrencePattern.Daily => baseDate.AddDays(1),
        RecurrencePattern.Weekly => baseDate.AddDays(7),
        RecurrencePattern.BiWeekly => baseDate.AddDays(14),
        RecurrencePattern.Monthly => baseDate.AddMonths(1),
        RecurrencePattern.FirstOfMonth => GetNextFirstOfMonth(baseDate),
        RecurrencePattern.LastOfMonth => GetNextLastOfMonth(baseDate),
        _ => baseDate.AddDays(1)
    };
}
```

---

## iOS Implementation

### Widgets

#### BalanceWidget.swift

```swift
import WidgetKit
import SwiftUI

struct BalanceWidgetEntry: TimelineEntry {
    let date: Date
    let childName: String
    let balance: Decimal
    let savingsBalance: Decimal?
    let changeToday: String
    let configuration: ConfigurationIntent
}

struct BalanceWidgetProvider: IntentTimelineProvider {
    func placeholder(in context: Context) -> BalanceWidgetEntry {
        BalanceWidgetEntry(
            date: Date(),
            childName: "Child",
            balance: 50.00,
            savingsBalance: 25.00,
            changeToday: "+$5.00",
            configuration: ConfigurationIntent()
        )
    }

    func getSnapshot(for configuration: ConfigurationIntent, in context: Context, completion: @escaping (BalanceWidgetEntry) -> Void) {
        let entry = placeholder(in: context)
        completion(entry)
    }

    func getTimeline(for configuration: ConfigurationIntent, in context: Context, completion: @escaping (Timeline<BalanceWidgetEntry>) -> Void) {
        Task {
            do {
                let childId = configuration.childId ?? ""
                let data = try await fetchBalanceData(childId: childId)

                let entry = BalanceWidgetEntry(
                    date: Date(),
                    childName: data.childName,
                    balance: data.currentBalance,
                    savingsBalance: data.savingsBalance,
                    changeToday: data.balanceChangeToday,
                    configuration: configuration
                )

                // Refresh every 15 minutes
                let nextUpdate = Calendar.current.date(byAdding: .minute, value: 15, to: Date())!
                let timeline = Timeline(entries: [entry], policy: .after(nextUpdate))
                completion(timeline)
            } catch {
                // Use placeholder on error
                let timeline = Timeline(entries: [placeholder(in: context)], policy: .after(Date().addingTimeInterval(60 * 15)))
                completion(timeline)
            }
        }
    }

    private func fetchBalanceData(childId: String) async throws -> BalanceWidgetData {
        // Fetch from API using stored credentials
        let token = KeychainHelper.shared.getToken()
        guard let token = token, let childUUID = UUID(uuidString: childId) else {
            throw WidgetError.notAuthenticated
        }

        let url = URL(string: "\(APIConfig.baseURL)/api/v1/widgets/balance/\(childId)")!
        var request = URLRequest(url: url)
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")

        let (data, _) = try await URLSession.shared.data(for: request)
        return try JSONDecoder().decode(BalanceWidgetData.self, from: data)
    }
}

struct BalanceWidgetSmallView: View {
    let entry: BalanceWidgetEntry

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            Text(entry.childName)
                .font(.caption)
                .foregroundStyle(.secondary)

            Text("$\(entry.balance, specifier: "%.2f")")
                .font(.title)
                .fontWeight(.bold)

            Spacer()

            HStack {
                Text(entry.changeToday)
                    .font(.caption)
                    .foregroundStyle(entry.changeToday.hasPrefix("+") ? .green : .red)
                Spacer()
            }
        }
        .padding()
        .containerBackground(.fill.tertiary, for: .widget)
    }
}

struct BalanceWidgetMediumView: View {
    let entry: BalanceWidgetEntry

    var body: some View {
        HStack {
            VStack(alignment: .leading, spacing: 4) {
                Text(entry.childName)
                    .font(.caption)
                    .foregroundStyle(.secondary)

                Text("$\(entry.balance, specifier: "%.2f")")
                    .font(.title)
                    .fontWeight(.bold)

                Text("Spending")
                    .font(.caption2)
                    .foregroundStyle(.secondary)
            }

            Spacer()

            if let savings = entry.savingsBalance {
                VStack(alignment: .trailing, spacing: 4) {
                    Text("Savings")
                        .font(.caption)
                        .foregroundStyle(.secondary)

                    Text("$\(savings, specifier: "%.2f")")
                        .font(.title2)
                        .fontWeight(.semibold)
                        .foregroundStyle(.green)
                }
            }
        }
        .padding()
        .containerBackground(.fill.tertiary, for: .widget)
    }
}

@main
struct BalanceWidget: Widget {
    let kind: String = "BalanceWidget"

    var body: some WidgetConfiguration {
        IntentConfiguration(
            kind: kind,
            intent: ConfigurationIntent.self,
            provider: BalanceWidgetProvider()
        ) { entry in
            if #available(iOS 17.0, *) {
                BalanceWidgetView(entry: entry)
                    .containerBackground(.fill.tertiary, for: .widget)
            } else {
                BalanceWidgetView(entry: entry)
                    .padding()
                    .background()
            }
        }
        .configurationDisplayName("Balance")
        .description("View your current balance at a glance.")
        .supportedFamilies([.systemSmall, .systemMedium])
    }
}

struct BalanceWidgetView: View {
    @Environment(\.widgetFamily) var family
    let entry: BalanceWidgetEntry

    var body: some View {
        switch family {
        case .systemSmall:
            BalanceWidgetSmallView(entry: entry)
        case .systemMedium:
            BalanceWidgetMediumView(entry: entry)
        default:
            BalanceWidgetSmallView(entry: entry)
        }
    }
}
```

#### GoalProgressWidget.swift

```swift
import WidgetKit
import SwiftUI

struct GoalProgressEntry: TimelineEntry {
    let date: Date
    let goalName: String
    let imageUrl: String?
    let currentAmount: Decimal
    let targetAmount: Decimal
    let progressPercentage: Double
    let daysRemaining: Int?
}

struct GoalProgressWidgetView: View {
    let entry: GoalProgressEntry

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack {
                Text(entry.goalName)
                    .font(.headline)
                    .lineLimit(1)
                Spacer()
                if let days = entry.daysRemaining {
                    Text("\(days)d")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }

            ZStack(alignment: .leading) {
                Rectangle()
                    .fill(Color.gray.opacity(0.3))
                    .frame(height: 8)
                    .cornerRadius(4)

                Rectangle()
                    .fill(progressColor)
                    .frame(width: CGFloat(entry.progressPercentage / 100) * 140, height: 8)
                    .cornerRadius(4)
            }

            HStack {
                Text("$\(entry.currentAmount, specifier: "%.0f")")
                    .font(.caption)
                    .fontWeight(.medium)
                Spacer()
                Text("$\(entry.targetAmount, specifier: "%.0f")")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
        .padding()
        .containerBackground(.fill.tertiary, for: .widget)
    }

    private var progressColor: Color {
        switch entry.progressPercentage {
        case 0..<50: return .blue
        case 50..<75: return .orange
        default: return .green
        }
    }
}
```

#### QuickActionsWidget.swift

```swift
import WidgetKit
import SwiftUI
import AppIntents

struct QuickAddIntent: AppIntent {
    static var title: LocalizedStringResource = "Quick Add Transaction"

    @Parameter(title: "Template")
    var templateId: String

    @Parameter(title: "Amount")
    var amount: Double?

    func perform() async throws -> some IntentResult {
        // Execute quick add via API
        return .result()
    }
}

struct QuickActionsWidgetView: View {
    let templates: [TransactionTemplate]
    let balance: Decimal

    var body: some View {
        VStack(spacing: 8) {
            Text("Balance: $\(balance, specifier: "%.2f")")
                .font(.caption)
                .foregroundStyle(.secondary)

            LazyVGrid(columns: [GridItem(.flexible()), GridItem(.flexible())], spacing: 8) {
                ForEach(templates.prefix(4)) { template in
                    Button(intent: QuickAddIntent(templateId: template.id.uuidString)) {
                        VStack(spacing: 4) {
                            Image(systemName: template.iconName)
                                .font(.title2)
                            Text(template.name)
                                .font(.caption2)
                                .lineLimit(1)
                            if let amount = template.defaultAmount {
                                Text("$\(amount, specifier: "%.0f")")
                                    .font(.caption2)
                                    .foregroundStyle(.secondary)
                            }
                        }
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 8)
                        .background(Color(hex: template.color ?? "#6B7280").opacity(0.2))
                        .cornerRadius(8)
                    }
                    .buttonStyle(.plain)
                }
            }
        }
        .padding()
        .containerBackground(.fill.tertiary, for: .widget)
    }
}
```

### Siri Shortcuts

#### SiriShortcuts.swift

```swift
import AppIntents
import SwiftUI

// MARK: - Check Balance Intent

struct CheckBalanceIntent: AppIntent {
    static var title: LocalizedStringResource = "Check Balance"
    static var description = IntentDescription("Check your current balance")

    static var openAppWhenRun: Bool = false

    @Parameter(title: "Child")
    var childId: String?

    func perform() async throws -> some IntentResult & ProvidesDialog {
        guard let token = KeychainHelper.shared.getToken() else {
            return .result(dialog: "Please open the app and sign in first.")
        }

        // Determine which child
        let targetChildId = childId ?? UserDefaults.shared.string(forKey: "defaultChildId")

        guard let childId = targetChildId else {
            return .result(dialog: "I couldn't determine which balance to check. Please specify a child.")
        }

        do {
            let response = try await APIService.shared.getSiriBalance(childId: childId)
            return .result(dialog: IntentDialog(response.spokenResponse))
        } catch {
            return .result(dialog: "Sorry, I couldn't get your balance right now.")
        }
    }
}

// MARK: - Check Goal Progress Intent

struct CheckGoalProgressIntent: AppIntent {
    static var title: LocalizedStringResource = "Check Goal Progress"
    static var description = IntentDescription("Check progress on a savings goal")

    @Parameter(title: "Goal")
    var goalName: String?

    func perform() async throws -> some IntentResult & ProvidesDialog {
        guard let token = KeychainHelper.shared.getToken() else {
            return .result(dialog: "Please open the app and sign in first.")
        }

        do {
            let response = try await APIService.shared.getSiriGoalProgress(goalName: goalName)
            return .result(dialog: IntentDialog(response.spokenResponse))
        } catch {
            return .result(dialog: "Sorry, I couldn't get your goal progress right now.")
        }
    }
}

// MARK: - Quick Transaction Intent

struct QuickTransactionIntent: AppIntent {
    static var title: LocalizedStringResource = "Add Transaction"
    static var description = IntentDescription("Quickly add a transaction")

    @Parameter(title: "Amount")
    var amount: Double

    @Parameter(title: "Description")
    var description: String?

    @Parameter(title: "Type")
    var isExpense: Bool

    func perform() async throws -> some IntentResult & ProvidesDialog {
        guard let token = KeychainHelper.shared.getToken() else {
            return .result(dialog: "Please open the app and sign in first.")
        }

        let type: TransactionType = isExpense ? .debit : .credit
        let desc = description ?? (isExpense ? "Expense" : "Income")

        do {
            try await APIService.shared.createQuickTransaction(
                amount: Decimal(amount),
                type: type,
                description: desc
            )

            let formatted = String(format: "$%.2f", amount)
            let verb = isExpense ? "recorded expense of" : "added"
            return .result(dialog: "Done! I've \(verb) \(formatted).")
        } catch {
            return .result(dialog: "Sorry, I couldn't add that transaction.")
        }
    }
}

// MARK: - App Shortcuts Provider

struct AllowanceTrackerShortcuts: AppShortcutsProvider {
    static var appShortcuts: [AppShortcut] {
        AppShortcut(
            intent: CheckBalanceIntent(),
            phrases: [
                "Check my balance in \(.applicationName)",
                "What's my balance in \(.applicationName)",
                "How much money do I have in \(.applicationName)"
            ],
            shortTitle: "Check Balance",
            systemImageName: "dollarsign.circle"
        )

        AppShortcut(
            intent: CheckGoalProgressIntent(),
            phrases: [
                "Check my savings goal in \(.applicationName)",
                "How close am I to my goal in \(.applicationName)",
                "What's my goal progress in \(.applicationName)"
            ],
            shortTitle: "Goal Progress",
            systemImageName: "target"
        )

        AppShortcut(
            intent: QuickTransactionIntent(),
            phrases: [
                "Add expense in \(.applicationName)",
                "Record spending in \(.applicationName)",
                "Add \(\.$amount) \(\.$description) in \(.applicationName)"
            ],
            shortTitle: "Add Transaction",
            systemImageName: "plus.circle"
        )
    }
}
```

### Recurring Transactions UI

```swift
import SwiftUI

@Observable
@MainActor
final class RecurringTransactionViewModel {
    var recurringTransactions: [RecurringTransaction] = []
    var templates: [TransactionTemplate] = []
    var isLoading = false
    var errorMessage: String?

    private let childId: UUID
    private let apiService: APIServiceProtocol

    init(childId: UUID, apiService: APIServiceProtocol = APIService()) {
        self.childId = childId
        self.apiService = apiService
    }

    func loadRecurringTransactions() async {
        isLoading = true
        do {
            recurringTransactions = try await apiService.get(
                endpoint: "/api/v1/children/\(childId)/recurring-transactions"
            )
        } catch {
            errorMessage = error.localizedDescription
        }
        isLoading = false
    }

    func loadTemplates() async {
        do {
            templates = try await apiService.get(endpoint: "/api/v1/templates")
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func pauseRecurring(_ id: UUID) async {
        do {
            let _: RecurringTransaction = try await apiService.post(
                endpoint: "/api/v1/recurring-transactions/\(id)/pause",
                body: EmptyBody()
            )
            await loadRecurringTransactions()
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func resumeRecurring(_ id: UUID) async {
        do {
            let _: RecurringTransaction = try await apiService.post(
                endpoint: "/api/v1/recurring-transactions/\(id)/resume",
                body: EmptyBody()
            )
            await loadRecurringTransactions()
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func useTemplate(_ template: TransactionTemplate, amount: Decimal?) async {
        do {
            let _: Transaction = try await apiService.post(
                endpoint: "/api/v1/templates/\(template.id)/use",
                body: UseTemplateRequest(childId: childId, amount: amount, description: nil)
            )
        } catch {
            errorMessage = error.localizedDescription
        }
    }
}

struct RecurringTransactionsView: View {
    @Bindable var viewModel: RecurringTransactionViewModel
    @State private var showingCreate = false

    var body: some View {
        List {
            if viewModel.recurringTransactions.isEmpty {
                ContentUnavailableView(
                    "No Recurring Transactions",
                    systemImage: "arrow.clockwise",
                    description: Text("Set up automatic transactions for subscriptions and regular expenses.")
                )
            } else {
                ForEach(viewModel.recurringTransactions) { recurring in
                    RecurringTransactionRow(
                        recurring: recurring,
                        onPause: { Task { await viewModel.pauseRecurring(recurring.id) } },
                        onResume: { Task { await viewModel.resumeRecurring(recurring.id) } }
                    )
                }
            }
        }
        .navigationTitle("Recurring")
        .toolbar {
            ToolbarItem(placement: .primaryAction) {
                Button(action: { showingCreate = true }) {
                    Image(systemName: "plus")
                }
            }
        }
        .sheet(isPresented: $showingCreate) {
            CreateRecurringView(viewModel: viewModel)
        }
        .refreshable {
            await viewModel.loadRecurringTransactions()
        }
    }
}

struct RecurringTransactionRow: View {
    let recurring: RecurringTransaction
    let onPause: () -> Void
    let onResume: () -> Void

    var body: some View {
        HStack {
            VStack(alignment: .leading, spacing: 4) {
                HStack {
                    Text(recurring.name)
                        .font(.headline)

                    if recurring.isPaused {
                        Text("PAUSED")
                            .font(.caption2)
                            .padding(.horizontal, 6)
                            .padding(.vertical, 2)
                            .background(Color.orange.opacity(0.2))
                            .foregroundStyle(.orange)
                            .cornerRadius(4)
                    }
                }

                Text(recurring.patternDescription)
                    .font(.caption)
                    .foregroundStyle(.secondary)

                Text("Next: \(recurring.nextExecutionDate, style: .date)")
                    .font(.caption2)
                    .foregroundStyle(.secondary)
            }

            Spacer()

            Text("$\(recurring.amount, specifier: "%.2f")")
                .font(.headline)
                .foregroundStyle(recurring.type == .debit ? .red : .green)
        }
        .swipeActions(edge: .trailing) {
            if recurring.isPaused {
                Button(action: onResume) {
                    Label("Resume", systemImage: "play.fill")
                }
                .tint(.green)
            } else {
                Button(action: onPause) {
                    Label("Pause", systemImage: "pause.fill")
                }
                .tint(.orange)
            }
        }
    }
}
```

---

## Global Templates (Seed Data)

```csharp
public static class TemplateSeeder
{
    public static List<TransactionTemplate> GetGlobalTemplates()
    {
        return new List<TransactionTemplate>
        {
            // Common expenses
            new() { Name = "Lunch", IconName = "fork.knife", Color = "#F59E0B", DefaultAmount = 5m, Type = TransactionType.Debit, Category = "Food", IsGlobal = true, SortOrder = 1 },
            new() { Name = "Snack", IconName = "cup.and.saucer", Color = "#8B5CF6", DefaultAmount = 2m, Type = TransactionType.Debit, Category = "Food", IsGlobal = true, SortOrder = 2 },
            new() { Name = "Movie", IconName = "film", Color = "#EC4899", DefaultAmount = 15m, Type = TransactionType.Debit, Category = "Entertainment", IsGlobal = true, SortOrder = 3 },
            new() { Name = "Game", IconName = "gamecontroller", Color = "#3B82F6", DefaultAmount = null, Type = TransactionType.Debit, Category = "Entertainment", IsGlobal = true, SortOrder = 4 },
            new() { Name = "Book", IconName = "book", Color = "#10B981", DefaultAmount = 10m, Type = TransactionType.Debit, Category = "Education", IsGlobal = true, SortOrder = 5 },
            new() { Name = "Toy", IconName = "teddybear", Color = "#F97316", DefaultAmount = null, Type = TransactionType.Debit, Category = "Toys", IsGlobal = true, SortOrder = 6 },

            // Common income
            new() { Name = "Gift", IconName = "gift", Color = "#EF4444", DefaultAmount = null, Type = TransactionType.Credit, Category = "Gift", IsGlobal = true, SortOrder = 10 },
            new() { Name = "Chore Bonus", IconName = "checkmark.circle", Color = "#22C55E", DefaultAmount = 5m, Type = TransactionType.Credit, Category = "Chores", IsGlobal = true, SortOrder = 11 },
            new() { Name = "Found Money", IconName = "magnifyingglass", Color = "#6366F1", DefaultAmount = null, Type = TransactionType.Credit, Category = "Other", IsGlobal = true, SortOrder = 12 }
        };
    }
}
```

---

## Testing Strategy

### Unit Tests - 40 tests

```csharp
public class RecurringTransactionServiceTests
{
    // CRUD (10 tests)
    [Fact]
    public async Task Create_SetsNextExecutionDate() { }

    [Fact]
    public async Task Create_ValidatesRecurrenceSettings() { }

    // Execution (15 tests)
    [Fact]
    public async Task ProcessDue_ExecutesReadyTransactions() { }

    [Fact]
    public async Task ProcessDue_SkipsInsufficientBalance() { }

    [Fact]
    public async Task ProcessDue_RespectsMaxOccurrences() { }

    [Fact]
    public async Task ProcessDue_RespectsEndDate() { }

    [Fact]
    public async Task CalculateNext_Daily_ReturnsNextDay() { }

    [Fact]
    public async Task CalculateNext_Weekly_ReturnsNextWeek() { }

    [Fact]
    public async Task CalculateNext_Monthly_ReturnsNextMonth() { }

    // Control (5 tests)
    [Fact]
    public async Task Pause_SetsIsPausedTrue() { }

    [Fact]
    public async Task Resume_SetsIsPausedFalse() { }
}

public class TransactionTemplateServiceTests
{
    // Templates (10 tests)
    [Fact]
    public async Task GetTemplates_IncludesGlobalAndFamily() { }

    [Fact]
    public async Task UseTemplate_CreatesTransaction() { }

    [Fact]
    public async Task UseTemplate_IncrementsUsageCount() { }

    [Fact]
    public async Task UseTemplate_AllowsAmountOverride() { }
}
```

---

## Implementation Phases

### Phase 1: Database & Models (2 days)
- [ ] Create RecurringTransaction model
- [ ] Create TransactionTemplate model
- [ ] Add database migration
- [ ] Seed global templates

### Phase 2: Recurring Service (3 days)
- [ ] Write IRecurringTransactionService tests
- [ ] Implement service methods
- [ ] Implement background job
- [ ] Test execution logic

### Phase 3: Templates Service (2 days)
- [ ] Write ITransactionTemplateService tests
- [ ] Implement service methods
- [ ] Test template usage

### Phase 4: Widget Data Service (2 days)
- [ ] Implement widget data endpoints
- [ ] Implement Siri response endpoints

### Phase 5: API Controllers (2 days)
- [ ] Write controller tests
- [ ] Implement controllers

### Phase 6: iOS Widgets (4 days)
- [ ] Create BalanceWidget
- [ ] Create GoalProgressWidget
- [ ] Create QuickActionsWidget
- [ ] Configure widget extension

### Phase 7: Siri Shortcuts (2 days)
- [ ] Implement AppIntents
- [ ] Configure AppShortcutsProvider
- [ ] Test voice commands

### Phase 8: iOS UI (2 days)
- [ ] Create RecurringTransactionsView
- [ ] Create TemplatesView

---

## Success Criteria

- [ ] Recurring transactions execute automatically
- [ ] Widgets display correct data
- [ ] Widgets refresh every 15 minutes
- [ ] Siri responds to voice commands
- [ ] Templates create transactions with one tap
- [ ] >90% test coverage on services

---

This specification provides a complete quick actions system following TDD principles.
