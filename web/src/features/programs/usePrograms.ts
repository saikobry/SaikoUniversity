import { keepPreviousData, useQueryClient } from '@tanstack/react-query';

import {
  getGetApiV1ProgramsQueryKey,
  useGetApiV1Programs,
  usePostApiV1Programs,
  usePostApiV1ProgramsIdRecognise,
  usePostApiV1ProgramsIdReinstate,
  usePostApiV1ProgramsIdRetire,
  usePostApiV1ProgramsIdWithdrawRecognition,
  usePutApiV1ProgramsId,
} from '@/api';
import type { ProgramResponse } from '@/api/schema.schemas';

export interface UseProgramsParams {
  page?: number;
  pageSize?: number;
  search?: string;
  canAcceptStudents?: boolean;
}

/**
 * Every data-fetching and mutation hook for the programs slice lives here, not in a component.
 *
 * Components receive results and trigger mutations. They never call useQuery/useMutation directly.
 * That keeps cache keys in one place and makes the mutation invalidations below the only place
 * that decides what a write should refresh.
 */
export function usePrograms(params?: UseProgramsParams) {
  const { page, pageSize, search, canAcceptStudents } = params ?? {};

  return useGetApiV1Programs(
    {
      page,
      pageSize,
      search,
      canAcceptStudents,
    },
    {
      query: {
        placeholderData: keepPreviousData,
      },
    },
  );
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

export function useRetireProgram() {
  const queryClient = useQueryClient();

  return usePostApiV1ProgramsIdRetire({
    mutation: {
      onSuccess: () => {
        void queryClient.invalidateQueries({
          queryKey: getGetApiV1ProgramsQueryKey(),
        });
      },
    },
  });
}

export function useReinstateProgram() {
  const queryClient = useQueryClient();

  return usePostApiV1ProgramsIdReinstate({
    mutation: {
      onSuccess: () => {
        void queryClient.invalidateQueries({
          queryKey: getGetApiV1ProgramsQueryKey(),
        });
      },
    },
  });
}

export function useRecogniseProgram() {
  const queryClient = useQueryClient();

  return usePostApiV1ProgramsIdRecognise({
    mutation: {
      onSuccess: () => {
        void queryClient.invalidateQueries({
          queryKey: getGetApiV1ProgramsQueryKey(),
        });
      },
    },
  });
}

export function useWithdrawRecognitionProgram() {
  const queryClient = useQueryClient();

  return usePostApiV1ProgramsIdWithdrawRecognition({
    mutation: {
      onSuccess: () => {
        void queryClient.invalidateQueries({
          queryKey: getGetApiV1ProgramsQueryKey(),
        });
      },
    },
  });
}
