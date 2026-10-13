import { useQueryClient } from '@tanstack/react-query';

import {
  getGetApiV1ProgramsQueryKey,
  useGetApiV1Programs,
  usePostApiV1Programs,
  usePutApiV1ProgramsId,
} from '@/api';
import type { GetApiV1ProgramsParams, ProgramResponse } from '@/api/schema.schemas';

/**
 * Every data-fetching and mutation hook for the programs slice lives here, not in a component.
 *
 * Components receive results and trigger mutations. They never call useQuery/useMutation directly.
 * That keeps cache keys in one place and makes the mutation invalidations below the only place
 * that decides what a write should refresh.
 */
export function usePrograms(params?: GetApiV1ProgramsParams) {
  return useGetApiV1Programs({
    page: 1,
    pageSize: 50,
    ...params,
  });
}

/**
 * Finds one program in the already-loaded list, keyed by route/record id.
 *
 * Avoids a second request for data the table already has. If the catalogue grows beyond one
 * page, replace this with a real by-id query.
 */
export function useProgram(id: string) {
  const { data, isPending, isError } = usePrograms();
  const program = data?.data?.items?.find((row) => row.id === id) as
    | ProgramResponse
    | undefined;
  return { program, isPending, isError };
}

export function useCreateProgram() {
  const queryClient = useQueryClient();

  return usePostApiV1Programs({
    mutation: {
      onSuccess: () => {
        void queryClient.invalidateQueries({
          queryKey: getGetApiV1ProgramsQueryKey(),
        });
      },
    },
  });
}

export function useUpdateProgram() {
  const queryClient = useQueryClient();

  return usePutApiV1ProgramsId({
    mutation: {
      onSuccess: () => {
        void queryClient.invalidateQueries({
          queryKey: getGetApiV1ProgramsQueryKey(),
        });
      },
    },
  });
}
