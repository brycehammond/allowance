import SwiftUI

/// View for inviting co-parents to join the family (Parent only)
struct InviteParentView: View {

    // MARK: - Properties

    @Environment(\.dismiss) private var dismiss
    @State private var viewModel: InviteParentViewModel

    // MARK: - Initialization

    init(apiService: APIServiceProtocol = APIService.shared) {
        _viewModel = State(wrappedValue: InviteParentViewModel(apiService: apiService))
    }

    // MARK: - Body

    var body: some View {
        NavigationStack {
            List {
                // Invite form section
                Section {
                    TextField("First Name", text: $viewModel.firstName)
                        .textContentType(.givenName)
                        .autocapitalization(.words)

                    TextField("Last Name", text: $viewModel.lastName)
                        .textContentType(.familyName)
                        .autocapitalization(.words)

                    TextField("Email Address", text: $viewModel.email)
                        .textContentType(.emailAddress)
                        .autocapitalization(.none)
                        .keyboardType(.emailAddress)
                } header: {
                    Text("Invite Co-Parent")
                } footer: {
                    Text("They'll receive an email with instructions to set up their account and join your family.")
                }

                // Send button section
                Section {
                    Button {
                        Task {
                            await viewModel.sendInvite()
                        }
                    } label: {
                        HStack {
                            Spacer()
                            if viewModel.isSending {
                                ProgressView()
                                    .padding(.trailing, 8)
                            }
                            Text("Send Invitation")
                                .fontWeight(.semibold)
                            Spacer()
                        }
                    }
                    .disabled(!viewModel.isFormValid || viewModel.isSending)
                }

                // Success message
                if let successMessage = viewModel.successMessage {
                    Section {
                        HStack {
                            Image(systemName: "envelope.badge.fill")
                                .foregroundStyle(DesignSystem.Colors.primary)
                            Text(successMessage)
                                .foregroundStyle(DesignSystem.Colors.primary)
                        }
                    }
                }

                // Error message
                if let errorMessage = viewModel.errorMessage {
                    Section {
                        Text(errorMessage)
                            .foregroundStyle(.red)
                    }
                }

                // Pending invites section
                if !viewModel.pendingInvites.isEmpty {
                    Section("Pending Invitations") {
                        ForEach(viewModel.pendingInvites) { invite in
                            PendingInviteRow(
                                invite: invite,
                                isResending: viewModel.resendingInviteId == invite.id,
                                onResend: {
                                    Task {
                                        await viewModel.resendInvite(inviteId: invite.id)
                                    }
                                },
                                onCancel: {
                                    Task {
                                        await viewModel.cancelInvite(inviteId: invite.id)
                                    }
                                }
                            )
                        }
                    }
                }
            }
            .navigationTitle("Invite Co-Parent")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Done") {
                        dismiss()
                    }
                }
            }
            .task {
                await viewModel.loadPendingInvites()
            }
        }
    }
}

// MARK: - Pending Invite Row

struct PendingInviteRow: View {
    let invite: PendingInvite
    let isResending: Bool
    let onResend: () -> Void
    let onCancel: () -> Void

    var body: some View {
        HStack {
            VStack(alignment: .leading, spacing: 4) {
                Text(invite.fullName)
                    .font(.body)
                    .fontWeight(.medium)

                Text(invite.email)
                    .font(.caption)
                    .foregroundStyle(.secondary)

                HStack(spacing: 4) {
                    Image(systemName: "clock")
                        .font(.caption2)
                    Text(invite.expirationDisplay)
                        .font(.caption2)

                    if invite.isExistingUser {
                        Text("Existing User")
                            .font(.caption2)
                            .padding(.horizontal, 6)
                            .padding(.vertical, 2)
                            .background(Color.blue.opacity(0.15))
                            .foregroundStyle(.blue)
                            .clipShape(Capsule())
                    }
                }
                .foregroundStyle(.secondary)
            }

            Spacer()

            // Resend button
            Button {
                onResend()
            } label: {
                if isResending {
                    ProgressView()
                        .scaleEffect(0.8)
                } else {
                    Image(systemName: "arrow.clockwise.circle.fill")
                        .font(.title3)
                        .foregroundStyle(DesignSystem.Colors.primary)
                }
            }
            .buttonStyle(.plain)
            .disabled(isResending)

            // Cancel button
            Button {
                onCancel()
            } label: {
                Image(systemName: "xmark.circle.fill")
                    .font(.title3)
                    .foregroundStyle(.secondary)
            }
            .buttonStyle(.plain)
            .disabled(isResending)
        }
        .padding(.vertical, 4)
    }
}

// MARK: - ViewModel

@Observable
@MainActor
final class InviteParentViewModel {

    // MARK: - Form Fields

    var firstName = ""
    var lastName = ""
    var email = ""

    // MARK: - State

    var isSending = false
    var isLoadingInvites = false
    var resendingInviteId: String?
    var successMessage: String?
    var errorMessage: String?
    var pendingInvites: [PendingInvite] = []

    // MARK: - Dependencies

    private let apiService: APIServiceProtocol

    // MARK: - Initialization

    init(apiService: APIServiceProtocol) {
        self.apiService = apiService
    }

    // MARK: - Computed Properties

    var isFormValid: Bool {
        !firstName.isEmpty &&
        !lastName.isEmpty &&
        !email.isEmpty &&
        isValidEmail(email)
    }

    // MARK: - Methods

    func sendInvite() async {
        guard isFormValid else { return }

        isSending = true
        successMessage = nil
        errorMessage = nil

        do {
            let request = SendParentInviteRequest(
                email: email,
                firstName: firstName,
                lastName: lastName
            )

            let response = try await apiService.sendParentInvite(request)
            successMessage = response.message

            // Clear form
            firstName = ""
            lastName = ""
            email = ""

            // Refresh pending invites
            await loadPendingInvites()
        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to send invitation. Please try again."
        }

        isSending = false
    }

    func loadPendingInvites() async {
        isLoadingInvites = true

        do {
            pendingInvites = try await apiService.getPendingInvites()
        } catch {
            // Silently fail - invites are optional to display
        }

        isLoadingInvites = false
    }

    func cancelInvite(inviteId: String) async {
        do {
            try await apiService.cancelInvite(inviteId: inviteId)
            pendingInvites.removeAll { $0.id == inviteId }
        } catch {
            errorMessage = "Failed to cancel invitation"
        }
    }

    func resendInvite(inviteId: String) async {
        resendingInviteId = inviteId
        successMessage = nil
        errorMessage = nil

        do {
            let response = try await apiService.resendInvite(inviteId: inviteId)
            successMessage = response.message

            // Refresh pending invites to get updated expiration
            await loadPendingInvites()
        } catch let error as APIError {
            errorMessage = error.localizedDescription
        } catch {
            errorMessage = "Failed to resend invitation. Please try again."
        }

        resendingInviteId = nil
    }

    private func isValidEmail(_ email: String) -> Bool {
        let emailRegex = #"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$"#
        return email.range(of: emailRegex, options: .regularExpression) != nil
    }
}

// MARK: - Preview Provider

#Preview("Invite Parent") {
    InviteParentView()
}
