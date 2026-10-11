import { QueryClient } from '@tanstack/react-query';

/**
 * Shared query client.
 *
 * `staleTime` is deliberately non-zero. The programs list changes when a registrar edits it, not
 * every few seconds, so a 30s window avoids a refetch storm when several components mount at once.
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
});
