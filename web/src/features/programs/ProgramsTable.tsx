import {
  createColumnHelper,
  flexRender,
  getCoreRowModel,
  getFilteredRowModel,
  useReactTable,
} from '@tanstack/react-table';
import type { ColumnDef, ColumnFiltersState } from '@tanstack/react-table';
import { useMemo } from 'react';
import { Pencil, RotateCcw, Ban } from 'lucide-react';

import type { ProgramResponse } from '@/api/schema.schemas';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { renderGuard } from '@/lib/renderGuard';

interface ProgramsTableProps {
  rows: readonly ProgramResponse[];
  globalFilter?: string;
  searchTerm?: string;
  acceptingOnly?: boolean;
  onClearFilters?: () => void;
  onEdit?: (program: ProgramResponse) => void;
  onToggleRetire?: (program: ProgramResponse) => void;
}

const columnHelper = createColumnHelper<ProgramResponse>();

const canGlobalFilterColumn = (column: { id: string }) =>
  column.id === 'code' || column.id === 'name';

/**
 * Presentational only. No hooks, no queries, no API calls. Receives rows and action handlers.
 * Sorting and filtering are handled via TanStack Table; the component does not fetch.
 */
export function ProgramsTable({
  rows,
  globalFilter,
  searchTerm,
  acceptingOnly = false,
  onClearFilters,
  onEdit,
  onToggleRetire,
}: ProgramsTableProps) {
  renderGuard.check('ProgramsTable');

  // Memoized because TanStack Table re-enters its row model whenever `columns` changes identity.
  // A fresh array here is the documented cause of an infinite render loop the moment anything
  // changes table state, such as a parent re-rendering when a modal opens.
  // See https://tanstack.com/table/v8/docs/faq
  const columns = useMemo<ColumnDef<ProgramResponse, any>[]>(
    () => [
      columnHelper.accessor('code', {
        header: 'Code',
        enableGlobalFilter: true,
        cell: (info) => <span className="font-mono text-sm">{info.getValue()}</span>,
      }),
      columnHelper.accessor('name', {
        header: 'Program',
        enableGlobalFilter: true,
        cell: (info) => <span className="font-medium">{info.getValue()}</span>,
      }),
      columnHelper.accessor('departmentName', {
        header: 'Department',
        enableGlobalFilter: false,
      }),
      columnHelper.accessor('academicLevelCode', {
        header: 'Level',
        enableGlobalFilter: false,
        cell: (info) => <Badge variant="secondary">{info.getValue()}</Badge>,
      }),
      columnHelper.accessor('regulator', {
        header: 'Regulator',
        enableGlobalFilter: false,
        cell: (info) => (
          <span className="text-muted-foreground text-sm">{info.getValue() ?? '—'}</span>
        ),
      }),
      columnHelper.accessor('requiredCredits', {
        header: 'Credits',
        enableGlobalFilter: false,
        cell: (info) => <span className="tabular-nums">{info.getValue()}</span>,
      }),
      columnHelper.accessor('lifecycle', {
        header: 'Lifecycle',
        enableGlobalFilter: false,
        cell: (info) => (
          <Badge variant={info.getValue() === 'Active' ? 'default' : 'outline'}>
            {info.getValue()}
          </Badge>
        ),
      }),
      columnHelper.accessor('canAcceptStudents', {
        header: 'Accepting',
        enableGlobalFilter: false,
        filterFn: 'equals',
        cell: (info) => (
          <span className={cn('text-sm', !info.getValue() && 'text-muted-foreground')}>
            {info.getValue() ? 'Yes' : 'No'}
          </span>
        ),
      }),
      columnHelper.display({
        id: 'actions',
        enableGlobalFilter: false,
        header: () => <span className="sr-only">Actions</span>,
        cell: (info) => {
          const program = info.row.original;
          const isRetired = program.lifecycle === 'Retired';

          return (
            <div className="flex items-center justify-end gap-1">
              {onEdit && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => onEdit(program)}
                  className="h-8 px-2 text-xs"
                  title="Edit program"
                >
                  <Pencil className="h-3.5 w-3.5 mr-1" />
                  Edit
                </Button>
              )}
              {onToggleRetire && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => onToggleRetire(program)}
                  className={cn(
                    'h-8 px-2 text-xs',
                    isRetired
                      ? 'text-foreground hover:bg-accent'
                      : 'text-destructive hover:bg-destructive/10',
                  )}
                  title={isRetired ? 'Reinstate program' : 'Retire program'}
                >
                  {isRetired ? (
                    <>
                      <RotateCcw className="h-3.5 w-3.5 mr-1" />
                      Reinstate
                    </>
                  ) : (
                    <>
                      <Ban className="h-3.5 w-3.5 mr-1" />
                      Retire
                    </>
                  )}
                </Button>
              )}
            </div>
          );
        },
      }),
    ],
    [onEdit, onToggleRetire],
  );

  const filterText = (globalFilter ?? searchTerm ?? '').trim();
  const hasActiveFilters = Boolean(filterText || acceptingOnly);

  const columnFilters = useMemo<ColumnFiltersState>(
    () => (acceptingOnly ? [{ id: 'canAcceptStudents', value: true }] : []),
    [acceptingOnly],
  );

  const state = useMemo(
    () => ({
      globalFilter: filterText || undefined,
      columnFilters,
    }),
    [filterText, columnFilters],
  );

  // `rows` arrives from the query cache and is already referentially stable. Spreading it into a new
  // array destroyed that stability, which is half of the same loop.
  const data = useMemo(() => rows, [rows]);

  const table = useReactTable({
    data: data as ProgramResponse[],
    columns,
    state,
    getCoreRowModel: getCoreRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    getColumnCanGlobalFilter: canGlobalFilterColumn,
    globalFilterFn: 'includesString',
  });

  if (table.getRowModel().rows.length === 0) {
    return (
      <div className="rounded-lg border border-dashed p-8 text-center">
        <p className="text-muted-foreground text-sm">No programs match this view.</p>
        {hasActiveFilters && onClearFilters && (
          <Button
            variant="link"
            size="sm"
            onClick={onClearFilters}
            className="mt-2"
          >
            Clear filters
          </Button>
        )}
      </div>
    );
  }

  return (
    <div className="overflow-hidden rounded-lg border">
      <table className="w-full border-collapse text-sm">
        <thead className="bg-muted/50">
          {table.getHeaderGroups().map((headerGroup) => (
            <tr key={headerGroup.id} className="border-b">
              {headerGroup.headers.map((header) => (
                <th
                  key={header.id}
                  className="text-muted-foreground px-4 py-3 text-left font-medium"
                >
                  {flexRender(header.column.columnDef.header, header.getContext())}
                </th>
              ))}
            </tr>
          ))}
        </thead>
        <tbody>
          {table.getRowModel().rows.map((row) => (
            <tr key={row.id} className="border-b last:border-0 hover:bg-muted/30">
              {row.getVisibleCells().map((cell) => (
                <td key={cell.id} className="px-4 py-3 align-middle">
                  {flexRender(cell.column.columnDef.cell, cell.getContext())}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
