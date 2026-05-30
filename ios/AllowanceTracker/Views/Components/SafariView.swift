//
//  SafariView.swift
//  AllowanceTracker
//
//  Presents a URL in an in-app Safari view (SFSafariViewController).
//

import SafariServices
import SwiftUI

struct SafariView: UIViewControllerRepresentable {
    let url: URL

    func makeUIViewController(context: Context) -> SFSafariViewController {
        SFSafariViewController(url: url)
    }

    func updateUIViewController(_ controller: SFSafariViewController, context: Context) {}
}

/// Wraps a `URL` so it can drive `.sheet(item:)` when presenting a `SafariView`.
struct IdentifiableURL: Identifiable {
    let url: URL
    var id: String { url.absoluteString }
}
