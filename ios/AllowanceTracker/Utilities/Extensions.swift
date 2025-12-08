import Foundation
import SwiftUI

// MARK: - Date Extensions
extension Date {
    var formattedDisplay: String {
        let formatter = DateFormatter()
        formatter.dateFormat = Constants.DateFormat.display
        return formatter.string(from: self)
    }

    var formattedWithTime: String {
        let formatter = DateFormatter()
        formatter.dateFormat = Constants.DateFormat.displayWithTime
        return formatter.string(from: self)
    }

    static func from(iso8601: String) -> Date? {
        let formatter = DateFormatter()
        formatter.dateFormat = Constants.DateFormat.iso8601
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.timeZone = TimeZone(secondsFromGMT: 0)
        return formatter.date(from: iso8601)
    }
}

// MARK: - Decimal Extensions
extension Decimal {
    var currencyFormatted: String {
        let formatter = NumberFormatter()
        formatter.numberStyle = .currency
        formatter.currencyCode = "USD"
        formatter.locale = Locale(identifier: "en_US")
        return formatter.string(from: self as NSDecimalNumber) ?? "$0.00"
    }

    var percentFormatted: String {
        let formatter = NumberFormatter()
        formatter.numberStyle = .percent
        formatter.minimumFractionDigits = 1
        formatter.maximumFractionDigits = 1
        return formatter.string(from: (self / 100) as NSDecimalNumber) ?? "0%"
    }

    var doubleValue: Double {
        NSDecimalNumber(decimal: self).doubleValue
    }
}

// MARK: - View Extensions
extension View {
    func cardStyle() -> some View {
        self
            .padding()
            .background(Color(.systemBackground))
            .cornerRadius(12)
            .shadow(color: Color.black.opacity(0.1), radius: 4, x: 0, y: 2)
    }

    func hideKeyboard() {
        UIApplication.shared.sendAction(#selector(UIResponder.resignFirstResponder), to: nil, from: nil, for: nil)
    }
}

// MARK: - JSONDecoder Extensions
extension JSONDecoder {
    static var `default`: JSONDecoder {
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        decoder.keyDecodingStrategy = .useDefaultKeys
        return decoder
    }
}

// MARK: - JSONEncoder Extensions
extension JSONEncoder {
    static var `default`: JSONEncoder {
        let encoder = JSONEncoder()
        encoder.dateEncodingStrategy = .iso8601
        encoder.keyEncodingStrategy = .useDefaultKeys
        return encoder
    }
}
