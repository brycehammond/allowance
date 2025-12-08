import Foundation

/// API-related errors
enum APIError: Error, Equatable {
    case unauthorized
    case notFound
    case conflict
    case serverError
    case networkError
    case decodingError
    case invalidURL
    case invalidResponse
    case validationError(String)
    case httpError(Int)
    case unknown

    var localizedDescription: String {
        switch self {
        case .unauthorized:
            return "Authentication failed. Please check your credentials."
        case .notFound:
            return "The requested resource was not found."
        case .conflict:
            return "A conflict occurred. This resource may already exist."
        case .serverError:
            return "Server error occurred. Please try again later."
        case .networkError:
            return "Network connection failed. Please check your internet connection."
        case .decodingError:
            return "Failed to process server response."
        case .invalidURL:
            return "Invalid URL configuration."
        case .invalidResponse:
            return "Invalid server response."
        case .validationError(let message):
            return message
        case .httpError(let statusCode):
            return "HTTP error: \(statusCode)"
        case .unknown:
            return "An unknown error occurred."
        }
    }
}
