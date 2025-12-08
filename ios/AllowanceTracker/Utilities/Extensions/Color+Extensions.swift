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

    // Note: Status colors (success, warning, error, info) are defined in DesignSystem.swift

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

// Note: init(hex:) is defined in DesignSystem.swift
