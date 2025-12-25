import SwiftUI

extension Color {

    // MARK: - Brand Colors

    /// Primary brand color (muted green for money/stability)
    static let brandPrimary = Color.green500

    /// Secondary brand color (amber for achievements/savings)
    static let brandSecondary = Color.amber500

    /// Accent color for important actions
    static let brandAccent = Color.amber500

    // MARK: - Transaction Colors

    /// Color for credit/income transactions
    static let transactionCredit = Color.green500

    /// Color for debit/expense transactions
    static let transactionDebit = Color.error

    /// Color for pending transactions
    static let transactionPending = Color.amber500

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
    static let balancePositive = Color.green500

    /// Negative balance color
    static let balanceNegative = Color.error

    /// Zero balance color
    static let balanceZero = Color.gray500
}

// Note: init(hex:) is defined in DesignSystem.swift
