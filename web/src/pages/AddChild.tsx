import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { childrenApi } from '../services/api';
import type { CreateChildRequest } from '../types';

export const AddChild: React.FC = () => {
  const navigate = useNavigate();
  const [formData, setFormData] = useState<CreateChildRequest>({
    email: '',
    password: '',
    confirmPassword: '',
    firstName: '',
    lastName: '',
    weeklyAllowance: 10.00,
    savingsAccountEnabled: false,
    savingsTransferType: 'Percentage',
    savingsTransferPercentage: 10,
    savingsTransferAmount: undefined,
  });
  const [error, setError] = useState<string>('');
  const [isLoading, setIsLoading] = useState(false);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value, type } = e.target;

    if (type === 'checkbox') {
      const checked = (e.target as HTMLInputElement).checked;
      setFormData({
        ...formData,
        [name]: checked,
      });
    } else if (type === 'number') {
      setFormData({
        ...formData,
        [name]: value === '' ? undefined : parseFloat(value),
      });
    } else {
      setFormData({
        ...formData,
        [name]: value,
      });
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    // Validate password match
    if (formData.password !== formData.confirmPassword) {
      setError('Passwords do not match');
      return;
    }

    // Validate savings settings
    if (formData.savingsAccountEnabled) {
      if (formData.savingsTransferType === 'Percentage') {
        if (!formData.savingsTransferPercentage || formData.savingsTransferPercentage <= 0 || formData.savingsTransferPercentage > 100) {
          setError('Savings percentage must be between 1 and 100');
          return;
        }
      } else {
        if (!formData.savingsTransferAmount || formData.savingsTransferAmount <= 0) {
          setError('Savings amount must be greater than 0');
          return;
        }
      }
    }

    setIsLoading(true);

    try {
      await childrenApi.create(formData);
      navigate('/dashboard');
    } catch (err: any) {
      setError(
        err.response?.data?.message || 'Failed to create child account. Please try again.'
      );
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-2xl mx-auto">
        <div className="mb-8">
          <button
            onClick={() => navigate('/dashboard')}
            className="inline-flex items-center text-sm text-gray-600 hover:text-gray-900"
          >
            <svg className="w-5 h-5 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
            </svg>
            Back to Dashboard
          </button>
        </div>

        <div className="bg-white shadow rounded-lg p-6">
          <h2 className="text-2xl font-bold text-gray-900 mb-6">Add Child Account</h2>

          <form onSubmit={handleSubmit} className="space-y-6">
            {error && (
              <div className="rounded-md bg-red-50 p-4">
                <div className="text-sm text-red-800">{error}</div>
              </div>
            )}

            {/* Personal Information */}
            <div className="space-y-4">
              <h3 className="text-lg font-medium text-gray-900">Personal Information</h3>

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
              </div>

              <div>
                <label htmlFor="password" className="block text-sm font-medium text-gray-700">
                  Password *
                </label>
                <input
                  id="password"
                  name="password"
                  type="password"
                  required
                  value={formData.password}
                  onChange={handleChange}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                />
              </div>

              <div>
                <label htmlFor="confirmPassword" className="block text-sm font-medium text-gray-700">
                  Confirm Password *
                </label>
                <input
                  id="confirmPassword"
                  name="confirmPassword"
                  type="password"
                  required
                  value={formData.confirmPassword}
                  onChange={handleChange}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                />
              </div>
            </div>

            {/* Allowance Settings */}
            <div className="space-y-4 pt-4 border-t border-gray-200">
              <h3 className="text-lg font-medium text-gray-900">Allowance Settings</h3>

              <div>
                <label htmlFor="weeklyAllowance" className="block text-sm font-medium text-gray-700">
                  Weekly Allowance *
                </label>
                <div className="mt-1 relative rounded-md shadow-sm">
                  <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                    <span className="text-gray-500 sm:text-sm">$</span>
                  </div>
                  <input
                    id="weeklyAllowance"
                    name="weeklyAllowance"
                    type="number"
                    step="0.01"
                    min="0"
                    required
                    value={formData.weeklyAllowance}
                    onChange={handleChange}
                    className="block w-full pl-7 pr-12 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                  />
                </div>
              </div>
            </div>

            {/* Savings Account Settings */}
            <div className="space-y-4 pt-4 border-t border-gray-200">
              <div className="flex items-center">
                <input
                  id="savingsAccountEnabled"
                  name="savingsAccountEnabled"
                  type="checkbox"
                  checked={formData.savingsAccountEnabled}
                  onChange={handleChange}
                  className="h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 rounded"
                />
                <label htmlFor="savingsAccountEnabled" className="ml-2 block text-sm font-medium text-gray-900">
                  Enable Savings Account
                </label>
              </div>

              {formData.savingsAccountEnabled && (
                <div className="ml-6 space-y-4">
                  <div>
                    <label htmlFor="savingsTransferType" className="block text-sm font-medium text-gray-700">
                      Transfer Type
                    </label>
                    <select
                      id="savingsTransferType"
                      name="savingsTransferType"
                      value={formData.savingsTransferType}
                      onChange={handleChange}
                      className="mt-1 block w-full pl-3 pr-10 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                    >
                      <option value="Percentage">Percentage</option>
                      <option value="FixedAmount">Fixed Amount</option>
                    </select>
                  </div>

                  {formData.savingsTransferType === 'Percentage' ? (
                    <div>
                      <label htmlFor="savingsTransferPercentage" className="block text-sm font-medium text-gray-700">
                        Savings Percentage
                      </label>
                      <div className="mt-1 relative rounded-md shadow-sm">
                        <input
                          id="savingsTransferPercentage"
                          name="savingsTransferPercentage"
                          type="number"
                          step="1"
                          min="1"
                          max="100"
                          value={formData.savingsTransferPercentage || ''}
                          onChange={handleChange}
                          className="block w-full pr-12 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                        />
                        <div className="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none">
                          <span className="text-gray-500 sm:text-sm">%</span>
                        </div>
                      </div>
                    </div>
                  ) : (
                    <div>
                      <label htmlFor="savingsTransferAmount" className="block text-sm font-medium text-gray-700">
                        Savings Amount
                      </label>
                      <div className="mt-1 relative rounded-md shadow-sm">
                        <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                          <span className="text-gray-500 sm:text-sm">$</span>
                        </div>
                        <input
                          id="savingsTransferAmount"
                          name="savingsTransferAmount"
                          type="number"
                          step="0.01"
                          min="0.01"
                          value={formData.savingsTransferAmount || ''}
                          onChange={handleChange}
                          className="block w-full pl-7 pr-12 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                        />
                      </div>
                    </div>
                  )}
                </div>
              )}
            </div>

            {/* Submit Buttons */}
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
                {isLoading ? 'Creating...' : 'Create Child Account'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};
