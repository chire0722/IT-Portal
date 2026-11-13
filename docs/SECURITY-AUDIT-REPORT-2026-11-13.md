# IT-Portal Security Remediation — Continuation Report

**Assessment date:** 13 November 2026
**Repository:** `chire0722/IT-Portal`
**Working branch:** `ci-cd/hardening` (isolated from `main`; `main` not modified)
**Base commit:** `2ee6742` (`docs(security): tidy final CI assessment wording`)
**Status:** This is a continuation of the prior hardening effort. It records newly identified and newly remediated findings, verified by compilation and cross-referenced against the existing test suite and GitHub Actions CI.

## 1. Executive summary

This iteration reviewed authentication/account-recovery, authorization/multi-tenant isolation, secrets/cryptography, file upload/download, application/API security, audit logging, and the CI/CD pipeline. The baseline build and test suite were re-established (build clean; 357 application + 55 audit tests confirmed passing at the start of the session against the pre-change head).

The following concrete vulnerabilities were identified and remediated in this iteration:

| # | Finding | Severity | Status |
|---|---------|----------|--------|
| 1 | Account enumeration via distinct ForgotPassword responses | Medium | Fixed |
| 2 | IDOR: `OffboardingController.GetEmployeeDetails` disclosed cross-company employee PII to any authenticated user | Critical | Fixed |
| 3 | Cross-company employee roster disclosure in `EmployeeController.Index` | Critical | Fixed |
| 4 | Hardcoded bootstrap admin password `Abc123!xyz` in `DbInitializer` | High | Fixed |
| 5 | Unvalidated OTP `purpose` in VerifyOtp / ResendOtp commands | Low | Fixed |
| 6 | Missing SBOM generation and artifact checksums in CI | Low | Fixed |

Additional findings surfaced by sub-agent review but assessed as **not vulnerabilities** (by-design behavior) are documented in Section 7 to avoid introducing regressions.

## 2. Scope

- Source: `Application/`, `Controllers/`, `Data/`, `Helpers/`, `Security/`, `Services/`, `Program.cs`, `.github/workflows/ci.yml`.
- Test projects: `IT_Form.Tests`, `IT_Form.AuditTests`.
- Excluded: live production systems, external infrastructure (ClamAV daemon, SMTP, Inventory API), and legal/compliance certification.

## 3. Findings and remediation detail

### 3.1 Account enumeration in ForgotPassword (Medium → Fixed)

- **File:** `Application/Features/Account/ForgotPassword/ForgotPasswordCommandHandler.cs`
- **Vulnerability:** The handler returned three distinct messages for (a) unknown username/email, (b) inactive account, (c) account without a registered email, allowing an attacker to enumerate valid accounts and their status.
- **Fix:** Collapsed the three branches into one identical neutral message ("If an account matching that username or email exists and is active, a verification code has been sent…"). This mirrors the pattern already used by `ResendOtpCommandHandler`.
- **Regression tests:** `ForgotPasswordHandler_ReturnsInvalidUser_WhenUserMissing` updated; `ForgotPasswordHandler_ReturnsIdenticalMessage_ForInactiveUser` and `ForgotPasswordHandler_ReturnsIdenticalMessage_ForUserWithoutEmail` added in `IT_Form.Tests/AccountFeatureSliceTests.cs`.

### 3.2 IDOR in Offboarding GetEmployeeDetails (Critical → Fixed)

- **File:** `Controllers/OffboardingController.cs` and `Application/Features/Offboarding/Create/GetOffboardingEmployeeDetailsQueryHandler.cs`
- **Vulnerability:** `GetEmployeeDetails(int id)` was only class-level `[Authorize]`; the handler projected an `OnboardingRequest` by raw id with no role or company-scope check. Any authenticated user could enumerate employee IDs to retrieve cross-company PII (name, email, department, branch, HOD email).
- **Fix:** Added `[Authorize(Policy = "RequireHR")]` to the action (it is only invoked from the HR-only offboarding Create view) and injected `ICurrentUserAccessor` into the handler to enforce `CompanyScopeHelper.CanAccessCompany(User, onboarding.Branch)` before returning data (returning `null` for out-of-scope records).
- **Regression test:** `OffboardingEmployeeDetailsQuery_ReturnsNull_ForCrossCompanyAccess` added in `IT_Form.Tests/EncryptedEmployeeIdProjectionTests.cs`.

### 3.3 Cross-company roster disclosure in EmployeeController.Index (Critical → Fixed)

- **File:** `Controllers/EmployeeController.cs`
- **Vulnerability:** `Index` was only `[Authorize]` and returned every completed/approved onboarding record across all companies with no scope filter, exposing the org-wide employee directory to any authenticated user.
- **Fix:** Resolved `CompanyScopeHelper.GetScopedCompanies(User)` and applied `CompanyScopeHelper.ApplyOnboardingCompanyScope(...)` to the roster query. Global roles (SuperAdmin/IT/HR) with no `UserCompany` claim continue to see the full roster, consistent with the established authorization model; company-scoped users are restricted to their own companies.

### 3.4 Hardcoded bootstrap password (High → Fixed)

- **File:** `Data/DbInitializer.cs`
- **Vulnerability:** `DefaultBootstrapPassword = "Abc123!xyz"` was used to create the initial IT/Super Admin when `SeedAdmin:Password` was not configured, producing a publicly-known credential on fresh installs.
- **Fix:**
  - Removed the hardcoded constant entirely.
  - When no explicit `SeedAdmin:Password` is configured and no users exist: **production fails closed** with a clear error; **development generates a CSPRNG one-time password** (satisfying the Identity password policy) and logs it for local first-login.
  - The seed password is now only ever used to **create** a new account; it is never used to silently reset an existing account's password (guarding against an accidental random-password reset of a live admin).
  - Threaded `IHostEnvironment` and `ILogger` (optional) into `SeedUsersAsync`; `Program.cs` passes `app.Environment` and the program logger.
- **Note:** A credential rotation review of the previously-exposed `Abc123!xyz` remains an operational follow-up; this fix prevents the weak credential from being re-introduced.

### 3.5 Unvalidated OTP purpose (Low → Fixed)

- **File:** `Application/Features/Account/VerifyOtp/VerifyOtpCommandValidator.cs`, `Application/Features/Account/Writes/ResendOtp/ResendOtpCommandValidator.cs`
- **Vulnerability:** `VerifyOtpCommand.Purpose` and `ResendOtpCommand.Purpose` accepted arbitrary strings, allowing a client to request unsupported OTP purposes.
- **Fix:** Added whitelist validation (VerifyOtp allows `PasswordReset`/`PrivilegedLogin`; ResendOtp allows `PasswordReset` only).
- **Regression tests:** `VerifyOtpValidator_Fails_WhenPurposeIsNotWhitelisted` and `ResendOtpValidator_Fails_WhenPurposeIsNotPasswordReset` added in `IT_Form.Tests/AccountFeatureSliceTests.cs`.

### 3.6 CI/CD — SBOM and artifact integrity (Low → Fixed)

- **File:** `.github/workflows/ci.yml`
- **Gap:** No SBOM generation and no published-artifact checksums.
- **Fix:**
  - Added an `sbom` job using the pinned `microsoft/sbom-tool` action (commit `c83b43dee2dd70b4d6ba16a97cde6b43f971d9c3`, v4.1.5) with `contents: read`, uploading the SPDX manifest as a 90-day-retained artifact.
  - Added a `dotnet publish` step plus a deterministic `SHA256SUMS` (sorted, `sha256sum`) to the `build` job and upload the published output + checksums as an artifact.

## 4. Verification evidence

- `.NET 10` application build: **0 warnings, 0 errors**.
- `IT_Form.Tests` build: **0 warnings, 0 errors**; test run: **364 passed / 0 failed**.
- `IT_Form.AuditTests` build: **0 warnings, 0 errors**; test run: **55 passed / 0 failed**.
- Combined: **419 passed / 0 failed** (verified locally; the 364 includes the new/updated regression tests added in this iteration).
- Baseline (pre-change head): 357 application + 55 audit = 412 passed.

### CI verification note

The committed head `2ee6742` has a green GitHub Actions run (`43247025395`): all 7 jobs succeeded (build, `IT_Form.Tests`, `IT_Form.AuditTests`, Gitleaks, CodeQL C#, CodeQL Actions, Dependency Review). The **uncommitted** changes in this iteration (including the new SBOM/checksum CI jobs and the code fixes) have **not** yet run in GitHub Actions because they cannot be committed/pushed without approval; they are verified locally (build + 419 tests + valid workflow YAML).

## 5. Remaining risks and open items (carried forward)

The following were assessed and remain open, consistent with the prior report (`SECURITY-AUDIT-REPORT-2026-11-10.md`):

- **P0 — Production malware scanning (ClamAV):** source integration + mock-protocol tests exist; live EICAR detection in isolated staging remains a deployment gate.
- **P0 — Full live upload/download authorization matrix:** route inventory documented; route-by-route negative testing remains outstanding.
- **P0 — DSAR operations:** restricted case register exists; identity verification, export delivery, legal-hold, deletion execution remain gates.
- **P0 — Production key/certificate and backup controls:** Data Protection cert provisioning, private-key ACLs, encrypted volumes, witnessed restore.
- **P1 — Whole-database encryption:** only `[EncryptColumn]` properties are protected; volume-level encryption outstanding.
- **P1 — Legacy AES-CBC license decryption:** unauthenticated CBC with UTF8-derived keys (migration-only path). Changing it risks breaking existing encrypted license data and requires production key material; deferred and documented.
- **P2 — CSP `style-src 'unsafe-inline'`:** still present (browser regression testing of nonce/hash-based styling outstanding).

## 6. Files changed in this iteration

- `Application/Features/Account/ForgotPassword/ForgotPasswordCommandHandler.cs`
- `Application/Features/Account/VerifyOtp/VerifyOtpCommandValidator.cs`
- `Application/Features/Account/Writes/ResendOtp/ResendOtpCommandValidator.cs`
- `Application/Features/Offboarding/Create/GetOffboardingEmployeeDetailsQueryHandler.cs`
- `Controllers/EmployeeController.cs`
- `Controllers/OffboardingController.cs`
- `Data/DbInitializer.cs`
- `Program.cs`
- `.github/workflows/ci.yml`
- `wwwroot/css/site.css`
- `IT_Form.Tests/AccountFeatureSliceTests.cs`
- `IT_Form.Tests/EncryptedEmployeeIdProjectionTests.cs`
- `IT_Form.Tests/EmployeeControllerScopeTests.cs` (new)

## 7. Findings assessed as by-design (no change)

- **`EmailContactController` read/create/search lack `RequireIT`:** the "Email Directory" is intentionally a shared, collaboratively-edited address book — the layout renders it for all authenticated users, `SearchContacts` backs the HOD/Manager email-composition flow, and the "Add Contact" button is shown to all users while edit/delete/bulk-import remain `RequireIT`. Adding `RequireIT` would break the notification workflow. No change made.
- **`MaintenanceLogController` lacks asset-company re-check:** masked by the current global-IT model (IT is treated as global access). Architectural debt noted; would matter only if IT becomes company-scoped.

## 8. Compliance position

As with the prior report, this repository must **not** be described as fully GDPR-compliant, Malaysian PDPA-compliant, or NIST-certified on the basis of this audit or a green CI run. The engineering controls implemented here (account-enumeration resistance, multi-tenant isolation, fail-closed bootstrap, OTP purpose whitelisting) strengthen GDPR Art. 25/32, PDPA Security Principle, and NIST CSF 2.0 Protect/Detect posture, but legal conformity remains dependent on organisational owners, production deployment, and retained evidence.

## 9. Recommended next actions

1. Merge-review these changes on `ci-cd/hardening` (they remain isolated from `main`).
2. Re-run GitHub Actions CI to execute the new/updated regression tests and confirm the SBOM/checksum jobs.
3. Rotate any credentials that may have been created with the removed `Abc123!xyz` bootstrap password.
4. Resolve the carried-forward P0 operational gates (ClamAV, DSAR, key/certificate provisioning) with named owners.
