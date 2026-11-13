# Modernization Implementation Report

**Date:** 13 November 2026
**Repository:** `chire0722/IT-Portal` (branch `ci-cd/hardening`)

## 1. Scope

Frontend modernization driven by `modern-web-guidance legacy-scan` and the EGM independent-review pass, plus the security fixes that preceded it. Only changes with a demonstrable benefit were made; no working code was blindly modernized, and backend API contracts were preserved.

## 2. Changes implemented

### 2.1 CSS scrollbar modernization (modern-web-guidance)

- **File:** `wwwroot/css/site.css`
- **Change:** Replaced vendor-only `::-webkit-scrollbar` customization with standard `scrollbar-color` / `scrollbar-width`, wrapped the legacy WebKit rules in `@supports not (scrollbar-color: auto)` fallback blocks (sidebar + global scrollbar).
- **Benefit:** Scrollbar styling now works in Firefox and Safari; follows the standard-first, legacy-fallback pattern.
- **Verification:** `legacy-scan` returned 0 findings after the change.

### 2.2 Cross-company dropdown scoping (from EGM design-critic review)

- **File:** `Controllers/EmployeeController.cs`
- **Change:** The `Departments` and `Branches` filter dropdown queries are now scoped via `CompanyScopeHelper.ApplyOnboardingCompanyScope`, closing a cross-company department/branch-name disclosure that the roster grid scoping had left open.
- **Benefit:** Tenant isolation is consistent between the data grid and its filter dropdowns.

### 2.3 ForgotPassword timing equalization (from EGM design-critic review)

- **File:** `Application/Features/Account/ForgotPassword/ForgotPasswordCommandHandler.cs`
- **Change:** The not-found path now performs an equivalent hashing workload (`AccountOtpSupport.HashCode`) to reduce the timing side-channel.
- **Benefit:** Reduces the observable latency difference between existing and non-existing accounts.

## 3. Prior changes carried forward (security fixes)

These were implemented in the preceding turns and remain in the working tree (see `docs/SECURITY-AUDIT-REPORT-2026-11-13.md`):

- Account enumeration fix (ForgotPassword unified message).
- IDOR fix (`OffboardingController.GetEmployeeDetails` → `RequireHR` + company scope).
- Cross-company roster fix (`EmployeeController.Index` scope).
- Hardcoded bootstrap password removal (`DbInitializer`).
- OTP purpose whitelisting (`VerifyOtp`/`ResendOtp` validators).
- CI SBOM + artifact checksums.

## 4. Tests executed

- `.NET 10` application build: **passed, 0 warnings / 0 errors**.
- `IT_Form.Tests` build + run: **passed — 364 / 0 failed**.
- `IT_Form.AuditTests` build + run: **passed — 55 / 0 failed**.
- `modern-web-guidance legacy-scan` on `wwwroot/css`: **0 findings** (after fix).

Local test execution succeeded (the earlier Windows Application Control `0x800711C7` block cleared). GitHub Actions CI is green on the committed head; the uncommitted changes have not yet run in CI.

## 5. Known limitations and remaining risks

- `legacy-scan` covers only ~20% of the guidance corpus; broader accessibility/performance/JS-audit items were not automated and remain open.
- The ForgotPassword transport channel (302-redirect-with-email vs 200) is a documented residual risk; a real reset flow inherently navigates to a code-entry page.
- The `RequiresExplicitCompanyScope` "empty scope = allow all" heuristic (pre-existing) is documented as fragile and is a candidate for a future explicit-role-based redesign.
