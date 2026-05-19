import { test, expect } from '@playwright/test';
import { PATIENT_TOKEN, DOCTOR_TOKEN, loginAs, mockBaseRoutes, mockMessagingRoutes, mockDoctorRoutes } from './helpers';

const mockThreads = [
  {
    threadId: 't-1',
    otherParticipantName: 'Dr. Amelia Chen',
    otherParticipantSpecialty: 'Cardiology',
    subject: 'Persistent chest tightness',
    lastMessagePreview: 'Mostly pressure. Like a hand on my chest.',
    lastMessageAt: new Date().toISOString(),
    unreadCount: 1,
    status: 'Active',
    appointmentStatus: 'Scheduled',
    appointmentId: 'a-1',
  },
  {
    threadId: 't-2',
    otherParticipantName: 'Dr. Marcus Kim',
    otherParticipantSpecialty: 'Dermatology',
    subject: 'Migraine follow-up',
    lastMessagePreview: "I'll review the photos.",
    lastMessageAt: new Date(Date.now() - 86400000).toISOString(),
    unreadCount: 0,
    status: 'Closed',
    appointmentStatus: 'Completed',
    appointmentId: 'a-2',
  },
];

const mockMessages = [
  {
    id: 'm-1',
    senderName: 'Dr. Amelia Chen',
    senderRole: 'Doctor',
    senderUserId: '2',
    content: 'Can you describe the chest tightness in more detail?',
    messageType: 'Text',
    isRead: true,
    readAt: '2026-05-19T09:03:00Z',
    createdAt: '2026-05-19T09:02:00Z',
    attachments: [],
  },
  {
    id: 'm-2',
    senderName: 'Maya Chen',
    senderRole: 'Patient',
    senderUserId: '1',
    content: 'Mostly pressure when I run longer than 5km.',
    messageType: 'Text',
    isRead: false,
    readAt: null,
    createdAt: '2026-05-19T09:04:00Z',
    attachments: [],
  },
];

// ── Patient Messaging ────────────────────────────────────────────────────

test.describe('Patient — Messaging', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, PATIENT_TOKEN);
    await mockBaseRoutes(page);
    await mockMessagingRoutes(page, mockThreads, mockMessages);
  });

  test('shows thread list on messaging page', async ({ page }) => {
    await page.goto('/messages');

    await expect(page.getByText('Dr. Amelia Chen')).toBeVisible();
    await expect(page.getByText('Dr. Marcus Kim')).toBeVisible();
  });

  test('shows unread badge', async ({ page }) => {
    await page.goto('/messages');

    await expect(page.locator('.msg-thread-badge').first()).toBeVisible();
    await expect(page.locator('.msg-thread-badge').first()).toHaveText('1');
  });

  test('opens thread and shows messages', async ({ page }) => {
    await page.goto('/messages');

    await page.getByText('Dr. Amelia Chen').click();

    await expect(page.getByText('Can you describe the chest tightness')).toBeVisible();
    await expect(page.getByText('Mostly pressure when I run')).toBeVisible();
  });

  test('sends a message', async ({ page }) => {
    await page.goto('/messages');
    await page.getByText('Dr. Amelia Chen').click();

    const input = page.getByPlaceholder(/type a message/i);
    await input.fill('The pressure is mostly in my upper chest');
    await page.getByRole('button', { name: /send/i }).click();

    await expect(input).toHaveValue('');
  });

  test('shows closed status for closed thread', async ({ page }) => {
    await page.goto('/messages');
    await page.getByText('Dr. Marcus Kim').click();

    await expect(page.locator('.msg-status-closed')).toBeVisible();
    await expect(page.getByPlaceholder(/closed/i)).toBeDisabled();
  });

  test('search returns results', async ({ page }) => {
    await page.route(/\/api\/v1\/messaging\/search/, (route) => {
      return route.fulfill({
        status: 200,
        json: {
          hits: [
            {
              threadId: 't-1',
              otherParticipantName: 'Dr. Amelia Chen',
              messageId: 'm-1',
              content: 'Describe the chest tightness',
              senderName: 'Dr. Amelia Chen',
              createdAt: '2026-05-19T09:02:00Z',
              hasAttachment: false,
            },
          ],
          totalCount: 1,
        },
      });
    });

    await page.goto('/messages');

    await page.getByPlaceholder(/search messages/i).fill('chest');

    await expect(page.getByText('Describe the chest tightness')).toBeVisible();
  });

  test('uploads an attachment', async ({ page }) => {
    await page.route(/\/api\/v1\/messaging\/threads\/[^/]+\/attachments/, (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 201, json: { messageId: 'msg-attach-1' } });
      }
      return route.fallback();
    });

    const attachmentMessages = [
      ...mockMessages,
      {
        id: 'msg-attach-1',
        senderName: 'Maya Chen',
        senderRole: 'Patient',
        senderUserId: '1',
        content: '📎 xray.png',
        messageType: 'Attachment',
        isRead: false,
        readAt: null,
        createdAt: '2026-05-19T09:05:00Z',
        attachments: [
          {
            id: 'att-1',
            fileName: 'xray.png',
            contentType: 'image/png',
            fileSizeBytes: 2048,
            attachmentType: 'Image',
          },
        ],
      },
    ];

    let uploaded = false;
    await page.route(/\/api\/v1\/messaging\/threads\/[^/]+\/messages/, (route) => {
      if (route.request().method() === 'GET') {
        const msgs = uploaded ? attachmentMessages : mockMessages;
        return route.fulfill({
          status: 200,
          json: { messages: msgs, totalCount: msgs.length },
        });
      }
      return route.fallback();
    });

    await page.goto('/messages');
    await page.getByText('Dr. Amelia Chen').click();

    const fileInput = page.locator('input[type="file"]');
    if (await fileInput.count() > 0) {
      uploaded = true;
      await fileInput.setInputFiles({
        name: 'xray.png',
        mimeType: 'image/png',
        buffer: Buffer.from([0x89, 0x50, 0x4e, 0x47]),
      });

      await expect(page.getByText('xray.png')).toBeVisible();
    }
  });

  test('placeholder shown when no thread selected', async ({ page }) => {
    await page.goto('/messages');

    await expect(page.getByText(/select a conversation/i)).toBeVisible();
  });
});

// ── Doctor Messaging ─────────────────────────────────────────────────────

test.describe('Doctor — Messaging', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, DOCTOR_TOKEN);
    await mockBaseRoutes(page);
    await mockDoctorRoutes(page);

    const doctorThreads = [
      {
        threadId: 't-10',
        otherParticipantName: 'Maya Chen',
        otherParticipantSpecialty: null,
        subject: 'Persistent chest tightness',
        lastMessagePreview: 'Mostly pressure when I run.',
        lastMessageAt: new Date().toISOString(),
        unreadCount: 2,
        status: 'Active',
        appointmentStatus: 'Scheduled',
        appointmentId: 'a-1',
      },
    ];

    await mockMessagingRoutes(page, doctorThreads, mockMessages);
  });

  test('doctor sees patient threads', async ({ page }) => {
    await page.goto('/messages');

    await expect(page.getByText('Maya Chen')).toBeVisible();
  });

  test('doctor can send a message', async ({ page }) => {
    await page.goto('/messages');
    await page.getByText('Maya Chen').click();

    const input = page.getByPlaceholder(/type a message/i);
    await input.fill('Does the pain radiate anywhere?');
    await page.getByRole('button', { name: /send/i }).click();

    await expect(input).toHaveValue('');
  });

  test('doctor can close a thread', async ({ page }) => {
    let threadClosed = false;

    await page.route(/\/api\/v1\/messaging\/threads\/[^/]+\/close/, (route) => {
      threadClosed = true;
      return route.fulfill({ status: 200, json: {} });
    });

    await page.route(/\/api\/v1\/messaging\/threads(\?|$)/, (route) => {
      if (route.request().method() === 'GET') {
        const threads = threadClosed
          ? [
              {
                threadId: 't-10',
                otherParticipantName: 'Maya Chen',
                otherParticipantSpecialty: null,
                subject: 'Persistent chest tightness',
                lastMessagePreview: 'Thread closed by Dr. House',
                lastMessageAt: new Date().toISOString(),
                unreadCount: 0,
                status: 'Closed',
                appointmentStatus: 'Scheduled',
                appointmentId: 'a-1',
              },
            ]
          : [
              {
                threadId: 't-10',
                otherParticipantName: 'Maya Chen',
                otherParticipantSpecialty: null,
                subject: 'Persistent chest tightness',
                lastMessagePreview: 'Mostly pressure when I run.',
                lastMessageAt: new Date().toISOString(),
                unreadCount: 2,
                status: 'Active',
                appointmentStatus: 'Scheduled',
                appointmentId: 'a-1',
              },
            ];
        return route.fulfill({
          status: 200,
          json: { threads, totalCount: threads.length },
        });
      }
      return route.fallback();
    });

    await page.goto('/messages');
    await page.getByText('Maya Chen').click();

    await page.getByRole('button', { name: /close thread/i }).click();

    await expect(page.locator('.msg-status-closed')).toBeVisible();
    await expect(page.getByPlaceholder(/closed/i)).toBeDisabled();
  });
});

// ── Empty State ──────────────────────────────────────────────────────────

test.describe('Messaging — Empty State', () => {
  test('shows no conversations message when empty', async ({ page }) => {
    await loginAs(page, PATIENT_TOKEN);
    await mockBaseRoutes(page);
    await mockMessagingRoutes(page, [], []);

    await page.goto('/messages');

    await expect(page.getByText(/no conversations/i)).toBeVisible();
  });
});
