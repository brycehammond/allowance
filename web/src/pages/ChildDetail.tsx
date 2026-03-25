import React, { useEffect, useState, useCallback } from 'react';
import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { childrenApi } from '../services/api';
import type { Child } from '../types';
import { TransactionsTab } from '../components/tabs/TransactionsTab';
import { AnalyticsTab } from '../components/tabs/AnalyticsTab';
import { SavingsTab } from '../components/tabs/SavingsTab';
import { SavingsGoalsTab } from '../components/tabs/SavingsGoalsTab';
import { SettingsTab } from '../components/tabs/SettingsTab';
import { ChoresTab } from '../components/tabs/ChoresTab';
import { Layout } from '../components/Layout';
import { ArrowLeft, Receipt, TrendingUp, Wallet, Settings, ClipboardList, Target } from 'lucide-react';

type TabType = 'transactions' | 'goals' | 'analytics' | 'chores' | 'savings' | 'settings' | 'giftlinks' | 'gifts' | 'thankyou';

export const ChildDetail: React.FC = () => {
  const { childId } = useParams<{ childId: string }>();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { user } = useAuth();
  const [child, setChild] = useState<Child | null>(null);
  const [activeTab, setActiveTab] = useState<TabType>(() => {
    const tabParam = searchParams.get('tab');
    if (tabParam && ['transactions', 'goals', 'analytics', 'chores', 'savings', 'settings', 'giftlinks', 'gifts', 'thankyou'].includes(tabParam)) {
      return tabParam as TabType;
    }
    return 'transactions';
  });
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
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-500 mx-auto"></div>
        </div>
      </Layout>
    );
  }

  if (error || !child) {
    return (
      <Layout>
        <div className="flex items-center justify-center py-12">
          <div className="text-center bg-white rounded-2xl shadow-sm p-8">
            <h3 className="text-lg font-semibold text-gray-900 font-headline mb-2">Error</h3>
            <p className="text-gray-600 mb-4">{error || 'Child not found'}</p>
            <button
              onClick={() => navigate('/dashboard')}
              className="inline-flex items-center px-4 py-2 text-sm font-semibold rounded-xl text-white bg-primary-600 hover:bg-primary-700 transition-colors"
            >
              <ArrowLeft className="w-4 h-4 mr-2" />
              Back to Dashboard
            </button>
          </div>
        </div>
      </Layout>
    );
  }

  const totalBalance = child.currentBalance + child.savingsBalance;

  const tabs: Array<{ id: TabType; label: string; icon: React.FC<{ className?: string }> }> = [
    { id: 'transactions', label: 'Transactions', icon: Receipt },
    { id: 'goals', label: 'Goals', icon: Target },
    { id: 'chores', label: 'Chores', icon: ClipboardList },
    { id: 'analytics', label: 'Analytics', icon: TrendingUp },
  ];

  if (isParent) {
    tabs.push({ id: 'savings', label: 'Savings', icon: Wallet });
    tabs.push({ id: 'settings', label: 'Settings', icon: Settings });
  }

  return (
    <Layout>
      <div className="space-y-5">
        {/* Back nav */}
        <button
          onClick={() => navigate('/dashboard')}
          className="inline-flex items-center text-sm text-gray-400 hover:text-gray-600 transition-colors"
        >
          <ArrowLeft className="w-4 h-4 mr-1" />
          Back
        </button>

        {/* Hero Section */}
        <div className="text-center pb-2">
          <div className="mx-auto h-20 w-20 rounded-full bg-primary-100 flex items-center justify-center mb-3">
            <span className="text-3xl font-bold text-primary-600">
              {child.firstName.charAt(0)}
            </span>
          </div>
          <h1 className="text-xl font-bold text-gray-900 font-headline">{child.fullName}</h1>
          <p className="text-xs text-gray-400 mt-0.5">{formatCurrency(child.weeklyAllowance)}/week</p>

          {/* Hero Balance */}
          <p className="text-4xl font-bold text-primary-600 font-headline mt-3">
            {formatCurrency(totalBalance)}
          </p>

          {/* Spending / Savings Pills */}
          <div className="flex justify-center gap-3 mt-3">
            <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-primary-50 text-sm">
              <span className="w-2 h-2 rounded-full bg-primary-500"></span>
              <span className="text-gray-600">Spending</span>
              <span className="font-semibold text-gray-800">{formatCurrency(child.currentBalance)}</span>
            </span>
            <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-tertiary-50 text-sm">
              <span className="w-2 h-2 rounded-full bg-tertiary-500"></span>
              <span className="text-gray-600">Savings</span>
              <span className="font-semibold text-gray-800">{formatCurrency(child.savingsBalance)}</span>
            </span>
          </div>
        </div>

        {/* Tab Bar */}
        <div className="flex gap-1.5 overflow-x-auto pb-1 scrollbar-hide">
          {tabs.map((tab) => {
            const Icon = tab.icon;
            const isActive = activeTab === tab.id;
            return (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`
                  flex items-center gap-1.5 px-3.5 py-2 rounded-xl text-sm font-medium whitespace-nowrap transition-colors flex-shrink-0
                  ${isActive
                    ? 'bg-primary-600 text-white shadow-sm'
                    : 'bg-white text-gray-500 hover:bg-gray-50'
                  }
                `}
              >
                <Icon className={`w-4 h-4 ${isActive ? 'text-white' : 'text-gray-400'}`} />
                {tab.label}
              </button>
            );
          })}
        </div>

        {/* Tab Content */}
        <div>
          {activeTab === 'transactions' && <TransactionsTab childId={child.id} currentBalance={child.currentBalance} savingsBalance={child.savingsBalance} allowDebt={child.allowDebt} onBalanceChange={loadChild} />}
          {activeTab === 'goals' && <SavingsGoalsTab childId={child.id} currentBalance={child.currentBalance} onBalanceChange={loadChild} />}
          {activeTab === 'chores' && <ChoresTab childId={child.id} />}
          {activeTab === 'analytics' && <AnalyticsTab childId={child.id} />}
          {activeTab === 'savings' && isParent && <SavingsTab childId={child.id} onBalanceChange={loadChild} />}
          {activeTab === 'settings' && isParent && (
            <SettingsTab childId={child.id} child={child} onUpdate={loadChild} />
          )}
        </div>
      </div>
    </Layout>
  );
};
