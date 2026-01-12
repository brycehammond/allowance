import SwiftUI
import PhotosUI

/// View for completing a task with optional photo proof
@MainActor
struct CompleteTaskView: View {

    // MARK: - Properties

    @Bindable var viewModel: TaskViewModel
    let task: ChoreTask
    @Environment(\.dismiss) private var dismiss

    // MARK: - Body

    var body: some View {
        NavigationStack {
            VStack(spacing: 24) {
                // Task info
                VStack(spacing: 8) {
                    Image(systemName: "checkmark.circle")
                        .font(.system(size: 48))
                        .foregroundStyle(Color.green500)

                    Text("Complete Task")
                        .font(.title2)
                        .fontWeight(.bold)

                    Text(task.title)
                        .font(.headline)
                        .foregroundStyle(.secondary)

                    Text(task.formattedReward)
                        .font(.title)
                        .fontWeight(.bold)
                        .foregroundStyle(Color.green500)
                }
                .padding(.top)

                // Notes
                VStack(alignment: .leading, spacing: 8) {
                    Text("Notes (optional)")
                        .font(.subheadline)
                        .fontWeight(.medium)

                    TextEditor(text: $viewModel.completionNotes)
                        .frame(minHeight: 80)
                        .padding(8)
                        .background(Color(.systemGray6))
                        .clipShape(RoundedRectangle(cornerRadius: 8))
                }
                .padding(.horizontal)

                // Photo picker
                VStack(alignment: .leading, spacing: 8) {
                    Text("Photo Proof (optional)")
                        .font(.subheadline)
                        .fontWeight(.medium)

                    PhotosPicker(selection: $viewModel.selectedPhoto, matching: .images) {
                        if let photoData = viewModel.selectedPhotoData,
                           let uiImage = UIImage(data: photoData) {
                            Image(uiImage: uiImage)
                                .resizable()
                                .scaledToFill()
                                .frame(maxWidth: .infinity)
                                .frame(height: 200)
                                .clipShape(RoundedRectangle(cornerRadius: 12))
                        } else {
                            VStack(spacing: 12) {
                                Image(systemName: "camera.fill")
                                    .font(.system(size: 32))
                                Text("Tap to add photo")
                                    .font(.subheadline)
                            }
                            .frame(maxWidth: .infinity)
                            .frame(height: 150)
                            .background(Color(.systemGray6))
                            .clipShape(RoundedRectangle(cornerRadius: 12))
                        }
                    }
                    .onChange(of: viewModel.selectedPhoto) {
                        Task {
                            await viewModel.loadPhotoData()
                        }
                    }
                }
                .padding(.horizontal)

                Spacer()

                // Submit button
                Button {
                    Task {
                        await completeTask()
                    }
                } label: {
                    if viewModel.isProcessing {
                        ProgressView()
                            .tint(.white)
                            .frame(maxWidth: .infinity)
                            .padding()
                    } else {
                        Text("Submit Completion")
                            .fontWeight(.semibold)
                            .frame(maxWidth: .infinity)
                            .padding()
                    }
                }
                .background(Color.green500)
                .foregroundStyle(.white)
                .clipShape(RoundedRectangle(cornerRadius: 12))
                .disabled(viewModel.isProcessing)
                .padding(.horizontal)
                .padding(.bottom)
            }
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") {
                        viewModel.clearFormState()
                        dismiss()
                    }
                }
            }
            .alert("Error", isPresented: .constant(viewModel.errorMessage != nil)) {
                Button("OK") {
                    viewModel.clearError()
                }
            } message: {
                if let error = viewModel.errorMessage {
                    Text(error)
                }
            }
        }
    }

    // MARK: - Methods

    private func completeTask() async {
        let success = await viewModel.completeTask(taskId: task.id)
        if success {
            dismiss()
        }
    }
}

// MARK: - Preview

#Preview {
    CompleteTaskView(
        viewModel: TaskViewModel(childId: UUID(), isParent: false),
        task: ChoreTask(
            id: UUID(),
            childId: UUID(),
            childName: "Test Child",
            title: "Clean Room",
            description: "Make your bed and tidy up",
            rewardAmount: 5.00,
            status: .Active,
            isRecurring: false,
            recurrenceType: nil,
            recurrenceDisplay: "One-time",
            createdAt: Date(),
            createdById: UUID(),
            createdByName: "Parent",
            totalCompletions: 0,
            pendingApprovals: 0,
            lastCompletedAt: nil
        )
    )
}
