import Foundation

// MARK: - Enums

enum ChoreTaskStatus: String, Codable, CaseIterable {
    case Active
    case Archived

    var displayName: String {
        rawValue
    }
}

enum RecurrenceType: String, Codable, CaseIterable {
    case Daily
    case Weekly
    case Monthly

    var displayName: String {
        rawValue
    }
}

enum CompletionStatus: String, Codable, CaseIterable {
    case PendingApproval
    case Approved
    case Rejected

    var displayName: String {
        switch self {
        case .PendingApproval: return "Pending"
        case .Approved: return "Approved"
        case .Rejected: return "Rejected"
        }
    }

    var color: String {
        switch self {
        case .PendingApproval: return "yellow"
        case .Approved: return "green"
        case .Rejected: return "red"
        }
    }
}

// MARK: - Chore Task Model

struct ChoreTask: Codable, Identifiable {
    let id: UUID
    let childId: UUID
    let childName: String
    let title: String
    let description: String?
    let rewardAmount: Decimal
    let status: ChoreTaskStatus
    let isRecurring: Bool
    let recurrenceType: RecurrenceType?
    let recurrenceDisplay: String
    let createdAt: Date
    let createdById: UUID
    let createdByName: String
    let totalCompletions: Int
    let pendingApprovals: Int
    let lastCompletedAt: Date?

    var formattedReward: String {
        rewardAmount.currencyFormatted
    }

    var formattedCreatedDate: String {
        let formatter = DateFormatter()
        formatter.dateStyle = .medium
        return formatter.string(from: createdAt)
    }

    var recurrenceInfo: String {
        if isRecurring {
            return recurrenceDisplay
        }
        return "One-time"
    }
}

// MARK: - Task Completion Model

struct TaskCompletion: Codable, Identifiable {
    let id: UUID
    let taskId: UUID
    let taskTitle: String
    let rewardAmount: Decimal
    let childId: UUID
    let childName: String
    let completedAt: Date
    let notes: String?
    let photoUrl: String?
    let status: CompletionStatus
    let approvedById: UUID?
    let approvedByName: String?
    let approvedAt: Date?
    let rejectionReason: String?
    let transactionId: UUID?

    var formattedReward: String {
        rewardAmount.currencyFormatted
    }

    var formattedCompletedDate: String {
        let formatter = DateFormatter()
        formatter.dateStyle = .medium
        formatter.timeStyle = .short
        return formatter.string(from: completedAt)
    }

    var formattedApprovedDate: String? {
        guard let date = approvedAt else { return nil }
        let formatter = DateFormatter()
        formatter.dateStyle = .medium
        formatter.timeStyle = .short
        return formatter.string(from: date)
    }
}

// MARK: - Task Statistics

struct TaskStatistics: Codable {
    let totalTasks: Int
    let activeTasks: Int
    let archivedTasks: Int
    let totalCompletions: Int
    let pendingApprovals: Int
    let totalEarned: Decimal
    let pendingEarnings: Decimal
    let completionRate: Double

    var formattedTotalEarned: String {
        totalEarned.currencyFormatted
    }

    var formattedPendingEarnings: String {
        pendingEarnings.currencyFormatted
    }

    var formattedCompletionRate: String {
        String(format: "%.0f%%", completionRate * 100)
    }
}

// MARK: - Request DTOs

struct CreateTaskRequest: Codable {
    let childId: UUID
    let title: String
    let description: String?
    let rewardAmount: Decimal
    let isRecurring: Bool
    let recurrenceType: RecurrenceType?
    let recurrenceDay: Weekday?
    let recurrenceDayOfMonth: Int?

    init(
        childId: UUID,
        title: String,
        description: String? = nil,
        rewardAmount: Decimal,
        isRecurring: Bool = false,
        recurrenceType: RecurrenceType? = nil,
        recurrenceDay: Weekday? = nil,
        recurrenceDayOfMonth: Int? = nil
    ) {
        self.childId = childId
        self.title = title
        self.description = description
        self.rewardAmount = rewardAmount
        self.isRecurring = isRecurring
        self.recurrenceType = recurrenceType
        self.recurrenceDay = recurrenceDay
        self.recurrenceDayOfMonth = recurrenceDayOfMonth
    }
}

struct UpdateTaskRequest: Codable {
    let title: String
    let description: String?
    let rewardAmount: Decimal
    let isRecurring: Bool
    let recurrenceType: RecurrenceType?
    let recurrenceDay: Weekday?
    let recurrenceDayOfMonth: Int?
}

struct CompleteTaskRequest: Codable {
    let notes: String?
    let photoUrl: String?
}

struct ReviewCompletionRequest: Codable {
    let isApproved: Bool
    let rejectionReason: String?
}
