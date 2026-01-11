import SwiftUI

/// A card view displaying an earned badge
struct BadgeCardView: View {
    let badge: ChildBadgeDto
    let isParent: Bool
    let onToggleDisplay: ((Bool) -> Void)?

    @State private var showingDetail = false

    var body: some View {
        Button {
            showingDetail = true
        } label: {
            VStack(spacing: 8) {
                // Badge icon with rarity glow
                ZStack {
                    // Rarity background glow
                    Circle()
                        .fill(rarityGradient)
                        .frame(width: 70, height: 70)
                        .blur(radius: 10)
                        .opacity(0.5)

                    // Badge icon
                    AsyncImage(url: URL(string: badge.iconUrl)) { phase in
                        switch phase {
                        case .empty:
                            ProgressView()
                                .frame(width: 50, height: 50)
                        case .success(let image):
                            image
                                .resizable()
                                .aspectRatio(contentMode: .fit)
                                .frame(width: 50, height: 50)
                        case .failure:
                            Image(systemName: badge.category.systemImage)
                                .font(.system(size: 30))
                                .foregroundStyle(rarityColor)
                        @unknown default:
                            Image(systemName: badge.category.systemImage)
                                .font(.system(size: 30))
                                .foregroundStyle(rarityColor)
                        }
                    }
                    .frame(width: 50, height: 50)

                    // New badge indicator
                    if badge.isNew {
                        Circle()
                            .fill(Color.red)
                            .frame(width: 12, height: 12)
                            .offset(x: 22, y: -22)
                    }
                }
                .frame(width: 70, height: 70)

                // Badge name
                Text(badge.badgeName)
                    .font(.caption)
                    .fontWeight(.semibold)
                    .lineLimit(2)
                    .multilineTextAlignment(.center)
                    .foregroundStyle(.primary)

                // Rarity tag
                Text(badge.rarityName)
                    .font(.caption2)
                    .fontWeight(.medium)
                    .foregroundStyle(rarityColor)
                    .padding(.horizontal, 6)
                    .padding(.vertical, 2)
                    .background(rarityColor.opacity(0.15))
                    .clipShape(Capsule())

                // Points value
                HStack(spacing: 2) {
                    Image(systemName: "star.fill")
                        .font(.system(size: 8))
                    Text("+\(badge.pointsValue)")
                        .font(.caption2)
                        .fontWeight(.medium)
                }
                .foregroundStyle(Color.amber500)
            }
            .frame(width: 100, height: 160)
            .padding(.vertical, 8)
            .background(Color(.systemBackground))
            .clipShape(RoundedRectangle(cornerRadius: 12))
            .shadow(color: rarityColor.opacity(0.3), radius: badge.rarity == .Legendary ? 8 : 2)
            .overlay(
                RoundedRectangle(cornerRadius: 12)
                    .stroke(badge.isDisplayed ? Color.green500 : Color.clear, lineWidth: 2)
            )
        }
        .buttonStyle(.plain)
        .sheet(isPresented: $showingDetail) {
            BadgeDetailSheet(
                badge: badge,
                isParent: isParent,
                onToggleDisplay: onToggleDisplay
            )
        }
    }

    // MARK: - Rarity Styling

    private var rarityColor: Color {
        switch badge.rarity {
        case .Common:
            return .gray
        case .Uncommon:
            return .green
        case .Rare:
            return .blue
        case .Epic:
            return .purple
        case .Legendary:
            return .orange
        }
    }

    private var rarityGradient: LinearGradient {
        switch badge.rarity {
        case .Common:
            return LinearGradient(colors: [.gray.opacity(0.3), .gray.opacity(0.1)], startPoint: .top, endPoint: .bottom)
        case .Uncommon:
            return LinearGradient(colors: [.green.opacity(0.3), .green.opacity(0.1)], startPoint: .top, endPoint: .bottom)
        case .Rare:
            return LinearGradient(colors: [.blue.opacity(0.3), .blue.opacity(0.1)], startPoint: .top, endPoint: .bottom)
        case .Epic:
            return LinearGradient(colors: [.purple.opacity(0.3), .purple.opacity(0.1)], startPoint: .top, endPoint: .bottom)
        case .Legendary:
            return LinearGradient(colors: [.orange, .yellow.opacity(0.5)], startPoint: .topLeading, endPoint: .bottomTrailing)
        }
    }
}

// MARK: - Badge Detail Sheet

struct BadgeDetailSheet: View {
    let badge: ChildBadgeDto
    let isParent: Bool
    let onToggleDisplay: ((Bool) -> Void)?

    @Environment(\.dismiss) private var dismiss

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 24) {
                    // Badge icon (larger)
                    AsyncImage(url: URL(string: badge.iconUrl)) { phase in
                        switch phase {
                        case .empty:
                            ProgressView()
                                .frame(width: 100, height: 100)
                        case .success(let image):
                            image
                                .resizable()
                                .aspectRatio(contentMode: .fit)
                                .frame(width: 100, height: 100)
                        case .failure:
                            Image(systemName: badge.category.systemImage)
                                .font(.system(size: 60))
                                .foregroundStyle(rarityColor)
                        @unknown default:
                            Image(systemName: badge.category.systemImage)
                                .font(.system(size: 60))
                                .foregroundStyle(rarityColor)
                        }
                    }
                    .frame(width: 100, height: 100)
                    .padding()
                    .background(
                        Circle()
                            .fill(rarityColor.opacity(0.1))
                    )

                    // Badge name and rarity
                    VStack(spacing: 8) {
                        Text(badge.badgeName)
                            .font(.title2)
                            .fontWeight(.bold)

                        HStack(spacing: 8) {
                            Text(badge.rarityName)
                                .font(.subheadline)
                                .fontWeight(.semibold)
                                .foregroundStyle(rarityColor)
                                .padding(.horizontal, 12)
                                .padding(.vertical, 4)
                                .background(rarityColor.opacity(0.15))
                                .clipShape(Capsule())

                            Text(badge.categoryName)
                                .font(.subheadline)
                                .foregroundStyle(.secondary)
                                .padding(.horizontal, 12)
                                .padding(.vertical, 4)
                                .background(Color(.systemGray5))
                                .clipShape(Capsule())
                        }
                    }

                    // Description
                    Text(badge.badgeDescription)
                        .font(.body)
                        .foregroundStyle(.secondary)
                        .multilineTextAlignment(.center)
                        .padding(.horizontal)

                    Divider()

                    // Stats
                    HStack(spacing: 32) {
                        VStack(spacing: 4) {
                            HStack(spacing: 4) {
                                Image(systemName: "star.fill")
                                    .foregroundStyle(Color.amber500)
                                Text("+\(badge.pointsValue)")
                                    .fontWeight(.bold)
                            }
                            Text("Points Earned")
                                .font(.caption)
                                .foregroundStyle(.secondary)
                        }

                        VStack(spacing: 4) {
                            Text(badge.formattedEarnedDate)
                                .fontWeight(.medium)
                            Text("Earned On")
                                .font(.caption)
                                .foregroundStyle(.secondary)
                        }
                    }

                    // Context (if available)
                    if let context = badge.earnedContext, !context.isEmpty {
                        VStack(spacing: 8) {
                            Text("How You Earned It")
                                .font(.headline)

                            Text(context)
                                .font(.body)
                                .foregroundStyle(.secondary)
                                .multilineTextAlignment(.center)
                        }
                        .padding()
                        .frame(maxWidth: .infinity)
                        .background(Color(.systemGray6))
                        .clipShape(RoundedRectangle(cornerRadius: 12))
                        .padding(.horizontal)
                    }

                    // Display toggle (parent only)
                    if isParent, let onToggle = onToggleDisplay {
                        VStack(spacing: 8) {
                            Toggle(isOn: Binding(
                                get: { badge.isDisplayed },
                                set: { onToggle($0) }
                            )) {
                                VStack(alignment: .leading, spacing: 4) {
                                    Text("Display on Profile")
                                        .font(.headline)
                                    Text("Show this badge on your profile")
                                        .font(.caption)
                                        .foregroundStyle(.secondary)
                                }
                            }
                        }
                        .padding()
                        .background(Color(.systemGray6))
                        .clipShape(RoundedRectangle(cornerRadius: 12))
                        .padding(.horizontal)
                    }

                    Spacer()
                }
                .padding(.vertical)
            }
            .navigationTitle("Badge Details")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .navigationBarTrailing) {
                    Button("Done") {
                        dismiss()
                    }
                }
            }
        }
    }

    private var rarityColor: Color {
        switch badge.rarity {
        case .Common:
            return .gray
        case .Uncommon:
            return .green
        case .Rare:
            return .blue
        case .Epic:
            return .purple
        case .Legendary:
            return .orange
        }
    }
}

// MARK: - Badge Progress Card

struct BadgeProgressCard: View {
    let progress: BadgeProgressDto

    var body: some View {
        HStack(spacing: 12) {
            // Badge icon
            AsyncImage(url: URL(string: progress.iconUrl)) { phase in
                switch phase {
                case .empty:
                    ProgressView()
                        .frame(width: 40, height: 40)
                case .success(let image):
                    image
                        .resizable()
                        .aspectRatio(contentMode: .fit)
                        .frame(width: 40, height: 40)
                case .failure:
                    Image(systemName: progress.category.systemImage)
                        .font(.system(size: 24))
                        .foregroundStyle(rarityColor.opacity(0.5))
                @unknown default:
                    Image(systemName: progress.category.systemImage)
                        .font(.system(size: 24))
                        .foregroundStyle(rarityColor.opacity(0.5))
                }
            }
            .frame(width: 40, height: 40)
            .padding(8)
            .background(rarityColor.opacity(0.1))
            .clipShape(Circle())
            .opacity(0.7)

            // Badge info and progress
            VStack(alignment: .leading, spacing: 6) {
                HStack {
                    Text(progress.badgeName)
                        .font(.subheadline)
                        .fontWeight(.semibold)

                    Spacer()

                    Text(progress.progressText)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                Text(progress.description)
                    .font(.caption)
                    .foregroundStyle(.secondary)
                    .lineLimit(1)

                // Progress bar
                ProgressView(value: progress.progressFraction)
                    .tint(rarityColor)

                HStack {
                    Text(progress.rarityName)
                        .font(.caption2)
                        .foregroundStyle(rarityColor)

                    Spacer()

                    HStack(spacing: 2) {
                        Image(systemName: "star.fill")
                            .font(.system(size: 8))
                        Text("+\(progress.pointsValue)")
                            .font(.caption2)
                    }
                    .foregroundStyle(Color.amber500)
                }
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .shadow(radius: 2)
    }

    private var rarityColor: Color {
        switch progress.rarity {
        case .Common:
            return .gray
        case .Uncommon:
            return .green
        case .Rare:
            return .blue
        case .Epic:
            return .purple
        case .Legendary:
            return .orange
        }
    }
}

// MARK: - Preview Provider

#Preview("Badge Card - Common") {
    BadgeCardView(
        badge: ChildBadgeDto(
            id: UUID(),
            badgeId: UUID(),
            badgeName: "First Saver",
            badgeDescription: "Made your first savings deposit",
            iconUrl: "",
            category: .Saving,
            categoryName: "Saving",
            rarity: .Common,
            rarityName: "Common",
            pointsValue: 10,
            earnedAt: Date(),
            isDisplayed: false,
            isNew: true,
            earnedContext: nil
        ),
        isParent: true,
        onToggleDisplay: { _ in }
    )
    .padding()
}

#Preview("Badge Card - Legendary") {
    BadgeCardView(
        badge: ChildBadgeDto(
            id: UUID(),
            badgeId: UUID(),
            badgeName: "Money Master",
            badgeDescription: "Reached 1000 in savings",
            iconUrl: "",
            category: .Milestones,
            categoryName: "Milestones",
            rarity: .Legendary,
            rarityName: "Legendary",
            pointsValue: 100,
            earnedAt: Date(),
            isDisplayed: true,
            isNew: false,
            earnedContext: "Saved consistently for 6 months!"
        ),
        isParent: true,
        onToggleDisplay: { _ in }
    )
    .padding()
}

#Preview("Badge Progress Card") {
    BadgeProgressCard(
        progress: BadgeProgressDto(
            badgeId: UUID(),
            badgeName: "Super Saver",
            description: "Save 10 times in a month",
            iconUrl: "",
            category: .Saving,
            categoryName: "Saving",
            rarity: .Rare,
            rarityName: "Rare",
            pointsValue: 50,
            currentProgress: 7,
            targetProgress: 10,
            progressPercentage: 70,
            progressText: "7 of 10"
        )
    )
    .padding()
}
