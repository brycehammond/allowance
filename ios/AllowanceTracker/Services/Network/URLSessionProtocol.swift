import Foundation

/// Protocol for URLSession to enable dependency injection and testing
protocol URLSessionProtocol {
    func data(for request: URLRequest) async throws -> (Data, URLResponse)
}

/// Conform URLSession to the protocol
extension URLSession: URLSessionProtocol {}
