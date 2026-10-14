# programs — Programs vertical slice

## Purpose

Programs (academic programmes) vertical slice: pages, table, modals, dialogs, hooks.

## Ownership

- `ProgramsPage.tsx` — route component, state orchestration, action feedback
- `ProgramsTable.tsx` — pure presentational table with `getCoreRowModel` only, row action menus, and badge indicators
- `CreateProgramModal.tsx` — modal dialog for program creation
- `EditProgramModal.tsx` — modal dialog for updating program title and required credits
- `ConfirmProgramActionDialog.tsx` — confirmation modal for destructive lifecycle transitions
- `usePrograms.ts` — TanStack Query hooks for queries and mutation actions
- `useProgramFilters.ts` — URL-synced search, filter, and pagination state hook

## Local Contracts

### Server-side pagination and filter synchronization

- **Pagination model**: Query parameters include `page` (1-indexed) and `pageSize` (options: 10, 25, 50; default: 25). The API response provides `pageCount` and `totalCount`.
- **Query integration**: `usePrograms` passes `page`, `pageSize`, `search`, and `canAcceptStudents` to the API endpoint and configures `placeholderData: keepPreviousData` to prevent layout flicker across page transitions.
- **URL synchronization (`useProgramFilters`)**:
  - Filter state (`search`, `status`, `page`, `pageSize`) is bidirectionally synchronized with URL query parameters via TanStack Router native `validateSearch` in `src/routes/index.tsx` (plain TypeScript validator without schema libraries).
  - Multi-word URL values use kebab-case (`not-accepting`), with backward-compatible normalization for legacy `notAccepting`.
  - Text search is debounced (300ms) before committing to URL with history replacement (`replace: true`).
  - Search or status filter updates atomically reset `page` to 1.
  - Status options: `'all' | 'accepting' | 'not-accepting'`.
  - Clean URLs omit default parameters (`search=""`, `status="all"`, `page=1`, `pageSize=25`).
  - Out-of-bounds page self-healing automatically clamps `page` to `pageCount` if total pages decrease.
- **Pagination component**: Reusable pagination control in `web/src/components/ui/pagination.tsx` renders item range, total items, page navigation controls (first, previous, next, last), and page size selector.

### Table presentation

- **Pure presentation**: `ProgramsTable.tsx` is strictly presentational. It initializes TanStack Table with `getCoreRowModel()` only, with no client-side filtering or pagination models.
- **Referential stability**: Data rows passed from `ProgramsPage` are memoized and preserved directly without spreading.
- **Visual feedback**: Displays background refresh state (`isFetching`) through reduced opacity and provides distinct empty states for filtered vs. empty catalogue views.

### Form contract

- Required credits: positive integer. Validate with `Number.isInteger` and `> 0` before submit.
- Code, name, academic level, and department are required on create. Name and required credits are editable.
- Update requests (`UpdateProgramRequest`) carry only `name` and `requiredCredits`.
- Code, level, department, regulator, and credit basis are immutable after creation.
- Academic levels and departments use reference-data read endpoints (`/api/v1/reference-data`) and render as selects.

### Lifecycle transition endpoints

Lifecycle and regulator recognition status are managed through dedicated transition endpoints rather than general update payloads:

- `POST /api/v1/programs/{id}/retire` — transitions active program to retired status (halts admissions)
- `POST /api/v1/programs/{id}/reinstate` — transitions retired program back to active status
- `POST /api/v1/programs/{id}/recognise` — marks program as recognized by its regulator
- `POST /api/v1/programs/{id}/withdraw-recognition` — removes regulator recognition from the program

### Asymmetric confirmation UX

Lifecycle interactions follow asymmetric confirmation patterns based on operation risk:

- **Destructive actions** (`retire`, `withdraw-recognition`): Require explicit confirmation via `ConfirmProgramActionDialog.tsx` before executing the mutation.
- **Restorative / non-destructive actions** (`reinstate`, `recognise`): Execute immediately on action selection without a confirmation prompt, reporting outcome via inline feedback/toast alert.
