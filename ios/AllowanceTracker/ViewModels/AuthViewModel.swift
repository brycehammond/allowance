import Foundation
import SwiftUI

/// ViewModel for authentication flow
@Observable
@MainActor
final class AuthViewModel {

    // MARK: - Observable Properties

    var currentUser: User?
    var isAuthenticated = false
    private(set) var isLoading = false
    var errorMessage: String?

    // MARK: - Biometric Authentication

    /// Whether biometric authentication is required before showing content
    var requiresBiometricAuth = false

    /// Whether we should prompt the user to enable biometric auth (e.g., right after first login on a device
    /// that supports Face ID / Touch ID but where the user hasn't enabled it yet).
    var shouldOfferBiometricEnrollment = false

    /// Whether biometric authentication is enabled by the user
    var isBiometricEnabled: Bool {
        keychainService.isBiometricEnabled()
    }

    /// The type of biometric authentication available
    var biometricType: BiometricType {
        biometricService.biometricType
    }

    /// Whether biometric authentication is available on this device
    var isBiometricAvailable: Bool {
        biometricService.isAvailable
    }

    // MARK: - Child View Mode

    /// The child currently being viewed as (nil = normal parent mode)
    var viewingAsChild: Child?

    /// Returns true if user is a parent AND not currently viewing as a child
    var effectiveIsParent: Bool {
        guard viewingAsChild == nil else { return false }
        return currentUser?.isParent ?? false
    }

    /// Returns true if viewing as a child OR the actual user is a child
    var effectiveIsChild: Bool {
        viewingAsChild != nil || currentUser?.role == .child
    }

    /// Whether child view mode is currently active
    var isViewingAsChild: Bool {
        viewingAsChild != nil
    }

    // MARK: - Dependencies

    private let apiService: APIServiceProtocol
    private let keychainService: KeychainServiceProtocol
    private let biometricService: BiometricServiceProtocol

    // MARK: - Initialization

    init(
        apiService: APIServiceProtocol = ServiceProvider.apiService,
        keychainService: KeychainServiceProtocol = ServiceProvider.keychainService,
        biometricService: BiometricServiceProtocol = BiometricService.shared
    ) {
        self.apiService = apiService
        self.keychainService = keychainService
        self.biometricService = biometricService
    }

    // MARK: - Public Methods

    /// Login with email and password
    /// - Parameters:
    ///   - email: User's email address
    ///   - password: User's password
    func login(email: String, password: String) async {
        // Clear previous errors
        errorMessage = nil

        // Validate inputs
        guard validateEmail(email) else {
            errorMessage = "Please enter a valid email address."
            return
        }

        guard !password.isEmpty else {
            errorMessage = "Please enter your password."
            return
        }

        // Set loading state
        isLoading = true
        defer { isLoading = false }

        do {
            let request = LoginRequest(email: email, password: password)
            let response = try await apiService.login(request)

            // Save token expiration
            try? keychainService.saveTokenExpiration(response.expiresAt)

            // Update state on success
            currentUser = response.user
            isAuthenticated = true
            updateBiometricEnrollmentPrompt()

        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "An unexpected error occurred. Please try again."
        }
    }

    /// Register a new user
    /// - Parameters:
    ///   - email: User's email address
    ///   - password: User's password
    ///   - firstName: User's first name
    ///   - lastName: User's last name
    ///   - role: User's role (Parent or Child)
    func register(
        email: String,
        password: String,
        firstName: String,
        lastName: String,
        role: UserRole
    ) async {
        // Clear previous errors
        errorMessage = nil

        // Validate inputs
        guard validateEmail(email) else {
            errorMessage = "Please enter a valid email address."
            return
        }

        guard password.count >= 6 else {
            errorMessage = "Password must be at least 6 characters long."
            return
        }

        guard !firstName.isEmpty else {
            errorMessage = "Please enter your first name."
            return
        }

        guard !lastName.isEmpty else {
            errorMessage = "Please enter your last name."
            return
        }

        // Set loading state
        isLoading = true
        defer { isLoading = false }

        do {
            let request = RegisterRequest(
                email: email,
                password: password,
                firstName: firstName,
                lastName: lastName,
                role: role
            )
            let response = try await apiService.register(request)

            // Save token expiration
            try? keychainService.saveTokenExpiration(response.expiresAt)

            // Update state on success
            currentUser = response.user
            isAuthenticated = true
            updateBiometricEnrollmentPrompt()

        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "An unexpected error occurred. Please try again."
        }
    }

    /// Logout current user
    func logout() async {
        errorMessage = nil
        isLoading = true
        defer { isLoading = false }

        do {
            try await apiService.logout()

            // Clear all auth data from keychain
            try? keychainService.clearAllAuthData()

            // Clear state on success
            viewingAsChild = nil
            currentUser = nil
            isAuthenticated = false
            requiresBiometricAuth = false
            shouldOfferBiometricEnrollment = false

        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to logout. Please try again."
        }
    }

    /// Clear error message
    func clearError() {
        errorMessage = nil
    }

    // MARK: - Biometric Authentication

    /// Check if the user should be automatically authenticated on app launch
    /// Call this when the app launches to restore session or prompt for biometric auth
    func checkAuthenticationStatus() async {
        // Check if we have a valid token
        guard keychainService.hasValidToken() else {
            isAuthenticated = false
            requiresBiometricAuth = false
            return
        }

        // If biometric is enabled, require authentication before showing content.
        // The refresh happens after the biometric prompt succeeds, inside restoreSession().
        if keychainService.isBiometricEnabled() && biometricService.isAvailable {
            requiresBiometricAuth = true
            isAuthenticated = false
        } else {
            // No biometric required, try to restore session (this also refreshes the token).
            await restoreSession()
        }
    }

    /// Called when the app moves to the foreground. Refreshes the JWT to keep the session sliding.
    /// We refresh on every foreground transition (regardless of expiry window) so that any active
    /// user gets an extended token, and only re-authenticate when the token is fully expired.
    func applicationDidBecomeActive() async {
        // Only refresh if we're already authenticated and not gated behind a biometric prompt.
        guard isAuthenticated, keychainService.hasValidToken() else { return }
        await silentlyRefreshToken()
    }

    /// Authenticate using Face ID or Touch ID
    /// - Returns: True if authentication succeeded
    func authenticateWithBiometric() async -> Bool {
        guard biometricService.isAvailable else {
            errorMessage = "Biometric authentication is not available."
            return false
        }

        do {
            let reason = "Unlock Allowance Tracker"
            let success = try await biometricService.authenticate(reason: reason)

            if success {
                requiresBiometricAuth = false
                await restoreSession()
                return true
            } else {
                errorMessage = "Authentication failed."
                return false
            }

        } catch let error as BiometricError {
            if error == .userCancelled {
                // Don't show error for user cancellation
                return false
            }
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "An error occurred during authentication."
            return false
        }
    }

    /// Enable or disable biometric authentication
    /// - Parameter enabled: Whether to enable biometric authentication
    func setBiometricEnabled(_ enabled: Bool) {
        do {
            try keychainService.saveBiometricEnabled(enabled)
        } catch {
            errorMessage = "Failed to save biometric setting."
        }
    }

    /// Refresh the authentication token. Called eagerly on app launch and foreground transitions
    /// to keep the user logged in as long as possible (sliding session). Silently no-ops on failure
    /// so transient network errors don't sign the user out.
    func refreshTokenIfNeeded() async {
        await silentlyRefreshToken()
    }

    // MARK: - Private Session Management

    /// Refresh the JWT without surfacing errors. If refresh fails but we still have a non-expired
    /// token in keychain, we keep the existing session — the next API call will surface a real auth
    /// error if the token is truly bad.
    private func silentlyRefreshToken() async {
        guard keychainService.hasValidToken() else { return }

        do {
            let response = try await apiService.refreshToken()
            try? keychainService.saveTokenExpiration(response.expiresAt)
            currentUser = response.user
        } catch {
            #if DEBUG
            print("Silent token refresh failed (keeping existing session): \(error)")
            #endif
        }
    }

    /// Restore the session from stored token
    private func restoreSession() async {
        guard keychainService.hasValidToken() else {
            isAuthenticated = false
            return
        }

        // Try to refresh token and get current user info
        do {
            let response = try await apiService.refreshToken()
            try? keychainService.saveTokenExpiration(response.expiresAt)
            currentUser = response.user
            isAuthenticated = true
        } catch APIError.unauthorized {
            // Token is genuinely invalid, clear auth state
            try? keychainService.clearAllAuthData()
            isAuthenticated = false
        } catch {
            // Likely a network blip — keep the user signed in with the existing token.
            // We already validated hasValidToken() above, so the JWT is still usable.
            isAuthenticated = true
            #if DEBUG
            print("Session refresh failed but keeping local session: \(error)")
            #endif
        }
    }

    /// Sets the biometric enrollment prompt flag if the device supports biometrics but the user
    /// hasn't enabled it yet. Called after a successful login or registration.
    private func updateBiometricEnrollmentPrompt() {
        shouldOfferBiometricEnrollment = biometricService.isAvailable
            && !keychainService.isBiometricEnabled()
    }

    /// Dismiss the biometric enrollment prompt (the user either accepted or declined).
    func dismissBiometricEnrollmentPrompt() {
        shouldOfferBiometricEnrollment = false
    }

    // MARK: - Child View Mode Methods

    /// Enter child view mode to see app as a specific child
    /// - Parameter child: The child to view as
    func enterChildViewMode(child: Child) {
        viewingAsChild = child
    }

    /// Exit child view mode and return to parent view
    func exitChildViewMode() {
        viewingAsChild = nil
    }

    /// Change password for current user
    /// - Parameters:
    ///   - currentPassword: User's current password
    ///   - newPassword: New password to set
    ///   - confirmPassword: Confirmation of new password
    /// - Returns: True if successful, false otherwise
    func changePassword(
        currentPassword: String,
        newPassword: String,
        confirmPassword: String
    ) async -> Bool {
        // Clear previous errors
        errorMessage = nil

        // Validate inputs
        guard !currentPassword.isEmpty else {
            errorMessage = "Please enter your current password."
            return false
        }

        guard newPassword.count >= 6 else {
            errorMessage = "New password must be at least 6 characters long."
            return false
        }

        guard newPassword == confirmPassword else {
            errorMessage = "New passwords do not match."
            return false
        }

        // Set loading state
        isLoading = true
        defer { isLoading = false }

        do {
            let request = ChangePasswordRequest(
                currentPassword: currentPassword,
                newPassword: newPassword
            )
            _ = try await apiService.changePassword(request)
            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to change password. Please try again."
            return false
        }
    }

    /// Request password reset email
    /// - Parameter email: User's email address
    /// - Returns: True if successful, false otherwise
    func forgotPassword(email: String) async -> Bool {
        // Clear previous errors
        errorMessage = nil

        // Validate input
        guard validateEmail(email) else {
            errorMessage = "Please enter a valid email address."
            return false
        }

        // Set loading state
        isLoading = true
        defer { isLoading = false }

        do {
            let request = ForgotPasswordRequest(email: email)
            _ = try await apiService.forgotPassword(request)
            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to send reset email. Please try again."
            return false
        }
    }

    /// Reset password with token from email
    /// - Parameters:
    ///   - email: User's email address
    ///   - token: Reset token from email
    ///   - newPassword: New password to set
    ///   - confirmPassword: Confirmation of new password
    /// - Returns: True if successful, false otherwise
    func resetPassword(
        email: String,
        token: String,
        newPassword: String,
        confirmPassword: String
    ) async -> Bool {
        // Clear previous errors
        errorMessage = nil

        // Validate inputs
        guard validateEmail(email) else {
            errorMessage = "Please enter a valid email address."
            return false
        }

        guard !token.isEmpty else {
            errorMessage = "Invalid reset token."
            return false
        }

        guard newPassword.count >= 6 else {
            errorMessage = "Password must be at least 6 characters long."
            return false
        }

        guard newPassword == confirmPassword else {
            errorMessage = "Passwords do not match."
            return false
        }

        // Set loading state
        isLoading = true
        defer { isLoading = false }

        do {
            let request = ResetPasswordRequest(
                email: email,
                resetToken: token,
                newPassword: newPassword
            )
            _ = try await apiService.resetPassword(request)
            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to reset password. Please try again."
            return false
        }
    }

    /// Delete current user's account
    /// - Returns: True if successful, false otherwise
    func deleteAccount() async -> Bool {
        // Clear previous errors
        errorMessage = nil

        // Set loading state
        isLoading = true
        defer { isLoading = false }

        do {
            try await apiService.deleteAccount()

            // Clear all auth data from keychain
            try? keychainService.clearAllAuthData()

            // Clear state
            viewingAsChild = nil
            currentUser = nil
            isAuthenticated = false
            requiresBiometricAuth = false
            shouldOfferBiometricEnrollment = false

            return true

        } catch let error as APIError {
            errorMessage = error.localizedDescription
            return false
        } catch {
            errorMessage = "Failed to delete account. Please try again."
            return false
        }
    }

    // MARK: - Private Helpers

    /// Validate email format
    /// - Parameter email: Email string to validate
    /// - Returns: True if valid email format
    private func validateEmail(_ email: String) -> Bool {
        guard !email.isEmpty else { return false }

        let emailRegex = "[A-Z0-9a-z._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,64}"
        let emailPredicate = NSPredicate(format: "SELF MATCHES %@", emailRegex)
        return emailPredicate.evaluate(with: email)
    }
}
