import {
  useGetApiV1ReferenceDataAcademicLevels,
  useGetApiV1ReferenceDataDepartments,
} from '@/api';

const REFERENCE_DATA_STALE_TIME_MS = 1000 * 60 * 10; // 10 minutes

/**
 * Every query hook for the reference-data slice lives here, not in a component.
 *
 * Reference data changes rarely, so queries configure a long staleTime (10 minutes)
 * to avoid duplicate network roundtrips across modals and pages.
 */
export function useAcademicLevels() {
  return useGetApiV1ReferenceDataAcademicLevels({
    query: {
      staleTime: REFERENCE_DATA_STALE_TIME_MS,
    },
  });
}

export function useDepartments() {
  return useGetApiV1ReferenceDataDepartments({
    query: {
      staleTime: REFERENCE_DATA_STALE_TIME_MS,
    },
  });
}
