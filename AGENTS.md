# ShopApi — Project Guide

## Architecture

- Clean Architecture (Domain, Application, Infrastructure, API)
- CQRS with MediatR
- Entity Framework Core + MSSQL
- .NET 8

## Coding Standards

- Follow SOLID, DRY, KISS principles
- Use Result Pattern for API responses
- Use FluentValidation for input validation
- Use AutoMapper for mapping
- No hardcoded secrets — use .env
- Use Repository Pattern + Unit of Work

## Project Layout

- `docs/prompts/` → Phase prompt files (input)
- `docs/plans/<phase>/` → Plans and validation reports (output)
- `src/` → Production code (Clean Architecture layers)

## Workflow

1. `/makeplan <phase-file>` → Generate plan in `docs/plans/<phase>/plan.md`
2. `/writecode <phase-name>` → Implement code from plan
3. `/validate <phase-name>` → Validate code and write `validation-report.md`

## Commands

- Build: `dotnet build`
- Run: `docker compose up --build`
- Migration: `dotnet ef migrations add <Name>`

## Commit Rules

- Use conventional commit messages: `type(scope): summary`
- Valid types: feat, fix, docs, chore, refactor, test
- One logical change per commit (atomic commits)
- Never use `git add -A`
- Show the diff and wait for explicit user confirmation before committing
