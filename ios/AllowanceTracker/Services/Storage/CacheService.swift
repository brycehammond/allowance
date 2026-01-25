import Foundation

/// Actor-based cache service for offline data persistence
/// Provides thread-safe caching for children and transactions
actor CacheService {
    // MARK: - Cache Storage

    private var childrenCache: [Child] = []
    private var transactionsCache: [UUID: [Transaction]] = [:]
    private var lastSyncDate: Date?

    // MARK: - Children Caching

    /// Get cached children
    /// - Returns: Array of cached children, or empty array if no cache exists
    func getCachedChildren() -> [Child] {
        return childrenCache
    }

    /// Cache children and update sync timestamp
    /// - Parameter children: Array of children to cache
    func cacheChildren(_ children: [Child]) {
        childrenCache = children
        lastSyncDate = Date()
    }

    // MARK: - Transaction Caching

    /// Get cached transactions for a specific child
    /// - Parameter childId: UUID of the child
    /// - Returns: Array of cached transactions, or empty array if no cache exists
    func getCachedTransactions(for childId: UUID) -> [Transaction] {
        return transactionsCache[childId] ?? []
    }

    /// Cache transactions for a specific child
    /// - Parameters:
    ///   - transactions: Array of transactions to cache
    ///   - childId: UUID of the child
    func cacheTransactions(_ transactions: [Transaction], for childId: UUID) {
        transactionsCache[childId] = transactions
    }

    // MARK: - Cache Management

    /// Check if cache needs refresh based on age
    /// - Parameter maxAge: Maximum age in seconds before refresh is needed (default: 300 = 5 minutes)
    /// - Returns: True if cache is stale or doesn't exist, false if cache is fresh
    func needsRefresh(maxAge: TimeInterval = 300) -> Bool {
        guard let lastSync = lastSyncDate else {
            return true // No cache exists
        }

        let age = Date().timeIntervalSince(lastSync)
        return age > maxAge
    }

    /// Clear all cached data
    func clearCache() {
        childrenCache = []
        transactionsCache = [:]
        lastSyncDate = nil
    }

    /// Get the last sync timestamp
    /// - Returns: Date of last sync, or nil if never synced
    func getLastSyncDate() -> Date? {
        return lastSyncDate
    }
}
