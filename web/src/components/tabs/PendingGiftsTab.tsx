import React, { useEffect, useState, useCallback } from 'react';
import { Gift, CheckCircle, XCircle, Clock, Target, PiggyBank } from 'lucide-react';
import { giftsApi, savingsGoalsApi } from '../../services/api';
import type { Gift as GiftType, ApproveGiftRequest, SavingsGoal, GiftStatus } from '../../types';

interface PendingGiftsTabProps {
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

const statusColors: Record<GiftStatus, string> = {
  Pending: 'bg-yellow-100 text-yellow-800',
  Approved: 'bg-green-100 text-green-800',
  Rejected: 'bg-red-100 text-red-800',
  Expired: 'bg-gray-100 text-gray-800',
};

export const PendingGiftsTab: React.FC<PendingGiftsTabProps> = ({ childId, childName }) => {
  const [gifts, setGifts] = useState<GiftType[]>([]);
  const [goals, setGoals] = useState<SavingsGoal[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [processingId, setProcessingId] = useState<string | null>(null);
  const [showApproveModal, setShowApproveModal] = useState<GiftType | null>(null);
  const [showRejectModal, setShowRejectModal] = useState<GiftType | null>(null);
  const [rejectReason, setRejectReason] = useState('');
  const [approvalData, setApprovalData] = useState<ApproveGiftRequest>({});
  const [filter, setFilter] = useState<'all' | 'pending'>('pending');

  const loadData = useCallback(async () => {
    try {
      setIsLoading(true);
      const [giftsData, goalsData] = await Promise.all([
        giftsApi.getByChild(childId),
        savingsGoalsApi.getByChild(childId),
      ]);
      setGifts(giftsData);
      setGoals(goalsData.filter(g => g.status === 'Active'));
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to load gifts');
    } finally {
      setIsLoading(false);
    }
  }, [childId]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleApprove = async () => {
    if (!showApproveModal) return;
    setProcessingId(showApproveModal.id);
    setError('');

    try {
      await giftsApi.approve(showApproveModal.id, approvalData);
      setShowApproveModal(null);
      setApprovalData({});
      await loadData();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to approve gift');
    } finally {
      setProcessingId(null);
    }
  };

  const handleReject = async () => {
    if (!showRejectModal) return;
    setProcessingId(showRejectModal.id);
    setError('');

    try {
      await giftsApi.reject(showRejectModal.id, { reason: rejectReason || undefined });
      setShowRejectModal(null);
      setRejectReason('');
      await loadData();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to reject gift');
    } finally {
      setProcessingId(null);
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

  const filteredGifts = filter === 'pending'
    ? gifts.filter(g => g.status === 'Pending')
    : gifts;

  const pendingCount = gifts.filter(g => g.status === 'Pending').length;

  if (isLoading) {
    return (
      <div className="flex justify-center items-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-500"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h3 className="text-lg font-medium text-gray-900">Gifts for {childName}</h3>
          {pendingCount > 0 && (
            <p className="text-sm text-yellow-600 font-medium">
              {pendingCount} gift{pendingCount !== 1 ? 's' : ''} awaiting approval
            </p>
          )}
        </div>
        <div className="flex space-x-2">
          <button
            onClick={() => setFilter('pending')}
            className={`px-3 py-1.5 text-sm font-medium rounded-md ${
              filter === 'pending'
                ? 'bg-primary-100 text-primary-700'
                : 'text-gray-500 hover:text-gray-700'
            }`}
          >
            Pending ({pendingCount})
          </button>
          <button
            onClick={() => setFilter('all')}
            className={`px-3 py-1.5 text-sm font-medium rounded-md ${
              filter === 'all'
                ? 'bg-primary-100 text-primary-700'
                : 'text-gray-500 hover:text-gray-700'
            }`}
          >
            All ({gifts.length})
          </button>
        </div>
      </div>

      {error && (
        <div className="rounded-md bg-red-50 p-4">
          <div className="text-sm text-red-800">{error}</div>
        </div>
      )}

      {/* Approve Modal */}
      {showApproveModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 max-w-md w-full mx-4">
            <h4 className="text-lg font-medium text-gray-900 mb-4">Approve Gift</h4>
            <p className="text-sm text-gray-600 mb-4">
              Approve {formatCurrency(showApproveModal.amount)} gift from {showApproveModal.giverName}?
            </p>

            {goals.length > 0 && (
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  <Target className="w-4 h-4 inline mr-1" />
                  Allocate to savings goal (optional)
                </label>
                <select
                  value={approvalData.allocateToGoalId || ''}
                  onChange={(e) => setApprovalData({ ...approvalData, allocateToGoalId: e.target.value || undefined })}
                  className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                >
                  <option value="">Add to spending balance</option>
                  {goals.map(goal => (
                    <option key={goal.id} value={goal.id}>
                      {goal.name} ({Math.round(goal.progressPercentage)}% complete)
                    </option>
                  ))}
                </select>
              </div>
            )}

            {!approvalData.allocateToGoalId && (
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  <PiggyBank className="w-4 h-4 inline mr-1" />
                  Savings percentage (optional)
                </label>
                <div className="flex items-center space-x-2">
                  <input
                    type="number"
                    min="0"
                    max="100"
                    value={approvalData.savingsPercentage || ''}
                    onChange={(e) => setApprovalData({ ...approvalData, savingsPercentage: e.target.value ? parseInt(e.target.value) : undefined })}
                    className="block w-24 px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                    placeholder="0"
                  />
                  <span className="text-sm text-gray-500">% to savings</span>
                </div>
                <p className="mt-1 text-xs text-gray-500">
                  Leave empty to add full amount to spending balance
                </p>
              </div>
            )}

            <div className="flex justify-end space-x-3 mt-6">
              <button
                onClick={() => {
                  setShowApproveModal(null);
                  setApprovalData({});
                }}
                className="px-4 py-2 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleApprove}
                disabled={processingId === showApproveModal.id}
                className="px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 disabled:opacity-50"
              >
                {processingId === showApproveModal.id ? 'Approving...' : 'Approve Gift'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Reject Modal */}
      {showRejectModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 max-w-md w-full mx-4">
            <h4 className="text-lg font-medium text-gray-900 mb-4">Reject Gift</h4>
            <p className="text-sm text-gray-600 mb-4">
              Reject {formatCurrency(showRejectModal.amount)} gift from {showRejectModal.giverName}?
            </p>

            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Reason (optional)
              </label>
              <textarea
                value={rejectReason}
                onChange={(e) => setRejectReason(e.target.value)}
                rows={3}
                className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                placeholder="Why is this gift being rejected?"
              />
            </div>

            <div className="flex justify-end space-x-3 mt-6">
              <button
                onClick={() => {
                  setShowRejectModal(null);
                  setRejectReason('');
                }}
                className="px-4 py-2 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleReject}
                disabled={processingId === showRejectModal.id}
                className="px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-red-600 hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 disabled:opacity-50"
              >
                {processingId === showRejectModal.id ? 'Rejecting...' : 'Reject Gift'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Gifts List */}
      {filteredGifts.length === 0 ? (
        <div className="text-center py-12 bg-white rounded-lg border border-gray-200">
          <Gift className="mx-auto h-12 w-12 text-gray-400" />
          <h3 className="mt-2 text-sm font-medium text-gray-900">
            {filter === 'pending' ? 'No pending gifts' : 'No gifts yet'}
          </h3>
          <p className="mt-1 text-sm text-gray-500">
            {filter === 'pending'
              ? 'All gifts have been reviewed!'
              : 'Share a gift link to receive gifts from family members.'}
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {filteredGifts.map((gift) => (
            <div
              key={gift.id}
              className={`bg-white rounded-lg shadow-sm border-2 p-6 ${
                gift.status === 'Pending' ? 'border-yellow-200' : 'border-gray-200'
              }`}
            >
              <div className="flex justify-between items-start">
                <div className="flex-1">
                  <div className="flex items-center space-x-3 mb-2">
                    <h4 className="text-lg font-medium text-gray-900">
                      {formatCurrency(gift.amount)}
                    </h4>
                    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${statusColors[gift.status]}`}>
                      {gift.status === 'Pending' && <Clock className="w-3 h-3 mr-1" />}
                      {gift.status === 'Approved' && <CheckCircle className="w-3 h-3 mr-1" />}
                      {gift.status === 'Rejected' && <XCircle className="w-3 h-3 mr-1" />}
                      {gift.status}
                    </span>
                  </div>

                  <div className="space-y-1 text-sm">
                    <p className="text-gray-600">
                      <span className="font-medium">From:</span> {gift.giverName}
                      {gift.giverRelationship && ` (${gift.giverRelationship})`}
                    </p>
                    <p className="text-gray-600">
                      <span className="font-medium">Occasion:</span>{' '}
                      {gift.customOccasion || occasionLabels[gift.occasion] || gift.occasion}
                    </p>
                    {gift.message && (
                      <p className="text-gray-600 italic">"{gift.message}"</p>
                    )}
                    <p className="text-gray-400 text-xs">
                      Received {formatDate(gift.createdAt)}
                    </p>
                  </div>

                  {gift.status === 'Approved' && gift.allocatedToGoalName && (
                    <div className="mt-2 inline-flex items-center px-2 py-1 bg-blue-50 rounded text-xs text-blue-700">
                      <Target className="w-3 h-3 mr-1" />
                      Allocated to: {gift.allocatedToGoalName}
                    </div>
                  )}

                  {gift.status === 'Rejected' && gift.rejectionReason && (
                    <div className="mt-2 inline-flex items-center px-2 py-1 bg-red-50 rounded text-xs text-red-700">
                      Reason: {gift.rejectionReason}
                    </div>
                  )}

                  {gift.hasThankYouNote && (
                    <div className="mt-2 inline-flex items-center px-2 py-1 bg-green-50 rounded text-xs text-green-700">
                      Thank you note sent
                    </div>
                  )}
                </div>

                {/* Actions for pending gifts */}
                {gift.status === 'Pending' && (
                  <div className="flex space-x-2 ml-4">
                    <button
                      onClick={() => setShowApproveModal(gift)}
                      className="inline-flex items-center px-3 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-green-600 hover:bg-green-700"
                    >
                      <CheckCircle className="w-4 h-4 mr-1" />
                      Approve
                    </button>
                    <button
                      onClick={() => setShowRejectModal(gift)}
                      className="inline-flex items-center px-3 py-2 border border-red-300 shadow-sm text-sm font-medium rounded-md text-red-700 bg-white hover:bg-red-50"
                    >
                      <XCircle className="w-4 h-4 mr-1" />
                      Reject
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
