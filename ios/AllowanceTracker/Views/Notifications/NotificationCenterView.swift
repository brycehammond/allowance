import SwiftUI

/// View displaying the notification center with all notifications
struct NotificationCenterView: View {

    // MARK: - Properties

    @State private var viewModel = NotificationViewModel()
    @Environment(\.dismiss) private var dismiss

    // MARK: - Body

    var body: some View {
        NavigationStack {
            Group {
                if viewModel.isLoading && viewModel.notifications.isEmpty {
                    ProgressView("Loading...")
                } else if viewModel.notifications.isEmpty {
                    emptyStateView
                } else {
                    notificationListView
                }
            }
            .navigationTitle("Notifications")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Close") {
                        dismiss()
                    }
                }

                if viewModel.hasUnread {
                    ToolbarItem(placement: .primaryAction) {
                        Button("Mark All Read") {
                            Task {
                                await viewModel.markAllAsRead()
                            }
                        }
                    }
                }
            }
            .refreshable {
                await viewModel.refresh()
            }
            .task {
                await viewModel.loadNotifications()
            }
            .alert("Error", isPresented: .constant(viewModel.errorMessage != nil)) {
                Button("OK") {
                    viewModel.clearError()
                }
            } message: {
                if let errorMessage = viewModel.errorMessage {
                    Text(errorMessage)
                }
            }
        }
    }

    // MARK: - Subviews

    private var emptyStateView: some View {
        ContentUnavailableView(
            "No Notifications",
            systemImage: "bell.slash",
            description: Text("You're all caught up!")
        )
    }

    private var notificationListView: some View {
        List {
            ForEach(viewModel.notifications) { notification in
                NotificationRowView(notification: notification)
                    .contentShape(Rectangle())
                    .onTapGesture {
                        Task {
                            await viewModel.markAsRead(notification)
                        }
                    }
                    .swipeActions(edge: .trailing, allowsFullSwipe: true) {
                        Button(role: .destructive) {
                            Task {
                                await viewModel.delete(notification)
                            }
                        } label: {
                            Label("Delete", systemImage: "trash")
                        }
                    }
                    .swipeActions(edge: .leading, allowsFullSwipe: true) {
                        if !notification.isRead {
                            Button {
                                Task {
                                    await viewModel.markAsRead(notification)
                                }
                            } label: {
                                Label("Read", systemImage: "envelope.open")
                            }
                            .tint(.blue)
                        }
                    }
                    .onAppear {
                        // Load more when reaching the end
                        if notification.id == viewModel.notifications.last?.id {
                            Task {
                                await viewModel.loadMoreNotifications()
                            }
                        }
                    }
            }

            if viewModel.isLoadingMore {
                HStack {
                    Spacer()
                    ProgressView()
                    Spacer()
                }
                .listRowSeparator(.hidden)
            }
        }
        .listStyle(.plain)
    }
}

// MARK: - Notification Row View

struct NotificationRowView: View {

    let notification: NotificationDto

    var body: some View {
        HStack(alignment: .top, spacing: 12) {
            // Icon
            ZStack {
                Circle()
                    .fill(iconBackgroundColor)
                    .frame(width: 40, height: 40)

                Image(systemName: notification.type.systemImage)
                    .font(.system(size: 16, weight: .semibold))
                    .foregroundStyle(iconForegroundColor)
            }

            // Content
            VStack(alignment: .leading, spacing: 4) {
                HStack {
                    Text(notification.title)
                        .font(.subheadline)
                        .fontWeight(notification.isRead ? .regular : .semibold)
                        .foregroundStyle(notification.isRead ? .secondary : .primary)

                    Spacer()

                    if !notification.isRead {
                        Circle()
                            .fill(DesignSystem.Colors.primary)
                            .frame(width: 8, height: 8)
                    }
                }

                Text(notification.body)
                    .font(.caption)
                    .foregroundStyle(.secondary)
                    .lineLimit(2)

                Text(notification.timeAgo)
                    .font(.caption2)
                    .foregroundStyle(.tertiary)
            }
        }
        .padding(.vertical, 4)
        .opacity(notification.isRead ? 0.7 : 1.0)
    }

    // MARK: - Computed Properties

    private var iconBackgroundColor: Color {
        switch notification.type.colorName {
        case "green": return .green.opacity(0.15)
        case "orange": return .orange.opacity(0.15)
        case "red": return .red.opacity(0.15)
        case "blue": return .blue.opacity(0.15)
        case "purple": return .purple.opacity(0.15)
        case "yellow": return .yellow.opacity(0.15)
        default: return .gray.opacity(0.15)
        }
    }

    private var iconForegroundColor: Color {
        switch notification.type.colorName {
        case "green": return .green
        case "orange": return .orange
        case "red": return .red
        case "blue": return .blue
        case "purple": return .purple
        case "yellow": return .yellow
        default: return .gray
        }
    }
}

// MARK: - Preview Provider

#Preview("Notification Center") {
    NotificationCenterView()
}

#Preview("Notification Row - Unread") {
    NotificationRowView(
        notification: NotificationDto(
            id: UUID(),
            type: .allowanceDeposit,
            typeName: "Allowance Deposit",
            title: "Weekly Allowance Received",
            body: "Your weekly allowance of $10.00 has been deposited.",
            data: nil,
            isRead: false,
            readAt: nil,
            createdAt: Date(),
            relatedEntityType: nil,
            relatedEntityId: nil,
            timeAgo: "2 minutes ago"
        )
    )
    .padding()
}

#Preview("Notification Row - Read") {
    NotificationRowView(
        notification: NotificationDto(
            id: UUID(),
            type: .taskApproved,
            typeName: "Task Approved",
            title: "Chore Completed!",
            body: "Your 'Clean Room' chore has been approved. You earned $5.00!",
            data: nil,
            isRead: true,
            readAt: Date(),
            createdAt: Date().addingTimeInterval(-3600),
            relatedEntityType: nil,
            relatedEntityId: nil,
            timeAgo: "1 hour ago"
        )
    )
    .padding()
}
