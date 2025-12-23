//
//  SnapshotHelper.swift
//  Fastlane Snapshot Helper
//
//  Created by Felix Krause on 10/8/15.
//  Copyright © 2015 Felix Krause. All rights reserved.
//

// swiftlint:disable all
import Foundation
import XCTest

var deviceLanguage = ""
var locale = ""

func setupSnapshot(_ app: XCUIApplication, waitForAnimations: Bool = true) {
    Snapshot.setupSnapshot(app, waitForAnimations: waitForAnimations)
}

func snapshot(_ name: String, waitForLoadingIndicator: Bool) {
    if waitForLoadingIndicator {
        Snapshot.snapshot(name, timeWaitingForIdle: 20)
    } else {
        Snapshot.snapshot(name)
    }
}

func snapshot(_ name: String) {
    Snapshot.snapshot(name)
}

open class Snapshot: NSObject {
    static var app: XCUIApplication?
    static var waitForAnimations = true
    static var cacheDirectory: URL?
    static var screenshotsDirectory: URL? {
        return cacheDirectory?.appendingPathComponent("screenshots", isDirectory: true)
    }

    open class func setupSnapshot(_ app: XCUIApplication, waitForAnimations: Bool = true) {
        Snapshot.app = app
        Snapshot.waitForAnimations = waitForAnimations

        do {
            let cacheDir = try getCacheDirectory()
            Snapshot.cacheDirectory = cacheDir
            setLanguage(app)
            setLocale(app)
            setLaunchArguments(app)
        } catch {
            NSLog("Snapshot: Error setting up snapshot: \(error)")
        }
    }

    class func setLanguage(_ app: XCUIApplication) {
        let path = cacheDirectory?.appendingPathComponent("language.txt")

        do {
            if let path = path {
                let trimCharacterSet = CharacterSet.whitespacesAndNewlines
                deviceLanguage = try String(contentsOf: path, encoding: .utf8).trimmingCharacters(in: trimCharacterSet)
                app.launchArguments += ["-AppleLanguages", "(\(deviceLanguage))"]
            }
        } catch {
            NSLog("Snapshot: Couldn't detect/set language, using default")
        }
    }

    class func setLocale(_ app: XCUIApplication) {
        let path = cacheDirectory?.appendingPathComponent("locale.txt")

        do {
            if let path = path {
                let trimCharacterSet = CharacterSet.whitespacesAndNewlines
                locale = try String(contentsOf: path, encoding: .utf8).trimmingCharacters(in: trimCharacterSet)
                app.launchArguments += ["-AppleLocale", "\"\(locale)\""]
            }
        } catch {
            NSLog("Snapshot: Couldn't detect/set locale, using default")
        }
    }

    class func setLaunchArguments(_ app: XCUIApplication) {
        let path = cacheDirectory?.appendingPathComponent("snapshot-launch_arguments.txt")

        if let path = path, let launchArgumentsString = try? String(contentsOf: path, encoding: .utf8) {
            let trimCharacterSet = CharacterSet.whitespacesAndNewlines
            let launchArguments = launchArgumentsString.components(separatedBy: trimCharacterSet)
            app.launchArguments += launchArguments
        }
    }

    open class func snapshot(_ name: String, timeWaitingForIdle timeout: TimeInterval = 20) {
        if timeout > 0 {
            waitForLoadingIndicatorToDisappear(within: timeout)
        }

        NSLog("Snapshot: Taking snapshot '\(name)'")

        sleep(1)

        guard let app = self.app else {
            NSLog("Snapshot: XCUIApplication not set, using XCUIApplication()")
            let screenshot = XCUIApplication().screenshot()
            saveScreenshot(screenshot: screenshot, name: name)
            return
        }

        let screenshot = app.windows.firstMatch.screenshot()
        saveScreenshot(screenshot: screenshot, name: name)
    }

    class func waitForLoadingIndicatorToDisappear(within timeout: TimeInterval) {
        guard let app = self.app else { return }

        let networkLoadingIndicator = app.otherElements.deviceStatusBars.networkLoadingIndicators.element
        let loadingIndicator = app.activityIndicators.element

        let start = Date()

        while networkLoadingIndicator.exists || loadingIndicator.exists {
            if Date().timeIntervalSince(start) > timeout {
                NSLog("Snapshot: Timeout waiting for loading indicators")
                return
            }
            sleep(1)
        }
    }

    class func getCacheDirectory() throws -> URL {
        let cachePath = "Library/Caches/tools.fastlane"
        let homeDir = ProcessInfo.processInfo.environment["SIMULATOR_HOST_HOME"]!
        return URL(fileURLWithPath: homeDir).appendingPathComponent(cachePath)
    }

    class func saveScreenshot(screenshot: XCUIScreenshot, name: String) {
        do {
            guard let screenshotsDir = screenshotsDirectory else {
                NSLog("Snapshot: Screenshots directory not set")
                return
            }

            try FileManager.default.createDirectory(at: screenshotsDir, withIntermediateDirectories: true, attributes: nil)

            let fileUrl = screenshotsDir.appendingPathComponent("\(name).png")
            try screenshot.pngRepresentation.write(to: fileUrl)
            NSLog("Snapshot: Saved '\(name)' to \(fileUrl.path)")

        } catch {
            NSLog("Snapshot: Error saving screenshot: \(error)")
        }
    }
}

extension XCUIElementQuery {
    var networkLoadingIndicators: XCUIElementQuery {
        let isNetworkLoadingIndicator = NSPredicate { evaluatedObject, _ in
            guard let element = evaluatedObject as? XCUIElementAttributes else { return false }
            return element.identifier == "Network connection in progress"
        }
        return self.containing(isNetworkLoadingIndicator)
    }

    var deviceStatusBars: XCUIElementQuery {
        return self.matching(identifier: "StatusBar")
    }
}
// swiftlint:enable all
