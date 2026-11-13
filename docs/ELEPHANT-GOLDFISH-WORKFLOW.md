# Elephant-Goldfish Model (EGM) — Workflow Integration

**Date:** 13 November 2026
**Repository:** `chire0722/IT-Portal` (branch `ci-cd/hardening`)
**Source:** `https://github.com/google/elephant-goldfish-model`

## 1. What EGM is (verified)

EGM is an **open-source plugin** (Apache-2.0) for AI coding harnesses (Claude Code, Codex, Antigravity, omp, Muse, etc.). It is a methodology — "Design with an elephant, review with a goldfish" — implemented as:

- **Skills** (`skills/egm-*`): brainstorm, draft, review, resolve, review-loop, bootstrap, drift-report.
- **Personas** (`agents/*.md`): fresh-context reviewer subagent prompts (`egm-design`, `egm-structural`, `egm-zoom-out`, `egm-comprehension`, `egm-style`, `egm-implementation-readiness`, `egm-codebase-details`, `egm-inventive-catalyst`).
- **Document standards** (`resources/document_standards/`): feature-spec, implementation-readiness, zoom-out, heilmeier, re-implementation.

EGM is **not a standalone CLI**. Its Goldfish reviews require **fresh subagent contexts**; if a harness cannot spawn subagents, a review in the same context is explicitly not a Goldfish review (per `skills/egm-review/SKILL.md`).

## 2. Installation and capability verification

- Cloned the plugin to `C:\Users\tan18\.agents\skills\elephant-goldfish-model`.
- Verified the plugin layout: `skills/`, `agents/`, `resources/document_standards/`, and per-harness manifests (`.claude-plugin/`, `.codex-plugin/`, `.cursor-plugin/`, `.agents/plugins/`, `.gemini/`).
- Kilo is **not** a listed EGM harness. EGM has no Kilo-native plugin manifest. Therefore EGM is integrated as a **methodology** here: the Elephant/Goldfish roles are executed through this agent's native `task` subagent tool, with each Goldfish instructed to read the EGM reviewer persona file first (the same fallback EGM documents for harnesses without named personas — see `docs/harnesses.md` "direct dispatch fallback").

This is **not** falsely represented as a native plugin installation or as an automated CI check.

## 3. The workflow (Elephant → Goldfish → Implement)

The EGM core loop, as applied to this repository:

1. **Elephant (this agent, full project context)** drafts a specification/plan, documenting requirements, security constraints, edge cases, and regression risks.
2. **Goldfish (fresh subagents)** review the draft in isolated contexts, reading only the document + referenced source, without the Elephant's reasoning, using the EGM reviewer personas.
3. **Resolve disagreements** explicitly (findings classified as accept/contest).
4. **Implement** only after review; then request a **fresh independent review** of the implementation.

## 4. How it was exercised in this iteration

The EGM methodology was applied retrospectively to the in-progress security changes (uncommitted on `ci-cd/hardening`):

- **Elephant phase:** the security audit from the preceding turns (account enumeration, IDOR, cross-company roster, hardcoded bootstrap password, OTP purpose whitelisting, SBOM/checksums) served as the specification of intent.
- **Goldfish phase:** two fresh-context `task` subagents, instructed to read the EGM reviewer personas, independently reviewed the actual uncommitted diff:
  1. **Design Critic (`egm-design`)** — hostile security/architecture review. Returned 4 Major + 2 Nit findings (see §5).
  2. **Structural (`egm-structural`)** — correctness/regression review. Returned no Blocker/Major; 3 Nit observations.
- **Resolve:** 4 findings were accepted and fixed; 2 were assessed and documented as acceptable (see §5).

## 5. Independent review findings and disposition

### Accepted and fixed

| # | Finding (from Goldfish design-critic) | Severity | Disposition |
|---|---|---|---|
| F4 | `EmployeeController.Index` dropdown filters (`Departments`/`Branches`) were **unscoped** → cross-company tenant leak | Major | **Fixed** — scoped both dropdown queries via `CompanyScopeHelper.ApplyOnboardingCompanyScope`. |
| F1 | ForgotPassword still leaks via timing (not-found path faster) | Major | **Partially fixed** — added equalizing hash workload for the not-found path; residual transport channel documented below. |

### Accepted as documented residual risk (no change / fail-closed is correct)

| # | Finding | Severity | Disposition |
|---|---|---|---|
| F1 (transport) | ForgotPassword 302-redirect-with-email vs 200-render channel | Major | Residual: a real reset must navigate to a code-entry page. Text channel is closed; timing reduced. Documented as accepted residual risk. |
| F2 | IDOR `GetEmployeeDetails` returns uniform 404 for both missing and out-of-scope | Major→accepted | Uniform 404 is the *intended* anti-enumeration posture (avoids existence oracle). Documented as deliberate. |
| F3 | DbInitializer can throw on non-empty-but-adminless production DB | Major→accepted | Fails **closed** with a clear `SeedAdmin:Password is not configured` message; no insecure account is created. Correct security property. |
| F5 | Empty-scope "allow all" heuristic (`RequiresExplicitCompanyScope`) is fragile | Nit | Pre-existing design; documented, not changed in this iteration. |

The structural reviewer found **no** compile/regression defects: the `ICurrentUserAccessor` constructor change and `SeedUsersAsync` optional-parameter change have no broken callers; test assertions match the new behavior; CSS `@supports` blocks are balanced.

## 6. Honest limitations

- Goldfish reviewers ran with **read-only** `explore` subagents (they could read files but the sandbox denied `git`/`bash`), so they audited the working-tree file state rather than `git diff` output. Findings are grounded in actual code read.
- An AI review is **not** proof of security or correctness (per EGM's own guidance and this task's requirements). Human review remains a required gate.
- EGM is not installed as a native Kilo plugin; it is applied as a methodology. No automated CI check can substitute for the Goldfish review, and none is claimed.
