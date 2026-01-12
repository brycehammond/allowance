import { useEffect } from 'react';
import { useNotifications } from '../../hooks/useNotifications';
import { NotificationType, type Notification } from '../../types';
import {
  Bell,
  Check,
  Gift,
  Target,
  DollarSign,
  AlertTriangle,
  CheckCircle,
  Trophy,
  Calendar,
  ClipboardList,
  Loader2,
} from 'lucide-react';

export function NotificationCenter() {
  const {
    notifications,
    unreadCount,
    isLoading,
    hasMore,
    loadNotifications,
    markAsRead,
    markAllAsRead,
  } = useNotifications();

  useEffect(() => {
    loadNotifications(true);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div className="w-96 max-h-[500px] bg-white rounded-lg shadow-xl border overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between p-4 border-b bg-gray-50">
        <h3 className="font-semibold text-gray-900">
          Notifications
          {unreadCount > 0 && (
            <span className="ml-2 bg-primary-600 text-white text-xs px-2 py-0.5 rounded-full">
              {unreadCount}
            </span>
          )}
        </h3>
        {unreadCount > 0 && (
          <button
            onClick={markAllAsRead}
            className="text-sm text-primary-600 hover:text-primary-800 font-medium"
          >
            Mark all read
          </button>
        )}
      </div>

      {/* Notification List */}
      <div className="overflow-y-auto max-h-[400px]">
        {notifications.length === 0 && !isLoading ? (
          <div className="p-8 text-center text-gray-500">
            <Bell className="w-12 h-12 mx-auto mb-2 opacity-30" />
            <p className="font-medium">No notifications</p>
            <p className="text-sm">You're all caught up!</p>
          </div>
        ) : (
          notifications.map((notification) => (
            <NotificationItem
              key={notification.id}
              notification={notification}
              onRead={() => markAsRead(notification.id)}
            />
          ))
        )}

        {isLoading && (
          <div className="p-4 text-center">
            <Loader2 className="w-6 h-6 animate-spin text-primary-600 mx-auto" />
          </div>
        )}

        {hasMore && !isLoading && notifications.length > 0 && (
          <button
            onClick={() => loadNotifications()}
            className="w-full p-3 text-sm text-primary-600 hover:bg-gray-50 font-medium"
          >
            Load more
          </button>
        )}
      </div>
    </div>
  );
}

interface NotificationItemProps {
  notification: Notification;
  onRead: () => void;
}

function NotificationItem({ notification, onRead }: NotificationItemProps) {
  const { icon: Icon, colorClass } = getNotificationStyle(notification.type);

  const handleClick = () => {
    if (!notification.isRead) {
      onRead();
    }
    // TODO: Handle navigation based on relatedEntityType
  };

  return (
    <div
      className={`p-4 border-b hover:bg-gray-50 cursor-pointer transition-colors ${
        notification.isRead ? 'opacity-60 bg-gray-50/50' : 'bg-white'
      }`}
      onClick={handleClick}
    >
      <div className="flex gap-3">
        <div className={`w-10 h-10 rounded-full flex items-center justify-center flex-shrink-0 ${colorClass}`}>
          <Icon className="w-5 h-5" />
        </div>

        <div className="flex-1 min-w-0">
          <div className="flex items-start justify-between gap-2">
            <p
              className={`text-sm ${
                notification.isRead ? 'text-gray-600' : 'text-gray-900 font-medium'
              }`}
            >
              {notification.title}
            </p>
            {!notification.isRead && (
              <div className="w-2 h-2 bg-primary-600 rounded-full flex-shrink-0 mt-1.5" />
            )}
          </div>
          <p className="text-sm text-gray-500 mt-0.5 line-clamp-2">
            {notification.body}
          </p>
          <p className="text-xs text-gray-400 mt-1">{notification.timeAgo}</p>
        </div>
      </div>
    </div>
  );
}

function getNotificationStyle(type: NotificationType): { icon: typeof Bell; colorClass: string } {
  switch (type) {
    // Money/Transactions
    case NotificationType.AllowanceDeposit:
    case NotificationType.TransactionCreated:
      return { icon: DollarSign, colorClass: 'bg-green-100 text-green-600' };

    // Warnings
    case NotificationType.BalanceAlert:
    case NotificationType.LowBalanceWarning:
    case NotificationType.BudgetWarning:
    case NotificationType.BudgetExceeded:
      return { icon: AlertTriangle, colorClass: 'bg-orange-100 text-orange-600' };

    // Goals
    case NotificationType.GoalProgress:
    case NotificationType.GoalMilestone:
      return { icon: Target, colorClass: 'bg-blue-100 text-blue-600' };
    case NotificationType.GoalCompleted:
      return { icon: CheckCircle, colorClass: 'bg-green-100 text-green-600' };
    case NotificationType.ParentMatchAdded:
      return { icon: Gift, colorClass: 'bg-purple-100 text-purple-600' };

    // Tasks
    case NotificationType.TaskAssigned:
    case NotificationType.TaskReminder:
      return { icon: ClipboardList, colorClass: 'bg-blue-100 text-blue-600' };
    case NotificationType.ApprovalRequired:
      return { icon: ClipboardList, colorClass: 'bg-yellow-100 text-yellow-600' };
    case NotificationType.TaskApproved:
    case NotificationType.TaskCompleted:
      return { icon: Check, colorClass: 'bg-green-100 text-green-600' };
    case NotificationType.TaskRejected:
      return { icon: ClipboardList, colorClass: 'bg-red-100 text-red-600' };

    // Achievements
    case NotificationType.AchievementUnlocked:
      return { icon: Trophy, colorClass: 'bg-purple-100 text-purple-600' };
    case NotificationType.StreakUpdate:
      return { icon: Trophy, colorClass: 'bg-orange-100 text-orange-600' };

    // Family
    case NotificationType.GiftReceived:
      return { icon: Gift, colorClass: 'bg-pink-100 text-pink-600' };
    case NotificationType.FamilyInvite:
    case NotificationType.ChildAdded:
      return { icon: Bell, colorClass: 'bg-blue-100 text-blue-600' };

    // Summaries
    case NotificationType.WeeklySummary:
    case NotificationType.MonthlySummary:
      return { icon: Calendar, colorClass: 'bg-gray-100 text-gray-600' };

    // Default
    default:
      return { icon: Bell, colorClass: 'bg-gray-100 text-gray-600' };
  }
}
