import Foundation

// MARK: - Category Service Protocol

protocol CategoryServiceProtocol {
    func getCategories(for type: TransactionType) async throws -> [TransactionCategory]
    func getAllCategories() async throws -> [TransactionCategory]
    func getCategorySpending(
        for childId: UUID,
        startDate: Date?,
        endDate: Date?
    ) async throws -> [CategorySpending]
    func getBudgetStatus(
        for childId: UUID,
        period: BudgetPeriod
    ) async throws -> [CategoryBudgetStatus]
    func checkBudget(
        for childId: UUID,
        category: TransactionCategory,
        amount: Decimal
    ) async throws -> BudgetCheckResult
}

// MARK: - Category Service Implementation

@MainActor
final class CategoryService: ObservableObject, CategoryServiceProtocol {

    // MARK: - Properties

    private let baseURL: URL
    private let urlSession: URLSessionProtocol
    private let keychainService: KeychainServiceProtocol

    private let jsonEncoder: JSONEncoder = {
        let encoder = JSONEncoder()
        encoder.dateEncodingStrategy = .iso8601
        return encoder
    }()

    private let jsonDecoder: JSONDecoder = {
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        return decoder
    }()

    // MARK: - Singleton

    static let shared = CategoryService()

    // MARK: - Initialization

    init(
        baseURL: URL? = nil,
        urlSession: URLSessionProtocol = URLSession.shared,
        keychainService: KeychainServiceProtocol = KeychainService.shared
    ) {
        self.baseURL = baseURL ?? Configuration.apiBaseURL
        self.urlSession = urlSession
        self.keychainService = keychainService
    }

    // MARK: - Public Methods

    /// Get categories for a specific transaction type
    func getCategories(for type: TransactionType) async throws -> [TransactionCategory] {
        let endpoint = baseURL.appendingPathComponent("/api/v1/categories")
            .appending(queryItems: [URLQueryItem(name: "type", value: type.rawValue)])

        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Get all categories
    func getAllCategories() async throws -> [TransactionCategory] {
        let endpoint = baseURL.appendingPathComponent("/api/v1/categories/all")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Get spending breakdown by category for a child
    func getCategorySpending(
        for childId: UUID,
        startDate: Date? = nil,
        endDate: Date? = nil
    ) async throws -> [CategorySpending] {
        var urlComponents = URLComponents(
            url: baseURL.appendingPathComponent("/api/v1/categories/spending/children/\(childId.uuidString)"),
            resolvingAgainstBaseURL: false
        )!

        var queryItems: [URLQueryItem] = []
        if let start = startDate {
            queryItems.append(URLQueryItem(name: "startDate", value: ISO8601DateFormatter().string(from: start)))
        }
        if let end = endDate {
            queryItems.append(URLQueryItem(name: "endDate", value: ISO8601DateFormatter().string(from: end)))
        }
        if !queryItems.isEmpty {
            urlComponents.queryItems = queryItems
        }

        guard let url = urlComponents.url else {
            throw APIError.invalidResponse
        }

        let urlRequest = try await createAuthenticatedRequest(url: url, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Get budget status for a child's budgets
    func getBudgetStatus(
        for childId: UUID,
        period: BudgetPeriod
    ) async throws -> [CategoryBudgetStatus] {
        let endpoint = baseURL.appendingPathComponent("/api/v1/categories/budget-status/children/\(childId.uuidString)")
            .appending(queryItems: [URLQueryItem(name: "period", value: period.rawValue)])

        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Check if a transaction would exceed budget
    func checkBudget(
        for childId: UUID,
        category: TransactionCategory,
        amount: Decimal
    ) async throws -> BudgetCheckResult {
        let endpoint = baseURL.appendingPathComponent("/api/v1/categories/check-budget/children/\(childId.uuidString)")
            .appending(queryItems: [
                URLQueryItem(name: "category", value: category.rawValue),
                URLQueryItem(name: "amount", value: String(describing: amount))
            ])

        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    // MARK: - Private Helpers

    private func createAuthenticatedRequest(
        url: URL,
        method: String,
        body: Data? = nil
    ) async throws -> URLRequest {
        guard let token = try? keychainService.getToken() else {
            throw APIError.unauthorized
        }

        var request = URLRequest(url: url)
        request.httpMethod = method
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")

        if let body = body {
            request.httpBody = body
        }

        return request
    }

    private func performRequest<T: Decodable>(_ request: URLRequest) async throws -> T {
        do {
            let (data, response) = try await urlSession.data(for: request)

            guard let httpResponse = response as? HTTPURLResponse else {
                throw APIError.invalidResponse
            }

            switch httpResponse.statusCode {
            case 200...299:
                do {
                    return try jsonDecoder.decode(T.self, from: data)
                } catch {
                    throw APIError.decodingError
                }
            case 401:
                throw APIError.unauthorized
            case 404:
                throw APIError.notFound
            case 409:
                throw APIError.conflict
            case 500...599:
                throw APIError.serverError
            default:
                throw APIError.unknown
            }
        } catch let error as APIError {
            throw error
        } catch is URLError {
            throw APIError.networkError
        } catch {
            throw APIError.unknown
        }
    }
}
