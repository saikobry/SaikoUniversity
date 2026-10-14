import { useCallback, useMemo, useState } from 'react';
import { Plus, Search } from 'lucide-react';

import type { CreateProgramRequest, ProgramResponse, UpdateProgramRequest } from '@/api/schema.schemas';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { ConfirmProgramActionDialog } from '@/features/programs/ConfirmProgramActionDialog';
import { CreateProgramModal } from '@/features/programs/CreateProgramModal';
import { EditProgramModal } from '@/features/programs/EditProgramModal';
import { ProgramsTable } from '@/features/programs/ProgramsTable';
import {
  useCreateProgram,
  usePrograms,
  useRecogniseProgram,
  useReinstateProgram,
  useRetireProgram,
  useUpdateProgram,
  useWithdrawRecognitionProgram,
} from '@/features/programs/usePrograms';
import { useAcademicLevels, useDepartments } from '@/features/reference-data/useReferenceData';
import { cn } from '@/lib/utils';
import { renderGuard } from '@/lib/renderGuard';

type DestructiveActionType = 'retire' | 'withdrawRecognition';

interface PendingDestructiveAction {
  type: DestructiveActionType;
  program: ProgramResponse;
}

interface ActionFeedback {
  type: 'success' | 'error';
  message: string;
}

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

  const [pendingDestructiveAction, setPendingDestructiveAction] =
    useState<PendingDestructiveAction | null>(null);
  const [actionFeedback, setActionFeedback] = useState<ActionFeedback | null>(null);

  const { data, isPending, isError, error, refetch, isFetching } = usePrograms();
  const {
    data: levelsData,
    isPending: levelsPending,
    isError: levelsError,
  } = useAcademicLevels();
  const {
    data: departmentsData,
    isPending: departmentsPending,
    isError: departmentsError,
  } = useDepartments();

  const createMutation = useCreateProgram();
  const updateMutation = useUpdateProgram();
  const retireMutation = useRetireProgram();
  const reinstateMutation = useReinstateProgram();
  const recogniseMutation = useRecogniseProgram();
  const withdrawRecognitionMutation = useWithdrawRecognitionProgram();

  const rows = useMemo(() => data?.data.items ?? [], [data]);
  const total = Number(data?.data.totalCount ?? 0);

  const levels = useMemo(
    () =>
      (levelsData?.data ?? []).map((x) => ({
        id: x.id,
        name: x.code,
      })),
    [levelsData],
  );

  const departments = useMemo(
    () =>
      (departmentsData?.data ?? []).map((x) => ({
        id: x.id,
        name: x.name,
      })),
    [departmentsData],
  );

  const handleCreate = async (request: CreateProgramRequest) => {
    setCreateError(null);
    try {
      await createMutation.mutateAsync({ data: request });
      setIsCreateOpen(false);
      setActionFeedback({
        type: 'success',
        message: `Program ${request.code} created successfully.`,
      });
    } catch (err) {
      setCreateError(err instanceof Error ? err.message : 'Could not create program.');
    }
  };

  const handleUpdate = async (id: string, request: UpdateProgramRequest) => {
    setEditError(null);
    try {
      await updateMutation.mutateAsync({ id, data: request });
      setEditingProgram(null);
      setActionFeedback({
        type: 'success',
        message: 'Program updated successfully.',
      });
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

  // Destructive actions: require confirmation dialog
  const handleInitiateRetire = useCallback((program: ProgramResponse) => {
    setActionFeedback(null);
    setPendingDestructiveAction({ type: 'retire', program });
  }, []);

  const handleInitiateWithdrawRecognition = useCallback((program: ProgramResponse) => {
    setActionFeedback(null);
    setPendingDestructiveAction({ type: 'withdrawRecognition', program });
  }, []);

  // Restorative actions: execute immediately without dialog confirmation
  const handleReinstate = useCallback(
    async (program: ProgramResponse) => {
      setActionFeedback(null);
      try {
        await reinstateMutation.mutateAsync({ id: program.id });
        setActionFeedback({
          type: 'success',
          message: `Program ${program.code} has been reinstated to Active status.`,
        });
      } catch (err) {
        setActionFeedback({
          type: 'error',
          message:
            err instanceof Error ? err.message : `Failed to reinstate program ${program.code}.`,
        });
      }
    },
    [reinstateMutation],
  );

  const handleRecognise = useCallback(
    async (program: ProgramResponse) => {
      setActionFeedback(null);
      try {
        await recogniseMutation.mutateAsync({ id: program.id });
        setActionFeedback({
          type: 'success',
          message: `Program ${program.code} is now marked as recognized by ${program.regulator || 'the regulator'}.`,
        });
      } catch (err) {
        setActionFeedback({
          type: 'error',
          message:
            err instanceof Error ? err.message : `Failed to recognise program ${program.code}.`,
        });
      }
    },
    [recogniseMutation],
  );

  const handleConfirmDestructiveAction = async () => {
    if (!pendingDestructiveAction) return;
    const { type, program } = pendingDestructiveAction;
    try {
      if (type === 'retire') {
        await retireMutation.mutateAsync({ id: program.id });
        setActionFeedback({
          type: 'success',
          message: `Program ${program.code} has been retired. Student admissions are halted.`,
        });
      } else {
        await withdrawRecognitionMutation.mutateAsync({ id: program.id });
        setActionFeedback({
          type: 'success',
          message: `Regulator recognition for program ${program.code} has been withdrawn.`,
        });
      }
      setPendingDestructiveAction(null);
    } catch (err) {
      setActionFeedback({
        type: 'error',
        message:
          err instanceof Error
            ? err.message
            : `Failed to execute action for program ${program.code}.`,
      });
      setPendingDestructiveAction(null);
    }
  };

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

  const isDestructivePending =
    retireMutation.isPending || withdrawRecognitionMutation.isPending;

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
          <Button
            onClick={() => { setCreateError(null); setIsCreateOpen(true); }}
            disabled={levelsPending || departmentsPending}
          >
            <Plus className="h-4 w-4 mr-1" />
            New Program
          </Button>
        </div>
      </div>

      {(levelsError || departmentsError) && (
        <div className="rounded-md border border-destructive/50 bg-destructive/10 p-3 text-destructive text-sm" role="alert">
          Could not load reference data (academic levels or departments). Creating new programs is unavailable.
        </div>
      )}

      {actionFeedback && (
        <div
          className={cn(
            'flex items-center justify-between rounded-md border p-3 text-sm',
            actionFeedback.type === 'success'
              ? 'border-primary/20 bg-primary/5 text-primary'
              : 'border-destructive/30 bg-destructive/10 text-destructive',
          )}
          role={actionFeedback.type === 'error' ? 'alert' : 'status'}
        >
          <span>{actionFeedback.message}</span>
          <button
            type="button"
            onClick={() => setActionFeedback(null)}
            className="text-xs hover:underline cursor-pointer font-medium ml-4"
          >
            Dismiss
          </button>
        </div>
      )}

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
        onRetire={handleInitiateRetire}
        onReinstate={handleReinstate}
        onRecognise={handleRecognise}
        onWithdrawRecognition={handleInitiateWithdrawRecognition}
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

      <ConfirmProgramActionDialog
        open={pendingDestructiveAction !== null}
        onOpenChange={(open) => {
          if (!open) setPendingDestructiveAction(null);
        }}
        title={
          pendingDestructiveAction?.type === 'retire'
            ? `Retire Program: ${pendingDestructiveAction.program.code}`
            : `Withdraw Recognition: ${pendingDestructiveAction?.program.code}`
        }
        description={
          pendingDestructiveAction?.type === 'retire'
            ? `Retiring this program halts all future student admissions and transitions its lifecycle to Retired. Existing enrolled students remain on record. Are you sure you want to retire "${pendingDestructiveAction.program.name}"?`
            : `Withdrawing regulator recognition marks this program as unrecognised and immediately halts new student admissions. Are you sure you want to withdraw recognition from "${pendingDestructiveAction?.program.name}"?`
        }
        confirmLabel={
          pendingDestructiveAction?.type === 'retire'
            ? 'Retire Program'
            : 'Withdraw Recognition'
        }
        variant="destructive"
        isPending={isDestructivePending}
        onConfirm={handleConfirmDestructiveAction}
      />
    </div>
  );
}
