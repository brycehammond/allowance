import GoogleSignIn
import UIKit

/// Wraps the Google Sign-In SDK to extract the ID token for backend authentication
@MainActor
final class GoogleSignInService {

    /// Initiates Google Sign-In and returns the ID token string on success.
    /// The presenting view controller is required by the Google SDK.
    func signIn() async throws -> (idToken: String, firstName: String?, lastName: String?) {
        guard let windowScene = UIApplication.shared.connectedScenes.first as? UIWindowScene,
              let rootViewController = windowScene.windows.first?.rootViewController else {
            throw GoogleSignInError.noPresenter
        }

        let result = try await GIDSignIn.sharedInstance.signIn(withPresenting: rootViewController)
        let user = result.user

        guard let idToken = user.idToken?.tokenString else {
            throw GoogleSignInError.missingIdToken
        }

        return (
            idToken: idToken,
            firstName: user.profile?.givenName,
            lastName: user.profile?.familyName
        )
    }
}

enum GoogleSignInError: Error, LocalizedError {
    case noPresenter
    case missingIdToken

    var errorDescription: String? {
        switch self {
        case .noPresenter:
            return "Unable to find a view controller to present Google Sign In."
        case .missingIdToken:
            return "Google Sign In succeeded but no ID token was returned."
        }
    }
}
