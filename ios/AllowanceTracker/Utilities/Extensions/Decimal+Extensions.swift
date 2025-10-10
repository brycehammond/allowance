import Foundation

extension Decimal {
    var currencyFormatted: String {
        let formatter = NumberFormatter()
        formatter.numberStyle = .currency
        formatter.currencyCode = "USD"
        formatter.locale = Locale(identifier: "en_US")
        return formatter.string(from: self as NSDecimalNumber) ?? "$0.00"
    }

    var doubleValue: Double {
        NSDecimalNumber(decimal: self).doubleValue
    }
}
