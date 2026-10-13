export interface Notification {
  id: string;
  title: string;
  message: string;
  linkUrl?: string | null;
  isRead: boolean;
  createdAt: string;
  type?: 'NEW_USER' | 'SYSTEM' | 'ALERT';
}