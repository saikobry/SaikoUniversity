import { createFileRoute } from '@tanstack/react-router';

import { ProgramsPage } from '@/features/programs/ProgramsPage';

export const ALLOWED_PAGE_SIZES = [10, 25, 50] as const;
export type PageSizeOption = (typeof ALLOWED_PAGE_SIZES)[number];

export const ENROLLMENT_FILTERS = ['all', 'accepting', 'not-accepting'] as const;
export type EnrollmentFilter = (typeof ENROLLMENT_FILTERS)[number];

export interface ProgramsSearch {
  page?: number;
  pageSize?: PageSizeOption;
  search?: string;
  status?: 'accepting' | 'not-accepting';
}

export function validateProgramsSearch(raw: Record<string, unknown>): ProgramsSearch {
  const result: ProgramsSearch = {};

  const rawSearch =
    typeof raw.search === 'string'
      ? raw.search
      : typeof raw.q === 'string'
        ? raw.q
        : undefined;
  if (rawSearch) {
    const trimmed = rawSearch.trim().slice(0, 100).trim();
    if (trimmed.length > 0) {
      result.search = trimmed;
    }
  }

  const rawStatus = raw.status;
  if (rawStatus === 'accepting' || rawStatus === 'not-accepting') {
    result.status = rawStatus;
  } else if (rawStatus === 'notAccepting') {
    result.status = 'not-accepting';
  }

  const rawPage =
    typeof raw.page === 'number'
      ? raw.page
      : typeof raw.page === 'string'
        ? Number(raw.page)
        : undefined;
  if (rawPage !== undefined && Number.isInteger(rawPage) && rawPage > 1) {
    result.page = rawPage;
  }

  const rawPageSize =
    typeof raw.pageSize === 'number'
      ? raw.pageSize
      : typeof raw.pageSize === 'string'
        ? Number(raw.pageSize)
        : undefined;
  if (rawPageSize === 10 || rawPageSize === 50) {
    result.pageSize = rawPageSize;
  }

  return result;
}

export const Route = createFileRoute('/')({
  validateSearch: (search: Record<string, unknown>): ProgramsSearch => validateProgramsSearch(search),
  component: ProgramsPage,
});
