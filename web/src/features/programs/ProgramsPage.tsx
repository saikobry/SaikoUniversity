import { ProgramsTable } from '@/features/programs/ProgramsTable';
import { usePrograms } from '@/features/programs/usePrograms';

/**
 * Route component. Owns loading and error states, then delegates rendering to the table.
 * It does not call useQuery directly. That is the feature hook's job.
 */
export function ProgramsPage() {
  const { data, isPending, isError, error, refetch, isFetching } = usePrograms();

  if (isPending) {
    return <p className="text-muted-foreground text-sm">Loading programs…</p>;
  }

  if (isError) {
    return (
      <div className="space-y-3">
        <p className="text-destructive text-sm">
          Could not load programs. {error instanceof Error ? error.message : 'Unknown error.'}
        </p>
        <button
          type="button"
          onClick={() => void refetch()}
          className="rounded-md border px-3 py-1.5 text-sm hover:bg-accent"
        >
          Retry
        </button>
      </div>
    );
  }

  const rows = data?.data.items ?? [];
  const total = Number(data?.data.totalCount ?? 0);

  return (
    <div className="space-y-6">
      <div className="flex items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Programs</h1>
          <p className="text-muted-foreground text-sm">
            {total} {total === 1 ? 'program' : 'programs'} in the catalogue
          </p>
        </div>
        {isFetching && <span className="text-muted-foreground text-xs">Refreshing…</span>}
      </div>

      {rows.length === 0 ? (
        <p className="text-muted-foreground text-sm">No programs match this view.</p>
      ) : (
        <ProgramsTable rows={rows} />
      )}
    </div>
  );
}
