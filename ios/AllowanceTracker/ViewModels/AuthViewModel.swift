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

        // Check if token is expiring soon (within 1 hour) and refresh it
        if keychainService.isTokenExpiringSoon(withinMinutes: 60) {
            await refreshTokenIfNeeded()
        }

        // If biometric is enabled, require authentication before showing content
        if keychainService.isBiometricEnabled() && biometricService.isAvailable {
            requiresBiometricAuth = true
            isAuthenticated = false
        } else {
            // No biometric required, try to restore session
            await restoreSession()
        }
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

    /// Refresh the authentication token if it's expiring soon
    func refreshTokenIfNeeded() async {
        guard keychainService.hasValidToken() else { return }
        guard keychainService.isTokenExpiringSoon(withinMinutes: 60) else { return }

        do {
            let response = try await apiService.refreshToken()
            try? keychainService.saveTokenExpiration(response.expiresAt)
            currentUser = response.user
        } catch {
            // Token refresh failed - user will need to re-authenticate on next app launch
            #if DEBUG
            print("Token refresh failed: \(error)")
            #endif
        }
    }

    // MARK: - Private Session Management

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
        } catch {
            // Token is invalid, clear auth state
            try? keychainService.clearAllAuthData()
            isAuthenticated = false
        }
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
