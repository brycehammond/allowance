import Foundation
import LocalAuthentication

// MARK: - Biometric Type

enum BiometricType {
    case none
    case touchID
    case faceID
    case opticID

    var displayName: String {
        switch self {
        case .none: return "None"
        case .touchID: return "Touch ID"
        case .faceID: return "Face ID"
        case .opticID: return "Optic ID"
        }
    }

    var iconName: String {
        switch self {
        case .none: return "lock"
        case .touchID: return "touchid"
        case .faceID: return "faceid"
        case .opticID: return "opticid"
        }
    }
}

// MARK: - Biometric Error

enum BiometricError: Error, Equatable {
    case notAvailable
    case notEnrolled
    case authenticationFailed
    case userCancelled
    case systemCancelled
    case passcodeNotSet
    case lockout
    case unknown

    var localizedDescription: String {
        switch self {
        case .notAvailable:
            return "Biometric authentication is not available on this device."
        case .notEnrolled:
            return "No biometric authentication is enrolled. Please set up Face ID or Touch ID in Settings."
        case .authenticationFailed:
            return "Authentication failed. Please try again."
        case .userCancelled:
            return "Authentication was cancelled."
        case .systemCancelled:
            return "Authentication was cancelled by the system."
        case .passcodeNotSet:
            return "A passcode must be set to use biometric authentication."
        case .lockout:
            return "Biometric authentication is locked. Please use your passcode."
        case .unknown:
            return "An unknown error occurred during authentication."
        }
    }
}

// MARK: - Biometric Service Protocol

protocol BiometricServiceProtocol {
    var biometricType: BiometricType { get }
    var isAvailable: Bool { get }
    func authenticate(reason: String) async throws -> Bool
}

// MARK: - Biometric Service

final class BiometricService: BiometricServiceProtocol {

    // MARK: - Singleton

    static let shared = BiometricService()

    private init() {}

    // MARK: - Properties

    private let context = LAContext()

    /// The type of biometric authentication available on the device
    var biometricType: BiometricType {
        let context = LAContext()
        var error: NSError?

        guard context.canEvaluatePolicy(.deviceOwnerAuthenticationWithBiometrics, error: &error) else {
            return .none
        }

        switch context.biometryType {
        case .touchID:
            return .touchID
        case .faceID:
            return .faceID
        case .opticID:
            return .opticID
        case .none:
            return .none
        @unknown default:
            return .none
        }
    }

    /// Whether biometric authentication is available on this device
    var isAvailable: Bool {
        biometricType != .none
    }

    // MARK: - Authentication

    /// Authenticate the user using biometric authentication
    /// - Parameter reason: The reason to display to the user
    /// - Returns: True if authentication succeeded
    /// - Throws: BiometricError if authentication fails
    func authenticate(reason: String) async throws -> Bool {
        let context = LAContext()
        context.localizedCancelTitle = "Use Password"

        var error: NSError?

        // Check if biometric authentication is available
        guard context.canEvaluatePolicy(.deviceOwnerAuthenticationWithBiometrics, error: &error) else {
            throw mapError(error)
        }

        do {
            let success = try await context.evaluatePolicy(
                .deviceOwnerAuthenticationWithBiometrics,
                localizedReason: reason
            )
            return success
        } catch let authError as LAError {
            throw mapLAError(authError)
        } catch {
            throw BiometricError.unknown
        }
    }

    // MARK: - Private Helpers

    private func mapError(_ error: NSError?) -> BiometricError {
        guard let error = error as? LAError else {
            return .notAvailable
        }
        return mapLAError(error)
    }

    private func mapLAError(_ error: LAError) -> BiometricError {
        switch error.code {
        case .biometryNotAvailable:
            return .notAvailable
        case .biometryNotEnrolled:
            return .notEnrolled
        case .authenticationFailed:
            return .authenticationFailed
        case .userCancel:
            return .userCancelled
        case .systemCancel:
            return .systemCancelled
        case .passcodeNotSet:
            return .passcodeNotSet
        case .biometryLockout:
            return .lockout
        default:
            return .unknown
        }
    }
}
