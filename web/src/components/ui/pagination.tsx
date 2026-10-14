import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';

export interface PaginationProps {
  page: number;
  pageSize: number;
  totalCount: number | string;
  pageCount: number | string;
  onPageChange: (newPage: number) => void;
  onPageSizeChange?: (newPageSize: number) => void;
  pageSizeOptions?: readonly number[];
  isLoading?: boolean;
  className?: string;
}

const DEFAULT_PAGE_SIZE_OPTIONS: readonly number[] = [10, 25, 50];

export function Pagination({
  page,
  pageSize,
  totalCount,
  pageCount,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = DEFAULT_PAGE_SIZE_OPTIONS,
  isLoading = false,
  className,
}: PaginationProps) {
  const numericTotalCount = Number(totalCount ?? 0);
  const normalizedPageCount = Math.max(1, Number(pageCount ?? 1));

  if (numericTotalCount <= 0) {
    return null;
  }

  const startItem = (page - 1) * pageSize + 1;
  const endItem = Math.min(page * pageSize, numericTotalCount);

  return (
    <nav
      aria-label="Pagination"
      className={cn('flex flex-col sm:flex-row items-center justify-between gap-4 px-2 py-4', className)}
    >
      <div className="text-sm text-muted-foreground">
        Showing {startItem}–{endItem} of {numericTotalCount}
      </div>

      <div className="flex flex-wrap items-center gap-4 sm:gap-6">
        {onPageSizeChange && (
          <div className="flex items-center gap-2">
            <label htmlFor="pagination-page-size" className="text-sm text-muted-foreground sr-only">
              Rows per page
            </label>
            <select
              id="pagination-page-size"
              aria-label="Rows per page"
              value={pageSize}
              disabled={isLoading}
              onChange={(e) => onPageSizeChange(Number(e.target.value))}
              className="h-8 rounded-md border border-input bg-background px-2.5 py-1 text-xs font-medium text-foreground shadow-xs focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
            >
              {pageSizeOptions.map((size) => (
                <option key={size} value={size}>
                  {size} / page
                </option>
              ))}
            </select>
          </div>
        )}

        <div className="text-sm font-medium text-foreground">
          Page {page} of {normalizedPageCount}
        </div>

        <div className="flex items-center gap-1">
          <Button
            variant="outline"
            size="sm"
            onClick={() => onPageChange(1)}
            disabled={page <= 1 || isLoading}
            aria-label="First page"
          >
            <ChevronsLeft className="h-4 w-4" aria-hidden="true" />
            <span className="sr-only">First page</span>
          </Button>

          <Button
            variant="outline"
            size="sm"
            onClick={() => onPageChange(page - 1)}
            disabled={page <= 1 || isLoading}
            aria-label="Previous page"
          >
            <ChevronLeft className="h-4 w-4" aria-hidden="true" />
            <span className="sr-only">Previous page</span>
          </Button>

          <Button
            variant="outline"
            size="sm"
            onClick={() => onPageChange(page + 1)}
            disabled={page >= normalizedPageCount || isLoading}
            aria-label="Next page"
          >
            <ChevronRight className="h-4 w-4" aria-hidden="true" />
            <span className="sr-only">Next page</span>
          </Button>

          <Button
            variant="outline"
            size="sm"
            onClick={() => onPageChange(normalizedPageCount)}
            disabled={page >= normalizedPageCount || isLoading}
            aria-label="Last page"
          >
            <ChevronsRight className="h-4 w-4" aria-hidden="true" />
            <span className="sr-only">Last page</span>
          </Button>
        </div>
      </div>
    </nav>
  );
}
