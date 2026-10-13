# reference-data — Reference data vertical slice

## Purpose

Reference data vertical slice: query hooks for quasi-static lookup tables (academic levels, departments).

## Local Contracts

### Option shape and display mapping

- Consuming components map reference items into an `Option` shape: `{ id: string, name: string }`.
- Academic levels display `Code` (`{ id: x.id, name: x.code }`).
- Departments display `Name` (`{ id: x.id, name: x.name }`).

### Loading and error behavior

- Actions requiring reference data (such as "New Program") must be disabled while reference queries are pending.
- If a reference query fails, display an error banner/message so failures are explicit rather than rendering empty select dropdowns.
- Queries configure a long `staleTime` (e.g. 10 minutes) because reference datasets change infrequently.
