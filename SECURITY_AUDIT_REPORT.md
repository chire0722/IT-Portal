# IT-Portal Security Audit & Hardening Report

**Repository:** `chire0722/IT-Portal`  
**Audit branch:** `security/audit-hardening`  
**Pull request:** #2 → `security/remediation` (draft)  
**Main branch:** Not modified or merged by this audit  
**Audit date:** 2026-11-10 (Malaysia time)

## Executive summary

A multi-pass security review was performed across authentication/OTP flows, authorization and company scoping, onboarding/offboarding signatures, notification/email workflows, file uploads/imports, backup and configuration handling, audit logging, and HTTP endpoint protections.

The work was isolated to `security/audit-hardening`. The proposed changes are not merged into `main`. The PR remains draft and must not be merged until the latest CI run and staging validation are complete.

## High-impact fixes implemented

### Authentication and OTP
- Enforce OTP purpose validation at command boundaries.
- Restrict anonymous resend to password reset or eligible initial Super Admin login.
- Keep account recovery responses enumeration-resistant.
- Expire device verification tokens, including tokens without an employee record.
- Add per-IP throttling and no-store/no-referrer response headers to public device verification.
- Audit privileged OTP actions while redacting OTP values and identifiers.

### Authorization and tenant boundaries
- Enforce company scope in employee detail reads, employee directory results, software employee assignment lookup, and offboarding detail access.
- Validate offboarding checker authorization before applying approval mutations.
- Restrict onboarding token regeneration and bulk exports to HR; cap bulk exports and reject partially unauthorized selections.
- Validate authorization in offboarding signature handlers and deletion workflows.
- Apply explicit company assignments to scoped IT administrators across license lookup, creation/editing, imports, key reads/decryption, and key/seat mutations. Super Admin and unscoped global IT retain global license administration. Employee assignment remains company-scoped.
- Keep unscoped legacy software licenses visible only to global administrators; scoped IT users see only company-linked license versions.
- Preserve global Super Admin access even when legacy identities still contain a company claim, while retaining explicit company restrictions for scoped IT identities.
- Bind newly imported license versions to the importing scoped IT user's companies; reject attempts to add keys to an existing license outside that scope.

### Signatures, integrity and privacy
- Revalidate required upstream HMAC signatures before employee acknowledgement, public acknowledgement/PDF reads, offboarding PDF generation, and notification attachments/previews.
- Enforce PDPA consent in the command handler as well as the controller.
- Validate signature data URLs at both validator and handler boundaries.
- Reject SVG, malformed and oversized signature data; allow bounded PNG/JPEG/GIF/WebP raster images only.
- Historical signatures created with the previously exposed HMAC key remain untrusted and require a controlled review/re-signing process.

### Email and notification safety
- Close an SMTP relay authorization gap by checking request context and authorized HR/IT, assigned HOD, or authorized Manager access.
- Restrict acknowledgement email to HR.
- Add per-user rate limiting to workflow email sends.
- Sanitize SMTP subject headers, encode user-controlled names in email HTML, bound recipients/content, avoid logging recipient email addresses, and guard nullable email bodies.

### File handling and import limits
- Sanitize ZIP entry names and download filenames against path/header injection.
- Reject SVG and malformed signature/logo image data.
- Bound spreadsheet imports to 5,000 data rows and 20 columns across the import paths reviewed.
- Hash imported license keys for idempotency, reject oversized key values, and prevent duplicate imports from creating duplicate key records.
- Cap generated subscription seats at 5,000 per import and 5,000 per license; enforce the same limit in add/edit/quick-allocation flows to prevent memory and database exhaustion.
- Validate upload fixtures and add regression coverage for safe image parsing.

### HTTP and configuration hardening
- Add anti-forgery validation to cookie-authenticated MVC/API mutation endpoints, including software key/license, Department, Onboarding, Offboarding, and device-token generation endpoints.
- Confirm the Standard Software AJAX calls send the configured `X-XSRF-TOKEN` header.
- Remove committed private-network addresses and UNC backup shares from active configuration/CSP.
- Require explicit production host/proxy/backup/HMAC/SMTP configuration.
- Keep production cookies HTTPS-only and require a newly generated HMAC secret outside Git.

### Audit logging
- Expand payload redaction for credentials and personal data.
- Record privileged OTP operations without storing OTP/email secrets.
- Store generic error messages in database audit records rather than raw exception text; preserve detailed diagnostics in application logs.
- Mark unsuccessful command results as failures.

## Validation

CI workflow: `.github/workflows/dotnet-security.yml`  
Validation stages: .NET 10 restore, application build, application tests, audit tests.

- Latest verified code head: `24f6114d8ce80893d0fe279b1d6744a276b8f881` on `security/audit-hardening`.
- GitHub Actions run **#465** completed successfully: restore passed, application build passed, **335 application tests passed**, and **14 audit tests passed** (349 tests total).
- A previous CI failure exposed two stale expectations from the old cross-company IT behavior; the tests were updated to assert the new explicit company boundary. The subsequent full run #465 passed.
- The application build and both test suites passed after the scoped-license, Excel import, subscription-seat cap, and global-identity fixes.
- Build/test success does not replace staging verification of database migrations, native SQLite compatibility, SMTP, backups, Data Protection, reverse-proxy headers, and browser workflows.

## Residual risks and required follow-up

1. **CSP:** inline scripts and `eval` remain permitted. Move to nonce/hash-based scripts and remove `unsafe-eval` after browser regression testing.
2. **OTP storage:** SHA-256 OTP hashes have low entropy and can be guessed offline after a database-only compromise. Introduce a server-side HMAC/pepper and rotate safely.
3. **Historical signatures:** signatures made with the previously exposed HMAC key cannot be treated as trustworthy. Review historical records and re-sign where necessary.
4. **Document integrity:** current HMAC protects signer/time/signature metadata, not every business field in the generated PDF snapshot. Add a versioned canonical document hash.
5. **SQLite native runtime:** verify the pinned SQLite native package on the actual deployment OS/runtime.
6. **Production secrets:** generate a fresh HMAC key with at least 32 random bytes, Base64-encode it, and store it outside Git. Rotate SMTP and inventory API credentials if they were ever exposed.
7. **Operations:** verify backup/restore compatibility, secure ASP.NET Data Protection key-ring backup, trusted proxy configuration, HTTPS termination, and production `AllowedHosts`.
8. **Security tooling:** add gated dependency vulnerability scanning and SAST/CodeQL after validating the CI runner/action versions and remediation policy.

## Release decision

**Do not merge or deploy yet.** Keep PR #2 as draft until the latest CI is green, all security-sensitive browser flows are tested in staging, and the residual-risk items have an owner and mitigation plan. This report records completed code changes and known limitations; it does not certify the application as vulnerability-free.
