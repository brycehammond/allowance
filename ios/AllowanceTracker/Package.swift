// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "AllowanceTracker",
    platforms: [
        .iOS(.v17)
    ],
    products: [
        .library(
            name: "AllowanceTracker",
            targets: ["AllowanceTracker"]
        )
    ],
    dependencies: [
        .package(url: "https://github.com/google/GoogleSignIn-iOS", from: "8.0.0")
    ],
    targets: [
        .target(
            name: "AllowanceTracker",
            dependencies: [
                .product(name: "GoogleSignIn", package: "GoogleSignIn-iOS"),
                .product(name: "GoogleSignInSwift", package: "GoogleSignIn-iOS")
            ],
            path: ".",
            exclude: [
                "Tests",
                "Package.swift"
            ],
            sources: [
                "App",
                "Models",
                "ViewModels",
                "Views",
                "Services",
                "Utilities"
            ]
        ),
        .testTarget(
            name: "AllowanceTrackerTests",
            dependencies: ["AllowanceTracker"],
            path: "Tests"
        )
    ]
)
