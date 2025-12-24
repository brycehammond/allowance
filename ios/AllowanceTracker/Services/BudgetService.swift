import Foundation

// MARK: - Budget Service Protocol

protocol BudgetServiceProtocol {
    func getBudgets(for childId: UUID) async throws -> [CategoryBudget]
    func getBudget(for childId: UUID, category: TransactionCategory) async throws -> CategoryBudget?
    func setBudget(_ budget: SetBudgetRequest) async throws -> CategoryBudget
    func deleteBudget(for childId: UUID, category: TransactionCategory) async throws
}

// MARK: - Budget Service Implementation

@MainActor
final class BudgetService: ObservableObject, BudgetServiceProtocol {

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

    static let shared = BudgetService()

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

    /// Get all budgets for a child
    func getBudgets(for childId: UUID) async throws -> [CategoryBudget] {
        let endpoint = baseURL.appendingPathComponent("/api/v1/children/\(childId.uuidString)/budgets")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")
        return try await performRequest(urlRequest)
    }

    /// Get a specific budget for a child and category
    func getBudget(
        for childId: UUID,
        category: TransactionCategory
    ) async throws -> CategoryBudget? {
        let endpoint = baseURL.appendingPathComponent("/api/v1/children/\(childId.uuidString)/budgets/\(category.rawValue)")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "GET")

        do {
            return try await performRequest(urlRequest)
        } catch APIError.notFound {
            // Budget doesn't exist - return nil instead of throwing
            return nil
        }
    }

    /// Create or update a budget
    func setBudget(_ budget: SetBudgetRequest) async throws -> CategoryBudget {
        let endpoint = baseURL.appendingPathComponent("/api/v1/budgets")
        let body = try jsonEncoder.encode(budget)
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "PUT", body: body)
        return try await performRequest(urlRequest)
    }

    /// Delete a budget
    func deleteBudget(
        for childId: UUID,
        category: TransactionCategory
    ) async throws {
        let endpoint = baseURL.appendingPathComponent("/api/v1/children/\(childId.uuidString)/budgets/\(category.rawValue)")
        let urlRequest = try await createAuthenticatedRequest(url: endpoint, method: "DELETE")

        // For DELETE, we expect an empty response
        let _: EmptyResponse = try await performRequest(urlRequest)
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
