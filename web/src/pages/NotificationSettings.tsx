import React, { useState, useEffect, useCallback } from 'react';
import { Layout } from '../components/Layout';
import { notificationsApi } from '../services/api';
import type { NotificationPreferences, NotificationPreferenceItem, NotificationType } from '../types';
import {
  Bell,
  BellOff,
  Smartphone,
  Mail,
  Clock,
  Save,
  Loader2,
  AlertCircle,
  CheckCircle,
  ChevronDown,
  ChevronUp,
} from 'lucide-react';

// Group notification types by category for better UX
const NOTIFICATION_CATEGORIES: Record<string, { label: string; description: string; types: number[] }> = {
  'Balance & Transactions': {
    label: 'Balance & Transactions',
    description: 'Alerts about balance changes and new transactions',
    types: [1, 2, 3], // BalanceAlert, LowBalanceWarning, TransactionCreated
  },
  'Allowance': {
    label: 'Allowance',
    description: 'Notifications about allowance deposits and changes',
    types: [10, 11, 12], // AllowanceDeposit, AllowancePaused, AllowanceResumed
  },
  'Goals & Savings': {
    label: 'Goals & Savings',
    description: 'Updates on savings goals progress and milestones',
    types: [20, 21, 22, 23], // GoalProgress, GoalMilestone, GoalCompleted, ParentMatchAdded
  },
  'Chores & Tasks': {
    label: 'Chores & Tasks',
    description: 'Task assignments, reminders, and approval notifications',
    types: [30, 31, 32, 33, 34, 35], // TaskAssigned through TaskRejected
  },
  'Budget': {
    label: 'Budget',
    description: 'Warnings when approaching or exceeding budgets',
    types: [40, 41], // BudgetWarning, BudgetExceeded
  },
  'Achievements': {
    label: 'Achievements',
    description: 'Badge unlocks and streak updates',
    types: [50, 51], // AchievementUnlocked, StreakUpdate
  },
  'Family': {
    label: 'Family',
    description: 'Family invites and additions',
    types: [60, 61, 62], // FamilyInvite, ChildAdded, GiftReceived
  },
  'Reports': {
    label: 'Reports',
    description: 'Weekly and monthly summary reports',
    types: [70, 71], // WeeklySummary, MonthlySummary
  },
};

export const NotificationSettings: React.FC = () => {
  const [preferences, setPreferences] = useState<NotificationPreferences | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [expandedCategories, setExpandedCategories] = useState<Set<string>>(new Set());

  // Quiet hours state
  const [quietHoursEnabled, setQuietHoursEnabled] = useState(false);
  const [quietHoursStart, setQuietHoursStart] = useState('22:00');
  const [quietHoursEnd, setQuietHoursEnd] = useState('07:00');

  // Track modified preferences
  const [modifiedPrefs, setModifiedPrefs] = useState<Map<NotificationType, NotificationPreferenceItem>>(new Map());

  const loadPreferences = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);
      const data = await notificationsApi.getPreferences();
      setPreferences(data);
      setQuietHoursEnabled(data.quietHoursEnabled);
      if (data.quietHoursStart) setQuietHoursStart(data.quietHoursStart.slice(0, 5));
      if (data.quietHoursEnd) setQuietHoursEnd(data.quietHoursEnd.slice(0, 5));
    } catch (err) {
      console.error('Failed to load notification preferences:', err);
      setError('Failed to load notification preferences');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadPreferences();
  }, [loadPreferences]);

  const toggleCategory = (category: string) => {
    setExpandedCategories(prev => {
      const next = new Set(prev);
      if (next.has(category)) {
        next.delete(category);
      } else {
        next.add(category);
      }
      return next;
    });
  };

  const getPreferenceByType = (type: NotificationType): NotificationPreferenceItem | undefined => {
    // Check modified first
    if (modifiedPrefs.has(type)) {
      return modifiedPrefs.get(type);
    }
    // Fall back to loaded preferences
    return preferences?.preferences.find(p => p.notificationType === type);
  };

  const updatePreference = (
    type: NotificationType,
    channel: 'inAppEnabled' | 'pushEnabled' | 'emailEnabled',
    value: boolean
  ) => {
    const current = getPreferenceByType(type);
    if (!current) return;

    const updated = { ...current, [channel]: value };
    setModifiedPrefs(prev => new Map(prev).set(type, updated));
    setSuccess(null);
  };

  const toggleAllInCategory = (categoryTypes: number[], channel: 'inAppEnabled' | 'pushEnabled' | 'emailEnabled', enable: boolean) => {
    const newModified = new Map(modifiedPrefs);
    categoryTypes.forEach(type => {
      const current = getPreferenceByType(type as NotificationType);
      if (current) {
        newModified.set(type as NotificationType, { ...current, [channel]: enable });
      }
    });
    setModifiedPrefs(newModified);
    setSuccess(null);
  };

  const handleSave = async () => {
    try {
      setIsSaving(true);
      setError(null);
      setSuccess(null);

      // Save preference changes if any
      if (modifiedPrefs.size > 0) {
        const prefsToSave = Array.from(modifiedPrefs.values()).map(p => ({
          notificationType: p.notificationType,
          inAppEnabled: p.inAppEnabled,
          pushEnabled: p.pushEnabled,
          emailEnabled: p.emailEnabled,
        }));
        await notificationsApi.updatePreferences({ preferences: prefsToSave });
      }

      // Save quiet hours
      await notificationsApi.updateQuietHours({
        enabled: quietHoursEnabled,
        startTime: quietHoursEnabled ? quietHoursStart : undefined,
        endTime: quietHoursEnabled ? quietHoursEnd : undefined,
      });

      // Clear modified and reload
      setModifiedPrefs(new Map());
      await loadPreferences();
      setSuccess('Settings saved successfully!');
    } catch (err) {
      console.error('Failed to save preferences:', err);
      setError('Failed to save settings. Please try again.');
    } finally {
      setIsSaving(false);
    }
  };

  const hasChanges = modifiedPrefs.size > 0 ||
    quietHoursEnabled !== preferences?.quietHoursEnabled ||
    (quietHoursEnabled && (
      quietHoursStart !== preferences?.quietHoursStart?.slice(0, 5) ||
      quietHoursEnd !== preferences?.quietHoursEnd?.slice(0, 5)
    ));

  if (isLoading) {
    return (
      <Layout>
        <div className="flex items-center justify-center min-h-[400px]">
          <Loader2 className="h-8 w-8 animate-spin text-primary-600" />
        </div>
      </Layout>
    );
  }

  return (
    <Layout>
      <div className="max-w-3xl mx-auto">
        <div className="mb-6">
          <h1 className="text-2xl font-bold text-gray-900">Notification Settings</h1>
          <p className="mt-1 text-sm text-gray-600">
            Control how and when you receive notifications
          </p>
        </div>

        {error && (
          <div className="mb-6 rounded-md bg-red-50 p-4 flex items-start">
            <AlertCircle className="h-5 w-5 text-red-400 mt-0.5 mr-3 flex-shrink-0" />
            <p className="text-sm text-red-800">{error}</p>
          </div>
        )}

        {success && (
          <div className="mb-6 rounded-md bg-green-50 p-4 flex items-start">
            <CheckCircle className="h-5 w-5 text-green-400 mt-0.5 mr-3 flex-shrink-0" />
            <p className="text-sm text-green-800">{success}</p>
          </div>
        )}

        {/* Quiet Hours */}
        <div className="bg-white shadow-sm rounded-lg p-6 mb-6">
          <div className="flex items-center justify-between">
            <div className="flex items-center">
              <Clock className="h-5 w-5 text-primary-500 mr-3" />
              <div>
                <h2 className="text-lg font-medium text-gray-900">Quiet Hours</h2>
                <p className="text-sm text-gray-500">
                  Pause push notifications during specific hours
                </p>
              </div>
            </div>
            <button
              type="button"
              onClick={() => setQuietHoursEnabled(!quietHoursEnabled)}
              className={`relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 ${
                quietHoursEnabled ? 'bg-primary-600' : 'bg-gray-200'
              }`}
            >
              <span
                className={`pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out ${
                  quietHoursEnabled ? 'translate-x-5' : 'translate-x-0'
                }`}
              />
            </button>
          </div>

          {quietHoursEnabled && (
            <div className="mt-4 grid grid-cols-2 gap-4">
              <div>
                <label htmlFor="quietStart" className="block text-sm font-medium text-gray-700 mb-1">
                  Start Time
                </label>
                <input
                  type="time"
                  id="quietStart"
                  value={quietHoursStart}
                  onChange={(e) => setQuietHoursStart(e.target.value)}
                  className="block w-full rounded-md border-gray-300 shadow-sm focus:border-primary-500 focus:ring-primary-500 sm:text-sm"
                />
              </div>
              <div>
                <label htmlFor="quietEnd" className="block text-sm font-medium text-gray-700 mb-1">
                  End Time
                </label>
                <input
                  type="time"
                  id="quietEnd"
                  value={quietHoursEnd}
                  onChange={(e) => setQuietHoursEnd(e.target.value)}
                  className="block w-full rounded-md border-gray-300 shadow-sm focus:border-primary-500 focus:ring-primary-500 sm:text-sm"
                />
              </div>
            </div>
          )}
        </div>

        {/* Notification Categories */}
        <div className="bg-white shadow-sm rounded-lg overflow-hidden">
          <div className="px-6 py-4 border-b border-gray-200">
            <div className="flex items-center">
              <Bell className="h-5 w-5 text-primary-500 mr-3" />
              <div>
                <h2 className="text-lg font-medium text-gray-900">Notification Preferences</h2>
                <p className="text-sm text-gray-500">
                  Choose which notifications you want to receive
                </p>
              </div>
            </div>
          </div>

          {/* Channel headers */}
          <div className="px-6 py-3 bg-gray-50 border-b border-gray-200 hidden sm:grid sm:grid-cols-12 gap-4">
            <div className="col-span-6"></div>
            <div className="col-span-2 text-center">
              <div className="flex items-center justify-center text-sm font-medium text-gray-700">
                <Bell className="h-4 w-4 mr-1" />
                In-App
              </div>
            </div>
            <div className="col-span-2 text-center">
              <div className="flex items-center justify-center text-sm font-medium text-gray-700">
                <Smartphone className="h-4 w-4 mr-1" />
                Push
              </div>
            </div>
            <div className="col-span-2 text-center">
              <div className="flex items-center justify-center text-sm font-medium text-gray-700">
                <Mail className="h-4 w-4 mr-1" />
                Email
              </div>
            </div>
          </div>

          {/* Categories */}
          <div className="divide-y divide-gray-200">
            {Object.entries(NOTIFICATION_CATEGORIES).map(([categoryKey, category]) => {
              const categoryPrefs = category.types
                .map(t => getPreferenceByType(t as NotificationType))
                .filter(Boolean) as NotificationPreferenceItem[];

              if (categoryPrefs.length === 0) return null;

              const isExpanded = expandedCategories.has(categoryKey);
              const allInAppEnabled = categoryPrefs.every(p => p.inAppEnabled);
              const allPushEnabled = categoryPrefs.every(p => p.pushEnabled);
              const allEmailEnabled = categoryPrefs.every(p => p.emailEnabled);

              return (
                <div key={categoryKey}>
                  {/* Category header */}
                  <div
                    className="px-6 py-4 cursor-pointer hover:bg-gray-50 transition-colors"
                    onClick={() => toggleCategory(categoryKey)}
                  >
                    <div className="grid grid-cols-12 gap-4 items-center">
                      <div className="col-span-12 sm:col-span-6 flex items-center">
                        {isExpanded ? (
                          <ChevronUp className="h-5 w-5 text-gray-400 mr-2" />
                        ) : (
                          <ChevronDown className="h-5 w-5 text-gray-400 mr-2" />
                        )}
                        <div>
                          <h3 className="text-sm font-medium text-gray-900">{category.label}</h3>
                          <p className="text-xs text-gray-500">{category.description}</p>
                        </div>
                      </div>
                      <div className="col-span-4 sm:col-span-2 text-center">
                        <button
                          type="button"
                          onClick={(e) => {
                            e.stopPropagation();
                            toggleAllInCategory(category.types, 'inAppEnabled', !allInAppEnabled);
                          }}
                          className={`p-1.5 rounded-full transition-colors ${
                            allInAppEnabled
                              ? 'bg-primary-100 text-primary-600'
                              : 'bg-gray-100 text-gray-400'
                          }`}
                        >
                          <Bell className="h-4 w-4" />
                        </button>
                      </div>
                      <div className="col-span-4 sm:col-span-2 text-center">
                        <button
                          type="button"
                          onClick={(e) => {
                            e.stopPropagation();
                            toggleAllInCategory(category.types, 'pushEnabled', !allPushEnabled);
                          }}
                          className={`p-1.5 rounded-full transition-colors ${
                            allPushEnabled
                              ? 'bg-primary-100 text-primary-600'
                              : 'bg-gray-100 text-gray-400'
                          }`}
                        >
                          <Smartphone className="h-4 w-4" />
                        </button>
                      </div>
                      <div className="col-span-4 sm:col-span-2 text-center">
                        <button
                          type="button"
                          onClick={(e) => {
                            e.stopPropagation();
                            toggleAllInCategory(category.types, 'emailEnabled', !allEmailEnabled);
                          }}
                          className={`p-1.5 rounded-full transition-colors ${
                            allEmailEnabled
                              ? 'bg-primary-100 text-primary-600'
                              : 'bg-gray-100 text-gray-400'
                          }`}
                        >
                          <Mail className="h-4 w-4" />
                        </button>
                      </div>
                    </div>
                  </div>

                  {/* Individual preferences */}
                  {isExpanded && (
                    <div className="bg-gray-50">
                      {categoryPrefs.map(pref => (
                        <div
                          key={pref.notificationType}
                          className="px-6 py-3 border-t border-gray-100 grid grid-cols-12 gap-4 items-center"
                        >
                          <div className="col-span-12 sm:col-span-6 pl-7">
                            <span className="text-sm text-gray-700">{pref.typeName}</span>
                          </div>
                          <div className="col-span-4 sm:col-span-2 text-center">
                            <button
                              type="button"
                              onClick={() => updatePreference(pref.notificationType, 'inAppEnabled', !pref.inAppEnabled)}
                              className={`p-1.5 rounded-full transition-colors ${
                                pref.inAppEnabled
                                  ? 'bg-primary-100 text-primary-600'
                                  : 'bg-gray-100 text-gray-400'
                              }`}
                            >
                              {pref.inAppEnabled ? <Bell className="h-4 w-4" /> : <BellOff className="h-4 w-4" />}
                            </button>
                          </div>
                          <div className="col-span-4 sm:col-span-2 text-center">
                            <button
                              type="button"
                              onClick={() => updatePreference(pref.notificationType, 'pushEnabled', !pref.pushEnabled)}
                              className={`p-1.5 rounded-full transition-colors ${
                                pref.pushEnabled
                                  ? 'bg-primary-100 text-primary-600'
                                  : 'bg-gray-100 text-gray-400'
                              }`}
                            >
                              <Smartphone className="h-4 w-4" />
                            </button>
                          </div>
                          <div className="col-span-4 sm:col-span-2 text-center">
                            <button
                              type="button"
                              onClick={() => updatePreference(pref.notificationType, 'emailEnabled', !pref.emailEnabled)}
                              className={`p-1.5 rounded-full transition-colors ${
                                pref.emailEnabled
                                  ? 'bg-primary-100 text-primary-600'
                                  : 'bg-gray-100 text-gray-400'
                              }`}
                            >
                              <Mail className="h-4 w-4" />
                            </button>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </div>

        {/* Save button */}
        <div className="mt-6 flex justify-end">
          <button
            type="button"
            onClick={handleSave}
            disabled={isSaving || !hasChanges}
            className="inline-flex items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isSaving ? (
              <>
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                Saving...
              </>
            ) : (
              <>
                <Save className="h-4 w-4 mr-2" />
                Save Changes
              </>
            )}
          </button>
        </div>
      </div>
    </Layout>
  );
};
