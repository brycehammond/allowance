import React, { useEffect, useState, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { childrenApi } from '../services/api';
import type { Child } from '../types';
import { TransactionsTab } from '../components/tabs/TransactionsTab';
import { WishListTab } from '../components/tabs/WishListTab';
import { AnalyticsTab } from '../components/tabs/AnalyticsTab';
import { SavingsTab } from '../components/tabs/SavingsTab';
import { Layout } from '../components/Layout';
import { ArrowLeft, Receipt, Star, TrendingUp, Wallet } from 'lucide-react';

type TabType = 'transactions' | 'wishlist' | 'analytics' | 'savings';

export const ChildDetail: React.FC = () => {
  const { childId } = useParams<{ childId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [child, setChild] = useState<Child | null>(null);
  const [activeTab, setActiveTab] = useState<TabType>('transactions');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string>('');

  const isParent = user?.role === 'Parent';

  const loadChild = useCallback(async () => {
    if (!childId) return;

    try {
      setIsLoading(true);
      const data = await childrenApi.getById(childId);
      setChild(data);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to load child details');
    } finally {
      setIsLoading(false);
    }
  }, [childId]);

  useEffect(() => {
    if (childId) {
      loadChild();
    }
  }, [childId, loadChild]);

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  if (isLoading) {
    return (
      <Layout>
        <div className="flex items-center justify-center py-12">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-500 mx-auto"></div>
            <p className="mt-4 text-gray-600">Loading...</p>
          </div>
        </div>
      </Layout>
    );
  }

  if (error || !child) {
    return (
      <Layout>
        <div className="flex items-center justify-center py-12">
          <div className="text-center bg-white rounded-lg shadow-sm p-8">
            <h3 className="text-lg font-medium text-gray-900 mb-2">Error</h3>
            <p className="text-gray-600 mb-4">{error || 'Child not found'}</p>
            <button
              onClick={() => navigate('/dashboard')}
              className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 transition-colors"
            >
              <ArrowLeft className="w-4 h-4 mr-2" />
              Back to Dashboard
            </button>
          </div>
        </div>
      </Layout>
    );
  }

  const tabs: Array<{ id: TabType; label: string; icon: React.FC<{ className?: string }> }> = [
    { id: 'transactions', label: 'Transactions', icon: Receipt },
    { id: 'wishlist', label: 'Wish List', icon: Star },
    { id: 'analytics', label: 'Analytics', icon: TrendingUp },
  ];

  // Only show savings tab to parents
  if (isParent) {
    tabs.push({ id: 'savings', label: 'Savings', icon: Wallet });
  }

  return (
    <Layout>
      <div>
        {/* Header */}
        <div className="mb-6">
          <button
            onClick={() => navigate('/dashboard')}
            className="inline-flex items-center text-sm text-gray-600 hover:text-gray-900 mb-4 transition-colors"
          >
            <ArrowLeft className="w-4 h-4 mr-1" />
            Back to Dashboard
          </button>

          <div className="bg-white shadow-sm rounded-lg p-6">
            <div className="flex items-center justify-between">
              <div className="flex items-center">
                <div className="h-16 w-16 rounded-full bg-primary-100 flex items-center justify-center mr-4">
                  <span className="text-2xl font-semibold text-primary-600">
                    {child.firstName.charAt(0)}
                  </span>
                </div>
                <div>
                  <h1 className="text-3xl font-bold text-gray-900">{child.fullName}</h1>
                  <p className="text-sm text-gray-600 mt-1">
                    Weekly Allowance: {formatCurrency(child.weeklyAllowance)}
                  </p>
                </div>
              </div>
              <div className="text-right">
                <div className="text-4xl font-bold text-primary-600">
                  {formatCurrency(child.currentBalance)}
                </div>
                <p className="text-sm text-gray-600 mt-1">Current Balance</p>
              </div>
            </div>
          </div>
        </div>

        {/* Tabs */}
        <div className="bg-white shadow-sm rounded-lg mb-6">
          <nav className="flex border-b border-gray-200">
            {tabs.map((tab) => {
              const Icon = tab.icon;
              return (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  className={`
                    flex-1 flex items-center justify-center px-4 py-4 text-sm font-medium border-b-2 transition-colors
                    ${activeTab === tab.id
                      ? 'border-primary-500 text-primary-600'
                      : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                    }
                  `}
                >
                  <Icon className={`w-5 h-5 mr-2 ${activeTab === tab.id ? 'text-primary-500' : 'text-gray-400'}`} />
                  {tab.label}
                </button>
              );
            })}
          </nav>
        </div>

        {/* Tab Content */}
        <div>
          {activeTab === 'transactions' && <TransactionsTab childId={child.id} />}
          {activeTab === 'wishlist' && <WishListTab childId={child.id} />}
          {activeTab === 'analytics' && <AnalyticsTab childId={child.id} />}
          {activeTab === 'savings' && isParent && <SavingsTab childId={child.id} />}
        </div>
      </div>
    </Layout>
  );
};
