import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { invitesApi } from '../services/api';
import { Layout } from '../components/Layout';
import { ArrowLeft, Mail, Clock, X, UserPlus } from 'lucide-react';
import type { PendingInvite } from '../types';

interface AddParentForm {
  email: string;
  firstName: string;
  lastName: string;
}

export const AddParent: React.FC = () => {
  const navigate = useNavigate();
  const [formData, setFormData] = useState<AddParentForm>({
    email: '',
    firstName: '',
    lastName: '',
  });
  const [error, setError] = useState<string>('');
  const [success, setSuccess] = useState<string>('');
  const [isLoading, setIsLoading] = useState(false);
  const [pendingInvites, setPendingInvites] = useState<PendingInvite[]>([]);
  const [loadingInvites, setLoadingInvites] = useState(true);

  useEffect(() => {
    loadPendingInvites();
  }, []);

  const loadPendingInvites = async () => {
    try {
      const invites = await invitesApi.getPendingInvites();
      setPendingInvites(invites);
    } catch (err) {
      console.error('Failed to load pending invites:', err);
    } finally {
      setLoadingInvites(false);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData({
      ...formData,
      [name]: value,
    });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setSuccess('');
    setIsLoading(true);

    try {
      const result = await invitesApi.sendInvite({
        email: formData.email,
        firstName: formData.firstName,
        lastName: formData.lastName,
      });

      setSuccess(result.message);
      setFormData({ email: '', firstName: '', lastName: '' });
      loadPendingInvites();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { error?: { message?: string }; message?: string } } }).response?.data?.error?.message ||
          (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to send invite. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCancelInvite = async (inviteId: string) => {
    try {
      await invitesApi.cancelInvite(inviteId);
      setPendingInvites(pendingInvites.filter(inv => inv.id !== inviteId));
    } catch (err) {
      console.error('Failed to cancel invite:', err);
    }
  };

  const formatExpirationDate = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffDays = Math.ceil((date.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));

    if (diffDays <= 0) return 'Expired';
    if (diffDays === 1) return 'Expires tomorrow';
    return `Expires in ${diffDays} days`;
  };

  return (
    <Layout>
      <div className="max-w-2xl">
        <div className="mb-6">
          <button
            onClick={() => navigate('/dashboard')}
            className="inline-flex items-center text-sm text-gray-600 hover:text-gray-900 transition-colors"
          >
            <ArrowLeft className="w-4 h-4 mr-1" />
            Back to Dashboard
          </button>
        </div>

        <div className="bg-white shadow-sm rounded-lg p-6 md:p-8">
          <h2 className="text-3xl font-bold text-gray-900 mb-6">Invite Co-Parent</h2>
          <p className="text-gray-600 mb-6">
            Send an invitation to another parent or guardian. They'll receive an email with a link
            to set up their account and join your family.
          </p>

          <form onSubmit={handleSubmit} className="space-y-6">
            {error && (
              <div className="rounded-md bg-red-50 p-4">
                <div className="text-sm text-red-800">{error}</div>
              </div>
            )}

            {success && (
              <div className="rounded-md bg-green-50 p-4">
                <div className="flex items-center">
                  <Mail className="w-5 h-5 text-green-600 mr-2" />
                  <div className="text-sm text-green-800">{success}</div>
                </div>
              </div>
            )}

            <div className="grid grid-cols-2 gap-4">
              <div>
                <label htmlFor="firstName" className="block text-sm font-medium text-gray-700">
                  First Name *
                </label>
                <input
                  id="firstName"
                  name="firstName"
                  type="text"
                  required
                  value={formData.firstName}
                  onChange={handleChange}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                />
              </div>
              <div>
                <label htmlFor="lastName" className="block text-sm font-medium text-gray-700">
                  Last Name *
                </label>
                <input
                  id="lastName"
                  name="lastName"
                  type="text"
                  required
                  value={formData.lastName}
                  onChange={handleChange}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                />
              </div>
            </div>

            <div>
              <label htmlFor="email" className="block text-sm font-medium text-gray-700">
                Email Address *
              </label>
              <input
                id="email"
                name="email"
                type="email"
                required
                value={formData.email}
                onChange={handleChange}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
              />
              <p className="mt-1 text-sm text-gray-500">
                They'll receive an email with instructions to set up their account.
              </p>
            </div>

            <div className="flex justify-end space-x-3 pt-4">
              <button
                type="button"
                onClick={() => navigate('/dashboard')}
                className="px-4 py-2 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isLoading}
                className="px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {isLoading ? 'Sending...' : 'Send Invitation'}
              </button>
            </div>
          </form>
        </div>

        {/* Pending Invites Section */}
        {!loadingInvites && pendingInvites.length > 0 && (
          <div className="mt-8 bg-white shadow-sm rounded-lg p-6 md:p-8">
            <h3 className="text-xl font-semibold text-gray-900 mb-4 flex items-center">
              <UserPlus className="w-5 h-5 mr-2" />
              Pending Invitations
            </h3>
            <div className="space-y-3">
              {pendingInvites.map((invite) => (
                <div
                  key={invite.id}
                  className="flex items-center justify-between p-4 bg-gray-50 rounded-lg"
                >
                  <div>
                    <p className="font-medium text-gray-900">
                      {invite.firstName} {invite.lastName}
                    </p>
                    <p className="text-sm text-gray-500">{invite.email}</p>
                    <p className="text-xs text-gray-400 flex items-center mt-1">
                      <Clock className="w-3 h-3 mr-1" />
                      {formatExpirationDate(invite.expiresAt)}
                      {invite.isExistingUser && (
                        <span className="ml-2 px-2 py-0.5 bg-blue-100 text-blue-700 rounded text-xs">
                          Existing User
                        </span>
                      )}
                    </p>
                  </div>
                  <button
                    onClick={() => handleCancelInvite(invite.id)}
                    className="p-2 text-gray-400 hover:text-red-600 transition-colors"
                    title="Cancel invitation"
                  >
                    <X className="w-5 h-5" />
                  </button>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </Layout>
  );
};
