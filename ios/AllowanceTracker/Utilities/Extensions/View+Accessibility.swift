import SwiftUI

// MARK: - View Accessibility Extensions

extension View {
    /// Add comprehensive accessibility support with label, hint, and traits
    /// - Parameters:
    ///   - label: The accessibility label describing the element
    ///   - hint: Optional hint describing the result of performing an action
    ///   - traits: Accessibility traits (button, header, etc.)
    ///   - value: Current value for adjustable elements
    func accessibility(
        label: String,
        hint: String? = nil,
        traits: AccessibilityTraits? = nil,
        value: String? = nil
    ) -> some View {
        self
            .accessibilityLabel(label)
            .if(hint != nil) { view in
                view.accessibilityHint(hint!)
            }
            .if(traits != nil) { view in
                view.accessibilityAddTraits(traits!)
            }
            .if(value != nil) { view in
                view.accessibilityValue(value!)
            }
    }

    /// Make view accessible as a button with proper labeling
    /// - Parameters:
    ///   - label: The button's accessibility label
    ///   - hint: Optional hint describing what happens when pressed
    func accessibleButton(label: String, hint: String? = nil) -> some View {
        self.accessibility(
            label: label,
            hint: hint,
            traits: .isButton
        )
    }

    /// Make view accessible as a header
    /// - Parameter label: The header's accessibility label
    func accessibleHeader(_ label: String) -> some View {
        self.accessibility(
            label: label,
            traits: .isHeader
        )
    }

    /// Combine multiple elements into single accessibility element
    /// - Parameter label: Combined accessibility label
    func accessibleElement(label: String) -> some View {
        self
            .accessibilityElement(children: .combine)
            .accessibilityLabel(label)
    }

    /// Hide from accessibility when not relevant
    func accessibilityHidden() -> some View {
        self.accessibilityHidden(true)
    }

    /// Make adjustable with proper increment/decrement actions
    /// - Parameters:
    ///   - label: The accessibility label
    ///   - value: Current value as string
    ///   - increment: Action when incrementing
    ///   - decrement: Action when decrementing
    func accessibleAdjustable(
        label: String,
        value: String,
        increment: @escaping () -> Void,
        decrement: @escaping () -> Void
    ) -> some View {
        self
            .accessibilityLabel(label)
            .accessibilityValue(value)
            .accessibilityAdjustableAction { direction in
                switch direction {
                case .increment:
                    increment()
                case .decrement:
                    decrement()
                @unknown default:
                    break
                }
            }
    }

    /// Conditional modifier
    @ViewBuilder
    func `if`<Content: View>(_ condition: Bool, transform: (Self) -> Content) -> some View {
        if condition {
            transform(self)
        } else {
            self
        }
    }
}

// MARK: - Currency Accessibility

extension Decimal {
    /// Format currency for accessibility with spoken pronunciation
    /// Example: $25.50 -> "25 dollars and 50 cents"
    var accessibilityCurrencyLabel: String {
        // Use NSDecimalNumber for reliable Decimal to Int conversion
        let nsDecimal = NSDecimalNumber(decimal: self)
        let dollarAmount = nsDecimal.intValue
        let fractionalPart = self - Decimal(dollarAmount)
        let centsNsDecimal = NSDecimalNumber(decimal: fractionalPart * 100)
        let centsAmount = centsNsDecimal.intValue

        if centsAmount == 0 {
            return "\(abs(dollarAmount)) dollar\(abs(dollarAmount) == 1 ? "" : "s")"
        } else {
            return "\(abs(dollarAmount)) dollar\(abs(dollarAmount) == 1 ? "" : "s") and \(abs(centsAmount)) cent\(abs(centsAmount) == 1 ? "" : "s")"
        }
    }
}

// MARK: - Transaction Type Accessibility

extension TransactionType {
    /// Accessibility description for transaction type
    var accessibilityLabel: String {
        switch self {
        case .credit:
            return "Money added"
        case .debit:
            return "Money spent"
        }
    }
}

// MARK: - Date Accessibility

extension Date {
    /// Format date for accessibility with relative descriptions
    var accessibilityLabel: String {
        let calendar = Calendar.current
        let now = Date()

        if calendar.isDateInToday(self) {
            let formatter = DateFormatter()
            formatter.timeStyle = .short
            return "Today at \(formatter.string(from: self))"
        } else if calendar.isDateInYesterday(self) {
            let formatter = DateFormatter()
            formatter.timeStyle = .short
            return "Yesterday at \(formatter.string(from: self))"
        } else if calendar.isDate(self, equalTo: now, toGranularity: .weekOfYear) {
            let formatter = DateFormatter()
            formatter.dateFormat = "EEEE 'at' h:mm a"
            return formatter.string(from: self)
        } else {
            let formatter = DateFormatter()
            formatter.dateStyle = .medium
            formatter.timeStyle = .short
            return formatter.string(from: self)
        }
    }
}

// MARK: - Accessibility Identifiers (for UI Testing)

enum AccessibilityIdentifier {
    // Auth
    static let loginEmailField = "login_email_field"
    static let loginPasswordField = "login_password_field"
    static let loginButton = "login_button"
    static let registerButton = "register_button"
    static let forgotPasswordButton = "forgot_password_button"

    // Registration
    static let registerFirstNameField = "register_first_name_field"
    static let registerLastNameField = "register_last_name_field"
    static let registerEmailField = "register_email_field"
    static let registerPasswordField = "register_password_field"
    static let registerConfirmPasswordField = "register_confirm_password_field"
    static let registerRolePicker = "register_role_picker"
    static let registerSubmitButton = "register_submit_button"
    static let registerCancelButton = "register_cancel_button"

    // Dashboard
    static let childCard = "child_card_"
    static let addChildButton = "add_child_button"
    static let refreshButton = "refresh_button"

    // Transactions
    static let transactionRow = "transaction_row_"
    static let createTransactionButton = "create_transaction_button"
    static let transactionAmountField = "transaction_amount_field"
    static let transactionDescriptionField = "transaction_description_field"
    static let transactionNotesField = "transaction_notes_field"
    static let transactionTypePicker = "transaction_type_picker"
    static let transactionCategoryPicker = "transaction_category_picker"
    static let transactionSaveButton = "transaction_save_button"
    static let transactionCancelButton = "transaction_cancel_button"
    static let transactionBalanceLabel = "transaction_balance_label"

    // Savings
    static let savingsAccountCard = "savings_account_card_"
    static let addSavingsAccountButton = "add_savings_account_button"
    static let depositButton = "deposit_button"
    static let withdrawButton = "withdraw_button"
    static let savingsNameField = "savings_name_field"
    static let savingsTargetField = "savings_target_field"
    static let savingsAmountField = "savings_amount_field"
    static let savingsTransaction = "savings_transaction_"

    // Profile
    static let profileNameLabel = "profile_name_label"
    static let profileEmailLabel = "profile_email_label"
    static let profileRoleLabel = "profile_role_label"
    static let changePasswordButton = "change_password_button"
    static let notificationsButton = "notifications_button"
    static let appearanceButton = "appearance_button"
    static let aboutButton = "about_button"
    static let signOutButton = "sign_out_button"
    static let signOutConfirmButton = "sign_out_confirm_button"
    static let deleteAccountButton = "delete_account_button"

    // Settings
    static let currentPasswordField = "current_password_field"
    static let newPasswordField = "new_password_field"
    static let confirmNewPasswordField = "confirm_new_password_field"

    // Common
    static let logoutButton = "logout_button"
    static let backButton = "back_button"
    static let cancelButton = "cancel_button"
    static let saveButton = "save_button"
    static let deleteButton = "delete_button"
    static let errorMessage = "error_message"
    static let loadingIndicator = "loading_indicator"
}

// MARK: - Accessibility Announcements

struct AccessibilityAnnouncement {
    /// Announce a message to VoiceOver users
    static func announce(_ message: String, priority: UIAccessibility.Notification = .announcement) {
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.1) {
            UIAccessibility.post(notification: priority, argument: message)
        }
    }

    /// Announce layout change
    static func layoutChanged(focusOn element: Any? = nil) {
        UIAccessibility.post(notification: .layoutChanged, argument: element)
    }

    /// Announce screen change
    static func screenChanged(focusOn element: Any? = nil) {
        UIAccessibility.post(notification: .screenChanged, argument: element)
    }
}

// MARK: - Dynamic Type Support

extension Font {
    /// Create scalable font that supports Dynamic Type
    /// - Parameters:
    ///   - style: Text style (title, body, etc.)
    ///   - weight: Font weight
    /// - Returns: Scalable font
    static func scalable(_ style: Font.TextStyle, weight: Font.Weight = .regular) -> Font {
        return .system(style, design: .default).weight(weight)
    }
}

// MARK: - Color Contrast Helpers

extension Color {
    /// Check if color has sufficient contrast with white background (WCAG AA)
    var hasContrastWithWhite: Bool {
        return contrastRatio(with: .white) >= 4.5
    }

    /// Check if color has sufficient contrast with black background (WCAG AA)
    var hasContrastWithBlack: Bool {
        return contrastRatio(with: .black) >= 4.5
    }

    /// Calculate contrast ratio with another color
    /// - Parameter color: Color to compare with
    /// - Returns: Contrast ratio (1.0 to 21.0)
    private func contrastRatio(with color: Color) -> Double {
        let luminance1 = self.luminance
        let luminance2 = color.luminance

        let lighter = max(luminance1, luminance2)
        let darker = min(luminance1, luminance2)

        return (lighter + 0.05) / (darker + 0.05)
    }

    /// Calculate relative luminance
    private var luminance: Double {
        // Convert to UIColor to get RGB components
        let uiColor = UIColor(self)
        var red: CGFloat = 0
        var green: CGFloat = 0
        var blue: CGFloat = 0
        var alpha: CGFloat = 0

        // getRed returns false if color can't be converted (e.g., in headless test environment)
        // In that case, try to resolve in a specific color space first
        let resolved = uiColor.resolvedColor(with: UITraitCollection(userInterfaceStyle: .light))
        guard resolved.getRed(&red, green: &green, blue: &blue, alpha: &alpha) else {
            // Fallback: assume mid-gray if we can't extract components
            // This ensures contrast checks don't crash in test environments
            return 0.5
        }

        // Convert to linear RGB
        func linearize(_ component: CGFloat) -> Double {
            let c = Double(component)
            return c <= 0.03928 ? c / 12.92 : pow((c + 0.055) / 1.055, 2.4)
        }

        let r = linearize(red)
        let g = linearize(green)
        let b = linearize(blue)

        // Calculate luminance
        return 0.2126 * r + 0.7152 * g + 0.0722 * b
    }
}
