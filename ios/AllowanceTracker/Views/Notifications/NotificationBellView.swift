import SwiftUI

/// A bell icon button that shows notification count and opens the notification center
struct NotificationBellView: View {

    // MARK: - Properties

    @State private var viewModel = NotificationViewModel()
    @State private var showingNotifications = false

    // MARK: - Body

    var body: some View {
        Button {
            showingNotifications = true
        } label: {
            ZStack(alignment: .topTrailing) {
                Image(systemName: "bell.fill")
                    .font(.title3)

                // Badge for unread count
                if viewModel.unreadCount > 0 {
                    Text(badgeText)
                        .font(.system(size: 10, weight: .bold))
                        .foregroundStyle(.white)
                        .padding(.horizontal, 5)
                        .padding(.vertical, 2)
                        .background(Color.red)
                        .clipShape(Capsule())
                        .offset(x: 8, y: -8)
                }
            }
        }
        .accessibilityLabel(accessibilityLabel)
        .sheet(isPresented: $showingNotifications) {
            NotificationCenterView()
        }
        .task {
            await viewModel.refreshUnreadCount()
        }
        .onChange(of: showingNotifications) { _, isShowing in
            if !isShowing {
                // Refresh count when sheet is dismissed
                Task {
                    await viewModel.refreshUnreadCount()
                }
            }
        }
    }

    // MARK: - Computed Properties

    private var badgeText: String {
        if viewModel.unreadCount > 9 {
            return "9+"
        }
        return String(viewModel.unreadCount)
    }

    private var accessibilityLabel: String {
        if viewModel.unreadCount == 0 {
            return "Notifications"
        } else if viewModel.unreadCount == 1 {
            return "Notifications, 1 unread"
        } else {
            return "Notifications, \(viewModel.unreadCount) unread"
        }
    }
}

// MARK: - Preview Provider

#Preview("Bell - No Notifications") {
    NotificationBellView()
}

#Preview("Bell - With Badge") {
    // Note: Preview won't show badge as it depends on API
    NotificationBellView()
}
