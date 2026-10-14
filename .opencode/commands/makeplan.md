---
description: Read phase files and generate detailed implementation plans in docs/plans/<phase-name>/
agent: build
---

You are in PLAN MODE. Do NOT write production code. You only write plan files.

## Phase Files to Process

$ARGUMENTS

## Instructions

Parse the list of phase file paths above (they are separated by spaces).

For EACH phase file, perform these steps:

### Step 1: Extract Phase Name

From each file path, extract the phase name by removing the directory and `.md` extension:

- `docs/prompts/phase-01-solution.md` → `phase-01-solution`
- `docs/prompts/phase-02-domain.md` → `phase-02-domain`
- `docs/prompts/phase-03-application.md` → `phase-03-application`

### Step 2: Create Plan Directory

Create a unique directory for each phase:
`docs/plans/<phase-name>/`

### Step 3: Read the Phase File

Read the full content of the phase file.

### Step 4: Write Detailed Plan

Save the plan as `docs/plans/<phase-name>/plan.md`.

## Plan Content Requirements

Each plan must be extremely detailed and include:

### 1. Overview

- What this phase accomplishes
- Which Clean Architecture layers are affected (Domain / Application / Infrastructure / API)
- Dependencies on previous phases

### 2. File-by-File Breakdown

| #   | File Path                                | Action | Layer  | Purpose        |
| --- | ---------------------------------------- | ------ | ------ | -------------- |
| 1   | `src/ShopApi.Domain/Entities/Product.cs` | Create | Domain | Product entity |

### 3. Detailed File Specifications

For EACH file:

#### File: `src/ShopApi.Domain/Entities/Product.cs`

- **Namespace**: `ShopApi.Domain.Entities`
- **Type**: `class Product : BaseEntity`
- **Properties**:
  - `string Name { get; private set; }`
  - `decimal Price { get; private set; }`
- **Methods**:
  - `void UpdatePrice(decimal newPrice)`
- **Dependencies**: `BaseEntity`, `Money` value object
- **Validation Rules**: Price must be > 0
- **Notes**: Follow DDD principles

### 4. Implementation Order

Numbered list of the exact order to create files.

### 5. Code Patterns & Standards

- Design patterns to use (Repository, CQRS, etc.)
- Naming conventions
- Error handling approach (Result Pattern, ProblemDetails)

### 6. Edge Cases & Validation

- Input validation rules
- Error scenarios to handle
- Security considerations

### 7. Verification Checklist

- [ ] All files created with correct namespaces
- [ ] Project builds successfully
- [ ] No compiler warnings
- [ ] All validation rules implemented
- [ ] Clean Code principles followed

## Rules

- Be EXTREMELY specific with file paths, namespaces, class names, and method signatures.
- Do NOT write actual production code in the plan — only specifications.
- Use the write tool to save each `plan.md`.
- Create ONE plan file per phase file.
- After creating all plans, print a summary table of what was created.
