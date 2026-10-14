import {
  createColumnHelper,
  flexRender,
  getCoreRowModel,
  useReactTable,
} from '@tanstack/react-table';
import type { ColumnDef } from '@tanstack/react-table';
import { useMemo } from 'react';
import { Ban, MoreHorizontal, Pencil, RotateCcw, ShieldAlert, ShieldCheck } from 'lucide-react';

import type { ProgramResponse } from '@/api/schema.schemas';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { cn } from '@/lib/utils';
import { renderGuard } from '@/lib/renderGuard';

export interface ProgramsTableProps {
  rows: readonly ProgramResponse[];
  programs?: readonly ProgramResponse[];
  isFetching?: boolean;
  hasActiveFilters?: boolean;
  onClearFilters?: () => void;
  onEdit?: (program: ProgramResponse) => void;
  onRetire?: (program: ProgramResponse) => void;
  onReinstate?: (program: ProgramResponse) => void;
  onRecognise?: (program: ProgramResponse) => void;
  onWithdrawRecognition?: (program: ProgramResponse) => void;
}

const columnHelper = createColumnHelper<ProgramResponse>();

/**
 * Presentational only. No hooks, no queries, no API calls. Receives rows and action handlers.
 * The component does not fetch.
 */
export function ProgramsTable({
  rows,
  programs,
  isFetching = false,
  hasActiveFilters = false,
  onClearFilters,
  onEdit,
  onRetire,
  onReinstate,
  onRecognise,
  onWithdrawRecognition,
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
        cell: (info) => <span className="font-mono text-sm">{info.getValue()}</span>,
      }),
      columnHelper.accessor('name', {
        header: 'Program',
        cell: (info) => <span className="font-medium">{info.getValue()}</span>,
      }),
      columnHelper.accessor('departmentName', {
        header: 'Department',
      }),
      columnHelper.accessor('academicLevelCode', {
        header: 'Level',
        cell: (info) => <Badge variant="secondary">{info.getValue()}</Badge>,
      }),
      columnHelper.accessor('regulator', {
        header: 'Regulator',
        cell: (info) => {
          const row = info.row.original;
          return (
            <div className="flex flex-col">
              <span className="text-muted-foreground text-sm">{info.getValue() ?? '—'}</span>
              <span className="text-xs text-muted-foreground">
                {row.isRegulatorRecognised ? 'Recognised' : 'Unrecognised'}
              </span>
            </div>
          );
        },
      }),
      columnHelper.accessor('requiredCredits', {
        header: 'Credits',
        cell: (info) => <span className="tabular-nums">{info.getValue()}</span>,
      }),
      columnHelper.accessor('lifecycle', {
        header: 'Lifecycle',
        cell: (info) => (
          <Badge variant={info.getValue() === 'Active' ? 'default' : 'outline'}>
            {info.getValue()}
          </Badge>
        ),
      }),
      columnHelper.accessor('canAcceptStudents', {
        header: 'Accepting',
        cell: (info) => (
          <span className={cn('text-sm', !info.getValue() && 'text-muted-foreground')}>
            {info.getValue() ? 'Yes' : 'No'}
          </span>
        ),
      }),
      columnHelper.display({
        id: 'actions',
        header: () => <span className="sr-only">Actions</span>,
        cell: (info) => {
          const program = info.row.original;
          const isRetired = program.lifecycle === 'Retired';
          const isRecognised = program.isRegulatorRecognised;

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
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8"
                    title="More actions"
                  >
                    <MoreHorizontal className="h-4 w-4" />
                    <span className="sr-only">Open actions menu</span>
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-48">
                  {isRetired ? (
                    <DropdownMenuItem
                      onClick={() => onReinstate?.(program)}
                      className="cursor-pointer"
                    >
                      <RotateCcw className="mr-2 h-4 w-4" />
                      <span>Reinstate</span>
                    </DropdownMenuItem>
                  ) : (
                    <DropdownMenuItem
                      onClick={() => onRetire?.(program)}
                      className="cursor-pointer text-destructive focus:text-destructive"
                    >
                      <Ban className="mr-2 h-4 w-4" />
                      <span>Retire</span>
                    </DropdownMenuItem>
                  )}
                  {isRecognised ? (
                    <DropdownMenuItem
                      onClick={() => onWithdrawRecognition?.(program)}
                      className="cursor-pointer text-destructive focus:text-destructive"
                    >
                      <ShieldAlert className="mr-2 h-4 w-4" />
                      <span>Withdraw Recognition</span>
                    </DropdownMenuItem>
                  ) : (
                    <DropdownMenuItem
                      disabled={isRetired}
                      onClick={() => {
                        if (!isRetired) {
                          onRecognise?.(program);
                        }
                      }}
                      className={cn(
                        'cursor-pointer',
                        isRetired && 'cursor-not-allowed opacity-50',
                      )}
                      title={isRetired ? 'Retired programs cannot be recognised' : undefined}
                    >
                      <ShieldCheck className="mr-2 h-4 w-4" />
                      <span>Recognise</span>
                      {isRetired && (
                        <span className="ml-auto text-xs text-muted-foreground">(Retired)</span>
                      )}
                    </DropdownMenuItem>
                  )}
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          );
        },
      }),
    ],
    [onEdit, onRetire, onReinstate, onRecognise, onWithdrawRecognition],
  );

  // `rows` arrives from the query cache and is already referentially stable. Spreading it into a new
  // array destroyed that stability, which is half of the same loop.
  const programList = programs ?? rows;
  const data = useMemo(() => programList, [programList]);

  const table = useReactTable({
    data: data as ProgramResponse[],
    columns,
    getCoreRowModel: getCoreRowModel(),
  });

  if (data.length === 0) {
    return (
      <div className="rounded-lg border border-dashed p-8 text-center">
        <p className="text-muted-foreground text-sm">
          {hasActiveFilters
            ? 'No programs match your current filters.'
            : 'No academic programs found.'}
        </p>
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
    <div
      className={cn(
        'overflow-x-auto rounded-lg border transition-opacity duration-200',
        isFetching && 'opacity-60',
      )}
    >
      <table className="w-full min-w-[750px] border-collapse text-sm">
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
