import { useCallback, useMemo, useState } from 'react';
import { Plus, Search } from 'lucide-react';

import type { CreateProgramRequest, ProgramResponse, UpdateProgramRequest } from '@/api/schema.schemas';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { CreateProgramModal } from '@/features/programs/CreateProgramModal';
import { EditProgramModal } from '@/features/programs/EditProgramModal';
import { ProgramsTable } from '@/features/programs/ProgramsTable';
import { useCreateProgram, usePrograms, useUpdateProgram } from '@/features/programs/usePrograms';
import { renderGuard } from '@/lib/renderGuard';

/**
 * Route component. Owns loading, error, and modal orchestration, then delegates
 * rendering to presentational components. It calls feature hooks, never raw useQuery/useMutation.
 */
export function ProgramsPage() {
  renderGuard.check('ProgramsPage');
  const [searchTerm, setSearchTerm] = useState('');
  const [acceptingOnly, setAcceptingOnly] = useState(false);

  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  const [editingProgram, setEditingProgram] = useState<ProgramResponse | null>(null);
  const [editError, setEditError] = useState<string | null>(null);

  const { data, isPending, isError, error, refetch, isFetching } = usePrograms();

  const createMutation = useCreateProgram();
  const updateMutation = useUpdateProgram();

  const rows = useMemo(() => data?.data.items ?? [], [data]);
  const total = Number(data?.data.totalCount ?? 0);

  // Derive available level and department options from the loaded dataset
  const levels = useMemo(() => {
    const map = new Map<string, string>();
    for (const r of rows) {
      if (r.academicLevelId && r.academicLevelCode) {
        map.set(r.academicLevelId, r.academicLevelCode);
      }
    }
    return Array.from(map.entries()).map(([id, name]) => ({ id, name }));
  }, [rows]);

  const departments = useMemo(() => {
    const map = new Map<string, string>();
    for (const r of rows) {
      if (r.departmentId && r.departmentName) {
        map.set(r.departmentId, r.departmentName);
      }
    }
    return Array.from(map.entries()).map(([id, name]) => ({ id, name }));
  }, [rows]);

  const handleCreate = async (request: CreateProgramRequest) => {
    setCreateError(null);
    try {
      await createMutation.mutateAsync({ data: request });
      setIsCreateOpen(false);
    } catch (err) {
      setCreateError(err instanceof Error ? err.message : 'Could not create program.');
    }
  };

  const handleUpdate = async (id: string, request: UpdateProgramRequest) => {
    setEditError(null);
    try {
      await updateMutation.mutateAsync({ id, data: request });
      setEditingProgram(null);
    } catch (err) {
      setEditError(err instanceof Error ? err.message : 'Could not update program.');
    }
  };

  const handleEdit = useCallback((program: ProgramResponse) => {
    setEditError(null);
    setEditingProgram(program);
  }, []);

  const handleClearFilters = useCallback(() => {
    setSearchTerm('');
    setAcceptingOnly(false);
  }, []);

  const handleToggleRetire = useCallback(
    async (program: ProgramResponse) => {
      const isRetired = program.lifecycle === 'Retired';
      const actionLabel = isRetired ? 'reinstate' : 'retire';
      if (!window.confirm(`Are you sure you want to ${actionLabel} ${program.code}?`)) {
        return;
      }

      try {
        await updateMutation.mutateAsync({
          id: program.id,
          data: {
            name: program.name,
            requiredCredits: program.requiredCredits,
            isRegulatorRecognised: program.isRegulatorRecognised,
            isRetired: !isRetired,
          },
        });
      } catch (err) {
        alert(`Could not update lifecycle: ${err instanceof Error ? err.message : 'Unknown error.'}`);
      }
    },
    [updateMutation],
  );

  // Only bail out when there is genuinely nothing to render yet. Typing in the search box changes the
  // query key, which puts React Query into a fresh pending state for that key, so this branch used to
  // fire on every keystroke, unmount the whole page including the search input, and steal focus.
  if (isPending && !data) {
    return <p className="text-muted-foreground text-sm">Loading programs…</p>;
  }

  if (isError) {
    return (
      <div className="space-y-3">
        <p className="text-destructive text-sm">
          Could not load programs. {error instanceof Error ? error.message : 'Unknown error.'}
        </p>
        <Button variant="outline" size="sm" onClick={() => void refetch()}>
          Retry
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Programs</h1>
          <p className="text-muted-foreground text-sm">
            {total} {total === 1 ? 'program' : 'programs'} in the catalogue
          </p>
        </div>

        <div className="flex items-center gap-2">
          {isFetching && <span className="text-muted-foreground text-xs mr-2">Refreshing…</span>}
          <Button onClick={() => { setCreateError(null); setIsCreateOpen(true); }}>
            <Plus className="h-4 w-4 mr-1" />
            New Program
          </Button>
        </div>
      </div>

      <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3">
        <div className="relative w-full sm:max-w-xs">
          <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search code or title…"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="pl-8"
          />
        </div>

        <label className="flex items-center gap-2 text-sm text-muted-foreground cursor-pointer select-none">
          <input
            type="checkbox"
            checked={acceptingOnly}
            onChange={(e) => setAcceptingOnly(e.target.checked)}
            className="h-4 w-4 rounded border-input"
          />
          <span>Accepting students only</span>
        </label>
      </div>

      <ProgramsTable
        rows={rows}
        searchTerm={searchTerm}
        acceptingOnly={acceptingOnly}
        onClearFilters={handleClearFilters}
        onEdit={handleEdit}
        onToggleRetire={handleToggleRetire}
      />

      <CreateProgramModal
        open={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
        levels={levels}
        departments={departments}
        onSubmit={handleCreate}
        isSubmitting={createMutation.isPending}
        errorMessage={createError}
      />

      <EditProgramModal
        program={editingProgram}
        open={editingProgram !== null}
        onClose={() => setEditingProgram(null)}
        onSubmit={handleUpdate}
        isSubmitting={updateMutation.isPending}
        errorMessage={editError}
      />
    </div>
  );
}
