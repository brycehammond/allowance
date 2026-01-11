import React, { useEffect, useState, useCallback } from 'react';
import { badgesApi } from '../../services/api';
import type { AchievementSummaryDto, ChildBadgeDto, BadgeProgressDto, BadgeCategory } from '../../types';

interface BadgesTabProps {
  childId: string;
}

const CATEGORIES: { id: BadgeCategory | 'all'; label: string }[] = [
  { id: 'all', label: 'All' },
  { id: 'Saving', label: 'Saving' },
  { id: 'Spending', label: 'Spending' },
  { id: 'Goals', label: 'Goals' },
  { id: 'Chores', label: 'Chores' },
  { id: 'Streaks', label: 'Streaks' },
  { id: 'Milestones', label: 'Milestones' },
  { id: 'Special', label: 'Special' },
];

const RARITY_STYLES: Record<string, { bg: string; text: string; border: string; glow?: string }> = {
  Common: { bg: 'bg-gray-100', text: 'text-gray-700', border: 'border-gray-300' },
  Uncommon: { bg: 'bg-green-100', text: 'text-green-700', border: 'border-green-300' },
  Rare: { bg: 'bg-blue-100', text: 'text-blue-700', border: 'border-blue-300' },
  Epic: { bg: 'bg-purple-100', text: 'text-purple-700', border: 'border-purple-300' },
  Legendary: { bg: 'bg-yellow-100', text: 'text-yellow-700', border: 'border-yellow-400', glow: 'shadow-lg shadow-yellow-200' },
};

export const BadgesTab: React.FC<BadgesTabProps> = ({ childId }) => {
  const [summary, setSummary] = useState<AchievementSummaryDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [selectedCategory, setSelectedCategory] = useState<BadgeCategory | 'all'>('all');

  const loadBadges = useCallback(async () => {
    try {
      setIsLoading(true);
      const data = await badgesApi.getAchievementSummary(childId);
      setSummary(data);

      // Mark new badges as seen
      const newBadgeIds = data.recentBadges
        .filter((b: ChildBadgeDto) => b.isNew)
        .map((b: ChildBadgeDto) => b.badgeId);
      if (newBadgeIds.length > 0) {
        await badgesApi.markBadgesSeen(childId, { badgeIds: newBadgeIds });
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined;
      setError(errorMessage || 'Failed to load achievements');
    } finally {
      setIsLoading(false);
    }
  }, [childId]);

  useEffect(() => {
    loadBadges();
  }, [loadBadges]);

  const filteredEarnedBadges = summary?.recentBadges.filter(
    (badge) => selectedCategory === 'all' || badge.category === selectedCategory
  ) ?? [];

  const filteredProgressBadges = summary?.inProgressBadges.filter(
    (badge) => selectedCategory === 'all' || badge.category === selectedCategory
  ) ?? [];

  if (isLoading) {
    return (
      <div className="flex justify-center items-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-500"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="rounded-md bg-red-50 p-4">
        <div className="text-sm text-red-800">{error}</div>
      </div>
    );
  }

  if (!summary) {
    return null;
  }

  return (
    <div className="space-y-6">
      {/* Points Summary */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-gradient-to-br from-yellow-50 to-amber-100 rounded-lg p-4 border border-yellow-200">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-yellow-200 rounded-full">
              <svg className="w-6 h-6 text-yellow-700" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <div>
              <p className="text-sm text-yellow-700">Total Points</p>
              <p className="text-2xl font-bold text-yellow-900">{summary.totalPoints}</p>
            </div>
          </div>
        </div>

        <div className="bg-gradient-to-br from-green-50 to-emerald-100 rounded-lg p-4 border border-green-200">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-green-200 rounded-full">
              <svg className="w-6 h-6 text-green-700" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <div>
              <p className="text-sm text-green-700">Badges Earned</p>
              <p className="text-2xl font-bold text-green-900">{summary.earnedBadges} / {summary.totalBadges}</p>
            </div>
          </div>
        </div>

        <div className="bg-gradient-to-br from-blue-50 to-indigo-100 rounded-lg p-4 border border-blue-200">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-blue-200 rounded-full">
              <svg className="w-6 h-6 text-blue-700" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
              </svg>
            </div>
            <div>
              <p className="text-sm text-blue-700">Available Points</p>
              <p className="text-2xl font-bold text-blue-900">{summary.availablePoints}</p>
            </div>
          </div>
        </div>
      </div>

      {/* Category Filter */}
      <div className="border-b border-gray-200">
        <nav className="-mb-px flex space-x-4 overflow-x-auto" aria-label="Categories">
          {CATEGORIES.map((category) => (
            <button
              key={category.id}
              onClick={() => setSelectedCategory(category.id)}
              className={`whitespace-nowrap py-2 px-3 border-b-2 font-medium text-sm ${
                selectedCategory === category.id
                  ? 'border-primary-500 text-primary-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              }`}
            >
              {category.label}
              {category.id !== 'all' && summary.badgesByCategory[category.id] !== undefined && (
                <span className="ml-1 text-xs text-gray-400">
                  ({summary.badgesByCategory[category.id] || 0})
                </span>
              )}
            </button>
          ))}
        </nav>
      </div>

      {/* Earned Badges Section */}
      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4">Earned Badges</h3>
        {filteredEarnedBadges.length === 0 ? (
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
                d="M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z"
              />
            </svg>
            <h4 className="mt-2 text-sm font-medium text-gray-900">No badges earned yet</h4>
            <p className="mt-1 text-sm text-gray-500">
              Complete activities to earn badges and points!
            </p>
          </div>
        ) : (
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
            {filteredEarnedBadges.map((badge) => (
              <BadgeCard key={badge.id} badge={badge} />
            ))}
          </div>
        )}
      </div>

      {/* In Progress Section */}
      {filteredProgressBadges.length > 0 && (
        <div>
          <h3 className="text-lg font-medium text-gray-900 mb-4">In Progress</h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {filteredProgressBadges.map((badge) => (
              <ProgressCard key={badge.badgeId} badge={badge} />
            ))}
          </div>
        </div>
      )}
    </div>
  );
};

// Badge Card Component
const BadgeCard: React.FC<{ badge: ChildBadgeDto }> = ({ badge }) => {
  const styles = RARITY_STYLES[badge.rarityName] || RARITY_STYLES.Common;

  return (
    <div className={`relative bg-white rounded-lg border-2 ${styles.border} p-4 text-center ${styles.glow || ''}`}>
      {badge.isNew && (
        <span className="absolute -top-2 -right-2 inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-red-500 text-white">
          New!
        </span>
      )}

      {/* Badge Icon */}
      <div className={`mx-auto w-16 h-16 rounded-full ${styles.bg} flex items-center justify-center mb-3`}>
        {badge.iconUrl ? (
          <img src={badge.iconUrl} alt={badge.badgeName} className="w-10 h-10" />
        ) : (
          <svg className={`w-8 h-8 ${styles.text}`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z"
            />
          </svg>
        )}
      </div>

      {/* Badge Name */}
      <h4 className="text-sm font-medium text-gray-900 mb-1">{badge.badgeName}</h4>

      {/* Rarity Tag */}
      <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${styles.bg} ${styles.text}`}>
        {badge.rarityName}
      </span>

      {/* Points Value */}
      <p className="mt-2 text-xs text-gray-500">+{badge.pointsValue} points</p>

      {/* Earned Date */}
      <p className="text-xs text-gray-400 mt-1">
        {new Date(badge.earnedAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
      </p>
    </div>
  );
};

// Progress Card Component
const ProgressCard: React.FC<{ badge: BadgeProgressDto }> = ({ badge }) => {
  const styles = RARITY_STYLES[badge.rarityName] || RARITY_STYLES.Common;
  const progressPercent = Math.min(badge.progressPercentage, 100);

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-4">
      <div className="flex items-start gap-4">
        {/* Badge Icon */}
        <div className={`w-12 h-12 rounded-full ${styles.bg} flex items-center justify-center flex-shrink-0`}>
          {badge.iconUrl ? (
            <img src={badge.iconUrl} alt={badge.badgeName} className="w-7 h-7 opacity-50" />
          ) : (
            <svg className={`w-6 h-6 ${styles.text} opacity-50`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z"
              />
            </svg>
          )}
        </div>

        {/* Badge Info */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center justify-between mb-1">
            <h4 className="text-sm font-medium text-gray-900 truncate">{badge.badgeName}</h4>
            <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${styles.bg} ${styles.text}`}>
              {badge.rarityName}
            </span>
          </div>

          <p className="text-xs text-gray-500 mb-2">{badge.description}</p>

          {/* Progress Bar */}
          <div className="flex items-center gap-2">
            <div className="flex-1 bg-gray-200 rounded-full h-2">
              <div
                className={`h-2 rounded-full transition-all duration-300 ${
                  badge.rarityName === 'Legendary' ? 'bg-yellow-500' :
                  badge.rarityName === 'Epic' ? 'bg-purple-500' :
                  badge.rarityName === 'Rare' ? 'bg-blue-500' :
                  badge.rarityName === 'Uncommon' ? 'bg-green-500' :
                  'bg-gray-500'
                }`}
                style={{ width: `${progressPercent}%` }}
              />
            </div>
            <span className="text-xs text-gray-600 whitespace-nowrap">{badge.progressText}</span>
          </div>

          {/* Points Value */}
          <p className="text-xs text-gray-400 mt-1">+{badge.pointsValue} points when earned</p>
        </div>
      </div>
    </div>
  );
};
