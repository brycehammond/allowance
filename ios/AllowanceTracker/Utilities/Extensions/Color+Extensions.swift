import SwiftUI

extension Color {

    // MARK: - Brand Colors

    /// Primary brand color (green for money)
    static let brandPrimary = Color.green

    /// Secondary brand color (blue for trust)
    static let brandSecondary = Color.blue

    /// Accent color for important actions
    static let brandAccent = Color.orange

    // MARK: - Transaction Colors

    /// Color for credit/income transactions
    static let transactionCredit = Color.green

    /// Color for debit/expense transactions
    static let transactionDebit = Color.red

    /// Color for pending transactions
    static let transactionPending = Color.orange

    // MARK: - Status Colors

    /// Success state color
    static let success = Color.green

    /// Warning state color
    static let warning = Color.orange

    /// Error state color
    static let error = Color.red

    /// Info state color
    static let info = Color.blue

    // MARK: - Background Colors

    /// Primary background color (adapts to light/dark mode)
    static let backgroundPrimary = Color(uiColor: .systemBackground)

    /// Secondary background color (adapts to light/dark mode)
    static let backgroundSecondary = Color(uiColor: .secondarySystemBackground)

    /// Tertiary background color (adapts to light/dark mode)
    static let backgroundTertiary = Color(uiColor: .tertiarySystemBackground)

    // MARK: - Text Colors

    /// Primary text color (adapts to light/dark mode)
    static let textPrimary = Color(uiColor: .label)

    /// Secondary text color (adapts to light/dark mode)
    static let textSecondary = Color(uiColor: .secondaryLabel)

    /// Tertiary text color (adapts to light/dark mode)
    static let textTertiary = Color(uiColor: .tertiaryLabel)

    // MARK: - Currency

    /// Positive balance color
    static let balancePositive = Color.green

    /// Negative balance color
    static let balanceNegative = Color.red

    /// Zero balance color
    static let balanceZero = Color.gray
}

// MARK: - Hex Color Support

extension Color {
    /// Initialize Color from hex string
    /// - Parameter hex: Hex color string (e.g., "#FF5733" or "FF5733")
    init(hex: String) {
        let hex = hex.trimmingCharacters(in: CharacterSet.alphanumerics.inverted)
        var int: UInt64 = 0
        Scanner(string: hex).scanHexInt64(&int)
        let a, r, g, b: UInt64
        switch hex.count {
        case 3: // RGB (12-bit)
            (a, r, g, b) = (255, (int >> 8) * 17, (int >> 4 & 0xF) * 17, (int & 0xF) * 17)
        case 6: // RGB (24-bit)
            (a, r, g, b) = (255, int >> 16, int >> 8 & 0xFF, int & 0xFF)
        case 8: // ARGB (32-bit)
            (a, r, g, b) = (int >> 24, int >> 16 & 0xFF, int >> 8 & 0xFF, int & 0xFF)
        default:
            (a, r, g, b) = (255, 0, 0, 0)
        }

        self.init(
            .sRGB,
            red: Double(r) / 255,
            green: Double(g) / 255,
            blue: Double(b) / 255,
            opacity: Double(a) / 255
        )
    }
}
