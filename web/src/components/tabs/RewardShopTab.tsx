import React, { useEffect, useState, useCallback } from 'react';
import { rewardsApi, badgesApi } from '../../services/api';
import type { RewardDto, RewardType, ChildPointsDto } from '../../types';

interface RewardShopTabProps {
  childId: string;
}

const REWARD_TYPES: { id: RewardType | 'all'; label: string; icon: string }[] = [
  { id: 'all', label: 'All', icon: '🎁' },
  { id: 'Avatar', label: 'Avatars', icon: '👤' },
  { id: 'Theme', label: 'Themes', icon: '🎨' },
  { id: 'Title', label: 'Titles', icon: '📝' },
  { id: 'Special', label: 'Special', icon: '⭐' },
];

const TYPE_COLORS: Record<RewardType, { bg: string; text: string; border: string }> = {
  Avatar: { bg: 'bg-blue-100', text: 'text-blue-700', border: 'border-blue-300' },
  Theme: { bg: 'bg-purple-100', text: 'text-purple-700', border: 'border-purple-300' },
  Title: { bg: 'bg-orange-100', text: 'text-orange-700', border: 'border-orange-300' },
  Special: { bg: 'bg-pink-100', text: 'text-pink-700', border: 'border-pink-300' },
};

export const RewardShopTab: React.FC<RewardShopTabProps> = ({ childId }) => {
  const [availableRewards, setAvailableRewards] = useState<RewardDto[]>([]);
  const [unlockedRewards, setUnlockedRewards] = useState<RewardDto[]>([]);
  const [points, setPoints] = useState<ChildPointsDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isProcessing, setIsProcessing] = useState<string | null>(null);
  const [error, setError] = useState<string>('');
  const [success, setSuccess] = useState<string>('');
  const [selectedType, setSelectedType] = useState<RewardType | 'all'>('all');
  const [showUnlockModal, setShowUnlockModal] = useState<RewardDto | null>(null);

  const loadRewards = useCallback(async () => {
    try {
      setIsLoading(true);
      setError('');

      const [available, unlocked, pointsData] = await Promise.all([
        rewardsApi.getAvailable(selectedType === 'all' ? undefined : selectedType, childId),
        rewardsApi.getChildRewards(childId),
        badgesApi.getChildPoints(childId),
      ]);

      setAvailableRewards(available);
      setUnlockedRewards(unlocked);
      setPoints(pointsData);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to load rewards');
    } finally {
      setIsLoading(false);
    }
  }, [childId, selectedType]);

  useEffect(() => {
    loadRewards();
  }, [loadRewards]);

  const handleUnlock = async (reward: RewardDto) => {
    setIsProcessing(reward.id);
    setError('');
    setSuccess('');

    try {
      const unlocked = await rewardsApi.unlock(childId, reward.id);
      setUnlockedRewards((prev) => [...prev, unlocked]);
      setAvailableRewards((prev) =>
        prev.map((r) => (r.id === reward.id ? { ...r, isUnlocked: true } : r))
      );
      setPoints(await badgesApi.getChildPoints(childId));
      setSuccess(`Unlocked ${reward.name}!`);
      setShowUnlockModal(null);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to unlock reward');
    } finally {
      setIsProcessing(null);
    }
  };

  const handleEquip = async (reward: RewardDto) => {
    setIsProcessing(reward.id);
    setError('');

    try {
      const equipped = await rewardsApi.equip(childId, reward.id);
      setUnlockedRewards((prev) =>
        prev.map((r) => ({
          ...r,
          isEquipped: r.id === reward.id ? true : r.type === reward.type ? false : r.isEquipped,
        }))
      );
      setSuccess(`Equipped ${equipped.name}!`);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to equip reward');
    } finally {
      setIsProcessing(null);
    }
  };

  const handleUnequip = async (reward: RewardDto) => {
    setIsProcessing(reward.id);
    setError('');

    try {
      await rewardsApi.unequip(childId, reward.id);
      setUnlockedRewards((prev) =>
        prev.map((r) => (r.id === reward.id ? { ...r, isEquipped: false } : r))
      );
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to unequip reward');
    } finally {
      setIsProcessing(null);
    }
  };

  const filteredRewards = selectedType === 'all'
    ? availableRewards
    : availableRewards.filter((r) => r.type === selectedType);

  if (isLoading) {
    return (
      <div className="flex justify-center items-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-500"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Error/Success Messages */}
      {error && (
        <div className="rounded-md bg-red-50 p-4">
          <div className="flex">
            <div className="flex-shrink-0">
              <svg className="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
            </div>
            <div className="ml-3">
              <p className="text-sm text-red-800">{error}</p>
            </div>
          </div>
        </div>
      )}

      {success && (
        <div className="rounded-md bg-green-50 p-4">
          <div className="flex">
            <div className="flex-shrink-0">
              <svg className="h-5 w-5 text-green-400" viewBox="0 0 20 20" fill="currentColor">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
              </svg>
            </div>
            <div className="ml-3">
              <p className="text-sm text-green-800">{success}</p>
            </div>
            <div className="ml-auto pl-3">
              <button
                onClick={() => setSuccess('')}
                className="inline-flex text-green-500 hover:text-green-600"
              >
                <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
                </svg>
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Points Balance */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="bg-gradient-to-br from-yellow-50 to-amber-100 rounded-lg p-4 border border-yellow-200">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-yellow-200 rounded-full">
              <svg className="w-6 h-6 text-yellow-700" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <div>
              <p className="text-sm text-yellow-700">Available Points</p>
              <p className="text-2xl font-bold text-yellow-900">{points?.availablePoints ?? 0}</p>
            </div>
          </div>
        </div>

        <div className="bg-gradient-to-br from-purple-50 to-indigo-100 rounded-lg p-4 border border-purple-200">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-purple-200 rounded-full">
              <svg className="w-6 h-6 text-purple-700" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v13m0-13V6a2 2 0 112 2h-2zm0 0V5.5A2.5 2.5 0 109.5 8H12zm-7 4h14M5 12a2 2 0 110-4h14a2 2 0 110 4M5 12v7a2 2 0 002 2h10a2 2 0 002-2v-7" />
              </svg>
            </div>
            <div>
              <p className="text-sm text-purple-700">Rewards Unlocked</p>
              <p className="text-2xl font-bold text-purple-900">{unlockedRewards.length}</p>
            </div>
          </div>
        </div>
      </div>

      {/* Type Filter */}
      <div className="border-b border-gray-200">
        <nav className="-mb-px flex space-x-4 overflow-x-auto" aria-label="Reward Types">
          {REWARD_TYPES.map((type) => (
            <button
              key={type.id}
              onClick={() => setSelectedType(type.id)}
              className={`whitespace-nowrap py-2 px-3 border-b-2 font-medium text-sm flex items-center gap-1 ${
                selectedType === type.id
                  ? 'border-purple-500 text-purple-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              }`}
            >
              <span>{type.icon}</span>
              {type.label}
            </button>
          ))}
        </nav>
      </div>

      {/* My Rewards (Unlocked) */}
      {unlockedRewards.length > 0 && (
        <div>
          <h3 className="text-lg font-medium text-gray-900 mb-4">My Rewards</h3>
          <div className="flex gap-4 overflow-x-auto pb-2">
            {unlockedRewards.map((reward) => (
              <UnlockedRewardCard
                key={reward.id}
                reward={reward}
                isProcessing={isProcessing === reward.id}
                onEquip={() => handleEquip(reward)}
                onUnequip={() => handleUnequip(reward)}
              />
            ))}
          </div>
        </div>
      )}

      {/* Available Rewards */}
      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4">Available Rewards</h3>
        {filteredRewards.length === 0 ? (
          <div className="text-center py-8 bg-white rounded-lg border border-gray-200">
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
                d="M12 8v13m0-13V6a2 2 0 112 2h-2zm0 0V5.5A2.5 2.5 0 109.5 8H12zm-7 4h14M5 12a2 2 0 110-4h14a2 2 0 110 4M5 12v7a2 2 0 002 2h10a2 2 0 002-2v-7"
              />
            </svg>
            <h4 className="mt-2 text-sm font-medium text-gray-900">No rewards available</h4>
            <p className="mt-1 text-sm text-gray-500">
              Check back later for new rewards!
            </p>
          </div>
        ) : (
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
            {filteredRewards.map((reward) => (
              <RewardCard
                key={reward.id}
                reward={reward}
                isProcessing={isProcessing === reward.id}
                onUnlock={() => setShowUnlockModal(reward)}
              />
            ))}
          </div>
        )}
      </div>

      {/* Unlock Confirmation Modal */}
      {showUnlockModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg max-w-md w-full p-6">
            <h3 className="text-lg font-medium text-gray-900 mb-4">Unlock Reward</h3>
            <p className="text-sm text-gray-600 mb-4">
              Spend <span className="font-bold text-yellow-600">{showUnlockModal.pointsCost} points</span> to unlock{' '}
              <span className="font-bold">{showUnlockModal.name}</span>?
            </p>
            <div className="flex justify-end gap-3">
              <button
                onClick={() => setShowUnlockModal(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200"
                disabled={isProcessing === showUnlockModal.id}
              >
                Cancel
              </button>
              <button
                onClick={() => handleUnlock(showUnlockModal)}
                className="px-4 py-2 text-sm font-medium text-white bg-purple-600 rounded-md hover:bg-purple-700 disabled:opacity-50"
                disabled={isProcessing === showUnlockModal.id}
              >
                {isProcessing === showUnlockModal.id ? 'Unlocking...' : 'Unlock'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

// Reward Card Component
const RewardCard: React.FC<{
  reward: RewardDto;
  isProcessing: boolean;
  onUnlock: () => void;
}> = ({ reward, isProcessing, onUnlock }) => {
  const styles = TYPE_COLORS[reward.type];

  return (
    <div className={`relative bg-white rounded-lg border-2 ${styles.border} p-4 text-center`}>
      {reward.isUnlocked && (
        <span className="absolute -top-2 -right-2 inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-green-500 text-white">
          Owned
        </span>
      )}

      {/* Reward Icon */}
      <div className={`mx-auto w-16 h-16 rounded-full ${styles.bg} flex items-center justify-center mb-3`}>
        {reward.previewUrl ? (
          <img src={reward.previewUrl} alt={reward.name} className="w-10 h-10 rounded-full" />
        ) : (
          <span className="text-2xl">
            {reward.type === 'Avatar' && '👤'}
            {reward.type === 'Theme' && '🎨'}
            {reward.type === 'Title' && '📝'}
            {reward.type === 'Special' && '⭐'}
          </span>
        )}
      </div>

      {/* Reward Name */}
      <h4 className="text-sm font-medium text-gray-900 mb-1">{reward.name}</h4>

      {/* Type Tag */}
      <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${styles.bg} ${styles.text}`}>
        {reward.typeName}
      </span>

      {/* Description */}
      <p className="mt-2 text-xs text-gray-500 line-clamp-2">{reward.description}</p>

      {/* Price/Unlock Button */}
      <div className="mt-3">
        {reward.isUnlocked ? (
          <span className="text-xs text-green-600 font-medium">✓ Unlocked</span>
        ) : (
          <button
            onClick={onUnlock}
            disabled={!reward.canAfford || isProcessing}
            className={`w-full px-3 py-2 text-sm font-medium rounded-md ${
              reward.canAfford
                ? 'bg-purple-600 text-white hover:bg-purple-700'
                : 'bg-gray-200 text-gray-500 cursor-not-allowed'
            } disabled:opacity-50`}
          >
            {isProcessing ? (
              'Unlocking...'
            ) : (
              <>
                <span className="mr-1">⭐</span>
                {reward.pointsCost} points
              </>
            )}
          </button>
        )}
      </div>
    </div>
  );
};

// Unlocked Reward Card Component
const UnlockedRewardCard: React.FC<{
  reward: RewardDto;
  isProcessing: boolean;
  onEquip: () => void;
  onUnequip: () => void;
}> = ({ reward, isProcessing, onEquip, onUnequip }) => {
  const styles = TYPE_COLORS[reward.type];

  return (
    <div
      className={`flex-shrink-0 w-24 bg-white rounded-lg border-2 ${
        reward.isEquipped ? 'border-green-500 ring-2 ring-green-200' : styles.border
      } p-3 text-center`}
    >
      {/* Preview */}
      <div className={`mx-auto w-12 h-12 rounded-full ${styles.bg} flex items-center justify-center mb-2`}>
        {reward.previewUrl ? (
          <img src={reward.previewUrl} alt={reward.name} className="w-8 h-8 rounded-full" />
        ) : (
          <span className="text-xl">
            {reward.type === 'Avatar' && '👤'}
            {reward.type === 'Theme' && '🎨'}
            {reward.type === 'Title' && '📝'}
            {reward.type === 'Special' && '⭐'}
          </span>
        )}
      </div>

      {/* Name */}
      <h4 className="text-xs font-medium text-gray-900 truncate">{reward.name}</h4>

      {/* Equip Button */}
      <button
        onClick={reward.isEquipped ? onUnequip : onEquip}
        disabled={isProcessing}
        className={`mt-2 w-full px-2 py-1 text-xs font-medium rounded ${
          reward.isEquipped
            ? 'bg-green-500 text-white hover:bg-green-600'
            : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
        } disabled:opacity-50`}
      >
        {isProcessing ? '...' : reward.isEquipped ? 'Equipped' : 'Equip'}
      </button>
    </div>
  );
};
