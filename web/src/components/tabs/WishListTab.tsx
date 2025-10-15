import React, { useEffect, useState } from 'react';
import { useAuth } from '../../contexts/AuthContext';
import { wishListApi } from '../../services/api';
import type { WishListItem, CreateWishListItemRequest } from '../../types';

interface WishListTabProps {
  childId: string;
}

export const WishListTab: React.FC<WishListTabProps> = ({ childId }) => {
  const { user } = useAuth();
  const [items, setItems] = useState<WishListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [showAddForm, setShowAddForm] = useState(false);
  const [formData, setFormData] = useState<CreateWishListItemRequest>({
    childId,
    itemName: '',
    targetAmount: 0,
  });
  const [isSubmitting, setIsSubmitting] = useState(false);

  const isParent = user?.role === 'Parent';

  useEffect(() => {
    loadWishList();
  }, [childId]);

  const loadWishList = async () => {
    try {
      setIsLoading(true);
      const data = await wishListApi.getByChild(childId);
      setItems(data);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load wish list');
    } finally {
      setIsLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (formData.targetAmount <= 0) {
      setError('Target amount must be greater than 0');
      return;
    }

    setIsSubmitting(true);

    try {
      await wishListApi.create(formData);
      setShowAddForm(false);
      setFormData({
        childId,
        itemName: '',
        targetAmount: 0,
      });
      await loadWishList();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to create wish list item');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleTogglePurchased = async (item: WishListItem) => {
    if (!isParent) return;

    try {
      if (item.isPurchased) {
        await wishListApi.markAsUnpurchased(item.id);
      } else {
        await wishListApi.markAsPurchased(item.id);
      }
      await loadWishList();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to update wish list item');
    }
  };

  const handleDelete = async (itemId: string) => {
    if (!confirm('Are you sure you want to delete this wish list item?')) return;

    try {
      await wishListApi.delete(itemId);
      await loadWishList();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to delete wish list item');
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
      <div className="flex justify-center items-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-500"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header with Add Item Button */}
      <div className="flex justify-between items-center">
        <h3 className="text-lg font-medium text-gray-900">Wish List</h3>
        <button
          onClick={() => setShowAddForm(!showAddForm)}
          className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500"
        >
          {showAddForm ? (
            <>
              <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
              Cancel
            </>
          ) : (
            <>
              <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
              </svg>
              Add Item
            </>
          )}
        </button>
      </div>

      {/* Add Item Form */}
      {showAddForm && (
        <div className="bg-white border border-gray-200 rounded-lg p-6">
          <h4 className="text-md font-medium text-gray-900 mb-4">New Wish List Item</h4>
          <form onSubmit={handleSubmit} className="space-y-4">
            {error && (
              <div className="rounded-md bg-red-50 p-4">
                <div className="text-sm text-red-800">{error}</div>
              </div>
            )}

            <div>
              <label htmlFor="itemName" className="block text-sm font-medium text-gray-700">
                Item Name
              </label>
              <input
                type="text"
                id="itemName"
                required
                value={formData.itemName}
                onChange={(e) => setFormData({ ...formData, itemName: e.target.value })}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                placeholder="e.g., New bicycle, Video game, etc."
              />
            </div>

            <div>
              <label htmlFor="targetAmount" className="block text-sm font-medium text-gray-700">
                Target Amount
              </label>
              <div className="mt-1 relative rounded-md shadow-sm">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <span className="text-gray-500 sm:text-sm">$</span>
                </div>
                <input
                  type="number"
                  id="targetAmount"
                  step="0.01"
                  min="0.01"
                  required
                  value={formData.targetAmount || ''}
                  onChange={(e) => setFormData({ ...formData, targetAmount: parseFloat(e.target.value) || 0 })}
                  className="block w-full pl-7 pr-12 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                />
              </div>
            </div>

            <div className="flex justify-end space-x-3">
              <button
                type="button"
                onClick={() => {
                  setShowAddForm(false);
                  setError('');
                }}
                className="px-4 py-2 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isSubmitting}
                className="px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50"
              >
                {isSubmitting ? 'Adding...' : 'Add Item'}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Wish List Items */}
      {items.length === 0 ? (
        <div className="text-center py-12 bg-white rounded-lg border border-gray-200">
          <svg
            className="mx-auto h-12 w-12 text-gray-400"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z"
            />
          </svg>
          <h3 className="mt-2 text-sm font-medium text-gray-900">No wish list items yet</h3>
          <p className="mt-1 text-sm text-gray-500">
            Add items you're saving up for!
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {items.map((item) => (
            <div
              key={item.id}
              className={`bg-white rounded-lg shadow-sm border-2 overflow-hidden ${
                item.isPurchased
                  ? 'border-green-200 bg-green-50'
                  : item.canAfford
                  ? 'border-primary-200'
                  : 'border-gray-200'
              }`}
            >
              <div className="p-6">
                <div className="flex items-start justify-between mb-4">
                  <h4 className={`text-lg font-medium ${item.isPurchased ? 'line-through text-gray-500' : 'text-gray-900'}`}>
                    {item.name}
                  </h4>
                  {item.isPurchased && (
                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                      Purchased
                    </span>
                  )}
                </div>

                <div className="space-y-2 mb-4">
                  <div className="flex justify-between text-sm">
                    <span className="text-gray-600">Price:</span>
                    <span className="font-medium text-gray-900">{formatCurrency(item.price)}</span>
                  </div>
                  {item.canAfford && !item.isPurchased && (
                    <div className="mt-2 p-2 bg-green-50 rounded-md">
                      <p className="text-xs text-green-800 font-medium text-center">
                        🎉 You can afford this!
                      </p>
                    </div>
                  )}
                </div>

                {/* Actions (Parent Only) */}
                {isParent && (
                  <div className="flex space-x-2">
                    <button
                      onClick={() => handleTogglePurchased(item)}
                      className={`flex-1 px-3 py-2 border text-sm font-medium rounded-md focus:outline-none focus:ring-2 focus:ring-offset-2 ${
                        item.isPurchased
                          ? 'border-gray-300 text-gray-700 bg-white hover:bg-gray-50 focus:ring-gray-500'
                          : 'border-transparent text-white bg-green-600 hover:bg-green-700 focus:ring-green-500'
                      }`}
                    >
                      {item.isPurchased ? 'Mark Unpurchased' : 'Mark Purchased'}
                    </button>
                    <button
                      onClick={() => handleDelete(item.id)}
                      className="px-3 py-2 border border-red-300 text-sm font-medium rounded-md text-red-700 bg-white hover:bg-red-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500"
                    >
                      <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                      </svg>
                    </button>
                  </div>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
