import client from './client';

export type Period = '7d' | '30d' | '90d' | 'ytd';

export interface KpiDto {
  total: number;
  deltaPct: number;
  sparkline: number[];
}

export interface DecimalKpiDto {
  value: number;
  deltaPct: number;
  sparkline: number[];
}

export interface RateKpiDto {
  rate: number;
  deltaPct: number;
  sparkline: number[];
}

export interface TimeSeriesDto {
  labels: string[];
  current: number[];
  previous: number[];
}

export interface BarSeriesDto {
  labels: string[];
  values: number[];
}

export interface FunnelStageDto {
  label: string;
  value: number;
}

export interface ActiveUsersDto {
  dau: number;
  wau: number;
  mau: number;
  dauDelta: number;
  wauDelta: number;
  mauDelta: number;
  stickiness: number;
}

export interface ClinicBreakdownDto {
  name: string;
  value: number;
}

export interface SpecialtyMixDto {
  label: string;
  value: number;
}

export interface AnalyticsOverviewDto {
  appointmentKpi: KpiDto;
  revenueKpi: DecimalKpiDto;
  activeUsersKpi: KpiDto;
  noShowKpi: RateKpiDto;
  appointmentSeries: TimeSeriesDto;
  revenueSeries: BarSeriesDto;
  funnel: FunnelStageDto[];
  activeUsers: ActiveUsersDto;
  clinicBreakdown: ClinicBreakdownDto[];
  specialtyMix: SpecialtyMixDto[];
}

export interface DayOfWeekDto {
  day: string;
  ratio: number;
}

export interface HourOfDayDto {
  hour: number;
  count: number;
}

export interface AppointmentAnalyticsDto {
  bookedKpi: KpiDto;
  completedKpi: KpiDto;
  cancelledKpi: KpiDto;
  avgLeadTimeKpi: DecimalKpiDto;
  dailySeries: TimeSeriesDto;
  byDayOfWeek: DayOfWeekDto[];
  byHourOfDay: HourOfDayDto[];
}

export interface SpecialtyAmountDto {
  specialty: string;
  amount: number;
}

export interface WeeklySpecialtyDto {
  week: string;
  segments: SpecialtyAmountDto[];
}

export interface PaymentStatusDto {
  label: string;
  amount: number;
  percentage: number;
}

export interface ClinicRevenueDto {
  name: string;
  consults: number;
  arpu: number;
  revenue: number;
  deltaPct: number;
}

export interface RevenueAnalyticsDto {
  grossKpi: DecimalKpiDto;
  netKpi: DecimalKpiDto;
  refundsKpi: DecimalKpiDto;
  arpuKpi: DecimalKpiDto;
  weeklyBySpecialty: WeeklySpecialtyDto[];
  paymentStatusBreakdown: PaymentStatusDto[];
  clinicRevenue: ClinicRevenueDto[];
}

export interface MonthDataPointDto {
  month: string;
  count: number;
}

export interface SpecialtySeasonDto {
  specialty: string;
  dataPoints: MonthDataPointDto[];
}

export interface SpecialtyWaitDto {
  specialty: string;
  days: number;
}

export interface HeatmapAnalyticsDto {
  grid: number[][];
  specialtySeries: SpecialtySeasonDto[];
  avgWaitBySpecialty: SpecialtyWaitDto[];
}

export interface RankedDoctorDto {
  name: string;
  specialty: string;
  consults: number;
  completionPct: number;
  rating: number;
  revenue: number;
  trend: number[];
  badge: string | null;
}

export interface TopRatedDoctorDto {
  name: string;
  rating: number;
  reviewCount: number;
  specialty: string;
}

export interface NeedsAttentionDto {
  name: string;
  message: string;
  severity: string;
}

export interface DoctorAnalyticsDto {
  ranked: RankedDoctorDto[];
  topRated: TopRatedDoctorDto[];
  needsAttention: NeedsAttentionDto[];
}

export interface RegionDto {
  name: string;
  consults: number;
  deltaPct: number;
  isTop: boolean;
}

export interface CohortWeekDto {
  cohortLabel: string;
  weeks: number[];
}

export interface CohortRetentionDto {
  cohorts: CohortWeekDto[];
}

export interface GeographyAnalyticsDto {
  regions: RegionDto[];
  retention: CohortRetentionDto;
}

export async function getAnalyticsOverview(period: Period) {
  const { data } = await client.get<AnalyticsOverviewDto>('/api/v1/analytics/overview', { params: { period } });
  return data;
}

export async function getAppointmentAnalytics(period: Period) {
  const { data } = await client.get<AppointmentAnalyticsDto>('/api/v1/analytics/appointments', { params: { period } });
  return data;
}

export async function getRevenueAnalytics(period: Period) {
  const { data } = await client.get<RevenueAnalyticsDto>('/api/v1/analytics/revenue', { params: { period } });
  return data;
}

export async function getHeatmapAnalytics(period: Period) {
  const { data } = await client.get<HeatmapAnalyticsDto>('/api/v1/analytics/heatmap', { params: { period } });
  return data;
}

export async function getDoctorAnalytics(period: Period) {
  const { data } = await client.get<DoctorAnalyticsDto>('/api/v1/analytics/doctors', { params: { period } });
  return data;
}

export async function getGeographyAnalytics(period: Period) {
  const { data } = await client.get<GeographyAnalyticsDto>('/api/v1/analytics/geography', { params: { period } });
  return data;
}
