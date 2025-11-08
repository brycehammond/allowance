import Foundation
import BackgroundTasks
import OSLog

/// Manages background app refresh tasks to keep data synchronized
/// Handles BGAppRefreshTask registration and scheduling
final class BackgroundRefreshManager {
    // MARK: - Singleton

    static let shared = BackgroundRefreshManager()

    // MARK: - Constants

    /// Background task identifier - must match Info.plist BGTaskSchedulerPermittedIdentifiers
    static let taskIdentifier = "com.allowancetracker.refresh"

    /// Default refresh interval (15 minutes)
    static let refreshInterval: TimeInterval = 15 * 60

    // MARK: - Logger

    private let logger = Logger(subsystem: "com.allowancetracker", category: "BackgroundRefresh")

    // MARK: - Dependencies

    private let apiService: APIServiceProtocol
    private let cacheService: CacheService

    // MARK: - Initialization

    private init(
        apiService: APIServiceProtocol = APIService.shared,
        cacheService: CacheService = CacheService()
    ) {
        self.apiService = apiService
        self.cacheService = cacheService
    }

    // MARK: - Registration

    /// Register background task handler with BGTaskScheduler
    /// Call this from application(_:didFinishLaunchingWithOptions:)
    func registerBackgroundTasks() {
        BGTaskScheduler.shared.register(
            forTaskWithIdentifier: Self.taskIdentifier,
            using: nil
        ) { task in
            guard let refreshTask = task as? BGAppRefreshTask else {
                self.logger.error("Invalid task type received")
                return
            }

            self.handleAppRefresh(task: refreshTask)
        }

        logger.info("Background refresh task registered")
    }

    // MARK: - Scheduling

    /// Schedule next background app refresh
    /// Call this after successfully completing a refresh or on app launch
    func scheduleAppRefresh() {
        let request = BGAppRefreshTaskRequest(identifier: Self.taskIdentifier)
        request.earliestBeginDate = Date(timeIntervalSinceNow: Self.refreshInterval)

        do {
            try BGTaskScheduler.shared.submit(request)
            logger.info("Background refresh scheduled for \(Self.refreshInterval) seconds from now")
        } catch {
            logger.error("Failed to schedule background refresh: \(error.localizedDescription)")
        }
    }

    // MARK: - Task Handling

    /// Handle background refresh task
    /// - Parameter task: BGAppRefreshTask to execute
    private func handleAppRefresh(task: BGAppRefreshTask) {
        logger.info("Background refresh task started")

        // Schedule next refresh immediately
        scheduleAppRefresh()

        // Create a task to perform the refresh
        let refreshTask = Task {
            do {
                try await performRefresh()
                logger.info("Background refresh completed successfully")
                task.setTaskCompleted(success: true)
            } catch {
                logger.error("Background refresh failed: \(error.localizedDescription)")
                task.setTaskCompleted(success: false)
            }
        }

        // Handle task expiration
        task.expirationHandler = {
            self.logger.warning("Background refresh task expired")
            refreshTask.cancel()
            task.setTaskCompleted(success: false)
        }
    }

    // MARK: - Refresh Logic

    /// Perform the actual background refresh
    /// Fetches latest data and updates cache
    private func performRefresh() async throws {
        logger.info("Starting data refresh...")

        // Fetch children data
        let endpoint = Endpoint.children
        let children: [Child] = try await apiService.request(
            endpoint: endpoint,
            method: .get,
            body: nil as String?
        )

        // Update cache
        await cacheService.cacheChildren(children)

        logger.info("Refreshed \(children.count) children in background")

        // Optionally: Fetch transactions for each child
        // This could be resource-intensive, so consider doing it selectively
        for child in children.prefix(3) { // Limit to first 3 children to avoid timeout
            do {
                let transactionEndpoint = Endpoint.childTransactions(child.id)
                let transactions: [Transaction] = try await apiService.request(
                    endpoint: transactionEndpoint,
                    method: .get,
                    body: nil as String?
                )

                await cacheService.cacheTransactions(transactions, for: child.id)
                logger.debug("Refreshed \(transactions.count) transactions for child \(child.id)")
            } catch {
                logger.error("Failed to refresh transactions for child \(child.id): \(error.localizedDescription)")
                // Continue with next child even if one fails
            }
        }
    }

    // MARK: - Manual Refresh

    /// Manually trigger a refresh (useful for testing or pull-to-refresh)
    /// - Returns: True if refresh was successful
    @discardableResult
    func manualRefresh() async -> Bool {
        do {
            try await performRefresh()
            return true
        } catch {
            logger.error("Manual refresh failed: \(error.localizedDescription)")
            return false
        }
    }

    // MARK: - Cancellation

    /// Cancel all pending background refresh tasks
    func cancelAllPendingRefreshTasks() {
        BGTaskScheduler.shared.cancel(taskRequestWithIdentifier: Self.taskIdentifier)
        logger.info("Cancelled all pending background refresh tasks")
    }
}

// MARK: - Endpoint Extension

extension Endpoint {
    static var children: Endpoint {
        return .children
    }

    static func childTransactions(_ childId: UUID) -> Endpoint {
        return .childTransactions(childId)
    }
}
