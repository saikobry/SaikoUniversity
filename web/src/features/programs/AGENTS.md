# programs — Programs vertical slice

## Purpose

Programs (academic programmes) vertical slice: pages, table, modals, hooks.

## Local Contracts

### Form contract

- Required credits: positive integer. Validate with `Number.isInteger` and `> 0` before submit.
- Code, name, academic level, and department are required on create. Name is required on edit.
- Update requests carry only `name`, `requiredCredits`, `isRegulatorRecognised`, and `isRetired`.
  Code, level, department, regulator, and credit basis are fixed after creation.
- Academic levels and departments now have lookup endpoints and are rendered as selects.
