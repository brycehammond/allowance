import React, { useEffect, useState, useCallback } from 'react';
import { useParams, Link } from 'react-router-dom';
import { Gift, Target, Star, CheckCircle, AlertCircle, Heart } from 'lucide-react';
import { giftsApi } from '../services/api';
import type { GiftPortalData, SubmitGiftRequest, GiftOccasion, GiftSubmissionResult } from '../types';

const occasionLabels: Record<string, string> = {
  Birthday: 'Birthday',
  Christmas: 'Christmas',
  Hanukkah: 'Hanukkah',
  Easter: 'Easter',
  Graduation: 'Graduation',
  GoodGrades: 'Good Grades',
  Holiday: 'Holiday',
  JustBecause: 'Just Because',
  Reward: 'Reward',
  Other: 'Other',
};

export const GiftPortal: React.FC = () => {
  const { token } = useParams<{ token: string }>();
  const [portalData, setPortalData] = useState<GiftPortalData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submissionResult, setSubmissionResult] = useState<GiftSubmissionResult | null>(null);
  const [formData, setFormData] = useState<SubmitGiftRequest>({
    giverName: '',
    giverEmail: '',
    giverRelationship: '',
    amount: 0,
    occasion: 'JustBecause' as GiftOccasion,
    customOccasion: '',
    message: '',
  });

  const loadPortalData = useCallback(async () => {
    if (!token) {
      setError('Invalid gift link');
      setIsLoading(false);
      return;
    }

    try {
      setIsLoading(true);
      const data = await giftsApi.getPortalData(token);
      setPortalData(data);
      // Set default occasion if specified
      if (data.defaultOccasion) {
        setFormData(prev => ({ ...prev, occasion: data.defaultOccasion as GiftOccasion }));
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'This gift link is invalid or has expired');
    } finally {
      setIsLoading(false);
    }
  }, [token]);

  useEffect(() => {
    loadPortalData();
  }, [loadPortalData]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token) return;

    setError('');
    setIsSubmitting(true);

    // Validate amount
    if (portalData?.minAmount && formData.amount < portalData.minAmount) {
      setError(`Minimum gift amount is ${formatCurrency(portalData.minAmount)}`);
      setIsSubmitting(false);
      return;
    }
    if (portalData?.maxAmount && formData.amount > portalData.maxAmount) {
      setError(`Maximum gift amount is ${formatCurrency(portalData.maxAmount)}`);
      setIsSubmitting(false);
      return;
    }

    try {
      const result = await giftsApi.submitGift(token, formData);
      setSubmissionResult(result);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to submit gift. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-primary-50 to-primary-100 flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  if (error && !portalData) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-primary-50 to-primary-100 flex items-center justify-center p-4">
        <div className="bg-white rounded-2xl shadow-xl p-8 max-w-md w-full text-center">
          <AlertCircle className="mx-auto h-16 w-16 text-red-500 mb-4" />
          <h1 className="text-2xl font-bold text-gray-900 mb-2">Oops!</h1>
          <p className="text-gray-600 mb-6">{error}</p>
          <Link
            to="/"
            className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700"
          >
            Go to Home
          </Link>
        </div>
      </div>
    );
  }

  if (submissionResult) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-primary-50 to-primary-100 flex items-center justify-center p-4">
        <div className="bg-white rounded-2xl shadow-xl p-8 max-w-md w-full text-center">
          <div className="mb-6">
            <div className="mx-auto w-20 h-20 bg-green-100 rounded-full flex items-center justify-center">
              <CheckCircle className="h-12 w-12 text-green-600" />
            </div>
          </div>
          <h1 className="text-2xl font-bold text-gray-900 mb-2">Thank You!</h1>
          <p className="text-gray-600 mb-4">
            Your gift of {formatCurrency(submissionResult.amount)} has been sent to {submissionResult.childFirstName}!
          </p>
          <p className="text-sm text-gray-500 mb-6">
            {submissionResult.confirmationMessage}
          </p>
          <div className="p-4 bg-primary-50 rounded-lg">
            <Heart className="mx-auto h-8 w-8 text-primary-600 mb-2" />
            <p className="text-sm text-primary-700">
              {submissionResult.childFirstName} may send you a thank you note soon!
            </p>
          </div>
        </div>
      </div>
    );
  }

  if (!portalData) return null;

  const showGoals = ['WithGoals', 'Full'].includes(portalData.visibility) && portalData.savingsGoals.length > 0;
  const showWishList = ['WithWishList', 'Full'].includes(portalData.visibility) && portalData.wishListItems.length > 0;

  return (
    <div className="min-h-screen bg-gradient-to-br from-primary-50 to-primary-100 py-8 px-4">
      <div className="max-w-2xl mx-auto">
        {/* Header */}
        <div className="text-center mb-8">
          <div className="mx-auto w-20 h-20 bg-white rounded-full shadow-lg flex items-center justify-center mb-4">
            <Gift className="h-10 w-10 text-primary-600" />
          </div>
          <h1 className="text-3xl font-bold text-gray-900 mb-2">
            Send a Gift to {portalData.childFirstName}
          </h1>
          <p className="text-gray-600">
            Your gift will be reviewed by their parents before being added to their account.
          </p>
        </div>

        <div className="grid gap-6">
          {/* Savings Goals (if visible) */}
          {showGoals && (
            <div className="bg-white rounded-xl shadow-sm p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4 flex items-center">
                <Target className="w-5 h-5 mr-2 text-primary-600" />
                {portalData.childFirstName}'s Savings Goals
              </h2>
              <div className="space-y-4">
                {portalData.savingsGoals.map((goal) => (
                  <div key={goal.id} className="border border-gray-200 rounded-lg p-4">
                    <div className="flex justify-between items-start mb-2">
                      <div>
                        <h3 className="font-medium text-gray-900">{goal.name}</h3>
                        {goal.description && (
                          <p className="text-sm text-gray-500">{goal.description}</p>
                        )}
                      </div>
                      <span className="text-sm font-medium text-primary-600">
                        {Math.round(goal.progressPercentage)}%
                      </span>
                    </div>
                    <div className="w-full bg-gray-200 rounded-full h-2">
                      <div
                        className="bg-primary-600 h-2 rounded-full transition-all"
                        style={{ width: `${Math.min(100, goal.progressPercentage)}%` }}
                      />
                    </div>
                    <div className="flex justify-between text-xs text-gray-500 mt-1">
                      <span>{formatCurrency(goal.currentAmount)}</span>
                      <span>{formatCurrency(goal.targetAmount)}</span>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Wish List (if visible) */}
          {showWishList && (
            <div className="bg-white rounded-xl shadow-sm p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4 flex items-center">
                <Star className="w-5 h-5 mr-2 text-secondary-600" />
                {portalData.childFirstName}'s Wish List
              </h2>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                {portalData.wishListItems.map((item) => (
                  <div key={item.id} className="border border-gray-200 rounded-lg p-3">
                    <div className="flex justify-between items-center">
                      <span className="font-medium text-gray-900">{item.name}</span>
                      <span className="text-sm text-gray-600">{formatCurrency(item.price)}</span>
                    </div>
                    {item.notes && (
                      <p className="text-xs text-gray-500 mt-1">{item.notes}</p>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Gift Form */}
          <div className="bg-white rounded-xl shadow-sm p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Your Gift</h2>

            {error && (
              <div className="rounded-md bg-red-50 p-4 mb-4">
                <div className="flex items-center">
                  <AlertCircle className="w-5 h-5 text-red-400 mr-2" />
                  <div className="text-sm text-red-800">{error}</div>
                </div>
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label htmlFor="giverName" className="block text-sm font-medium text-gray-700">
                    Your Name *
                  </label>
                  <input
                    type="text"
                    id="giverName"
                    required
                    value={formData.giverName}
                    onChange={(e) => setFormData({ ...formData, giverName: e.target.value })}
                    className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                    placeholder="Grandma Smith"
                  />
                </div>
                <div>
                  <label htmlFor="giverRelationship" className="block text-sm font-medium text-gray-700">
                    Relationship (optional)
                  </label>
                  <input
                    type="text"
                    id="giverRelationship"
                    value={formData.giverRelationship || ''}
                    onChange={(e) => setFormData({ ...formData, giverRelationship: e.target.value })}
                    className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                    placeholder="Grandmother, Uncle, etc."
                  />
                </div>
              </div>

              <div>
                <label htmlFor="giverEmail" className="block text-sm font-medium text-gray-700">
                  Email Address (optional - for thank you notes)
                </label>
                <input
                  type="email"
                  id="giverEmail"
                  value={formData.giverEmail || ''}
                  onChange={(e) => setFormData({ ...formData, giverEmail: e.target.value })}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                  placeholder="grandma@example.com"
                />
                <p className="mt-1 text-xs text-gray-500">
                  {portalData.childFirstName} can send you a thank you note if you provide your email
                </p>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label htmlFor="amount" className="block text-sm font-medium text-gray-700">
                    Gift Amount *
                  </label>
                  <div className="mt-1 relative rounded-md shadow-sm">
                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                      <span className="text-gray-500 sm:text-sm">$</span>
                    </div>
                    <input
                      type="number"
                      id="amount"
                      step="0.01"
                      min={portalData.minAmount || 0.01}
                      max={portalData.maxAmount || undefined}
                      required
                      value={formData.amount || ''}
                      onChange={(e) => setFormData({ ...formData, amount: parseFloat(e.target.value) || 0 })}
                      className="block w-full pl-7 pr-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                      placeholder="25.00"
                    />
                  </div>
                  {(portalData.minAmount || portalData.maxAmount) && (
                    <p className="mt-1 text-xs text-gray-500">
                      {portalData.minAmount && portalData.maxAmount
                        ? `Between ${formatCurrency(portalData.minAmount)} and ${formatCurrency(portalData.maxAmount)}`
                        : portalData.minAmount
                        ? `Minimum ${formatCurrency(portalData.minAmount)}`
                        : `Maximum ${formatCurrency(portalData.maxAmount!)}`}
                    </p>
                  )}
                </div>
                <div>
                  <label htmlFor="occasion" className="block text-sm font-medium text-gray-700">
                    Occasion *
                  </label>
                  <select
                    id="occasion"
                    required
                    value={formData.occasion}
                    onChange={(e) => setFormData({ ...formData, occasion: e.target.value as GiftOccasion })}
                    className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                  >
                    {Object.entries(occasionLabels).map(([value, label]) => (
                      <option key={value} value={value}>{label}</option>
                    ))}
                  </select>
                </div>
              </div>

              {formData.occasion === 'Other' && (
                <div>
                  <label htmlFor="customOccasion" className="block text-sm font-medium text-gray-700">
                    Custom Occasion
                  </label>
                  <input
                    type="text"
                    id="customOccasion"
                    value={formData.customOccasion || ''}
                    onChange={(e) => setFormData({ ...formData, customOccasion: e.target.value })}
                    className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                    placeholder="What's the occasion?"
                  />
                </div>
              )}

              <div>
                <label htmlFor="message" className="block text-sm font-medium text-gray-700">
                  Personal Message (optional)
                </label>
                <textarea
                  id="message"
                  rows={3}
                  value={formData.message || ''}
                  onChange={(e) => setFormData({ ...formData, message: e.target.value })}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                  placeholder={`Write a message for ${portalData.childFirstName}...`}
                />
              </div>

              <button
                type="submit"
                disabled={isSubmitting || formData.amount <= 0}
                className="w-full flex justify-center items-center px-6 py-3 border border-transparent text-base font-medium rounded-lg text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                {isSubmitting ? (
                  <>
                    <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-white mr-2" />
                    Sending Gift...
                  </>
                ) : (
                  <>
                    <Gift className="w-5 h-5 mr-2" />
                    Send Gift{formData.amount > 0 && ` (${formatCurrency(formData.amount)})`}
                  </>
                )}
              </button>
            </form>
          </div>

          {/* Footer */}
          <div className="text-center text-sm text-gray-500">
            <p>
              Gifts are subject to parental approval before being added to {portalData.childFirstName}'s account.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};
