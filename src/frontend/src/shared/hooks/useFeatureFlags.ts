import { useQuery } from '@tanstack/react-query';
import { getMyFeatures } from '../api/clinics';
import { useAuth } from '../components/AuthProvider';

export function useFeatureFlags() {
  const { token } = useAuth();

  const { data, isLoading } = useQuery({
    queryKey: ['feature-flags'],
    queryFn: getMyFeatures,
    enabled: !!token,
    refetchOnWindowFocus: true,
    staleTime: 60_000,
  });

  const flags: Record<string, boolean> = {};
  if (data) {
    for (const f of data) {
      flags[f.featureKey] = f.isEnabled;
    }
  }

  function hasFeature(key: string): boolean {
    return flags[key] ?? false;
  }

  return { flags, isLoading, hasFeature };
}
