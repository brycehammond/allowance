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
    dependencies: [],
    targets: [
        .target(
            name: "AllowanceTracker",
            dependencies: [],
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
