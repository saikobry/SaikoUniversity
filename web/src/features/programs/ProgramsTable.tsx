import {
  createColumnHelper,
  flexRender,
  getCoreRowModel,
  useReactTable,
} from '@tanstack/react-table';

import type { ProgramResponse } from '@/api/schema.schemas';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';

/**
 * Presentational only. No hooks, no queries, no API calls. Receives rows and renders them.
 * Sorting and paging arrive as props; the component does not fetch.
 */
export function ProgramsTable({ rows }: { rows: readonly ProgramResponse[] }) {
  const columnHelper = createColumnHelper<ProgramResponse>();

  const columns = [
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
      cell: (info) => (
        <span className="text-muted-foreground text-sm">{info.getValue() ?? '—'}</span>
      ),
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
  ];

  const table = useReactTable({
    data: [...rows],
    columns,
    getCoreRowModel: getCoreRowModel(),
  });

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
