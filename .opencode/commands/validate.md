---
description: Validate implemented code against a phase plan and write a validation report
agent: build
---

You are in REVIEW MODE. Do NOT modify production code. Only write validation reports.

## Phase to Validate

Phase: $1

## Instructions

1. Read the plan file at `docs/plans/$1/plan.md`.
2. Read the original phase file at `docs/prompts/$1.md` (if it exists).
3. For EACH file listed in the plan:
   - Check if the file exists at the expected path.
   - Compare the actual implementation against the plan specifications.
   - Verify namespaces, class names, method signatures, properties, and access modifiers.
4. Run `dotnet build` and capture the output.
5. If tests exist, run `dotnet test` and capture the output.
6. Check the code against Clean Code principles:
   - SOLID
   - DRY (no duplicated logic)
   - KISS (simple solutions)
   - Meaningful naming
   - No magic numbers/strings
   - Proper error handling (Result Pattern / ProblemDetails)

## Output

Write a validation report at `docs/plans/$1/validation-report.md` with this structure:

# Validation Report: <Phase Name>

## Summary

- **Status**: ✅ PASSED / ❌ FAILED / ⚠️ PARTIAL
- **Build Status**: ✅ / ❌
- **Test Status**: ✅ / ❌ / N/A

## Files Checked

| #   | Planned File         | Exists | Matches Plan | Notes |
| --- | -------------------- | ------ | ------------ | ----- |
| 1   | `src/.../Product.cs` | ✅     | ✅           | -     |

## ✅ Passed

- ...

## ❌ Failed / Missing

- ...

## ⚠️ Warnings

- ...

## 📋 Clean Code Checklist

- [ ] SOLID principles followed
- [ ] No code duplication (DRY)
- [ ] Simple solutions (KISS)
- [ ] Meaningful names
- [ ] No magic numbers/strings
- [ ] Proper error handling
- [ ] All planned files exist
- [ ] Namespaces match plan
- [ ] Build succeeds with no errors
- [ ] No compiler warnings

## 🔧 Recommended Fixes

1. ...
2. ...

## Rules

- Be thorough and honest.
- Report missing files, wrong signatures, and deviations from the plan.
- Do NOT fix code yourself — only report.
- Use the write tool to save the report at `docs/plans/$1/validation-report.md`.
