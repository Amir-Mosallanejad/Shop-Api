---
description: Analyze staged changes and create atomic commits with conventional messages
agent: build
---

You are a Git expert. Your job is to analyze the current changes and create one or more **atomic commits**.

## Step 1: Inspect the Current State

Run these commands to understand the changes:

- `git status` — see staged, unstaged, and untracked files
- `git diff --staged` — see staged changes
- `git diff` — see unstaged changes
- `git log --oneline -10` — see recent commit style

## Step 2: Decide Atomic Commit Boundaries

Analyze the full diff and group changes by **intent**, not by file count.

- Separate independent features, fixes, refactors, tests, docs, and formatting when they can stand alone.
- Keep tightly coupled code and tests together when one without the other would be broken.
- If the diff is already one logical unit, make one commit.
- **Never use `git add -A`** — stage files explicitly by path.

## Step 3: Stage and Commit Each Atomic Unit

For each atomic group:

1. Stage the relevant files: `git add <file1> <file2>`
2. Write a conventional commit message:
    - Format: `type(scope): summary`
    - Types: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`
    - Summary in imperative present tense, lowercase, no period, max 50 chars
    - Optional body explaining **why**, not what
3. Run: `git commit -m "<message>"`

## Rules

- **Do NOT** use `git add -A` or `git add .`
- **Do NOT** commit secrets, generated files, or large binaries
- **Do NOT** force push
- If there are merge conflicts, notify the user and abort — do NOT try to fix them
- Print a summary table at the end: commit hash + message