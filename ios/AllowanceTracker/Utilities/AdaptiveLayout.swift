//
//  AdaptiveLayout.swift
//  AllowanceTracker
//
//  Adaptive layout utilities for iPhone and iPad optimization
//

import SwiftUI

// MARK: - Device Layout Environment

/// Provides information about the current device layout context
struct DeviceLayout {
    let horizontalSizeClass: UserInterfaceSizeClass?
    let verticalSizeClass: UserInterfaceSizeClass?

    /// True if running on iPad in regular width (landscape or large iPad)
    var isRegularWidth: Bool {
        horizontalSizeClass == .regular
    }

    /// True if running on iPad in regular height
    var isRegularHeight: Bool {
        verticalSizeClass == .regular
    }

    /// True if in a compact width environment (iPhone portrait, iPad split view)
    var isCompactWidth: Bool {
        horizontalSizeClass == .compact
    }

    /// True if likely iPad in landscape or large screen
    var isPadLandscape: Bool {
        isRegularWidth && isRegularHeight
    }

    /// True if likely iPad in portrait
    var isPadPortrait: Bool {
        isCompactWidth && isRegularHeight
    }

    /// True if likely iPhone
    var isPhone: Bool {
        horizontalSizeClass == .compact && verticalSizeClass == .regular
    }

    /// Number of columns for grid layouts
    var gridColumns: Int {
        if isPadLandscape { return 3 }
        if isPadPortrait || isRegularWidth { return 2 }
        return 1
    }

    /// Maximum content width for forms and centered content
    var formMaxWidth: CGFloat {
        isRegularWidth ? 600 : .infinity
    }

    /// Maximum content width for cards in lists
    var cardMaxWidth: CGFloat {
        isRegularWidth ? 400 : .infinity
    }

    /// Spacing multiplier for larger screens
    var spacingMultiplier: CGFloat {
        isRegularWidth ? 1.5 : 1.0
    }

    /// Padding for content areas
    var contentPadding: CGFloat {
        isRegularWidth ? 24 : 16
    }
}

// MARK: - Environment Key

private struct DeviceLayoutKey: EnvironmentKey {
    static let defaultValue = DeviceLayout(horizontalSizeClass: nil, verticalSizeClass: nil)
}

extension EnvironmentValues {
    var deviceLayout: DeviceLayout {
        get { self[DeviceLayoutKey.self] }
        set { self[DeviceLayoutKey.self] = newValue }
    }
}

// MARK: - View Modifier for Layout Detection

struct AdaptiveLayoutModifier: ViewModifier {
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass
    @Environment(\.verticalSizeClass) private var verticalSizeClass

    func body(content: Content) -> some View {
        content
            .environment(\.deviceLayout, DeviceLayout(
                horizontalSizeClass: horizontalSizeClass,
                verticalSizeClass: verticalSizeClass
            ))
    }
}

extension View {
    /// Enables adaptive layout detection for this view hierarchy
    func adaptiveLayout() -> some View {
        modifier(AdaptiveLayoutModifier())
    }
}

// MARK: - Adaptive Grid

/// A grid that automatically adjusts columns based on screen size
struct AdaptiveGrid<Content: View>: View {
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass
    @Environment(\.verticalSizeClass) private var verticalSizeClass

    let minItemWidth: CGFloat
    let spacing: CGFloat
    let content: Content

    init(
        minItemWidth: CGFloat = 300,
        spacing: CGFloat = 16,
        @ViewBuilder content: () -> Content
    ) {
        self.minItemWidth = minItemWidth
        self.spacing = spacing
        self.content = content()
    }

    private var columns: [GridItem] {
        let isRegular = horizontalSizeClass == .regular
        if isRegular {
            // iPad: Use flexible columns that adapt to available space
            return [
                GridItem(.adaptive(minimum: minItemWidth, maximum: 500), spacing: spacing)
            ]
        } else {
            // iPhone: Single column
            return [GridItem(.flexible(), spacing: spacing)]
        }
    }

    var body: some View {
        LazyVGrid(columns: columns, spacing: spacing) {
            content
        }
    }
}

// MARK: - Adaptive Form Container

/// Container that centers and constrains form content on larger screens
struct AdaptiveFormContainer<Content: View>: View {
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass

    let maxWidth: CGFloat
    let content: Content

    init(maxWidth: CGFloat = 600, @ViewBuilder content: () -> Content) {
        self.maxWidth = maxWidth
        self.content = content()
    }

    var body: some View {
        GeometryReader { geometry in
            ScrollView {
                content
                    .frame(maxWidth: horizontalSizeClass == .regular ? maxWidth : .infinity)
                    .frame(maxWidth: .infinity)
            }
        }
    }
}

// MARK: - Adaptive Presentation Modifier

/// Presents content as sheet on iPhone, popover on iPad
struct AdaptivePresentationModifier<PresentedContent: View>: ViewModifier {
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass
    @Binding var isPresented: Bool
    let arrowEdge: Edge
    let presentedContent: () -> PresentedContent

    func body(content: Content) -> some View {
        if horizontalSizeClass == .regular {
            content
                .popover(isPresented: $isPresented, arrowEdge: arrowEdge) {
                    presentedContent()
                        .frame(minWidth: 350, idealWidth: 400, maxWidth: 500)
                        .frame(minHeight: 300, idealHeight: 400, maxHeight: 600)
                }
        } else {
            content
                .sheet(isPresented: $isPresented) {
                    presentedContent()
                }
        }
    }
}

extension View {
    /// Presents content as sheet on iPhone, popover on iPad
    func adaptivePresentation<Content: View>(
        isPresented: Binding<Bool>,
        arrowEdge: Edge = .bottom,
        @ViewBuilder content: @escaping () -> Content
    ) -> some View {
        modifier(AdaptivePresentationModifier(
            isPresented: isPresented,
            arrowEdge: arrowEdge,
            presentedContent: content
        ))
    }
}

// MARK: - Adaptive Navigation

/// Navigation container that uses split view on iPad, stack on iPhone
struct AdaptiveNavigation<Sidebar: View, Detail: View>: View {
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass

    let sidebar: Sidebar
    let detail: Detail

    init(
        @ViewBuilder sidebar: () -> Sidebar,
        @ViewBuilder detail: () -> Detail
    ) {
        self.sidebar = sidebar()
        self.detail = detail()
    }

    var body: some View {
        if horizontalSizeClass == .regular {
            NavigationSplitView {
                sidebar
            } detail: {
                detail
            }
        } else {
            NavigationStack {
                sidebar
            }
        }
    }
}

// MARK: - Responsive Value Helper

/// Returns different values based on size class
struct ResponsiveValue<T> {
    let compact: T
    let regular: T

    func value(for sizeClass: UserInterfaceSizeClass?) -> T {
        sizeClass == .regular ? regular : compact
    }
}

extension View {
    /// Applies different padding based on size class
    func adaptivePadding(_ edges: Edge.Set = .all) -> some View {
        modifier(AdaptivePaddingModifier(edges: edges))
    }
}

struct AdaptivePaddingModifier: ViewModifier {
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass
    let edges: Edge.Set

    func body(content: Content) -> some View {
        let padding: CGFloat = horizontalSizeClass == .regular ? 24 : 16
        return content.padding(edges, padding)
    }
}

// MARK: - Preview

#if DEBUG
struct AdaptiveLayout_Previews: PreviewProvider {
    static var previews: some View {
        Group {
            // iPhone preview
            AdaptiveGrid {
                ForEach(0..<6) { i in
                    RoundedRectangle(cornerRadius: 12)
                        .fill(Color.green600)
                        .frame(height: 100)
                        .overlay(Text("Card \(i + 1)").foregroundColor(.white))
                }
            }
            .padding()
            .previewDevice("iPhone 15")
            .previewDisplayName("iPhone - Single Column")

            // iPad preview
            AdaptiveGrid {
                ForEach(0..<6) { i in
                    RoundedRectangle(cornerRadius: 12)
                        .fill(Color.green600)
                        .frame(height: 100)
                        .overlay(Text("Card \(i + 1)").foregroundColor(.white))
                }
            }
            .padding()
            .previewDevice("iPad Pro (12.9-inch) (6th generation)")
            .previewDisplayName("iPad - Multi Column")
        }
    }
}
#endif
