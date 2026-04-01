import React, { useState, useRef } from 'react';
import { GoogleLogin } from '@react-oauth/google';
import type { CredentialResponse } from '@react-oauth/google';
import AppleSignIn from 'react-apple-signin-auth';
import { useAuth } from '../contexts/AuthContext';
import type { ExternalLoginRequest } from '../types';

interface SocialSignInButtonsProps {
  onSuccess: () => void;
  onError: (message: string) => void;
}

export const SocialSignInButtons: React.FC<SocialSignInButtonsProps> = ({ onSuccess, onError }) => {
  const { externalLogin } = useAuth();
  const [showFamilyNameModal, setShowFamilyNameModal] = useState(false);
  const [familyName, setFamilyName] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const pendingRequestRef = useRef<ExternalLoginRequest | null>(null);

  const handleExternalLoginResult = async (request: ExternalLoginRequest) => {
    try {
      const result = await externalLogin(request);
      if (result.needsFamilyName) {
        pendingRequestRef.current = request;
        setShowFamilyNameModal(true);
      } else {
        onSuccess();
      }
    } catch {
      onError('Sign-in failed. Please try again.');
    }
  };

  const handleGoogleSuccess = async (credentialResponse: CredentialResponse) => {
    const idToken = credentialResponse.credential;
    if (!idToken) {
      onError('Failed to get credentials from Google. Please try again.');
      return;
    }

    await handleExternalLoginResult({ provider: 'Google', idToken });
  };

  const handleGoogleError = () => {
    onError('Google sign-in was cancelled or failed. Please try again.');
  };

  const handleAppleSuccess = async (response: { authorization: { id_token?: string }; user?: { name?: { firstName?: string; lastName?: string } } }) => {
    const idToken = response.authorization?.id_token;
    if (!idToken) {
      onError('Failed to get credentials from Apple. Please try again.');
      return;
    }

    await handleExternalLoginResult({
      provider: 'Apple',
      idToken,
      firstName: response.user?.name?.firstName ?? undefined,
      lastName: response.user?.name?.lastName ?? undefined,
    });
  };

  const handleAppleError = (error: { error: string }) => {
    // user_cancelled_authorize is normal dismissal, not an error
    if (error?.error !== 'user_cancelled_authorize') {
      onError('Apple sign-in failed. Please try again.');
    }
  };

  const handleFamilyNameSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!pendingRequestRef.current || !familyName.trim()) return;

    setIsSubmitting(true);
    try {
      const request: ExternalLoginRequest = {
        ...pendingRequestRef.current,
        familyName: familyName.trim(),
      };
      const result = await externalLogin(request);
      if (result.needsFamilyName) {
        onError('Family name is still required. Please try again.');
      } else {
        setShowFamilyNameModal(false);
        pendingRequestRef.current = null;
        setFamilyName('');
        onSuccess();
      }
    } catch {
      onError('Sign-in failed. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleModalClose = () => {
    setShowFamilyNameModal(false);
    pendingRequestRef.current = null;
    setFamilyName('');
  };

  return (
    <>
      <div className="mt-6">
        <div className="relative">
          <div className="absolute inset-0 flex items-center">
            <div className="w-full border-t border-gray-300" />
          </div>
          <div className="relative flex justify-center text-sm">
            <span className="px-2 bg-gray-50 text-gray-500">or continue with</span>
          </div>
        </div>

        <div className="mt-6 space-y-3">
          <div className="flex justify-center">
            <GoogleLogin
              onSuccess={handleGoogleSuccess}
              onError={handleGoogleError}
              width="400"
              text="continue_with"
              shape="rectangular"
              size="large"
            />
          </div>

          <AppleSignIn
            authOptions={{
              clientId: import.meta.env.VITE_APPLE_SERVICE_ID || '',
              scope: 'email name',
              redirectURI: import.meta.env.VITE_APPLE_REDIRECT_URI || window.location.origin,
              usePopup: true,
            }}
            uiType="dark"
            className="w-full h-[44px] rounded-md text-base"
            noDefaultStyle={false}
            buttonExtraChildren="Continue with Apple"
            onSuccess={handleAppleSuccess}
            onError={handleAppleError}
          />
        </div>
      </div>

      {showFamilyNameModal && (
        <div className="fixed inset-0 z-50 overflow-y-auto">
          <div className="flex items-center justify-center min-h-screen px-4 pt-4 pb-20 text-center sm:p-0">
            <div className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" onClick={handleModalClose} />
            <div className="relative bg-white rounded-lg px-4 pt-5 pb-4 text-left overflow-hidden shadow-xl transform sm:my-8 sm:max-w-sm sm:w-full sm:p-6">
              <div>
                <h3 className="text-lg leading-6 font-medium text-gray-900">
                  Welcome! Set up your family
                </h3>
                <p className="mt-2 text-sm text-gray-500">
                  Since this is your first time signing in, please provide a family name to create your account.
                </p>
              </div>
              <form onSubmit={handleFamilyNameSubmit} className="mt-4">
                <div>
                  <label htmlFor="familyNameModal" className="block text-sm font-medium text-gray-700">
                    Family Name
                  </label>
                  <input
                    id="familyNameModal"
                    type="text"
                    required
                    value={familyName}
                    onChange={(e) => setFamilyName(e.target.value)}
                    className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                    placeholder="e.g., Smith Family"
                    autoFocus
                  />
                </div>
                <div className="mt-5 sm:mt-6 flex gap-3">
                  <button
                    type="button"
                    onClick={handleModalClose}
                    className="flex-1 inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    disabled={isSubmitting || !familyName.trim()}
                    className="flex-1 inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-primary-500 text-sm font-medium text-white hover:bg-primary-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {isSubmitting ? 'Creating...' : 'Continue'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}
    </>
  );
};
