import Foundation

enum Constants {
    enum API {
        static let baseURL = Configuration.apiBaseURL

        #if DEBUG
        static let timeout: TimeInterval = 30
        #else
        static let timeout: TimeInterval = 15
        #endif
    }

    enum Keychain {
        static let tokenKey = "com.allowancetracker.jwt"
    }

    enum Legal {
        static let website = URL(string: "https://www.earnandlearn.app")!
        static let terms = URL(string: "https://www.earnandlearn.app/terms")!
        static let privacy = URL(string: "https://www.earnandlearn.app/privacy")!
    }

    enum DateFormat {
        static let iso8601 = "yyyy-MM-dd'T'HH:mm:ss.SSSZ"
        static let display = "MMM d, yyyy"
        static let displayWithTime = "MMM d, yyyy h:mm a"
    }
}
