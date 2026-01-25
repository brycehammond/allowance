import React, { useEffect, useState, useCallback } from 'react';
import { Link2, Copy, Check, Trash2, RefreshCw, BarChart3, Plus, X } from 'lucide-react';
import { giftLinksApi } from '../../services/api';
import type { GiftLink, CreateGiftLinkRequest, GiftLinkStats, GiftLinkVisibility, GiftOccasion } from '../../types';

interface GiftLinksTabProps {
  childId: string;
  childName: string;
}

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

const visibilityLabels: Record<string, string> = {
  Minimal: 'Minimal - Just child\'s name',
  WithGoals: 'With Goals - Show savings goals',
  Full: 'Full - Show all savings goals',
};

export const GiftLinksTab: React.FC<GiftLinksTabProps> = ({ childId, childName }) => {
  const [links, setLinks] = useState<GiftLink[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [showAddForm, setShowAddForm] = useState(false);
  const [copiedId, setCopiedId] = useState<string | null>(null);
  const [selectedStats, setSelectedStats] = useState<GiftLinkStats | null>(null);
  const [loadingStats, setLoadingStats] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formData, setFormData] = useState<CreateGiftLinkRequest>({
    childId,
    name: '',
    description: '',
    visibility: 'Minimal' as GiftLinkVisibility,
    maxUses: undefined,
    minAmount: undefined,
    maxAmount: undefined,
    defaultOccasion: undefined,
  });

  const loadLinks = useCallback(async () => {
    try {
      setIsLoading(true);
      const allLinks = await giftLinksApi.getAll();
      // Filter to only show links for this child
      const childLinks = allLinks.filter(link => link.childId === childId);
      setLinks(childLinks);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to load gift links');
    } finally {
      setIsLoading(false);
    }
  }, [childId]);

  useEffect(() => {
    loadLinks();
  }, [loadLinks]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsSubmitting(true);

    try {
      await giftLinksApi.create({
        ...formData,
        childId,
      });
      setShowAddForm(false);
      setFormData({
        childId,
        name: '',
        description: '',
        visibility: 'Minimal' as GiftLinkVisibility,
      });
      await loadLinks();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to create gift link');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCopyLink = async (link: GiftLink) => {
    try {
      await navigator.clipboard.writeText(link.portalUrl);
      setCopiedId(link.id);
      setTimeout(() => setCopiedId(null), 2000);
    } catch {
      setError('Failed to copy link to clipboard');
    }
  };

  const handleDeactivate = async (linkId: string) => {
    if (!confirm('Are you sure you want to deactivate this gift link? It will no longer accept new gifts.')) return;

    try {
      await giftLinksApi.deactivate(linkId);
      await loadLinks();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to deactivate link');
    }
  };

  const handleRegenerateToken = async (linkId: string) => {
    if (!confirm('Are you sure you want to regenerate this link? The old link will stop working.')) return;

    try {
      await giftLinksApi.regenerateToken(linkId);
      await loadLinks();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to regenerate token');
    }
  };

  const handleViewStats = async (linkId: string) => {
    setLoadingStats(linkId);
    try {
      const stats = await giftLinksApi.getStats(linkId);
      setSelectedStats(stats);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to load stats');
    } finally {
      setLoadingStats(null);
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString();
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
      {/* Header with Add Button */}
      <div className="flex justify-between items-center">
        <div>
          <h3 className="text-lg font-medium text-gray-900">Gift Links</h3>
          <p className="text-sm text-gray-500">Create shareable links for family members to send gifts to {childName}</p>
        </div>
        <button
          onClick={() => setShowAddForm(!showAddForm)}
          className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500"
        >
          {showAddForm ? (
            <>
              <X className="w-5 h-5 mr-2" />
              Cancel
            </>
          ) : (
            <>
              <Plus className="w-5 h-5 mr-2" />
              Create Link
            </>
          )}
        </button>
      </div>

      {error && (
        <div className="rounded-md bg-red-50 p-4">
          <div className="text-sm text-red-800">{error}</div>
        </div>
      )}

      {/* Add Link Form */}
      {showAddForm && (
        <div className="bg-white border border-gray-200 rounded-lg p-6">
          <h4 className="text-md font-medium text-gray-900 mb-4">Create New Gift Link</h4>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label htmlFor="name" className="block text-sm font-medium text-gray-700">
                Link Name *
              </label>
              <input
                type="text"
                id="name"
                required
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                placeholder="e.g., Birthday 2024, Holiday Gifts"
              />
            </div>

            <div>
              <label htmlFor="description" className="block text-sm font-medium text-gray-700">
                Description (optional)
              </label>
              <textarea
                id="description"
                value={formData.description || ''}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                rows={2}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                placeholder="Optional description for your reference"
              />
            </div>

            <div>
              <label htmlFor="visibility" className="block text-sm font-medium text-gray-700">
                Visibility
              </label>
              <select
                id="visibility"
                value={formData.visibility}
                onChange={(e) => setFormData({ ...formData, visibility: e.target.value as GiftLinkVisibility })}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
              >
                {Object.entries(visibilityLabels).map(([value, label]) => (
                  <option key={value} value={value}>{label}</option>
                ))}
              </select>
              <p className="mt-1 text-xs text-gray-500">What information can gift givers see?</p>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <label htmlFor="minAmount" className="block text-sm font-medium text-gray-700">
                  Min Amount (optional)
                </label>
                <div className="mt-1 relative rounded-md shadow-sm">
                  <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                    <span className="text-gray-500 sm:text-sm">$</span>
                  </div>
                  <input
                    type="number"
                    id="minAmount"
                    step="0.01"
                    min="0"
                    value={formData.minAmount || ''}
                    onChange={(e) => setFormData({ ...formData, minAmount: e.target.value ? parseFloat(e.target.value) : undefined })}
                    className="block w-full pl-7 pr-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                  />
                </div>
              </div>
              <div>
                <label htmlFor="maxAmount" className="block text-sm font-medium text-gray-700">
                  Max Amount (optional)
                </label>
                <div className="mt-1 relative rounded-md shadow-sm">
                  <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                    <span className="text-gray-500 sm:text-sm">$</span>
                  </div>
                  <input
                    type="number"
                    id="maxAmount"
                    step="0.01"
                    min="0"
                    value={formData.maxAmount || ''}
                    onChange={(e) => setFormData({ ...formData, maxAmount: e.target.value ? parseFloat(e.target.value) : undefined })}
                    className="block w-full pl-7 pr-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                  />
                </div>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <label htmlFor="maxUses" className="block text-sm font-medium text-gray-700">
                  Max Uses (optional)
                </label>
                <input
                  type="number"
                  id="maxUses"
                  min="1"
                  value={formData.maxUses || ''}
                  onChange={(e) => setFormData({ ...formData, maxUses: e.target.value ? parseInt(e.target.value) : undefined })}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                  placeholder="Unlimited"
                />
              </div>
              <div>
                <label htmlFor="defaultOccasion" className="block text-sm font-medium text-gray-700">
                  Default Occasion (optional)
                </label>
                <select
                  id="defaultOccasion"
                  value={formData.defaultOccasion || ''}
                  onChange={(e) => setFormData({ ...formData, defaultOccasion: e.target.value as GiftOccasion || undefined })}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                >
                  <option value="">Select occasion...</option>
                  {Object.entries(occasionLabels).map(([value, label]) => (
                    <option key={value} value={value}>{label}</option>
                  ))}
                </select>
              </div>
            </div>

            <div className="flex justify-end space-x-3 pt-4">
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
                {isSubmitting ? 'Creating...' : 'Create Link'}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Stats Modal */}
      {selectedStats && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 max-w-md w-full mx-4">
            <div className="flex justify-between items-center mb-4">
              <h4 className="text-lg font-medium text-gray-900">Link Statistics</h4>
              <button onClick={() => setSelectedStats(null)} className="text-gray-400 hover:text-gray-600">
                <X className="w-5 h-5" />
              </button>
            </div>
            <div className="space-y-3">
              <div className="flex justify-between">
                <span className="text-gray-600">Total Gifts:</span>
                <span className="font-medium">{selectedStats.totalGifts}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-600">Pending:</span>
                <span className="font-medium text-yellow-600">{selectedStats.pendingGifts}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-600">Approved:</span>
                <span className="font-medium text-green-600">{selectedStats.approvedGifts}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-600">Rejected:</span>
                <span className="font-medium text-red-600">{selectedStats.rejectedGifts}</span>
              </div>
              <div className="border-t pt-3 mt-3">
                <div className="flex justify-between">
                  <span className="text-gray-600">Total Received:</span>
                  <span className="font-medium text-green-600">{formatCurrency(selectedStats.totalAmountReceived)}</span>
                </div>
              </div>
              {selectedStats.lastGiftAt && (
                <div className="flex justify-between text-sm">
                  <span className="text-gray-500">Last Gift:</span>
                  <span className="text-gray-500">{formatDate(selectedStats.lastGiftAt)}</span>
                </div>
              )}
            </div>
            <button
              onClick={() => setSelectedStats(null)}
              className="mt-4 w-full px-4 py-2 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
            >
              Close
            </button>
          </div>
        </div>
      )}

      {/* Gift Links List */}
      {links.length === 0 ? (
        <div className="text-center py-12 bg-white rounded-lg border border-gray-200">
          <Link2 className="mx-auto h-12 w-12 text-gray-400" />
          <h3 className="mt-2 text-sm font-medium text-gray-900">No gift links yet</h3>
          <p className="mt-1 text-sm text-gray-500">
            Create a shareable link so family members can send gifts to {childName}!
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {links.map((link) => (
            <div
              key={link.id}
              className={`bg-white rounded-lg shadow-sm border-2 p-6 ${
                link.isActive ? 'border-gray-200' : 'border-gray-100 bg-gray-50 opacity-75'
              }`}
            >
              <div className="flex justify-between items-start mb-4">
                <div>
                  <h4 className="text-lg font-medium text-gray-900">{link.name}</h4>
                  {link.description && (
                    <p className="text-sm text-gray-500 mt-1">{link.description}</p>
                  )}
                </div>
                <div className="flex items-center space-x-2">
                  {link.isActive ? (
                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                      Active
                    </span>
                  ) : (
                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800">
                      Inactive
                    </span>
                  )}
                </div>
              </div>

              {/* Link URL */}
              {link.isActive && (
                <div className="flex items-center space-x-2 mb-4 p-3 bg-gray-50 rounded-md">
                  <input
                    type="text"
                    readOnly
                    value={link.portalUrl}
                    className="flex-1 bg-transparent text-sm text-gray-600 border-none focus:ring-0"
                  />
                  <button
                    onClick={() => handleCopyLink(link)}
                    className="inline-flex items-center px-3 py-1 text-sm font-medium text-primary-600 hover:text-primary-700"
                  >
                    {copiedId === link.id ? (
                      <>
                        <Check className="w-4 h-4 mr-1" />
                        Copied!
                      </>
                    ) : (
                      <>
                        <Copy className="w-4 h-4 mr-1" />
                        Copy
                      </>
                    )}
                  </button>
                </div>
              )}

              {/* Link Details */}
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 text-sm mb-4">
                <div>
                  <span className="text-gray-500">Visibility:</span>
                  <p className="font-medium">{link.visibility}</p>
                </div>
                <div>
                  <span className="text-gray-500">Uses:</span>
                  <p className="font-medium">
                    {link.currentUses}{link.maxUses ? ` / ${link.maxUses}` : ' (unlimited)'}
                  </p>
                </div>
                <div>
                  <span className="text-gray-500">Amount Range:</span>
                  <p className="font-medium">
                    {link.minAmount || link.maxAmount
                      ? `${link.minAmount ? formatCurrency(link.minAmount) : '$0'} - ${link.maxAmount ? formatCurrency(link.maxAmount) : 'Any'}`
                      : 'Any amount'}
                  </p>
                </div>
                <div>
                  <span className="text-gray-500">Default Occasion:</span>
                  <p className="font-medium">{link.defaultOccasion ? occasionLabels[link.defaultOccasion] : 'None'}</p>
                </div>
              </div>

              {/* Actions */}
              <div className="flex flex-wrap gap-2 pt-4 border-t">
                <button
                  onClick={() => handleViewStats(link.id)}
                  disabled={loadingStats === link.id}
                  className="inline-flex items-center px-3 py-1.5 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
                >
                  {loadingStats === link.id ? (
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-gray-500 mr-2" />
                  ) : (
                    <BarChart3 className="w-4 h-4 mr-2" />
                  )}
                  Stats
                </button>
                {link.isActive && (
                  <>
                    <button
                      onClick={() => handleRegenerateToken(link.id)}
                      className="inline-flex items-center px-3 py-1.5 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
                    >
                      <RefreshCw className="w-4 h-4 mr-2" />
                      New Link
                    </button>
                    <button
                      onClick={() => handleDeactivate(link.id)}
                      className="inline-flex items-center px-3 py-1.5 border border-red-300 shadow-sm text-sm font-medium rounded-md text-red-700 bg-white hover:bg-red-50"
                    >
                      <Trash2 className="w-4 h-4 mr-2" />
                      Deactivate
                    </button>
                  </>
                )}
              </div>

              <div className="text-xs text-gray-400 mt-4">
                Created {formatDate(link.createdAt)}
                {link.expiresAt && ` • Expires ${formatDate(link.expiresAt)}`}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
