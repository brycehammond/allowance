import React, { useState, useEffect } from 'react';
import { useNavigate, useSearchParams, Link } from 'react-router-dom';
import { invitesApi } from '../services/api';
import { useAuth } from '../contexts/AuthContext';
import type { ValidateInviteResponse } from '../types';

export const AcceptJoinRequest: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { user, refreshUser } = useAuth();

  const token = searchParams.get('token') || '';

  const [validationResult, setValidationResult] = useState<ValidateInviteResponse | null>(null);
  const [isValidating, setIsValidating] = useState(true);
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    // If not logged in, redirect to login with return URL
    if (!user) {
      const returnUrl = `/accept-join?token=${encodeURIComponent(token)}`;
      navigate(`/login?returnUrl=${encodeURIComponent(returnUrl)}`);
      return;
    }

    const validateToken = async () => {
      if (!token) {
        setValidationResult({
          isValid: false,
          isExistingUser: false,
          firstName: null,
          lastName: null,
          familyName: null,
          inviterName: null,
          errorMessage: 'Invalid invitation link. Please check your email for the correct link.',
        });
        setIsValidating(false);
        return;
      }

      try {
        // For existing users, we validate with their email
        const result = await invitesApi.validateToken(token, user?.email || '');
        setValidationResult(result);
      } catch {
        setValidationResult({
          isValid: false,
          isExistingUser: false,
          firstName: null,
          lastName: null,
          familyName: null,
          inviterName: null,
          errorMessage: 'Failed to validate invitation. Please try again.',
        });
      } finally {
        setIsValidating(false);
      }
    };

    validateToken();
  }, [token, user, navigate]);

  const handleAccept = async () => {
    setError('');
    setIsSubmitting(true);

    try {
      await invitesApi.acceptJoinRequest({ token });

      // Refresh user data to get new family info
      if (refreshUser) {
        await refreshUser();
      }

      navigate('/dashboard');
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { error?: { message?: string }; message?: string } } }).response?.data?.error?.message ||
          (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to join family. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isValidating) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  if (!validationResult?.isValid) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-md w-full space-y-8">
          <div className="bg-white shadow-md rounded-lg p-8 text-center">
            <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-red-100 mb-4">
              <svg className="h-6 w-6 text-red-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </div>
            <h2 className="text-2xl font-bold text-gray-900 mb-2">Invalid Invitation</h2>
            <p className="text-gray-600 mb-6">
              {validationResult?.errorMessage || 'This invitation link is invalid or has expired.'}
            </p>
            <Link
              to="/dashboard"
              className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700"
            >
              Go to Dashboard
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div className="bg-white shadow-md rounded-lg p-8">
          <div className="text-center mb-8">
            <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-primary-100 mb-4">
              <svg className="h-6 w-6 text-primary-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
              </svg>
            </div>
            <h2 className="text-2xl font-bold text-gray-900">Join Family</h2>
            <p className="mt-2 text-gray-600">
              {validationResult.inviterName} has invited you to join{' '}
              <span className="font-semibold">{validationResult.familyName}</span>.
            </p>
          </div>

          {error && (
            <div className="rounded-md bg-red-50 p-4 mb-6">
              <div className="text-sm text-red-800">{error}</div>
            </div>
          )}

          <div className="bg-amber-50 border border-amber-200 rounded-md p-4 mb-6">
            <div className="flex">
              <svg className="h-5 w-5 text-amber-400 mr-2 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
              <div className="text-sm text-amber-700">
                <p className="font-medium">Important</p>
                <p>Accepting this invitation will transfer you to the {validationResult.familyName} family. You will no longer be a member of your current family.</p>
              </div>
            </div>
          </div>

          <div className="flex space-x-4">
            <button
              onClick={() => navigate('/dashboard')}
              className="flex-1 py-2 px-4 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500"
            >
              Decline
            </button>
            <button
              onClick={handleAccept}
              disabled={isSubmitting}
              className="flex-1 py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isSubmitting ? 'Joining...' : 'Accept & Join'}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
