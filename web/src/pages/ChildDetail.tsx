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
// TODO: Re-enable badges and rewards imports when feature is ready
// import { BadgesTab } from '../components/tabs/BadgesTab';
// import { RewardShopTab } from '../components/tabs/RewardShopTab';
import { ChoresTab } from '../components/tabs/ChoresTab';
// TODO: Re-enable gifting imports when feature is ready
// import { GiftLinksTab } from '../components/tabs/GiftLinksTab';
// import { PendingGiftsTab } from '../components/tabs/PendingGiftsTab';
// import { ThankYouNotesTab } from '../components/tabs/ThankYouNotesTab';
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
    { id: 'goals', label: 'Goals', icon: Target },
    { id: 'chores', label: 'Chores', icon: ClipboardList },
    { id: 'analytics', label: 'Analytics', icon: TrendingUp },
    // TODO: Re-enable badges and rewards tabs when feature is ready
    // { id: 'badges', label: 'Badges', icon: Award },
    // { id: 'rewards', label: 'Rewards', icon: Gift },
  ];

  // Only show savings and settings tabs to parents
  if (isParent) {
    // TODO: Re-enable gifting tabs when feature is ready
    // tabs.push({ id: 'giftlinks', label: 'Gift Links', icon: Link2 });
    // tabs.push({ id: 'gifts', label: 'Pending Gifts', icon: Inbox });
    tabs.push({ id: 'savings', label: 'Savings', icon: Wallet });
    tabs.push({ id: 'settings', label: 'Settings', icon: Settings });
  }
  // TODO: Re-enable thank you notes tab when gifting feature is ready
  // else {
  //   tabs.push({ id: 'thankyou', label: 'Thank You', icon: Heart });
  // }

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

          <div className="bg-white shadow-sm rounded-lg p-4 sm:p-6">
            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
              <div className="flex items-center">
                <div className="h-12 w-12 sm:h-16 sm:w-16 rounded-full bg-primary-100 flex items-center justify-center mr-3 sm:mr-4 flex-shrink-0">
                  <span className="text-xl sm:text-2xl font-semibold text-primary-600">
                    {child.firstName.charAt(0)}
                  </span>
                </div>
                <div>
                  <h1 className="text-2xl sm:text-3xl font-bold text-gray-900">{child.fullName}</h1>
                  <p className="text-sm text-gray-600 mt-1">
                    Weekly Allowance: {formatCurrency(child.weeklyAllowance)}
                  </p>
                </div>
              </div>
              <div className="text-left sm:text-right border-t sm:border-t-0 pt-4 sm:pt-0">
                <div className="text-3xl sm:text-4xl font-bold text-gray-900">
                  {formatCurrency(child.currentBalance + child.savingsBalance)}
                </div>
                <p className="text-sm text-gray-600 mb-3">Total Balance</p>
                <div className="flex gap-6 sm:justify-end">
                  <div>
                    <p className="text-lg font-semibold text-gray-900">{formatCurrency(child.currentBalance)}</p>
                    <p className="text-xs text-gray-500">Spending</p>
                  </div>
                  <div>
                    <p className="text-lg font-semibold text-primary-600">{formatCurrency(child.savingsBalance)}</p>
                    <p className="text-xs text-gray-500">Savings</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Tabs */}
        <div className="bg-white shadow-sm rounded-lg mb-6">
          <nav className="flex border-b border-gray-200 overflow-x-auto scrollbar-hide">
            {tabs.map((tab) => {
              const Icon = tab.icon;
              return (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  className={`
                    flex-1 sm:flex-initial flex items-center justify-center px-3 sm:px-4 py-3 sm:py-4 text-sm font-medium border-b-2 transition-colors min-w-[44px] flex-shrink-0
                    ${activeTab === tab.id
                      ? 'border-primary-500 text-primary-600'
                      : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                    }
                  `}
                >
                  <Icon className={`w-5 h-5 sm:mr-2 ${activeTab === tab.id ? 'text-primary-500' : 'text-gray-400'}`} />
                  <span className="hidden sm:inline">{tab.label}</span>
                </button>
              );
            })}
          </nav>
        </div>

        {/* Tab Content */}
        <div>
          {activeTab === 'transactions' && <TransactionsTab childId={child.id} currentBalance={child.currentBalance} savingsBalance={child.savingsBalance} allowDebt={child.allowDebt} onBalanceChange={loadChild} />}
          {activeTab === 'goals' && <SavingsGoalsTab childId={child.id} currentBalance={child.currentBalance} onBalanceChange={loadChild} />}
          {activeTab === 'chores' && <ChoresTab childId={child.id} />}
          {activeTab === 'analytics' && <AnalyticsTab childId={child.id} />}
          {/* TODO: Re-enable badges and rewards tabs when feature is ready */}
          {/* {activeTab === 'badges' && <BadgesTab childId={child.id} />} */}
          {/* {activeTab === 'rewards' && <RewardShopTab childId={child.id} />} */}
          {activeTab === 'savings' && isParent && <SavingsTab childId={child.id} onBalanceChange={loadChild} />}
          {activeTab === 'settings' && isParent && (
            <SettingsTab childId={child.id} child={child} onUpdate={loadChild} />
          )}
          {/* TODO: Re-enable gifting tabs when feature is ready */}
          {/* {activeTab === 'giftlinks' && isParent && <GiftLinksTab childId={child.id} childName={child.firstName} />} */}
          {/* {activeTab === 'gifts' && isParent && <PendingGiftsTab childId={child.id} childName={child.firstName} />} */}
          {/* {activeTab === 'thankyou' && !isParent && <ThankYouNotesTab childId={child.id} />} */}
        </div>
      </div>
    </Layout>
  );
};
