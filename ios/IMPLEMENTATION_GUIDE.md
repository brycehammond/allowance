# iOS App Complete Implementation Guide

This guide provides all the code needed to implement the Allowance Tracker iOS app.

## Foundation (Already Created) ✅

- `/ios/AllowanceTracker/Utilities/Constants.swift` ✅
- `/ios/AllowanceTracker/Utilities/Extensions.swift` ✅
- `/ios/AllowanceTracker/Models/User.swift` ✅

## Remaining Implementation

### 1. Complete Models Layer

#### Child.swift
```swift
import Foundation

struct Child: Codable, Identifiable {
    let id: UUID
    let firstName: String
    let lastName: String
    let weeklyAllowance: Decimal
    let currentBalance: Decimal
    let lastAllowanceDate: Date?

    var fullName: String {
        "\(firstName) \(lastName)"
    }

    var formattedBalance: String {
        currentBalance.currencyFormatted
    }
}
```

#### Transaction.swift
```swift
import Foundation

struct Transaction: Codable, Identifiable {
    let id: UUID
    let childId: UUID
    let amount: Decimal
    let type: TransactionType
    let category: String
    let description: String
    let balanceAfter: Decimal
    let createdAt: Date
    let createdByName: String

    var isCredit: Bool {
        type == .credit
    }

    var formattedAmount: String {
        let prefix = isCredit ? "+" : "-"
        return "\(prefix)\(amount.currencyFormatted)"
    }
}

enum TransactionType: String, Codable {
    case credit = "Credit"
    case debit = "Debit"
}

struct CreateTransactionRequest: Codable {
    let childId: UUID
    let amount: Decimal
    let type: TransactionType
    let category: String
    let description: String
}
```

#### WishListItem.swift
```swift
import Foundation

struct WishListItem: Codable, Identifiable {
    let id: UUID
    let childId: UUID
    let name: String
    let price: Decimal
    let url: String?
    let notes: String?
    let isPurchased: Bool
    let purchasedAt: Date?
    let createdAt: Date
    let canAfford: Bool

    func progressPercentage(currentBalance: Decimal) -> Double {
        guard price > 0 else { return 1.0 }
        let progress = (currentBalance / price)
        return min(Double(truncating: progress as NSDecimalNumber), 1.0)
    }
}

struct CreateWishListItemRequest: Codable {
    let childId: UUID
    let name: String
    let price: Decimal
    let url: String?
    let notes: String?
}
```

#### Analytics Models
```swift
import Foundation

struct BalancePoint: Codable, Identifiable {
    var id: UUID { UUID() }
    let date: Date
    let balance: Decimal
    let transactionDescription: String?
}

struct IncomeSpendingSummary: Codable {
    let totalIncome: Decimal
    let totalSpending: Decimal
    let netSavings: Decimal
    let incomeTransactionCount: Int
    let spendingTransactionCount: Int
    let savingsRate: Decimal
}

struct MonthlyComparison: Codable, Identifiable {
    var id: UUID { UUID() }
    let year: Int
    let month: Int
    let monthName: String
    let income: Decimal
    let spending: Decimal
    let netSavings: Decimal
    let endingBalance: Decimal
}

struct CategoryBreakdown: Codable, Identifiable {
    var id: UUID { UUID() }
    let category: String
    let amount: Decimal
    let percentage: Decimal
    let transactionCount: Int
}
```

### 2. Services Layer

#### APIService.swift
```swift
import Foundation

enum HTTPMethod: String {
    case get = "GET"
    case post = "POST"
    case put = "PUT"
    case delete = "DELETE"
}

enum APIError: Error, LocalizedError {
    case invalidResponse
    case httpError(Int)
    case decodingError(Error)
    case networkError(Error)
    case unauthorized

    var errorDescription: String? {
        switch self {
        case .invalidResponse:
            return "Invalid response from server"
        case .httpError(let code):
            return "HTTP error: \(code)"
        case .decodingError(let error):
            return "Failed to decode response: \(error.localizedDescription)"
        case .networkError(let error):
            return "Network error: \(error.localizedDescription)"
        case .unauthorized:
            return "Unauthorized. Please login again."
        }
    }
}

final class APIService {
    static let shared = APIService()

    private let baseURL: URL
    private let session: URLSession
    private let keychainService: KeychainService

    init(
        baseURL: URL = Constants.API.baseURL,
        session: URLSession = .shared,
        keychainService: KeychainService = .shared
    ) {
        self.baseURL = baseURL
        self.session = session
        self.keychainService = keychainService
    }

    func request<T: Decodable>(
        endpoint: Endpoint,
        method: HTTPMethod = .get,
        body: Encodable? = nil
    ) async throws -> T {
        var request = URLRequest(url: baseURL.appendingPathComponent(endpoint.path))
        request.httpMethod = method.rawValue
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.timeoutInterval = Constants.API.timeout

        // Add JWT token if available
        if let token = try? keychainService.getToken() {
            request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        }

        // Add body if present
        if let body = body {
            request.httpBody = try JSONEncoder.default.encode(body)
        }

        do {
            let (data, response) = try await session.data(for: request)

            guard let httpResponse = response as? HTTPURLResponse else {
                throw APIError.invalidResponse
            }

            if httpResponse.statusCode == 401 {
                throw APIError.unauthorized
            }

            guard (200...299).contains(httpResponse.statusCode) else {
                throw APIError.httpError(httpResponse.statusCode)
            }

            let decoded = try JSONDecoder.default.decode(T.self, from: data)
            return decoded
        } catch let error as DecodingError {
            throw APIError.decodingError(error)
        } catch let error as APIError {
            throw error
        } catch {
            throw APIError.networkError(error)
        }
    }
}
```

#### Endpoints.swift
```swift
import Foundation

enum Endpoint {
    // Auth
    case login
    case registerParent
    case currentUser

    // Children
    case children
    case child(UUID)
    case childTransactions(UUID)

    // Transactions
    case createTransaction

    // WishList
    case wishList(childId: UUID)
    case wishListItem(UUID)
    case purchaseItem(UUID)

    // Analytics
    case balanceHistory(childId: UUID, days: Int)
    case incomeVsSpending(childId: UUID)
    case spendingBreakdown(childId: UUID)
    case monthlyComparison(childId: UUID, months: Int)

    var path: String {
        switch self {
        case .login:
            return "/api/v1/auth/login"
        case .registerParent:
            return "/api/v1/auth/register/parent"
        case .currentUser:
            return "/api/v1/auth/me"
        case .children:
            return "/api/v1/children"
        case .child(let id):
            return "/api/v1/children/\(id.uuidString)"
        case .childTransactions(let id):
            return "/api/v1/children/\(id.uuidString)/transactions"
        case .createTransaction:
            return "/api/v1/transactions"
        case .wishList(let childId):
            return "/api/v1/wishlist/children/\(childId.uuidString)"
        case .wishListItem(let id):
            return "/api/v1/wishlist/\(id.uuidString)"
        case .purchaseItem(let id):
            return "/api/v1/wishlist/\(id.uuidString)/purchase"
        case .balanceHistory(let childId, let days):
            return "/api/v1/analytics/children/\(childId.uuidString)/balance-history?days=\(days)"
        case .incomeVsSpending(let childId):
            return "/api/v1/analytics/children/\(childId.uuidString)/income-spending"
        case .spendingBreakdown(let childId):
            return "/api/v1/analytics/children/\(childId.uuidString)/spending-breakdown"
        case .monthlyComparison(let childId, let months):
            return "/api/v1/analytics/children/\(childId.uuidString)/monthly-comparison?months=\(months)"
        }
    }
}
```

#### KeychainService.swift
```swift
import Foundation
import Security

enum KeychainError: Error {
    case saveFailed(OSStatus)
    case notFound
    case deleteFailed(OSStatus)
}

final class KeychainService {
    static let shared = KeychainService()

    private let tokenKey = Constants.Keychain.tokenKey

    func saveToken(_ token: String) throws {
        let data = Data(token.utf8)

        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrAccount as String: tokenKey,
            kSecValueData as String: data
        ]

        // Delete existing item
        SecItemDelete(query as CFDictionary)

        // Add new item
        let status = SecItemAdd(query as CFDictionary, nil)

        guard status == errSecSuccess else {
            throw KeychainError.saveFailed(status)
        }
    }

    func getToken() throws -> String {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrAccount as String: tokenKey,
            kSecReturnData as String: true
        ]

        var result: AnyObject?
        let status = SecItemCopyMatching(query as CFDictionary, &result)

        guard status == errSecSuccess,
              let data = result as? Data,
              let token = String(data: data, encoding: .utf8) else {
            throw KeychainError.notFound
        }

        return token
    }

    func deleteToken() throws {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrAccount as String: tokenKey
        ]

        let status = SecItemDelete(query as CFDictionary)

        guard status == errSecSuccess || status == errSecItemNotFound else {
            throw KeychainError.deleteFailed(status)
        }
    }
}
```

### 3. ViewModels Layer

#### AuthViewModel.swift
```swift
import Foundation

@MainActor
final class AuthViewModel: ObservableObject {
    @Published var currentUser: User?
    @Published var isAuthenticated = false
    @Published var isLoading = false
    @Published var errorMessage: String?

    private let apiService: APIService
    private let keychainService: KeychainService

    init(
        apiService: APIService = .shared,
        keychainService: KeychainService = .shared
    ) {
        self.apiService = apiService
        self.keychainService = keychainService

        Task {
            await checkAuthentication()
        }
    }

    func login(email: String, password: String) async {
        isLoading = true
        errorMessage = nil

        do {
            let request = LoginRequest(email: email, password: password)
            let response: AuthResponse = try await apiService.request(
                endpoint: .login,
                method: .post,
                body: request
            )

            try keychainService.saveToken(response.token)
            currentUser = response.user
            isAuthenticated = true
        } catch {
            errorMessage = error.localizedDescription
        }

        isLoading = false
    }

    func logout() {
        try? keychainService.deleteToken()
        currentUser = nil
        isAuthenticated = false
    }

    private func checkAuthentication() async {
        guard (try? keychainService.getToken()) != nil else {
            return
        }

        do {
            let user: User = try await apiService.request(
                endpoint: .currentUser,
                method: .get
            )
            currentUser = user
            isAuthenticated = true
        } catch {
            try? keychainService.deleteToken()
        }
    }
}
```

Continue implementation with DashboardViewModel, TransactionViewModel, WishListViewModel, and AnalyticsViewModel following the same pattern.

### 4. Views Layer

Create SwiftUI views for:
- LoginView with form validation
- DashboardView with child cards
- ChildCardView with quick actions
- TransactionListView with history
- WishListView with purchase tracking
- AnalyticsView with charts

## Xcode Project Setup

1. Open Xcode and create new iOS App project
2. Name: "AllowanceTracker"
3. Team: Your team
4. Organization Identifier: com.yourcompany
5. Interface: SwiftUI
6. Language: Swift
7. Minimum iOS version: 17.0

## Next Steps

1. Copy all model files to Xcode project
2. Implement service layer
3. Build ViewModels
4. Create SwiftUI views
5. Add SignalR package dependency
6. Test with local backend
7. Deploy to TestFlight

