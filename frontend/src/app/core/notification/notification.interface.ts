export interface Notification {
  id: string;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
  type?: 'NEW_USER' | 'SYSTEM' | 'ALERT';
}