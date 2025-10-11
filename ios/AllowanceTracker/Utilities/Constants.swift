import Foundation

enum Constants {
    enum API {
        static let baseURL = URL(string: "http://localhost:5000")!

        #if DEBUG
        static let timeout: TimeInterval = 30
        #else
        static let timeout: TimeInterval = 15
        #endif
    }

    enum SignalR {
        static let hubURL = URL(string: "http://localhost:5000/hubs/family")!
    }

    enum Keychain {
        static let tokenKey = "com.allowancetracker.jwt"
    }

    enum DateFormat {
        static let iso8601 = "yyyy-MM-dd'T'HH:mm:ss.SSSZ"
        static let display = "MMM d, yyyy"
        static let displayWithTime = "MMM d, yyyy h:mm a"
    }
}
