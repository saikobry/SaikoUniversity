# .agents — Agent Assets

## Purpose

Skills and agent-instruction files checked into the repository so that behaviour travels with the
code. Anyone cloning this repo gets the same tooling contract the maintainer uses.

## Ownership

- Skill definitions under `skills/<skill-name>/SKILL.md`
- Any future shared agent instructions or prompt assets
- Skill frontmatter: `name` and `description`

## Local Contracts

- One skill per folder. The folder name matches the `name` in the frontmatter.
- Frontmatter requires exactly `name` and `description`. The description states when to invoke the
  skill, in terms of the trigger a user would actually type.
- Skills live here in the repo, mirrored to `~/.config/opencode/skills/` for the local toolchain.
  The repo copy is the source of truth.
- A skill must never mutate repository state it was not asked to change. The `commit-message` skill
  reads staged changes and emits text; it never stages and never commits.

## Work Guidance

- Keep a skill stateless and side-effect free wherever the task allows.
- Reference paths relative to the repo root so the skill works in any clone location.
- When a skill's workflow changes, update it here first, then re-mirror to the global skills folder.
- Do not vendor third-party skills without recording the source URL.
- Do not commit anything under `skills/` that contains a credential, token, or machine-specific
  absolute path.

## Verification

- The folder loads as a skill: invoking `/<skill-name>` or its trigger phrase resolves to this file.
- Frontmatter parses and `name` equals the parent folder name.

## Child DOX Index

- `skills/commit-message/` — generates a conventional commit message from staged changes; never
  stages and never commits