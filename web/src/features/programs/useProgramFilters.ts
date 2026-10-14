import { useCallback, useEffect, useState } from 'react';
import { getRouteApi } from '@tanstack/react-router';

import type {
  EnrollmentFilter,
  PageSizeOption,
  ProgramsSearch,
} from '@/routes/index';

export type { EnrollmentFilter, PageSizeOption, ProgramsSearch };
export type ProgramStatusFilter = EnrollmentFilter;
export type ProgramPageSize = PageSizeOption;

export interface UseProgramFiltersReturn {
  page: number;
  pageSize: PageSizeOption;
  search?: string;
  canAcceptStudents?: boolean;
  searchInput: string;
  enrollmentFilter: EnrollmentFilter;
  status: EnrollmentFilter;
  hasActiveFilters: boolean;
  setSearchInput: (value: string) => void;
  setEnrollmentFilter: (filter: EnrollmentFilter) => void;
  setStatus: (status: EnrollmentFilter) => void;
  setPage: (page: number) => void;
  setPageSize: (pageSize: number) => void;
  resetFilters: () => void;
}

const route = getRouteApi('/');

/**
 * Manages URL search params, debounced text search, status filters, and pagination state
 * using native TanStack Router search params.
 */
export function useProgramFilters(): UseProgramFiltersReturn {
  const searchParams = route.useSearch();
  const navigate = route.useNavigate();

  const page = searchParams.page ?? 1;
  const pageSize: PageSizeOption = searchParams.pageSize ?? 25;
  const search = searchParams.search;
  const enrollmentFilter: EnrollmentFilter = searchParams.status ?? 'all';
  const status = enrollmentFilter;

  const canAcceptStudents =
    searchParams.status === 'accepting'
      ? true
      : searchParams.status === 'not-accepting'
        ? false
        : undefined;

  const hasActiveFilters = Boolean(searchParams.search || searchParams.status);

  // Local state for instant typing feedback
  const [searchInput, setSearchInput] = useState<string>(searchParams.search ?? '');

  // Sync searchInput when URL search changes externally (e.g. browser navigation or reset)
  useEffect(() => {
    setSearchInput(searchParams.search ?? '');
  }, [searchParams.search]);

  // 300ms debounce: when searchInput differs from search, commit to URL with replace: true and page: undefined
  useEffect(() => {
    const trimmed = searchInput.trim();
    const currentSearch = searchParams.search ?? '';
    if (trimmed === currentSearch) {
      return;
    }

    const timer = setTimeout(() => {
      void navigate({
        search: (prev) => ({
          ...prev,
          search: trimmed || undefined,
          page: undefined,
        }),
        replace: true,
      });
    }, 300);

    return () => clearTimeout(timer);
  }, [searchInput, searchParams.search, navigate]);

  // Changing status updates the param and atomically resets page: undefined (page 1)
  const setEnrollmentFilter = useCallback(
    (filter: EnrollmentFilter) => {
      void navigate({
        search: (prev) => ({
          ...prev,
          status: filter === 'all' ? undefined : filter,
          page: undefined,
        }),
        replace: true,
      });
    },
    [navigate],
  );

  // Changing pageSize updates the param and atomically resets page: undefined (page 1)
  const setPageSize = useCallback(
    (newPageSize: number) => {
      const validSize: PageSizeOption | undefined =
        newPageSize === 10 || newPageSize === 50 ? newPageSize : undefined;
      void navigate({
        search: (prev) => ({
          ...prev,
          pageSize: validSize,
          page: undefined,
        }),
        replace: true,
      });
    },
    [navigate],
  );

  // setPage: update page (omitted if <= 1)
  const setPage = useCallback(
    (newPage: number) => {
      const validPage = Number.isInteger(newPage) && newPage > 1 ? newPage : undefined;
      void navigate({
        search: (prev) => ({
          ...prev,
          page: validPage,
        }),
        replace: true,
      });
    },
    [navigate],
  );

  // resetFilters: clears searchInput and resets search params while preserving custom pageSize
  const resetFilters = useCallback(() => {
    setSearchInput('');
    void navigate({
      search: (prev) => ({
        pageSize: prev.pageSize,
      }),
      replace: true,
    });
  }, [navigate]);

  return {
    page,
    pageSize,
    search,
    canAcceptStudents,
    searchInput,
    enrollmentFilter,
    status,
    hasActiveFilters,
    setSearchInput,
    setEnrollmentFilter,
    setStatus: setEnrollmentFilter,
    setPage,
    setPageSize,
    resetFilters,
  };
}
