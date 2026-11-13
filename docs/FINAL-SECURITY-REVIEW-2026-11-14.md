# IT-Portal — Independent Final Security Review (13–14 November 2026)

**Repository:** `chire0722/IT-Portal`
**Working branch:** `ci-cd/hardening` (isolated; `main` untouched)
**Committed head:** `2ee6742` (CI green, run `43247025395`, 7/7 jobs)
**Uncommitted changes:** present in working tree; NOT committed, NOT pushed, NOT merged.

This is an independent second-pass review of the uncommitted changes plus a fresh audit of the authorization, file, and CI/CD surfaces. It distinguishes **verified**, **unverified**, and **deployment-dependent** items.

---

## 1. Findings fixed in this review pass

| # | Severity | Location | Finding | Fix | Test |
|---|----------|----------|---------|-----|------|
| F1 | **High** | `Controllers/EmployeeApiController.cs:377` `GetHodsByCompany` | Cross-tenant IDOR: HR endpoint enumerated HOD `id`/`email`/`fullName` for any caller-supplied company with no `CompanyScopeHelper.CanAccessCompany` check (unlike sibling `GetEmployeeDetails`/`Import`). | Added `CanAccessCompany(User, company)` → `Forbid()` | Updated positive test scope + added `GetHodsByCompany_ForbidsCrossCompanyLookup` |

## 2. Findings assessed as BY-DESIGN (documented, no change)

| # | Severity | Location | Finding | Why no change |
|---|----------|----------|---------|---------------|
| D1 | Medium | `DataSubjectRequestsController` List/Get/Transition | No ownership/scope check | `RequireHR` + `HrRecordReadCompanyScopeSupport.HasGlobalReadAccess` deliberately treats HR as global-read. Consistent with the established model (same as EmailContact). Changing it risks breaking HR workflow; flagged as a product decision. |
| D2 | Low | `MaintenanceLogController.Create` | Asset-tag disclosed without scope re-check | `RequireIT` = global (`PortalTitles.HasGlobalCompanyAccess`). Defense-in-depth consistency gap only. |

## 3. File upload/download review (independent) — no Critical/High

Path-traversal (multiple `..`/`:`/control-char rejections in `SensitiveFileStorage`), extension+MIME+magic-byte validation (`UploadFileSignatureValidator`), OOXML/zip-bomb bounds (`FileUploadHelper`), fail-closed malware scan (`ClamAvMalwareScanner` + `MalwareScanHelper`), atomic temp-file writes, and cross-tenant download authorization (`SecureFilesController` uniform-`NotFound`) are all implemented to a high standard.

Residual (non-blocking): orphaned-file-on-partial-commit in `OffboardingCreateSupport.cs` (Medium), latent un-scanned helper reuse (Low), `MoveExistingFileToSecureStorage` weaker filename re-check (Low), `PreviewAttachment` missing `nosniff`/`no-store` (Low).

## 4. CI/CD review (independent) — no Critical/High

- Permissions: correct least-privilege (`contents: read`; `security-events: write` + `actions: read` correctly job-scoped to CodeQL).
- Pinning: all actions full-SHA; `microsoft/sbom-tool` SHA verified against signed v4.1.5 commit.
- Secrets: no secrets in logs/artifacts; `cat SHA256SUMS` echoes only digests.
- Fail-on-findings: NuGet audit, dependency-review (`low`), gitleaks current-tree all block correctly.

**Actionable residual:**
- **Medium** — `SHA256SUMS` self-inclusion fragility + no cryptographic attestation (integrity-only, not authenticity). Recommendation: emit manifest outside the hashed tree + add `actions/attest-build-provenance`/cosign.
- **Low** — gitleaks history scan is non-blocking by design; consider GitHub push protection as compensating control.

## 5. Compliance mapping (OWASP ASVS / NIST CSF 2.0 / GDPR / PDPA)

Implemented (verified in code): ASVS V4 Access Control (company scope on onboarding/offboarding/asset/employee/API reads; uniform 404 anti-oracle), V3 session/auth (Argon2id, OTP purpose whitelist, account-enumeration resistance), V5/V10 (validation, CSRF, CSP, rate limiting), V12 file security (as §3). NIST CSF: Identify (SBOM, inventory), Protect (authz/encryption/files), Detect (audit logging, CodeQL/Gitleaks/dependency-review).

Not met (requires deployment/org): ASVS V9 client-side full pass (no axe/Lighthouse run), GDPR Art. 33–34 breach-notification execution, Art. 35 DPIA, Art. 30 records-of-processing register, PDPA DPO registration/notification, cross-border transfer legal basis, witnessed backup restore, Data Protection certificate provisioning, live ClamAV/EICAR in staging.

## 6. Verified vs unverified vs deployment-dependent

### Verified (local, this session)
- Application build: **0 warnings / 0 errors**.
- Test projects build: **0 warnings / 0 errors**.
- `IT_Form.AuditTests`: **55 passed / 0 failed** (runs successfully).
- Workflow YAML: valid (parsed).
- CI committed head `2ee6742`: green (GitHub API).

### Unverified (blocked / not yet run)
- `IT_Form.Tests` execution **currently blocked** by a Windows Application Control policy (`0x800711C7`) that intermittently rejects the freshly-recompiled `IT_Form.Tests.dll`. It ran successfully earlier this session (364 passed), and the AuditTests assembly runs fine — confirming the harness works and the block is specific to the recompiled binary's signature, not the code. New/updated tests (including the `GetHodsByCompany` fix) compile cleanly but their run is blocked at this moment.
- The uncommitted changes (SBOM job, checksum step, all authorization fixes) have **not** run in GitHub Actions because committing/pushing is not permitted without approval.

### Deployment-dependent (external gates)
- Live ClamAV + EICAR test, SMTP/Inventory credentials, Data Protection certificate, witnessed restore, DSAR identity-verification/export/erasure execution.

## 7. Conclusion

The codebase and its uncommitted hardening changes are in a strong state. This review fixed one genuine cross-tenant IDOR (`GetHodsByCompany`) that the prior passes missed. No other Critical/High authorization, file, or CI findings remain. Two Medium items (DSAR scoping and `SHA256SUMS` attestation) are documented as product/hardening decisions, not blocking defects. **This is not evidence of full system security or legal compliance** — final assurance requires a real CI run on the committed changes and completion of the deployment-dependent gates.

**Recommendation:** do not commit yet. Keep the uncommitted changes on `ci-cd/hardening`; the next step is a real CI run once you authorize a commit, plus resolution of the WDAC local-test blocker.
