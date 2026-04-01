import AuthenticationServices
import SwiftUI

/// A SwiftUI wrapper around SignInWithAppleButton that matches the app's style
struct AppleSignInButton: View {

    // MARK: - Properties

    var onCompletion: (Result<ASAuthorization, Error>) -> Void

    @Environment(\.colorScheme) private var colorScheme

    // MARK: - Body

    var body: some View {
        SignInWithAppleButton(.signIn) { request in
            request.requestedScopes = [.fullName, .email]
        } onCompletion: { result in
            onCompletion(result)
        }
        .signInWithAppleButtonStyle(colorScheme == .dark ? .white : .black)
        .frame(height: 50)
        .cornerRadius(12)
    }
}

// MARK: - Preview

#Preview {
    VStack(spacing: 16) {
        AppleSignInButton { result in
            print("Apple Sign In result: \(result)")
        }
    }
    .padding()
}
