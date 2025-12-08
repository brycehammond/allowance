//
//  DesignSystem.swift
//  AllowanceTracker
//
//  Design System - Muted Green Theme
//  Created: 2025
//

import SwiftUI

// MARK: - DesignSystem Namespace

/// Design system namespace for accessing design tokens
enum DesignSystem {
    /// Color palette
    enum Colors {
        static let primary = Color(hex: "2da370")
        static let primaryHover = Color(hex: "248c5f")
        static let primaryActive = Color(hex: "1c6e4a")

        static let secondary = Color(hex: "f59e0b")
        static let secondaryHover = Color(hex: "d97706")

        static let success = Color(hex: "2da370")
        static let warning = Color(hex: "f59e0b")
        static let error = Color(hex: "dc2626")
        static let info = Color(hex: "3b82f6")
    }
}

// MARK: - Color Extensions

extension Color {
    // MARK: - Color Initialization from Hex
    init(hex: String) {
        let scanner = Scanner(string: hex)
        var rgbValue: UInt64 = 0
        scanner.scanHexInt64(&rgbValue)

        let r = Double((rgbValue & 0xff0000) >> 16) / 255.0
        let g = Double((rgbValue & 0x00ff00) >> 8) / 255.0
        let b = Double(rgbValue & 0x0000ff) / 255.0

        self.init(red: r, green: g, blue: b)
    }

    // MARK: - Primary Green Palette
    static let green50  = Color(hex: "f0f9f4")
    static let green100 = Color(hex: "d1f0df")
    static let green200 = Color(hex: "a3e1c0")
    static let green300 = Color(hex: "72d0a0")
    static let green400 = Color(hex: "4bb885")
    static let green500 = Color(hex: "2da370")  // PRIMARY
    static let green600 = Color(hex: "248c5f")
    static let green700 = Color(hex: "1c6e4a")
    static let green800 = Color(hex: "145537")
    static let green900 = Color(hex: "0d3d27")

    // MARK: - Secondary Amber Palette
    static let amber50  = Color(hex: "fffbf0")
    static let amber100 = Color(hex: "fef3c7")
    static let amber200 = Color(hex: "fde68a")
    static let amber300 = Color(hex: "fcd34d")
    static let amber400 = Color(hex: "fbbf24")
    static let amber500 = Color(hex: "f59e0b")  // SECONDARY
    static let amber600 = Color(hex: "d97706")
    static let amber700 = Color(hex: "b45309")
    static let amber800 = Color(hex: "92400e")
    static let amber900 = Color(hex: "78350f")

    // MARK: - Neutral Grays
    static let gray50  = Color(hex: "f9fafb")
    static let gray100 = Color(hex: "f3f4f6")
    static let gray200 = Color(hex: "e5e7eb")
    static let gray300 = Color(hex: "d1d5db")
    static let gray400 = Color(hex: "9ca3af")
    static let gray500 = Color(hex: "6b7280")
    static let gray600 = Color(hex: "4b5563")
    static let gray700 = Color(hex: "374151")
    static let gray800 = Color(hex: "1f2937")
    static let gray900 = Color(hex: "111827")

    // MARK: - Semantic Colors
    static let success = green500
    static let successLight = green100
    static let successDark = green700

    static let warning = amber500
    static let warningLight = amber100
    static let warningDark = amber700

    static let error = Color(hex: "dc2626")
    static let errorLight = Color(hex: "fee2e2")
    static let errorDark = Color(hex: "991b1b")

    static let info = Color(hex: "3b82f6")
    static let infoLight = Color(hex: "dbeafe")
    static let infoDark = Color(hex: "1e40af")

    // MARK: - Semantic Aliases
    static let primary = green500
    static let primaryHover = green600
    static let primaryActive = green700

    static let secondary = amber500
    static let secondaryHover = amber600

    // MARK: - Chart Colors
    static let chart1 = green500
    static let chart2 = amber500
    static let chart3 = Color(hex: "3b82f6")
    static let chart4 = Color(hex: "8b5cf6")
    static let chart5 = Color(hex: "ec4899")
    static let chart6 = Color(hex: "14b8a6")
    static let chart7 = Color(hex: "f97316")
    static let chart8 = Color(hex: "06b6d4")

    static var chartColors: [Color] {
        [chart1, chart2, chart3, chart4, chart5, chart6, chart7, chart8]
    }
}

// MARK: - Font Extensions

extension Font {
    // MARK: - Display Fonts
    static let displayLarge = Font.system(size: 57, weight: .bold)
    static let displayMedium = Font.system(size: 45, weight: .bold)
    static let displaySmall = Font.system(size: 36, weight: .bold)

    // MARK: - Headlines
    static let headlineLarge = Font.system(size: 32, weight: .semibold)
    static let headlineMedium = Font.system(size: 28, weight: .semibold)
    static let headlineSmall = Font.system(size: 24, weight: .semibold)

    // MARK: - Titles
    static let titleLarge = Font.system(size: 22, weight: .medium)
    static let titleMedium = Font.system(size: 18, weight: .medium)
    static let titleSmall = Font.system(size: 16, weight: .medium)

    // MARK: - Body
    static let bodyLarge = Font.system(size: 16, weight: .regular)
    static let bodyMedium = Font.system(size: 14, weight: .regular)
    static let bodySmall = Font.system(size: 12, weight: .regular)

    // MARK: - Labels
    static let labelLarge = Font.system(size: 14, weight: .medium)
    static let labelMedium = Font.system(size: 12, weight: .medium)
    static let labelSmall = Font.system(size: 10, weight: .medium)

    // MARK: - Monospace (for monetary values)
    static let monoLarge = Font.system(size: 32, weight: .semibold, design: .monospaced)
    static let monoMedium = Font.system(size: 24, weight: .semibold, design: .monospaced)
    static let monoSmall = Font.system(size: 16, weight: .medium, design: .monospaced)
}

// MARK: - Spacing Constants

extension CGFloat {
    static let spacing0: CGFloat = 0
    static let spacing1: CGFloat = 4     // BASE smallest
    static let spacing2: CGFloat = 8     // BASE unit
    static let spacing3: CGFloat = 12
    static let spacing4: CGFloat = 16    // Default spacing
    static let spacing5: CGFloat = 20
    static let spacing6: CGFloat = 24
    static let spacing8: CGFloat = 32
    static let spacing10: CGFloat = 40
    static let spacing12: CGFloat = 48
    static let spacing16: CGFloat = 64
    static let spacing20: CGFloat = 80
    static let spacing24: CGFloat = 96
}

// MARK: - Button Styles

struct PrimaryButtonStyle: ButtonStyle {
    func makeBody(configuration: ButtonStyleConfiguration) -> some View {
        configuration.label
            .font(.system(size: 14, weight: .medium))
            .foregroundColor(.white)
            .padding(.horizontal, 24)
            .padding(.vertical, 12)
            .background(configuration.isPressed ? Color.green700 : Color.green500)
            .cornerRadius(8)
            .scaleEffect(configuration.isPressed ? 0.98 : 1.0)
    }
}

struct SecondaryButtonStyle: ButtonStyle {
    func makeBody(configuration: ButtonStyleConfiguration) -> some View {
        configuration.label
            .font(.system(size: 14, weight: .medium))
            .foregroundColor(.white)
            .padding(.horizontal, 24)
            .padding(.vertical, 12)
            .background(configuration.isPressed ? Color.amber700 : Color.amber500)
            .cornerRadius(8)
            .scaleEffect(configuration.isPressed ? 0.98 : 1.0)
    }
}

struct OutlineButtonStyle: ButtonStyle {
    func makeBody(configuration: ButtonStyleConfiguration) -> some View {
        configuration.label
            .font(.system(size: 14, weight: .medium))
            .foregroundColor(Color.green600)
            .padding(.horizontal, 24)
            .padding(.vertical, 12)
            .background(configuration.isPressed ? Color.green50 : Color.clear)
            .overlay(
                RoundedRectangle(cornerRadius: 8)
                    .stroke(Color.green500, lineWidth: 2)
            )
    }
}

struct DangerButtonStyle: ButtonStyle {
    func makeBody(configuration: ButtonStyleConfiguration) -> some View {
        configuration.label
            .font(.system(size: 14, weight: .medium))
            .foregroundColor(.white)
            .padding(.horizontal, 24)
            .padding(.vertical, 12)
            .background(configuration.isPressed ? Color.errorDark : Color.error)
            .cornerRadius(8)
            .scaleEffect(configuration.isPressed ? 0.98 : 1.0)
    }
}

struct GhostButtonStyle: ButtonStyle {
    func makeBody(configuration: ButtonStyleConfiguration) -> some View {
        configuration.label
            .font(.system(size: 14, weight: .medium))
            .foregroundColor(Color.gray700)
            .padding(.horizontal, 24)
            .padding(.vertical, 12)
            .background(configuration.isPressed ? Color.gray100 : Color.clear)
            .cornerRadius(8)
    }
}

// MARK: - Card Styles

struct CardModifier: ViewModifier {
    var style: CardStyle = .base

    func body(content: Content) -> some View {
        content
            .padding(.spacing6)
            .background(Color.white)
            .cornerRadius(12)
            .shadow(color: style.shadowColor, radius: style.shadowRadius, y: style.shadowY)
    }
}

enum CardStyle {
    case base
    case elevated
    case interactive

    var shadowColor: Color {
        switch self {
        case .base: return .black.opacity(0.1)
        case .elevated: return .black.opacity(0.15)
        case .interactive: return .black.opacity(0.12)
        }
    }

    var shadowRadius: CGFloat {
        switch self {
        case .base: return 2
        case .elevated: return 8
        case .interactive: return 4
        }
    }

    var shadowY: CGFloat {
        switch self {
        case .base: return 1
        case .elevated: return 4
        case .interactive: return 2
        }
    }
}

extension View {
    func card(style: CardStyle = .base) -> some View {
        modifier(CardModifier(style: style))
    }
}

// MARK: - Helper Extensions (Double only - Decimal extension is in Extensions.swift)

extension Double {
    var currencyFormatted: String {
        let formatter = NumberFormatter()
        formatter.numberStyle = .currency
        formatter.currencyCode = "USD"
        return formatter.string(from: NSNumber(value: self)) ?? "$0.00"
    }
}

// MARK: - Example Usage

#if DEBUG
struct DesignSystemPreview: View {
    var body: some View {
        ScrollView {
            VStack(spacing: .spacing6) {
                // Colors
                VStack(alignment: .leading, spacing: .spacing4) {
                    Text("Colors")
                        .font(.headlineMedium)

                    HStack(spacing: .spacing3) {
                        ColorSwatch(color: .green500, name: "Primary")
                        ColorSwatch(color: .amber500, name: "Secondary")
                        ColorSwatch(color: .gray500, name: "Neutral")
                    }
                }
                .card()

                // Typography
                VStack(alignment: .leading, spacing: .spacing4) {
                    Text("Typography")
                        .font(.headlineMedium)

                    Text("Display Large")
                        .font(.displayLarge)
                    Text("Headline Medium")
                        .font(.headlineMedium)
                    Text("Body Medium")
                        .font(.bodyMedium)
                    Text("$1,234.56")
                        .font(.monoMedium)
                        .foregroundColor(.green600)
                }
                .card()

                // Buttons
                VStack(alignment: .leading, spacing: .spacing4) {
                    Text("Buttons")
                        .font(.headlineMedium)

                    Button("Primary Button") {}
                        .buttonStyle(PrimaryButtonStyle())

                    Button("Secondary Button") {}
                        .buttonStyle(SecondaryButtonStyle())

                    Button("Outline Button") {}
                        .buttonStyle(OutlineButtonStyle())

                    Button("Danger Button") {}
                        .buttonStyle(DangerButtonStyle())

                    Button("Ghost Button") {}
                        .buttonStyle(GhostButtonStyle())
                }
                .card()
            }
            .padding()
        }
        .background(Color.gray50)
    }
}

struct ColorSwatch: View {
    let color: Color
    let name: String

    var body: some View {
        VStack {
            RoundedRectangle(cornerRadius: 8)
                .fill(color)
                .frame(width: 60, height: 60)

            Text(name)
                .font(.labelSmall)
                .foregroundColor(.gray600)
        }
    }
}

struct DesignSystemPreview_Previews: PreviewProvider {
    static var previews: some View {
        DesignSystemPreview()
    }
}
#endif
