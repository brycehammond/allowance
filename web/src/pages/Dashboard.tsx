import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { childrenApi } from '../services/api';
import type { Child } from '../types';
import { ChildCard } from '../components/ChildCard';
import { Layout } from '../components/Layout';
import { useAuth } from '../contexts/AuthContext';
import { UserPlus, Plus, ArrowLeftRight, ClipboardList } from 'lucide-react';

function getGreeting(): string {
  const hour = new Date().getHours();
  if (hour < 12) return 'Good morning';
  if (hour < 17) return 'Good afternoon';
  return 'Good evening';
}

export const Dashboard: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [children, setChildren] = useState<Child[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string>('');

  useEffect(() => {
    loadChildren();
  }, []);

  // Auto-navigate to child detail if there's only one child
  useEffect(() => {
    if (!isLoading && children.length === 1) {
      navigate(`/children/${children[0].id}`, { replace: true });
    }
  }, [isLoading, children, navigate]);

  const loadChildren = async () => {
    try {
      setIsLoading(true);
      const data = await childrenApi.getAll();
      setChildren(data);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to load children');
    } finally {
      setIsLoading(false);
    }
  };

  // Compute family totals
  const familyTotal = children.reduce((sum, c) => sum + c.currentBalance + c.savingsBalance, 0);
  const familySpending = children.reduce((sum, c) => sum + c.currentBalance, 0);
  const familySavings = children.reduce((sum, c) => sum + c.savingsBalance, 0);
  const savingsRatio = familyTotal > 0 ? (familySavings / familyTotal) * 100 : 0;

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  return (
    <Layout>
      <div className="space-y-6">
        {/* Greeting */}
        <div>
          <h2 className="text-2xl font-bold text-gray-900 font-headline">
            {getGreeting()}, {user?.firstName}
          </h2>
          <p className="text-sm text-gray-400 mt-0.5">Earn &amp; Learn Parent</p>
        </div>

        {error && (
          <div className="rounded-xl bg-red-50 p-4">
            <div className="text-sm text-red-800">{error}</div>
          </div>
        )}

        {isLoading ? (
          <div className="flex justify-center items-center py-12">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-500"></div>
          </div>
        ) : children.length === 0 ? (
          <div className="text-center py-12 bg-white rounded-2xl shadow-sm p-8">
            <UserPlus className="mx-auto h-16 w-16 text-gray-300" />
            <h3 className="mt-4 text-lg font-semibold text-gray-900 font-headline">No children yet</h3>
            <p className="mt-2 text-sm text-gray-500">
              Get started by adding a child to your family.
            </p>
            <div className="mt-6">
              <button
                type="button"
                onClick={() => navigate('/children/add')}
                className="inline-flex items-center px-6 py-3 text-sm font-semibold rounded-xl text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 transition-colors shadow-sm"
              >
                <UserPlus className="w-5 h-5 mr-2" />
                Add Child
              </button>
            </div>
          </div>
        ) : (
          <>
            {/* Family Summary Card */}
            <div className="bg-white rounded-2xl shadow-sm p-6">
              <p className="text-xs font-medium text-gray-400 uppercase tracking-wider">Family Total</p>
              <p className="text-3xl font-bold text-primary-600 font-headline mt-1">
                {formatCurrency(familyTotal)}
              </p>

              <div className="mt-4 grid grid-cols-2 gap-4">
                <div className="flex items-center gap-2">
                  <span className="w-2.5 h-2.5 rounded-full bg-primary-500"></span>
                  <div>
                    <p className="text-xs text-gray-400">Spending</p>
                    <p className="text-sm font-semibold text-gray-700">{formatCurrency(familySpending)}</p>
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  <span className="w-2.5 h-2.5 rounded-full bg-tertiary-500"></span>
                  <div>
                    <p className="text-xs text-gray-400">Savings</p>
                    <p className="text-sm font-semibold text-gray-700">{formatCurrency(familySavings)}</p>
                  </div>
                </div>
              </div>

              {/* Ratio bar */}
              {familyTotal > 0 && (
                <div className="mt-4 h-2 rounded-full bg-gray-100 overflow-hidden flex">
                  <div
                    className="bg-primary-500 rounded-l-full transition-all"
                    style={{ width: `${100 - savingsRatio}%` }}
                  />
                  <div
                    className="bg-tertiary-500 rounded-r-full transition-all"
                    style={{ width: `${savingsRatio}%` }}
                  />
                </div>
              )}
            </div>

            {/* Kids Accounts */}
            <div>
              <div className="flex items-center justify-between mb-3">
                <h3 className="text-sm font-semibold text-gray-900">Kids' Accounts</h3>
                <button
                  onClick={() => navigate('/children/add')}
                  className="text-xs font-medium text-primary-600 hover:text-primary-500"
                >
                  + Add Child
                </button>
              </div>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {children.map((child) => (
                  <ChildCard key={child.id} child={child} />
                ))}
              </div>
            </div>

            {/* Quick Actions */}
            <div className="flex justify-center gap-6 py-2">
              <button
                onClick={() => children.length === 1 ? navigate(`/children/${children[0].id}`) : null}
                className="flex flex-col items-center gap-1.5 group"
              >
                <div className="w-12 h-12 rounded-full bg-primary-100 flex items-center justify-center group-hover:bg-primary-200 transition-colors">
                  <Plus className="w-5 h-5 text-primary-600" />
                </div>
                <span className="text-xs font-medium text-gray-500">Add Money</span>
              </button>
              <button
                onClick={() => children.length === 1 ? navigate(`/children/${children[0].id}`) : null}
                className="flex flex-col items-center gap-1.5 group"
              >
                <div className="w-12 h-12 rounded-full bg-tertiary-100 flex items-center justify-center group-hover:bg-tertiary-200 transition-colors">
                  <ArrowLeftRight className="w-5 h-5 text-tertiary-600" />
                </div>
                <span className="text-xs font-medium text-gray-500">Transfer</span>
              </button>
              <button
                onClick={() => children.length === 1 ? navigate(`/children/${children[0].id}`) : null}
                className="flex flex-col items-center gap-1.5 group"
              >
                <div className="w-12 h-12 rounded-full bg-secondary-50 flex items-center justify-center group-hover:bg-secondary-100 transition-colors">
                  <ClipboardList className="w-5 h-5 text-secondary-500" />
                </div>
                <span className="text-xs font-medium text-gray-500">Add Chore</span>
              </button>
            </div>
          </>
        )}
      </div>
    </Layout>
  );
};
