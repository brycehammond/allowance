import { useState, useEffect, useCallback, useRef } from 'react';
import { HubConnectionBuilder, HubConnection, HubConnectionState } from '@microsoft/signalr';
import { notificationsApi } from '../services/api';
import type { Notification } from '../types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'https://localhost:7071';

export function useNotifications() {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const [page, setPage] = useState(1);
  const [error, setError] = useState<string | null>(null);
  const connectionRef = useRef<HubConnection | null>(null);
  const isConnectingRef = useRef(false);

  // Setup SignalR connection
  useEffect(() => {
    const token = localStorage.getItem('token');
    if (!token) return;

    // Prevent duplicate connection attempts
    if (isConnectingRef.current || connectionRef.current) return;
    isConnectingRef.current = true;

    const newConnection = new HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/notifications`, {
        accessTokenFactory: () => localStorage.getItem('token') || '',
      })
      .withAutomaticReconnect()
      .build();

    connectionRef.current = newConnection;

    // Start connection
    newConnection
      .start()
      .then(() => {
        console.log('SignalR connected');

        // Subscribe to events
        newConnection.on('ReceiveNotification', (notification: Notification) => {
          setNotifications((prev) => [notification, ...prev]);
          setUnreadCount((prev) => prev + 1);
        });

        newConnection.on('UnreadCountChanged', (count: number) => {
          setUnreadCount(count);
        });

        newConnection.on('NotificationRead', (notificationId: string) => {
          setNotifications((prev) =>
            prev.map((n) =>
              n.id === notificationId ? { ...n, isRead: true, readAt: new Date().toISOString() } : n
            )
          );
        });
      })
      .catch((err) => {
        console.error('SignalR connection error:', err);
        isConnectingRef.current = false;
      });

    return () => {
      if (connectionRef.current?.state === HubConnectionState.Connected) {
        connectionRef.current.stop();
      }
      connectionRef.current = null;
      isConnectingRef.current = false;
    };
  }, []);

  const loadNotifications = useCallback(async (refresh = false) => {
    if (isLoading || (!hasMore && !refresh)) return;

    setIsLoading(true);
    setError(null);
    const currentPage = refresh ? 1 : page;

    try {
      const response = await notificationsApi.getNotifications(currentPage, 20, false);

      if (refresh) {
        setNotifications(response.notifications);
      } else {
        setNotifications((prev) => [...prev, ...response.notifications]);
      }

      setUnreadCount(response.unreadCount);
      setHasMore(response.hasMore);
      setPage(currentPage + 1);
    } catch (err) {
      console.error('Failed to load notifications:', err);
      setError('Failed to load notifications');
    } finally {
      setIsLoading(false);
    }
  }, [isLoading, hasMore, page]);

  const refreshUnreadCount = useCallback(async () => {
    try {
      const { count } = await notificationsApi.getUnreadCount();
      setUnreadCount(count);
    } catch (err) {
      console.error('Failed to refresh unread count:', err);
    }
  }, []);

  const markAsRead = useCallback(async (notificationId: string) => {
    try {
      await notificationsApi.markAsRead(notificationId);
      setNotifications((prev) =>
        prev.map((n) =>
          n.id === notificationId ? { ...n, isRead: true, readAt: new Date().toISOString() } : n
        )
      );
      setUnreadCount((prev) => Math.max(0, prev - 1));
    } catch (err) {
      console.error('Failed to mark notification as read:', err);
    }
  }, []);

  const markAllAsRead = useCallback(async () => {
    try {
      await notificationsApi.markMultipleAsRead({ notificationIds: undefined });
      setNotifications((prev) =>
        prev.map((n) => ({ ...n, isRead: true, readAt: new Date().toISOString() }))
      );
      setUnreadCount(0);
    } catch (err) {
      console.error('Failed to mark all as read:', err);
    }
  }, []);

  const deleteNotification = useCallback(async (notificationId: string) => {
    try {
      await notificationsApi.delete(notificationId);
      setNotifications((prev) => prev.filter((n) => n.id !== notificationId));
    } catch (err) {
      console.error('Failed to delete notification:', err);
    }
  }, []);

  return {
    notifications,
    unreadCount,
    isLoading,
    hasMore,
    error,
    loadNotifications,
    refreshUnreadCount,
    markAsRead,
    markAllAsRead,
    deleteNotification,
  };
}
