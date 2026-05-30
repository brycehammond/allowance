//
//  LegalAgreementView.swift
//  AllowanceTracker
//
//  Shared "you agree to our Terms / Privacy" footer used on the
//  Login and Registration screens. Opens each document in an in-app
//  Safari sheet so the user is never taken out of the auth flow.
//

import SwiftUI

struct LegalAgreementView: View {

    /// Leading sentence, e.g. "By signing in, you agree to our".
    let introText: String

    @State private var presentedURL: IdentifiableURL?

    var body: some View {
        VStack(spacing: 4) {
            Text(introText)
                .foregroundStyle(.secondary)

            HStack(spacing: 4) {
                Button("Terms of Service") {
                    presentedURL = IdentifiableURL(url: Constants.Legal.terms)
                }
                .foregroundStyle(Color.green600)
                .accessibilityIdentifier(AccessibilityIdentifier.legalTermsLink)

                Text("and")
                    .foregroundStyle(.secondary)

                Button("Privacy Policy") {
                    presentedURL = IdentifiableURL(url: Constants.Legal.privacy)
                }
                .foregroundStyle(Color.green600)
                .accessibilityIdentifier(AccessibilityIdentifier.legalPrivacyLink)
            }
        }
        .font(.scalable(.caption))
        .multilineTextAlignment(.center)
        .padding(.top, 8)
        .sheet(item: $presentedURL) { item in
            SafariView(url: item.url)
                .ignoresSafeArea()
        }
    }
}

/// Wraps a `URL` so it can drive `.sheet(item:)`.
private struct IdentifiableURL: Identifiable {
    let url: URL
    var id: String { url.absoluteString }
}

// MARK: - Preview

#Preview {
    LegalAgreementView(introText: "By signing in, you agree to our")
}
