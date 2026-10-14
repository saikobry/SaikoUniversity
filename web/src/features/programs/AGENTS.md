# programs — Programs vertical slice

## Purpose

Programs (academic programmes) vertical slice: pages, table, modals, dialogs, hooks.

## Ownership

- `ProgramsPage.tsx` — route component, state orchestration, action feedback
- `ProgramsTable.tsx` — presentational table with row action menus and badge indicators
- `CreateProgramModal.tsx` — modal dialog for program creation
- `EditProgramModal.tsx` — modal dialog for updating program title and required credits
- `ConfirmProgramActionDialog.tsx` — confirmation modal for destructive lifecycle transitions
- `usePrograms.ts` — TanStack Query hooks for queries and mutation actions

## Local Contracts

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
