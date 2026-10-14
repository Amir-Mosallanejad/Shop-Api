---
description: Implement production code based on a phase plan in docs/plans/<phase-name>/plan.md
agent: build
---

You are in BUILD MODE. You must write production code.

## Phase to Implement

Phase: $1

## Instructions

1. Read the plan file at `docs/plans/$1/plan.md`.
2. Read the original phase file at `docs/prompts/$1.md` (if it exists) for extra context.
3. Implement EVERYTHING specified in the plan:
   - Create every file listed in the "File-by-File Breakdown".
   - Follow the exact namespaces, class names, method signatures, and properties.
   - Respect the implementation order defined in the plan.
4. After creating all files:
   - Run `dotnet build` to verify compilation.
   - If there are build errors, fix them.
   - Report the final build status.

## Rules

- Follow the plan EXACTLY.
- Do NOT create extra files not in the plan.
- Follow Clean Code, SOLID, and the project's architecture (Clean Architecture + CQRS).
- Use the write and edit tools to create/modify files.
- If the plan is ambiguous, ask me BEFORE proceeding.
- At the end, print a summary table: file path + status (created / modified).
