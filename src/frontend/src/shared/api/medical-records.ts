import client from './client';

// ── Enums (string unions matching backend) ──────────────────────────────

export type ConditionSeverity = 'Mild' | 'Moderate' | 'Severe';
export type ConditionStatus = 'Active' | 'InRemission' | 'Resolved';
export type VitalType = 'BloodPressure' | 'HeartRate' | 'Weight' | 'SpO2' | 'Temperature' | 'BloodGlucose';
export type VitalSource = 'Manual' | 'Device' | 'ClinicalEntry';
export type MedicalDocumentType = 'LabResult' | 'Imaging' | 'DischargeSummary' | 'Referral' | 'Other';
export type RecordAccessLevel = 'Full' | 'Limited' | 'Audit' | 'Revoked' | 'None';

// ── Query DTOs ──────────────────────────────────────────────────────────

export interface ConditionDto {
  id: string;
  icdCode: string;
  icdDescription: string;
  dateOfOnset: string | null;
  severity: ConditionSeverity;
  status: ConditionStatus;
  clinicalNotes: string | null;
  addedBy: string;
  createdAt: string;
}

export interface AllergyDto {
  id: string;
  allergenName: string;
  reaction: string | null;
  severity: ConditionSeverity;
  dateIdentified: string | null;
  isActive: boolean;
  addedBy: string;
  createdAt: string;
}

export interface MedicationDto {
  id: string;
  medicationName: string;
  dosage: string;
  frequency: string;
  startDate: string | null;
  endDate: string | null;
  isActive: boolean;
  prescribedByName: string | null;
  linkedPrescriptionId: string | null;
  addedBy: string;
  createdAt: string;
}

export interface FamilyHistoryDto {
  id: string;
  relationship: string;
  conditionName: string;
  ageAtOnset: number | null;
  notes: string | null;
  addedBy: string;
  createdAt: string;
}

export interface VitalReadingDetailDto {
  id: string;
  value: number;
  secondaryValue: number | null;
  unit: string;
  measuredAt: string;
  source: VitalSource;
  notes: string | null;
  addedBy: string;
}

export interface DocumentDto {
  id: string;
  documentType: MedicalDocumentType;
  title: string;
  description: string | null;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedBy: string;
  createdAt: string;
}

export interface TimelineEntryDto {
  id: string;
  type: string;
  occurredAt: string;
  title: string;
  subtitle: string | null;
  addedBy: string | null;
}

export interface TimelineResponse {
  entries: TimelineEntryDto[];
  totalCount: number;
}

// ── Snapshot sub-DTOs ───────────────────────────────────────────────────

export interface AllergySnapshotDto {
  id: string;
  allergenName: string;
  reaction: string | null;
  severity: ConditionSeverity;
}

export interface ConditionSnapshotDto {
  id: string;
  icdDescription: string;
  severity: ConditionSeverity;
}

export interface MedicationSnapshotDto {
  id: string;
  medicationName: string;
  dosage: string;
  frequency: string;
  startDate: string | null;
}

export interface FamilyHistorySnapshotDto {
  id: string;
  relationship: string;
  conditionName: string;
}

export interface VitalReadingDto {
  value: number;
  secondaryValue: number | null;
  unit: string;
  measuredAt: string;
}

export interface LatestVitalsDto {
  bloodPressure: VitalReadingDto | null;
  heartRate: VitalReadingDto | null;
  weight: VitalReadingDto | null;
  spO2: VitalReadingDto | null;
}

export interface PatientSnapshotDto {
  allergies: AllergySnapshotDto[];
  activeConditions: ConditionSnapshotDto[];
  activeMedications: MedicationSnapshotDto[];
  familyHistory: FamilyHistorySnapshotDto[];
  latestVitals: LatestVitalsDto | null;
  onboardingProgress: number;
}

// ── Header ──────────────────────────────────────────────────────────────

export interface PatientHeaderDto {
  patientProfileId: string;
  patientName: string;
  city: string | null;
  allergyChips: string[];
  conditionChips: string[];
}

// ── Record Access DTOs ──────────────────────────────────────────────────

export interface RecordAccessEntryDto {
  id: string;
  userName: string;
  userRole: string;
  accessLevel: RecordAccessLevel;
  grantedAt: string;
  grantedBy: string;
  revokedAt: string | null;
  lastViewed: string | null;
}

export interface RecordAccessLogDto {
  id: string;
  accessedBy: string;
  action: string;
  resourceType: string;
  accessedAt: string;
}

export interface AccessMatrixResponse {
  entries: RecordAccessEntryDto[];
  totalCount: number;
}

export interface AccessLogsResponse {
  logs: RecordAccessLogDto[];
  totalCount: number;
}

// ── Mutation payloads ───────────────────────────────────────────────────

export interface AddConditionPayload {
  icdCode: string;
  icdDescription: string;
  dateOfOnset?: string;
  severity: ConditionSeverity;
  status: ConditionStatus;
  clinicalNotes?: string;
  linkedAppointmentId?: string;
}

export interface UpdateConditionPayload {
  severity: ConditionSeverity;
  status: ConditionStatus;
  clinicalNotes?: string;
}

export interface AddAllergyPayload {
  allergenName: string;
  reaction?: string;
  severity: ConditionSeverity;
  dateIdentified?: string;
}

export interface UpdateAllergyPayload {
  reaction?: string;
  severity: ConditionSeverity;
  isActive: boolean;
}

export interface AddVitalPayload {
  vitalType: VitalType;
  value: number;
  secondaryValue?: number;
  unit: string;
  measuredAt: string;
  source: VitalSource;
  notes?: string;
}

export interface AddMedicationPayload {
  medicationName: string;
  dosage: string;
  frequency: string;
  startDate?: string;
  endDate?: string;
  prescribedByName?: string;
  linkedPrescriptionId?: string;
}

export interface UpdateMedicationPayload {
  dosage: string;
  frequency: string;
  endDate?: string;
  isActive: boolean;
}

export interface AddFamilyHistoryPayload {
  relationship: string;
  conditionName: string;
  ageAtOnset?: number;
  notes?: string;
}

export interface UpdateFamilyHistoryPayload {
  conditionName: string;
  ageAtOnset?: number;
  notes?: string;
}

export interface UploadDocumentMetadata {
  documentType: MedicalDocumentType;
  title: string;
  description?: string;
  linkedAppointmentId?: string;
}

export interface GrantAccessPayload {
  grantToUserId: string;
  accessLevel: RecordAccessLevel;
  reason?: string;
}

// ── Query params ────────────────────────────────────────────────────────

export interface TimelineParams {
  filterTypes?: string[];
  from?: string;
  to?: string;
  filterDoctorId?: string;
  page?: number;
  pageSize?: number;
}

// ── Base route constants ────────────────────────────────────────────────

const RECORDS = '/api/v1/medical-records';
const ACCESS = '/api/v1/record-access';

// ── GET endpoints (queries) ─────────────────────────────────────────────

export async function getPatientTimeline(
  patientProfileId: string,
  params: TimelineParams = {},
): Promise<TimelineResponse> {
  const { data } = await client.get<TimelineResponse>(
    `${RECORDS}/${patientProfileId}/timeline`,
    { params },
  );
  return data;
}

export async function getPatientSnapshot(
  patientProfileId: string,
): Promise<PatientSnapshotDto> {
  const { data } = await client.get<PatientSnapshotDto>(
    `${RECORDS}/${patientProfileId}/snapshot`,
  );
  return data;
}

export async function getPatientVitalsHistory(
  patientProfileId: string,
  vitalType: VitalType,
  daysBack?: number,
): Promise<VitalReadingDetailDto[]> {
  const { data } = await client.get<VitalReadingDetailDto[]>(
    `${RECORDS}/${patientProfileId}/vitals/${vitalType}/history`,
    { params: daysBack !== undefined ? { daysBack } : undefined },
  );
  return data;
}

export async function getPatientConditions(
  patientProfileId: string,
): Promise<ConditionDto[]> {
  const { data } = await client.get<ConditionDto[]>(
    `${RECORDS}/${patientProfileId}/conditions`,
  );
  return data;
}

export async function getPatientMedications(
  patientProfileId: string,
): Promise<MedicationDto[]> {
  const { data } = await client.get<MedicationDto[]>(
    `${RECORDS}/${patientProfileId}/medications`,
  );
  return data;
}

export async function getPatientAllergies(
  patientProfileId: string,
): Promise<AllergyDto[]> {
  const { data } = await client.get<AllergyDto[]>(
    `${RECORDS}/${patientProfileId}/allergies`,
  );
  return data;
}

export async function getPatientFamilyHistory(
  patientProfileId: string,
): Promise<FamilyHistoryDto[]> {
  const { data } = await client.get<FamilyHistoryDto[]>(
    `${RECORDS}/${patientProfileId}/family-history`,
  );
  return data;
}

export async function getPatientDocuments(
  patientProfileId: string,
  filterType?: MedicalDocumentType,
): Promise<DocumentDto[]> {
  const { data } = await client.get<DocumentDto[]>(
    `${RECORDS}/${patientProfileId}/documents`,
    { params: filterType !== undefined ? { filterType } : undefined },
  );
  return data;
}

export async function downloadDocument(documentId: string): Promise<Blob> {
  const { data } = await client.get<Blob>(
    `${RECORDS}/documents/${documentId}/download`,
    { responseType: 'blob' },
  );
  return data;
}

export async function getPatientHeader(
  patientProfileId: string,
): Promise<PatientHeaderDto> {
  const { data } = await client.get<PatientHeaderDto>(
    `${RECORDS}/${patientProfileId}/header`,
  );
  return data;
}

// ── POST/PUT/DELETE endpoints (mutations) ───────────────────────────────

export async function addCondition(
  patientProfileId: string,
  payload: AddConditionPayload,
): Promise<{ conditionId: string }> {
  const { data } = await client.post<{ conditionId: string }>(
    `${RECORDS}/${patientProfileId}/conditions`,
    payload,
  );
  return data;
}

export async function updateCondition(
  conditionId: string,
  payload: UpdateConditionPayload,
): Promise<void> {
  await client.put(`${RECORDS}/conditions/${conditionId}`, payload);
}

export async function addAllergy(
  patientProfileId: string,
  payload: AddAllergyPayload,
): Promise<{ allergyId: string }> {
  const { data } = await client.post<{ allergyId: string }>(
    `${RECORDS}/${patientProfileId}/allergies`,
    payload,
  );
  return data;
}

export async function updateAllergy(
  allergyId: string,
  payload: UpdateAllergyPayload,
): Promise<void> {
  await client.put(`${RECORDS}/allergies/${allergyId}`, payload);
}

export async function addVital(
  patientProfileId: string,
  payload: AddVitalPayload,
): Promise<{ vitalId: string }> {
  const { data } = await client.post<{ vitalId: string }>(
    `${RECORDS}/${patientProfileId}/vitals`,
    payload,
  );
  return data;
}

export async function addMedication(
  patientProfileId: string,
  payload: AddMedicationPayload,
): Promise<{ medicationId: string }> {
  const { data } = await client.post<{ medicationId: string }>(
    `${RECORDS}/${patientProfileId}/medications`,
    payload,
  );
  return data;
}

export async function updateMedication(
  medicationId: string,
  payload: UpdateMedicationPayload,
): Promise<void> {
  await client.put(`${RECORDS}/medications/${medicationId}`, payload);
}

export async function addFamilyHistory(
  patientProfileId: string,
  payload: AddFamilyHistoryPayload,
): Promise<{ familyHistoryId: string }> {
  const { data } = await client.post<{ familyHistoryId: string }>(
    `${RECORDS}/${patientProfileId}/family-history`,
    payload,
  );
  return data;
}

export async function updateFamilyHistory(
  familyHistoryId: string,
  payload: UpdateFamilyHistoryPayload,
): Promise<void> {
  await client.put(`${RECORDS}/family-history/${familyHistoryId}`, payload);
}

export async function uploadDocument(
  patientProfileId: string,
  file: File,
  metadata: UploadDocumentMetadata,
): Promise<{ documentId: string }> {
  const form = new FormData();
  form.append('file', file);
  form.append('documentType', metadata.documentType);
  form.append('title', metadata.title);
  if (metadata.description) form.append('description', metadata.description);
  if (metadata.linkedAppointmentId) form.append('linkedAppointmentId', metadata.linkedAppointmentId);

  const { data } = await client.post<{ documentId: string }>(
    `${RECORDS}/${patientProfileId}/documents`,
    form,
    { headers: { 'Content-Type': 'multipart/form-data' } },
  );
  return data;
}

export async function deleteDocument(documentId: string): Promise<void> {
  await client.delete(`${RECORDS}/documents/${documentId}`);
}

// ── Record Access (admin only) ──────────────────────────────────────────

export async function getAccessMatrix(
  patientProfileId: string,
  page?: number,
  pageSize?: number,
): Promise<AccessMatrixResponse> {
  const { data } = await client.get<AccessMatrixResponse>(
    `${ACCESS}/${patientProfileId}/matrix`,
    { params: { page, pageSize } },
  );
  return data;
}

export async function getAccessLogs(
  patientProfileId: string,
  page?: number,
  pageSize?: number,
): Promise<AccessLogsResponse> {
  const { data } = await client.get<AccessLogsResponse>(
    `${ACCESS}/${patientProfileId}/logs`,
    { params: { page, pageSize } },
  );
  return data;
}

export async function grantAccess(
  patientProfileId: string,
  payload: GrantAccessPayload,
): Promise<{ accessId: string }> {
  const { data } = await client.post<{ accessId: string }>(
    `${ACCESS}/${patientProfileId}/grant`,
    payload,
  );
  return data;
}

export async function revokeAccess(
  accessId: string,
  reason?: string,
): Promise<void> {
  await client.post(`${ACCESS}/${accessId}/revoke`, reason ? { reason } : undefined);
}
