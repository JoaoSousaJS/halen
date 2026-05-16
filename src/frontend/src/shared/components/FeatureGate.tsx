import type { ReactNode } from 'react';
import { useFeatureFlags } from '../hooks/useFeatureFlags';

interface FeatureGateProps {
  feature: string;
  children: ReactNode;
  fallback?: ReactNode;
}

export function FeatureGate({ feature, children, fallback = null }: FeatureGateProps) {
  const { hasFeature, isLoading } = useFeatureFlags();

  if (isLoading) return null;
  if (!hasFeature(feature)) return <>{fallback}</>;
  return <>{children}</>;
}
