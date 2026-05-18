import client from './client';

export type ThreadStatus = 'Active' | 'Closed';
export type MessageType = 'Text' | 'Attachment' | 'SystemEvent';
export type AttachmentType = 'Image' | 'Document' | 'VoiceMemo';

export interface ThreadSummaryDto {
  threadId: string;
  otherParticipantName: string;
  otherParticipantSpecialty: string | null;
  subject: string;
  lastMessagePreview: string | null;
  lastMessageAt: string | null;
  unreadCount: number;
  status: ThreadStatus;
  appointmentStatus: string;
  appointmentId: string;
}

export interface AttachmentDto {
  id: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  attachmentType: AttachmentType;
}

export interface MessageDto {
  id: string;
  senderName: string;
  senderRole: string;
  senderUserId: string;
  content: string;
  messageType: MessageType;
  isRead: boolean;
  readAt: string | null;
  createdAt: string;
  attachments: AttachmentDto[];
}

export interface SearchHitDto {
  threadId: string;
  otherParticipantName: string;
  messageId: string;
  content: string;
  senderName: string;
  createdAt: string;
  hasAttachment: boolean;
}

export async function getMyThreads(
  filter?: string,
  search?: string,
  page = 1,
  pageSize = 50,
): Promise<{ threads: ThreadSummaryDto[]; totalCount: number }> {
  const params = new URLSearchParams();
  if (filter) params.set('filter', filter);
  if (search) params.set('search', search);
  params.set('page', String(page));
  params.set('pageSize', String(pageSize));
  const { data } = await client.get<{ threads: ThreadSummaryDto[]; totalCount: number }>(
    `/api/v1/messaging/threads?${params}`,
  );
  return data;
}

export async function getThreadMessages(
  threadId: string,
  page = 1,
  pageSize = 50,
): Promise<{ messages: MessageDto[]; totalCount: number }> {
  const { data } = await client.get<{ messages: MessageDto[]; totalCount: number }>(
    `/api/v1/messaging/threads/${threadId}/messages?page=${page}&pageSize=${pageSize}`,
  );
  return data;
}

export async function sendMessage(
  threadId: string,
  content: string,
): Promise<{ messageId: string }> {
  const { data } = await client.post<{ messageId: string }>(
    `/api/v1/messaging/threads/${threadId}/messages`,
    { content },
  );
  return data;
}

export async function sendAttachment(
  threadId: string,
  file: File,
): Promise<{ messageId: string }> {
  const form = new FormData();
  form.append('file', file);
  const { data } = await client.post<{ messageId: string }>(
    `/api/v1/messaging/threads/${threadId}/attachments`,
    form,
  );
  return data;
}

export async function markAsRead(threadId: string): Promise<void> {
  await client.post(`/api/v1/messaging/threads/${threadId}/read`);
}

export async function closeThread(
  threadId: string,
  reason?: string,
): Promise<void> {
  await client.post(`/api/v1/messaging/threads/${threadId}/close`, { reason });
}

export async function searchMessages(
  query: string,
  page = 1,
  pageSize = 50,
): Promise<{ hits: SearchHitDto[]; totalCount: number }> {
  const { data } = await client.get<{ hits: SearchHitDto[]; totalCount: number }>(
    `/api/v1/messaging/search?q=${encodeURIComponent(query)}&page=${page}&pageSize=${pageSize}`,
  );
  return data;
}
