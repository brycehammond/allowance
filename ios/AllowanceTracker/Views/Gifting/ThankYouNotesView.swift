import SwiftUI

/// View displaying thank you notes for a child (child only)
@MainActor
struct ThankYouNotesView: View {

    // MARK: - Properties

    @State private var viewModel: ThankYouNotesViewModel
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass
    @State private var showingWriteNote = false

    // MARK: - Initialization

    init(childId: UUID, apiService: APIServiceProtocol = ServiceProvider.apiService) {
        _viewModel = State(wrappedValue: ThankYouNotesViewModel(childId: childId, apiService: apiService))
    }

    // MARK: - Computed Properties

    private var isRegularWidth: Bool {
        horizontalSizeClass == .regular
    }

    // MARK: - Body

    var body: some View {
        ZStack {
            if viewModel.isLoading && viewModel.pendingThankYous.isEmpty {
                ProgressView("Loading...")
            } else if viewModel.pendingThankYous.isEmpty {
                emptyStateView
            } else {
                thankYousListView
            }
        }
        .navigationTitle("Thank You Notes")
        .sheet(isPresented: $showingWriteNote) {
            if let pending = viewModel.selectedPendingThankYou {
                WriteThankYouNoteView(viewModel: viewModel, pendingThankYou: pending)
            }
        }
        .refreshable {
            await viewModel.refresh()
        }
        .alert("Error", isPresented: .constant(viewModel.errorMessage != nil)) {
            Button("OK") { viewModel.clearError() }
        } message: {
            if let error = viewModel.errorMessage {
                Text(error)
            }
        }
        .task {
            await viewModel.loadPendingThankYous()
        }
    }

    // MARK: - Subviews

    private var thankYousListView: some View {
        ScrollView {
            VStack(spacing: 16) {
                // Summary
                summaryCard

                // Overdue warning
                if viewModel.hasOverdueThankYous {
                    overdueWarning
                }

                // List
                ForEach(viewModel.pendingThankYous) { pending in
                    pendingCard(pending)
                }
            }
            .padding(.horizontal, isRegularWidth ? 24 : 16)
            .padding(.vertical)
            .frame(maxWidth: isRegularWidth ? 700 : .infinity)
            .frame(maxWidth: .infinity)
        }
    }

    private var summaryCard: some View {
        HStack {
            VStack(alignment: .leading, spacing: 4) {
                Text("Thank You Notes")
                    .font(.headline)
                HStack(spacing: 16) {
                    Label("\(viewModel.waitingCount) waiting", systemImage: "clock")
                        .foregroundStyle(.yellow)
                    Label("\(viewModel.draftCount) drafts", systemImage: "doc.text")
                        .foregroundStyle(Color.green600)
                }
                .font(.caption)
            }
            Spacer()
            Image(systemName: "heart.fill")
                .font(.largeTitle)
                .foregroundStyle(.pink.opacity(0.3))
        }
        .padding()
        .background(Color.pink.opacity(0.1))
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }

    private var overdueWarning: some View {
        HStack {
            Image(systemName: "exclamationmark.triangle.fill")
                .foregroundStyle(.yellow)
            Text("Some thank you notes are overdue! Try to send them within a week.")
                .font(.subheadline)
            Spacer()
        }
        .padding()
        .background(Color.yellow.opacity(0.1))
        .clipShape(RoundedRectangle(cornerRadius: 8))
    }

    private func pendingCard(_ pending: PendingThankYouDto) -> some View {
        Button {
            Task {
                await viewModel.selectGift(pending)
                showingWriteNote = true
            }
        } label: {
            VStack(alignment: .leading, spacing: 12) {
                HStack {
                    VStack(alignment: .leading, spacing: 4) {
                        Text(pending.giverName)
                            .font(.headline)
                            .foregroundStyle(.primary)
                        if let relationship = pending.giverRelationship {
                            Text(relationship)
                                .font(.caption)
                                .foregroundStyle(.secondary)
                        }
                    }

                    Spacer()

                    if pending.hasNote {
                        Label("Draft", systemImage: "doc.text")
                            .font(.caption)
                            .padding(.horizontal, 8)
                            .padding(.vertical, 4)
                            .background(Color.green600.opacity(0.2))
                            .foregroundStyle(Color.green600)
                            .clipShape(Capsule())
                    } else if pending.daysSinceReceived > 7 {
                        Label("Overdue", systemImage: "clock.badge.exclamationmark")
                            .font(.caption)
                            .padding(.horizontal, 8)
                            .padding(.vertical, 4)
                            .background(Color.yellow.opacity(0.2))
                            .foregroundStyle(.orange)
                            .clipShape(Capsule())
                    }
                }

                HStack {
                    Text(pending.formattedAmount)
                        .fontWeight(.semibold)
                        .foregroundStyle(.primary)

                    Text("for \(pending.occasionDisplay)")
                        .foregroundStyle(.secondary)

                    Spacer()

                    Text(pending.formattedDate)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
                .font(.subheadline)

                HStack {
                    Text(pending.hasNote ? "Tap to edit your note" : "Tap to write a thank you note")
                        .font(.caption)
                        .foregroundStyle(Color.green600)
                    Spacer()
                    Image(systemName: "chevron.right")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }
            .padding()
            .background(Color(.systemBackground))
            .clipShape(RoundedRectangle(cornerRadius: 12))
            .shadow(radius: 2)
            .overlay(
                RoundedRectangle(cornerRadius: 12)
                    .stroke(pending.hasNote ? Color.green600.opacity(0.3) : Color.clear, lineWidth: 2)
            )
        }
        .buttonStyle(.plain)
    }

    private var emptyStateView: some View {
        VStack(spacing: 24) {
            Image(systemName: "heart.circle.fill")
                .font(.system(size: 60))
                .foregroundStyle(.pink)

            VStack(spacing: 8) {
                Text("All Caught Up!")
                    .font(.title2)
                    .fontWeight(.bold)

                Text("You've thanked everyone who sent you a gift. Great job!")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)
            }
        }
        .frame(maxWidth: isRegularWidth ? 500 : .infinity)
        .padding()
    }
}

// MARK: - Write Thank You Note View

private struct WriteThankYouNoteView: View {
    let viewModel: ThankYouNotesViewModel
    let pendingThankYou: PendingThankYouDto
    @Environment(\.dismiss) private var dismiss

    @State private var message = ""
    @State private var imageUrl = ""

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    LabeledContent("To", value: pendingThankYou.giverName)
                    LabeledContent("Gift", value: pendingThankYou.formattedAmount)
                    LabeledContent("Occasion", value: pendingThankYou.occasionDisplay)
                }

                Section("Your Message") {
                    TextEditor(text: $message)
                        .frame(minHeight: 150)
                }

                Section {
                    TextField("Image URL (optional)", text: $imageUrl)
                        .textInputAutocapitalization(.never)
                        .keyboardType(.URL)
                } footer: {
                    Text("Add a photo URL to include with your thank you note.")
                }

                if let note = viewModel.currentNote, note.isSent {
                    Section {
                        Label("This note has been sent!", systemImage: "checkmark.circle.fill")
                            .foregroundStyle(.green)
                    }
                }
            }
            .navigationTitle("Thank You Note")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") {
                        viewModel.clearSelection()
                        dismiss()
                    }
                }
                ToolbarItemGroup(placement: .confirmationAction) {
                    if viewModel.currentNote?.isSent != true {
                        Button("Save") {
                            Task { await save() }
                        }
                        .disabled(message.isEmpty || viewModel.isProcessing)

                        if viewModel.currentNote != nil {
                            Button("Send") {
                                Task { await send() }
                            }
                            .disabled(viewModel.isProcessing)
                            .tint(Color.green600)
                        }
                    } else {
                        Button("Done") { dismiss() }
                    }
                }
            }
            .onAppear {
                if let note = viewModel.currentNote {
                    message = note.message
                    imageUrl = note.imageUrl ?? ""
                } else {
                    // Default placeholder
                    message = "Dear \(pendingThankYou.giverName),\n\nThank you so much for the wonderful gift!\n\n"
                }
            }
        }
    }

    private func save() async {
        let success = await viewModel.saveNote(
            forGiftId: pendingThankYou.giftId,
            message: message,
            imageUrl: imageUrl.isEmpty ? nil : imageUrl
        )
        if success && viewModel.successMessage?.contains("saved") == true {
            // Stay on screen for editing/sending
        }
    }

    private func send() async {
        // Save first if there are changes
        if viewModel.currentNote == nil || message != viewModel.currentNote?.message {
            let saved = await viewModel.saveNote(
                forGiftId: pendingThankYou.giftId,
                message: message,
                imageUrl: imageUrl.isEmpty ? nil : imageUrl
            )
            if !saved { return }
        }

        let success = await viewModel.sendNote(forGiftId: pendingThankYou.giftId)
        if success {
            dismiss()
        }
    }
}

// MARK: - Preview

#Preview {
    NavigationStack {
        ThankYouNotesView(childId: UUID())
    }
}
