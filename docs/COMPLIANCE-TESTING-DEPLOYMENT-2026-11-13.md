# IT-Portal — Compliance, Testing & Deployment Readiness (13 November 2026)

This document supplements `SECURITY-AUDIT-REPORT-2026-11-13.md`. It distinguishes implemented technical controls, missing technical controls, organisational responsibilities, and legal obligations requiring professional/management confirmation. It does not assert legal compliance or certification.

## A. Compliance control mapping

### GDPR (where applicable — Article 3 territorial scope to be confirmed by legal owner)

| Article | Engineering control | Status |
|---|---|---|
| Art. 5 (principles) | Data minimisation via scoped queries; retention service; audit privacy | Implemented (partial) |
| Art. 6 (lawfulness) | Legal-basis capture is a product/legal decision | Organisational/legal |
| Art. 12–22 (subject rights) | Restricted HR-only DSAR case register + state machine (`DataSubjectRequestsController`) | Implemented (discovery/export/erasure execution outstanding) |
| Art. 25 (by design/default) | Multi-tenant scope helpers, least-privilege policies, account-enumeration resistance, fail-closed bootstrap | Implemented |
| Art. 30 (records of processing) | Audit log + encrypted-field inventory | Implemented (controller register organisational) |
| Art. 32 (security) | Argon2id, HMAC key ring, Data Protection + X.509, CSRF, CSP, rate limiting, malware scan | Implemented (production deployment evidence outstanding) |
| Art. 33–34 (breach notification) | Incident-response playbook (template) | Organisational/legal |
| Art. 35 (DPIA) | Not automated | Organisational/legal |

### Malaysia PDPA 2010 (Act 709)

| Principle | Engineering control | Status |
|---|---|---|
| General / Notice & Choice | Login branding + consent capture is a product decision | Organisational/legal |
| Disclosure | N/A (engineering) | Organisational |
| Security | Argon2id, encryption, CSRF, malware scan, rate limiting | Implemented |
| Retention | `DataRetentionService` removes expired terminal email outbox records | Implemented (periods to validate) |
| Data Integrity | HMAC-SHA256 signature integrity; fixed-time comparisons | Implemented |
| Access | DSAR register + access request type | Implemented (delivery outstanding) |
| DPO/registration/breach notification/data portability/cross-border | Policy/legal decisions | Organisational/legal |

### NIST CSF 2.0

| Function | Control | Status |
|---|---|---|
| Govern | Documented risk/governance docs | Partial |
| Identify | Asset/license/employee inventory; encrypted-field inventory; SBOM (new) | Implemented |
| Protect | AuthN/AuthZ, encryption, file security, headers/CSP, secret scanning | Implemented |
| Detect | Audit logging, failed-login events, CodeQL, Gitleaks, NuGet audit | Implemented |
| Respond | Incident-response playbook (template) | Organisational |
| Recover | Backup/restore service, key-ring recovery docs | Implemented (witnessed restore outstanding) |

## B. Testing report

- Application build (.NET 10): **passed, 0 warnings / 0 errors**.
- Test projects build: **passed, 0 warnings / 0 errors**.
- Current suite (verified locally): `IT_Form.Tests` **364 passed / 0 failed**, `IT_Form.AuditTests` **55 passed / 0 failed** (419 total).
- Baseline suite (pre-change head): `IT_Form.Tests` 357 passed, `IT_Form.AuditTests` 55 passed (412 total).
- Regression tests added in this iteration (all passing):
  - ForgotPassword enumeration resistance (2 new + 1 updated).
  - Cross-company employee-detail rejection (1 new).
  - OTP purpose whitelisting (2 new).
  - Employee roster + dropdown company-scope isolation (2 new).
- CI/CD: workflow now includes build, unit/integration tests, CodeQL (C# + Actions), Gitleaks (current-tree blocking + history-visible), Dependency Review, NuGet audit (`-warnaserror:NU1901..1904`), **SBOM generation (new)**, and **artifact checksums (new)**.
- CI status: committed head `2ee6742` is green (7/7 jobs, run `43247025395`). The uncommitted changes have not run in CI (cannot commit/push without approval); they are verified locally.

## C. Deployment readiness

- HTTPS + HSTS: enforced via `CookieSecurePolicy=Always` and `UseHsts()` in non-development.
- Trusted reverse proxy: forwarded-headers configured with explicit `KnownProxies`.
- Production secrets: `SeedAdmin:Password` now mandatory for first bootstrap (fails closed otherwise); SMTP/Inventory/HMAC keys remain environment-provided placeholders in `appsettings.json`.
- Data Protection: production mandates an X.509 cert (RSA ≥ 2048) for key-ring encryption; unprotect-cert rotation supported.
- Backup/recovery: backup + `ProtectedDatabaseRestoreService` present; witnessed restore still an external gate.
- SQLite native runtime: `Microsoft.EntityFrameworkCore.Sqlite` with native SQLite support; migrations run at startup.

**Deployment blockers (external):** ClamAV daemon + signature freshness, Data Protection certificate provisioning, witnessed restore, live EICAR test, SMTP/Inventory API credentials.

## D. GitHub summary

- Branch used: `ci-cd/hardening` (isolated from protected `main`).
- Open PRs (unchanged; not merged, per instruction): #1 `security/remediation → main`, #2 `security/audit-hardening → security/remediation`, #3 `ci-cd/hardening → main`.
- Files modified this iteration: 12 modified + 1 new test file (see audit report §6).
- No commits created and no PRs updated/merged (changes are uncommitted on the working branch; awaiting your approval to commit/push).
