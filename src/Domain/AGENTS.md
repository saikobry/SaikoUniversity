# Domain

## Purpose

The domain model: entities, enums, value objects, and the invariants that belong to the business
itself. This layer answers "what is a student, a term, an enrollment" and nothing about how any of
it is stored, served, or displayed.

## Ownership

- Entity types and their relationships
- Enumerations and value objects
- Domain-level invariants and state transitions
- The `Common/` primitives every other entity inherits

## Local Contracts

- **Zero dependencies.** No project references, no package references. Not EF Core, not
  ASP.NET Core, not a logging abstraction.
- Entities are `class`, not `record`. They carry identity and mutable state.
- No data annotations. `[Required]`, `[MaxLength]`, `[Table]` are EF concerns and belong in
  `Infrastructure/Persistence/Configurations`.
- No `DbSet<T>`, no `DbContext`, no LINQ-to-SQL expressions.
- No DTOs. Transport shapes belong to `Application`.
- Public setters are the exception, not the rule. Collections are exposed as `IReadOnlyCollection<T>`
  and mutated through explicit methods.

### Common primitives

`Common/Entity.cs` is the base for all entities:

- `Id` — UUIDv7 surrogate key, `Guid`
- `CreatedAt`, `UpdatedAt` — set by the auditable interceptor, not by hand

Soft delete and domain events are not implemented yet. `Entity` carries those three members and
nothing else. If you add either, update this section in the same change.

## Work Guidance

- Put a rule here when it is true regardless of storage or transport: "a dropped enrollment cannot
  be reinstated after the add/drop deadline." Not: "enrollment status must be one of these strings
  in the database column."
- Naming: entity names are singular nouns with no suffix. `Student`, `Term`, `Enrollment`,
  `CourseSection`. Never `StudentEntity` or `StudentModel`.
- Enums live in `Enums/` and are named without an `Enum` suffix: `EnrollmentStatus`.
- When an entity gains a relationship, own the foreign key on the many side.

## Verification

- `dotnet build src/Domain`
- `dotnet test tests/Domain.Tests`

## Child DOX Index

None. This project is a single boundary; subfolders do not carry their own contracts.
