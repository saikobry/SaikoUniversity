import { useGetApiV1Programs } from '@/api';

/**
 * Every data-fetching hook for the programs slice lives here, not in a component.
 *
 * Components receive the result and render it. They never call useQuery themselves. That keeps
 * cache keys in one place and makes the mutation invalidations below the only place that decides
 * what a write should refresh.
 */
export function usePrograms() {
  return useGetApiV1Programs({
    // The list endpoint is paged. Ask for a page large enough that a registrar sees their whole
    // catalogue, so the table has no paging control to own yet.
    page: 1,
    pageSize: 50,
  });
}
