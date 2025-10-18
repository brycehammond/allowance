import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { childrenApi } from '../services/api';
import type { Child } from '../types';
import { ChildCard } from '../components/ChildCard';
import { Layout } from '../components/Layout';
import { UserPlus } from 'lucide-react';

export const Dashboard: React.FC = () => {
  const navigate = useNavigate();
  const [children, setChildren] = useState<Child[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string>('');

  useEffect(() => {
    loadChildren();
  }, []);

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

  return (
    <Layout>
      <div>
        <div className="mb-8 flex justify-between items-center">
          <div>
            <h2 className="text-3xl font-bold text-gray-900 mb-2">Dashboard</h2>
            <p className="text-gray-600">Manage your family's allowances and track spending</p>
          </div>
          {children.length > 0 && (
            <button
              onClick={() => navigate('/children/add')}
              className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 transition-colors"
            >
              <UserPlus className="w-5 h-5 mr-2" />
              Add Child
            </button>
          )}
        </div>

        {error && (
          <div className="mb-6 rounded-md bg-red-50 p-4">
            <div className="text-sm text-red-800">{error}</div>
          </div>
        )}

        {isLoading ? (
          <div className="flex justify-center items-center py-12">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-500"></div>
          </div>
        ) : children.length === 0 ? (
          <div className="text-center py-12 bg-white rounded-lg shadow-sm p-8">
            <UserPlus className="mx-auto h-16 w-16 text-gray-400" />
            <h3 className="mt-4 text-lg font-medium text-gray-900">No children yet</h3>
            <p className="mt-2 text-sm text-gray-500">
              Get started by adding a child to your family.
            </p>
            <div className="mt-6">
              <button
                type="button"
                onClick={() => navigate('/children/add')}
                className="inline-flex items-center px-6 py-3 border border-transparent shadow-sm text-base font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 transition-colors"
              >
                <UserPlus className="w-5 h-5 mr-2" />
                Add Child
              </button>
            </div>
          </div>
        ) : (
          <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {children.map((child) => (
              <ChildCard key={child.id} child={child} />
            ))}
          </div>
        )}
      </div>
    </Layout>
  );
};
