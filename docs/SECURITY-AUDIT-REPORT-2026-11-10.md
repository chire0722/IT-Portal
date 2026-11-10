# IT-Portal Security Audit and Hardening Report

**Assessment date:** 10 November 2026  
**Repository:** `chire0722/IT-Portal`  
**Assessment branch:** `ci-cd/hardening`  
**Pull request:** #3 — CI/CD hardening: .NET 10 build, tests, CodeQL and Dependabot  
**Audited code head:** `30fe9ffbfc3d65b3ca3ab2b766ffd77325b999fc`  
**Status:** Engineering remediation and source-level verification completed for this iteration. This is not a penetration-test attestation, legal opinion, certification, or declaration of GDPR, Malaysian PDPA, or NIST conformity.

## 1. Executive summary

This iteration hardened authentication/authorization-sensitive workflows, signature integrity, file uploads and storage, export safety, audit privacy, HMAC key rotation, and a restricted DSAR case workflow.

The final GitHub Actions run for the current code head passed 391 tests (336 application tests and 55 audit tests). GitHub is the authoritative current-head build and test evidence.

- .NET 10 application build on the current code head: passed in GitHub Actions; the previously reported nullable warning is fixed.
- Current-head `IT_Form.Tests`: 336 passed, 0 failed.
- Current-head `IT_Form.AuditTests`: 55 passed, 0 failed.
- Current-head GitHub CI run: `42802133045`, passed.

**Current-head total: 391 passed, 0 failed, 0 skipped.**

The report-only follow-up run `42803077210` also passed all executable checks on the final PR tree; Dependency Review was skipped because that follow-up changed documentation only.

The hardening branch remains separate from `main`. Pull request #3 is open and has not been merged. These results prove the recorded source/build/test checks only; they do not prove production configuration, deployment controls, or organisational compliance.

## 2. Remediations implemented

### 2.1 Authorization and data isolation

- Rechecked onboarding/offboarding deletion, secure-file download, and PDF/export paths for record-level and company-scope authorization.
- Sensitive-file downloads authorize against the owning offboarding record before checking whether the physical file exists, reducing file-existence oracles.
- Bulk PDF export rejects mixed-scope selections instead of silently exporting only an accessible subset.
- Offboarding bulk PDF export requires HR authorization and verifies signature integrity before rendering each document.
- Existing route-level authorization and company-scope helpers remain in place; a complete negative-test matrix for every route is still outstanding.
- Added malware scanning to the direct employee Excel import, IT asset Excel import, and offboarding hardware-damage/hardware-return attachment paths; those paths also use bounded request bodies and cancellation-aware copying.

### 2.2 Digital signature integrity

- Removed the synthetic `SYSTEM-IMPORT-` HMAC bypass. A marker string is no longer accepted as cryptographic proof.
- Added `SignatureIntegrityGuard` so partial timestamp/signature/HMAC metadata fails closed.
- Applied integrity checks to reviewed onboarding/offboarding detail, PDF generation, bulk export, and public employee-acknowledgement paths.
- PDF rendering displays invalid-signature states instead of silently presenting tampered signature data as verified.
- Added bounded PNG data-URL validation, including encoded-size and image-dimension limits, for signature inputs.
- Added regression tests for synthetic HMAC markers and partial signature metadata.
- Added a versioned HMAC key ring with current-version prefixes, previous-key verification, unversioned legacy verification during migration, malformed-version rejection, and configuration fail-closed behavior. Rotation regression tests pass in current-head CI.

**Compatibility note:** historical records carrying synthetic import markers or incomplete signature metadata may now be rejected as unverified. Review and, where appropriate, re-sign those records through an approved process. Do not restore the bypass to make legacy records appear valid.

### 2.3 Malware scanning and upload validation

- Added `IFileMalwareScanner` and `ClamAvMalwareScanner` using ClamAV's NUL-terminated `zINSTREAM` protocol.
- The scanner streams content in bounded chunks.
- Only a clean `stream: OK` verdict is accepted. Infected, malformed, unknown, timed-out, unconfigured, or unavailable scanner states fail closed.
- Integrated scanning into the reviewed offboarding resignation-letter, company-logo create/update, and supported Excel import workflows (department, email contact, email group, server folder, shared mailbox, and standard software imports).
- Excel uploads validate extension, declared media type, OOXML package structure, entry count, expanded size, per-entry size, macro presence, unsafe ZIP paths, and ZIP symlink entries.
- Supported document/image/video uploads validate both declared MIME type and file signatures.
- Added deep StbImageSharp parsing, dimension/pixel limits, metadata stripping and canonical PNG re-encoding for company logos, email signature images, and offboarding raster attachments before storage or rendering. Truncated/malformed image regression tests are included.
- Added tests for ClamAV protocol verdicts, malformed responses, scanner-not-configured behavior, Excel ZIP traversal rejection, logo file signatures, and the remaining direct employee/asset/offboarding attachment paths.

**Deployment limitation:** a real ClamAV daemon, signature freshness, network ACLs, monitoring/alerting, and EICAR detection in an isolated staging environment still require deployment verification. Source integration and mock-protocol tests are not proof that production malware scanning is operational. The source-level upload/attachment inventory is recorded in `docs/FILE-SECURITY-ROUTE-INVENTORY.md`; route-by-route live negative authorization tests remain outstanding.

### 2.4 File storage, path safety, and cleanup

- Sensitive uploads continue to be stored outside the static web root and published through authenticated routes.
- Sensitive-file path parsing now rejects dot segments and unsafe final file names.
- Company logos use unique names, temporary files, bounded stream copying, and atomic same-directory publication.
- Company logo resolution and deletion are confined to the managed logo directory; traversal paths and reparse-point files are rejected for PDF logo reads.
- Replacing a logo deletes the old file only after the database update succeeds.
- Deleting a company or offboarding request deletes associated files only after the database deletion succeeds.
- Failed sensitive-file path persistence cleans up the newly written file; failed cleanup is logged or left for operator remediation rather than silently treated as successful.

**Remaining limitation:** production filesystem ACL verification, encrypted-volume proof, and runtime evidence for the complete cross-role upload/download matrix remain release gates. The source-level route inventory is recorded in `docs/FILE-SECURITY-ROUTE-INVENTORY.md`.

### 2.5 Encryption, backup, and key management

- Data Protection key-ring protection supports a current certificate for new protection and configured previous certificates for historical decryption during certificate rollover.
- Added deployment documentation for prior-certificate retention and restore validation.
- Plaintext SQLite backup snapshots are staged locally rather than in remote backup destinations; encrypted backup publication uses temporary files and atomic movement.
- Backup recovery still depends on durable key-ring storage, certificate private-key availability, restrictive ACLs, and tested restoration.

**Important limitation:** the SQLite database itself is not encrypted as a whole. EF Core converters protect only properties explicitly marked with `EncryptColumn`. Production volume encryption, key custody, certificate provisioning, historical key exposure assessment, and witnessed restore testing remain outstanding.

### 2.6 Export and audit hardening

- Offboarding CSV export uses formula-injection sanitization for spreadsheet-active values.
- PDF generation no longer exposes raw internal exception messages to end users in the reviewed paths. Bulk onboarding and offboarding PDF exports are capped at 100 requests and 100 MB of generated PDF data, bounded at the HTTP request, validator, and service layers, and fail rather than returning a partial archive.
- Existing audit payload and request-log sanitization remains in place.
- Audit failure records no longer persist raw exception messages; free-text redaction covers common secret/email patterns; audit notification emails sanitize JSON and HTML-encode user-controlled values.
- Background email HTML now encodes interpolated user values, stores only sanitized exception types in the outbox error field, and has a regression test for markup injection.
- Email confirmation POSTs now use a Data Protection integrity token bound to the request, email type, server-generated HTML/signature context, and current actor; request-level authorization, recipient limits/format validation, and subject header-injection checks run before dispatch.
- A restricted HR-only DSAR case register now supports access, portability, correction and deletion request types, idempotent submission, allow-listed state transitions, optimistic concurrency, immutable events, and completion evidence references. It does not yet perform data discovery, secure export delivery, legal-hold evaluation, or deletion.
- The retention service now permanently removes expired terminal email outbox records while preserving pending/processing messages for delivery recovery. The retention regression test passes. Email outbox payloads are now protected with the versioned application data-protection service, while legacy plaintext rows remain readable for migration. SMTP now fails closed without valid host/port/TLS and applies a bounded send timeout. No webhook controller or webhook callback handler was found in the repository, so no webhook-specific patch was applicable; any future webhook must use authenticated, replay-resistant, signed payload validation.
- Added `docs/ENCRYPTED-FIELD-INVENTORY.md` with the exact current `[EncryptColumn]` properties and the unprotected field categories requiring a field-by-field product decision.
- Removed inline event attributes from Razor views, disabled `script-src-attr` inline execution, replaced the verification page's Tailwind browser runtime with local CSS, and removed `unsafe-eval`.

### 2.7 Documentation and governance evidence

Updated the engineering readiness and control-mapping documents for:

- NIST CSF 2.0 (Govern, Identify, Protect, Detect, Respond, Recover);
- Malaysian PDPA and conditional GDPR applicability;
- privacy operations and data-subject request handling;
- incident and personal-data breach response;
- file-security operations;
- encryption and key management.

The documents explicitly distinguish source-code evidence from organisation-owned and deployment-owned evidence.

## 3. CI verification evidence

**Current GitHub Actions run:** `42802133045`  
**Code head tested:** `30fe9ffbfc3d65b3ca3ab2b766ffd77325b999fc`  
**Run conclusion:** success

| Check | Result |
|---|---:|
| .NET 10 build | Passed |
| IT_Form.Tests | 336 passed / 0 failed |
| IT_Form.AuditTests | 55 passed / 0 failed |
| CodeQL C# | Passed |
| CodeQL Actions | Passed |
| Gitleaks current tree | No leaks found |
| Dependency Review | Passed |

These are automated CI results. They do not replace dynamic testing against a deployed environment, a human security review, or an independent penetration test.

## 4. Outstanding release blockers and residual risks

### P0 — Must be resolved before production approval

1. **Production malware scanning:** deploy ClamAV on an isolated private network; restrict access to the application host; verify signature updates, health monitoring, alerting, timeout behavior, and EICAR detection in isolated staging.
2. **Upload/download authorization evidence:** the source route inventory is complete for the reviewed repository paths, but the full live cross-company/cross-role matrix still must run and be recorded; the direct employee/asset/offboarding attachment paths are covered in source and focused tests.
3. **DSAR operations:** the code now has a restricted case register and state machine, but identity verification, data inventory-driven access/portability export, correction execution, legal holds, response deadlines, secure delivery, processor propagation, and deletion execution remain release gates.
4. **Incident management:** the incident-response playbook is a procedure template; a case-managed incident register, named incident commander, on-call contacts, processor escalation, and tabletop evidence remain outstanding.
5. **Privacy governance:** confirm the data controller, processor/sub-processor register, applicable Malaysian registration and DPO requirements, and GDPR Article 3 territorial scope with the responsible legal/privacy owner.
6. **Production key and backup controls:** provision the Data Protection certificate and durable key ring on all production instances; verify private-key ACLs, encrypted volumes, backup custody, recovery-key availability, and a witnessed restore.

### P1 — Required engineering and operational follow-up

1. **Encryption scope:** the exact marked-field inventory is documented. A field-by-field classification of remaining personal/business data, query/index impact, migration/backfill, and re-protection plan is still required; marked-column encryption is not whole-database encryption.
2. **HMAC production migration:** versioned key-ring code and regression tests are present; provision real versioned secrets and retain previous keys through migration before production rotation.
3. **Image processing:** source-level re-encoding and deep parsing are implemented for the reviewed image ingress paths; verify decoder limits and rendering behavior in the deployed runtime.
4. **CSP:** source-level exceptions are removed (`script-src-attr 'none'` and no `unsafe-eval`); browser regression testing across all views remains required.
5. **Authorization testing:** complete negative tests for every API, download, export, signed-link, and cross-company access route, including role changes and stale claims.
6. **Audit privacy:** review nested, renamed, free-text, and custom audit/logging paths for personal data and secrets.
7. **Retention:** terminal email outbox records are now removed by the retention service, but validate periods against actual business/legal purposes and test deletion/redaction across database rows, files, exports, logs, backups, and restored backups. Current redaction is not proven irreversible anonymisation.
8. **Resilience:** establish off-host/immutable backups, RTO/RPO, restore monitoring, and periodic witnessed recovery exercises.
9. **Background jobs and integrations:** the email outbox has bounded retry/lease/dead-letter behavior, protected payloads with legacy-read compatibility, and current tests cover dispatch. SMTP source controls now include TLS/host/port validation and a bounded timeout. External delivery remains at-least-once: an SMTP connection failure after remote acceptance can still duplicate a message, so exactly-once/idempotency requires a provider-supported message key or delivery receipt. Production gateway authentication, queue ACLs, PDF limits, scanner health, and deployment evidence remain release gates.

### P2 — Continuous improvement

- Add recurring independent security reviews and penetration testing.
- Establish patch SLAs, vulnerability triage ownership, and measurable remediation targets.
- Maintain a control evidence register with owner, environment, test date, result, exception, and review date.

## 5. Compliance position

The repository must **not** be described as fully GDPR-compliant, Malaysian PDPA-compliant, or NIST-certified on the basis of this audit or a green CI run.

Compliance depends on the actual controller/processor roles, data flows, legal applicability, approved policies, organisational owners, production deployment, supplier contracts, user operations, and retained evidence. The NIST CSF 2.0 mapping is an engineering evidence map, not a certification.

## 6. Recommended next actions

1. Keep the changes on `ci-cd/hardening` and review pull request #3; do not merge automatically.
2. Resolve the P0 operational and governance blockers with named owners.
3. Complete the remaining upload and authorization inventories and negative tests.
4. Provision and exercise the versioned HMAC key ring before any production HMAC-key replacement; do not remove previous keys until historical verification is complete.
5. Deploy to staging, run the approved EICAR test, validate certificate/key permissions, and perform a witnessed backup restore.
6. Re-run CI and an independent security review after those actions.

**Final assessment:** substantial source-level hardening is implemented, and current-head GitHub CI passed 391 tests with CodeQL C#, CodeQL Actions, Gitleaks, and Dependency Review successful. Production security and legal/compliance readiness remain conditional on the outstanding release gates above.

