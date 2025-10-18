import Foundation
import SwiftUI

/// ViewModel for authentication flow
@MainActor
final class AuthViewModel: ObservableObject {

    // MARK: - Published Properties

    @Published private(set) var currentUser: User?
    @Published private(set) var isAuthenticated = false
    @Published private(set) var isLoading = false
    @Published var errorMessage: String?

    // MARK: - Dependencies

    private let apiService: APIServiceProtocol

    // MARK: - Initialization

    init(apiService: APIServiceProtocol = APIService()) {
        self.apiService = apiService
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

            // Clear state on success
            currentUser = nil
            isAuthenticated = false

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
