import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../../shared/components/AuthProvider';
import { getApiError } from '../../shared/api/errors';
import {
  getMyThreads,
  getThreadMessages,
  sendMessage,
  markAsRead,
  closeThread,
  searchMessages,
} from '../../shared/api/messaging';
import type { ThreadSummaryDto } from '../../shared/api/messaging';
import { DashboardShell } from '../../shared/components/DashboardShell';
import { ThreadList } from './components/ThreadList';
import { MessagePanel } from './components/MessagePanel';
import { useChat } from './hooks/useChat';
import './messaging.css';

export default function MessagingPage() {
  const { token, user } = useAuth();
  const queryClient = useQueryClient();
  const [selectedThreadId, setSelectedThreadId] = useState<string | null>(null);
  const [filter, setFilter] = useState<string | undefined>();
  const [searchQuery, setSearchQuery] = useState('');
  const [error, setError] = useState<string | null>(null);

  const threadsQuery = useQuery({
    queryKey: ['messaging-threads', filter],
    queryFn: () => getMyThreads(filter),
  });

  const messagesQuery = useQuery({
    queryKey: ['messaging-messages', selectedThreadId],
    queryFn: () => getThreadMessages(selectedThreadId!),
    enabled: !!selectedThreadId,
    refetchInterval: 5000,
  });

  const searchQ = useQuery({
    queryKey: ['messaging-search', searchQuery],
    queryFn: () => searchMessages(searchQuery),
    enabled: searchQuery.length >= 2,
  });

  const sendMutation = useMutation({
    mutationFn: (content: string) => sendMessage(selectedThreadId!, content),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['messaging-messages', selectedThreadId] });
      queryClient.invalidateQueries({ queryKey: ['messaging-threads'] });
      setError(null);
    },
    onError: (err: unknown) => setError(getApiError(err)),
  });

  const closeMutation = useMutation({
    mutationFn: (threadId: string) => closeThread(threadId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['messaging-threads'] });
      queryClient.invalidateQueries({ queryKey: ['messaging-messages', selectedThreadId] });
    },
    onError: (err: unknown) => setError(getApiError(err)),
  });

  const chat = useChat(selectedThreadId ?? '', token);

  const selectedThread = threadsQuery.data?.threads.find(
    (t: ThreadSummaryDto) => t.threadId === selectedThreadId,
  );

  const handleSelectThread = async (threadId: string) => {
    setSelectedThreadId(threadId);
    setSearchQuery('');
    try {
      await markAsRead(threadId);
      queryClient.invalidateQueries({ queryKey: ['messaging-threads'] });
    } catch (e) {
      console.warn('Failed to mark thread as read', e);
    }
  };

  const filterOptions = [
    { value: undefined, label: 'All' },
    { value: 'unread', label: 'Unread' },
    { value: 'needs_reply', label: 'Needs Reply' },
    { value: 'closed', label: 'Closed' },
  ];

  return (
    <DashboardShell
      subtitle="messages"
      userName={`${user?.given_name ?? ''} ${user?.family_name ?? ''}`.trim()}
      wide
    >
      <div className="msg-page">
        <aside className="msg-sidebar">
          <div className="msg-sidebar-header">
            <h2 className="msg-title">Messages</h2>
            <input
              className="msg-search"
              type="text"
              placeholder="Search messages…"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
            <div className="msg-filters">
              {filterOptions.map((opt) => (
                <button
                  key={opt.label}
                  className={`msg-filter-btn ${filter === opt.value ? 'msg-filter-active' : ''}`}
                  onClick={() => setFilter(opt.value)}
                  type="button"
                >
                  {opt.label}
                </button>
              ))}
            </div>
          </div>

          {searchQuery.length >= 2 && searchQ.data ? (
            <div className="msg-search-results" role="list">
              {searchQ.data.hits.map((hit) => (
                <button
                  key={hit.messageId}
                  className="msg-search-hit"
                  onClick={() => handleSelectThread(hit.threadId)}
                  type="button"
                  role="listitem"
                >
                  <span className="msg-search-sender">{hit.senderName}</span>
                  <span className="msg-search-content">{hit.content}</span>
                </button>
              ))}
              {searchQ.data.hits.length === 0 && (
                <div className="msg-empty">No results found</div>
              )}
            </div>
          ) : (
            <ThreadList
              threads={threadsQuery.data?.threads ?? []}
              selectedId={selectedThreadId}
              onSelect={handleSelectThread}
            />
          )}
        </aside>

        <main className="msg-main">
          {selectedThreadId && selectedThread ? (
            <>
              <div className="msg-main-header">
                <h3 className="msg-main-name">{selectedThread.otherParticipantName}</h3>
                {selectedThread.otherParticipantSpecialty && (
                  <span className="msg-main-specialty">
                    {selectedThread.otherParticipantSpecialty}
                  </span>
                )}
                <span className={`msg-status msg-status-${selectedThread.status.toLowerCase()}`}>
                  {selectedThread.status}
                </span>
                {user?.role === 'Doctor' && selectedThread.status === 'Active' && (
                  <button
                    className="msg-close-btn"
                    onClick={() => closeMutation.mutate(selectedThreadId!)}
                    disabled={closeMutation.isPending}
                    type="button"
                  >
                    Close Thread
                  </button>
                )}
              </div>
              {error && <div className="msg-error">{error}</div>}
              <MessagePanel
                messages={messagesQuery.data?.messages ?? []}
                currentUserId={user?.sub ?? ''}
                onSend={(content) => sendMutation.mutate(content)}
                onTyping={chat.sendTyping}
                typingUser={chat.typingUser}
                threadStatus={selectedThread.status}
              />
            </>
          ) : (
            <div className="msg-placeholder">
              Select a conversation to start messaging
            </div>
          )}
        </main>
      </div>
    </DashboardShell>
  );
}
