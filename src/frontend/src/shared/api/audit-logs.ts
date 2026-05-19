import client from './client';

export interface AuditLogDto {
  id: string;
  timestamp: string;
  actorId: string;
  actorName: string;
  action: string;
  targetId: string;
  metadata: string | null;
  ipAddress: string;
}

export interface SearchAuditLogsParams {
  actorId?: string;
  action?: string;
  targetId?: string;
  from?: string;
  to?: string;
  clinicId?: string;
  page?: number;
  pageSize?: number;
}

export interface SearchAuditLogsResponse {
  logs: AuditLogDto[];
  totalCount: number;
}

export async function searchAuditLogs(params: SearchAuditLogsParams): Promise<SearchAuditLogsResponse> {
  const { data } = await client.get<SearchAuditLogsResponse>('/api/v1/audit-logs', { params });
  return data;
}

export async function exportAuditLogsCsv(params: Omit<SearchAuditLogsParams, 'page' | 'pageSize'>): Promise<Blob> {
  const { data } = await client.get<Blob>('/api/v1/audit-logs/export', {
    params,
    responseType: 'blob',
  });
  return data;
}
