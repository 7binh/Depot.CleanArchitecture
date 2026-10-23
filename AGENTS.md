# Depot - agent rules (Claude Code, Codex, Antigravity, Copilot, any agent)

Stack: .NET 10 Clean Architecture (`src/`), Angular + Tailwind (`web/`).

## Definition of done (mandatory)
Run `sh scripts/check.sh` and make it pass before you say a task is finished.
It runs the Roslyn + SonarAnalyzer build (warnings are errors), tests, ESLint
(angular-eslint + sonarjs) and Prettier. Never silence a rule to get green:
no `#pragma warning disable`, `[SuppressMessage]`, `eslint-disable`, `@ts-ignore`,
or severity edits in `.editorconfig` / `eslint.config.js` / `Directory.Build.props`
unless the user explicitly asks. Fix the code instead.

## Requirements (read before modelling the domain)
- `docs/requirements/SNP_HE_THONG_QUAN_LY_DEPOT.md`: business requirements (yard, containers, gate in/out, release orders).
- `docs/internship/SSC-Dev-Developer-New-011026-1020-100.md`: scope, deliverables and mandatory checklist.

## Backend rules
- Layers: Domain <- Application <- Infrastructure/Api. Domain depends on nothing.
- Errors are values: return `Result` / `Result<T>` with an `Error`; do not throw for expected failures.
- Validate with `IBusinessRule` + `BusinessRuleValidator` (`Depot.Domain.Common.Rules`); map to HTTP with `ToHttpResult()`.
- File-scoped namespaces, braces always, `sealed` by default, no unused usings, `_camelCase` private fields.
- Cognitive complexity low: small methods, no nested ternaries, early returns.
- Async all the way, no `.Result` / `.Wait()`, no `async void`.

## Frontend rules
- Standalone components, signals, new control flow (`@if`, `@for`), `inject()`.
- No `any`, no `console.log`, `===` only, function complexity <= 10, <= 60 lines.
- Style with Tailwind utilities; no ad-hoc global CSS.
- Read `web/Design.md` before writing any UI and follow it exactly (colours, buttons, borders, shadows, "never do this"). Layout is free; visual style is not.

## Hygiene
- No secrets in code; use configuration. Do not commit `bin/`, `obj/`, `node_modules/`, `dist/`.
- Keep changes minimal and focused; match surrounding style.
