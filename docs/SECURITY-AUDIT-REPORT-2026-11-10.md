# IT-Portal Security Audit and Hardening Report

**Assessment date:** 10 November 2026  
**Repository:** `chire0722/IT-Portal`  
**Assessment branch:** `ci-cd/hardening`  
**Pull request:** #3 — CI/CD hardening: .NET 10 build, tests, CodeQL and Dependabot  
**Audited head commit:** `988f9790446c1940b22206ead2b158d04e78a13c`  
**Status:** Engineering remediation and source-level verification completed for this iteration. This is not a penetration-test attestation, legal opinion, certification, or declaration of GDPR, Malaysian PDPA, or NIST conformity.

## 1. Executive summary

This iteration hardened authentication/authorization-sensitive workflows, signature integrity, file uploads and storage, export safety, backup-key rollover, and CI security checks.

The latest GitHub Actions run for the audited head commit completed successfully:

- .NET 10 application build: passed.
- `IT_Form.Tests`: 325 passed, 0 failed.
- `IT_Form.AuditTests`: 54 passed, 0 failed.
- CodeQL C#: passed.
- CodeQL Actions: passed.
- Gitleaks: passed.
- Dependency Review: passed.

**Total automated tests: 379 passed, 0 failed.**

The hardening branch remains separate from `main`. Pull request #3 is open and has not been merged. These results prove the recorded source/build/test checks only; they do not prove production configuration, deployment controls, or organisational compliance.

## 2. Remediations implemented

### 2.1 Authorization and data isolation

- Rechecked onboarding/offboarding deletion, secure-file download, and PDF/export paths for record-level and company-scope authorization.
- Sensitive-file downloads authorize against the owning offboarding record before checking whether the physical file exists, reducing file-existence oracles.
- Bulk PDF export rejects mixed-scope selections instead of silently exporting only an accessible subset.
- Offboarding bulk PDF export requires HR authorization and verifies signature integrity before rendering each document.
- Existing route-level authorization and company-scope helpers remain in place; a complete negative-test matrix for every route is still outstanding.

### 2.2 Digital signature integrity

- Removed the synthetic `SYSTEM-IMPORT-` HMAC bypass. A marker string is no longer accepted as cryptographic proof.
- Added `SignatureIntegrityGuard` so partial timestamp/signature/HMAC metadata fails closed.
- Applied integrity checks to reviewed onboarding/offboarding detail, PDF generation, bulk export, and public employee-acknowledgement paths.
- PDF rendering displays invalid-signature states instead of silently presenting tampered signature data as verified.
- Added bounded PNG data-URL validation, including encoded-size and image-dimension limits, for signature inputs.
- Added regression tests for synthetic HMAC markers and partial signature metadata.

**Compatibility note:** historical records carrying synthetic import markers or incomplete signature metadata may now be rejected as unverified. Review and, where appropriate, re-sign those records through an approved process. Do not restore the bypass to make legacy records appear valid.

### 2.3 Malware scanning and upload validation

- Added `IFileMalwareScanner` and `ClamAvMalwareScanner` using ClamAV's NUL-terminated `zINSTREAM` protocol.
- The scanner streams content in bounded chunks.
- Only a clean `stream: OK` verdict is accepted. Infected, malformed, unknown, timed-out, unconfigured, or unavailable scanner states fail closed.
- Integrated scanning into the reviewed offboarding resignation-letter, company-logo create/update, and supported Excel import workflows (department, email contact, email group, server folder, shared mailbox, and standard software imports).
- Excel uploads validate extension, declared media type, OOXML package structure, entry count, expanded size, per-entry size, macro presence, unsafe ZIP paths, and ZIP symlink entries.
- Supported document/image/video uploads validate both declared MIME type and file signatures.
- Added tests for ClamAV protocol verdicts, malformed responses, scanner-not-configured behavior, Excel ZIP traversal rejection, and logo file signatures.

**Deployment limitation:** a real ClamAV daemon, signature freshness, network ACLs, monitoring/alerting, and EICAR detection in an isolated staging environment still require deployment verification. Source integration and mock-protocol tests are not proof that production malware scanning is operational. Remaining upload/attachment routes must be inventoried and checked before claiming complete coverage.

### 2.4 File storage, path safety, and cleanup

- Sensitive uploads continue to be stored outside the static web root and published through authenticated routes.
- Sensitive-file path parsing now rejects dot segments and unsafe final file names.
- Company logos use unique names, temporary files, bounded stream copying, and atomic same-directory publication.
- Company logo resolution and deletion are confined to the managed logo directory; traversal paths and reparse-point files are rejected for PDF logo reads.
- Replacing a logo deletes the old file only after the database update succeeds.
- Deleting a company or offboarding request deletes associated files only after the database deletion succeeds.
- Failed sensitive-file path persistence cleans up the newly written file; failed cleanup is logged or left for operator remediation rather than silently treated as successful.

**Remaining limitation:** safe image re-encoding, deeper file-format parsing, production filesystem ACL verification, encrypted-volume proof, and complete upload-route coverage remain release gates.

### 2.5 Encryption, backup, and key management

- Data Protection key-ring protection supports a current certificate for new protection and configured previous certificates for historical decryption during certificate rollover.
- Added deployment documentation for prior-certificate retention and restore validation.
- Plaintext SQLite backup snapshots are staged locally rather than in remote backup destinations; encrypted backup publication uses temporary files and atomic movement.
- Backup recovery still depends on durable key-ring storage, certificate private-key availability, restrictive ACLs, and tested restoration.

**Important limitation:** the SQLite database itself is not encrypted as a whole. EF Core converters protect only properties explicitly marked with `EncryptColumn`. Production volume encryption, key custody, certificate provisioning, historical key exposure assessment, and witnessed restore testing remain outstanding.

### 2.6 Export and audit hardening

- Offboarding CSV export uses formula-injection sanitization for spreadsheet-active values.
- PDF generation no longer exposes raw internal exception messages to end users in the reviewed paths.
- Existing audit payload and request-log sanitization remains in place.
- Audit free-text and custom log statements still require a wider privacy review; structured payload sanitization does not guarantee that arbitrary free text contains no personal data.

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

**GitHub Actions run:** `42767916053`  
**Head commit:** `988f9790446c1940b22206ead2b158d04e78a13c`  
**Run conclusion:** success

| Check | Result |
|---|---:|
| .NET 10 build | Passed |
| IT_Form.Tests | 325 passed / 0 failed |
| IT_Form.AuditTests | 54 passed / 0 failed |
| CodeQL C# | Passed |
| CodeQL Actions | Passed |
| Gitleaks | Passed |
| Dependency Review | Passed |

These are automated CI results. They do not replace dynamic testing against a deployed environment, a human security review, or an independent penetration test.

## 4. Outstanding release blockers and residual risks

### P0 — Must be resolved before production approval

1. **Production malware scanning:** deploy ClamAV on an isolated private network; restrict access to the application host; verify signature updates, health monitoring, alerting, timeout behavior, and EICAR detection in isolated staging.
2. **Upload coverage:** complete a route-by-route inventory of every upload and attachment endpoint; prove that each required path validates, scans, fails closed, and cleans up partial files.
3. **DSAR operations:** the repository still does not evidence a complete case-managed data-subject request workflow for identity verification, access, correction, portability, deletion approval, legal holds, response deadlines, and completion evidence.
4. **Incident management:** the incident-response playbook is a procedure template; a case-managed incident register, named incident commander, on-call contacts, processor escalation, and tabletop evidence remain outstanding.
5. **Privacy governance:** confirm the data controller, processor/sub-processor register, applicable Malaysian registration and DPO requirements, and GDPR Article 3 territorial scope with the responsible legal/privacy owner.
6. **Production key and backup controls:** provision the Data Protection certificate and durable key ring on all production instances; verify private-key ACLs, encrypted volumes, backup custody, recovery-key availability, and a witnessed restore.

### P1 — Required engineering and operational follow-up

1. **Encryption scope:** inventory all personal and business-identifying fields. Marked-column encryption is not whole-database encryption.
2. **HMAC key rotation:** the current signature-integrity service still uses a single configured HMAC key. Implement and test a versioned key migration before routine key replacement; retain the old key until historical signatures have been migrated or intentionally invalidated.
3. **Image processing:** add safe image re-encoding and deeper parsing where required by the threat model; confirm no untrusted image is rendered by an unsafe decoder.
4. **CSP:** continue removing remaining `unsafe-inline` and route-specific unsafe exceptions after inventorying inline scripts and testing all views.
5. **Authorization testing:** complete negative tests for every API, download, export, signed-link, and cross-company access route, including role changes and stale claims.
6. **Audit privacy:** review nested, renamed, free-text, and custom audit/logging paths for personal data and secrets.
7. **Retention:** validate retention periods against actual business/legal purposes and test deletion/redaction across database rows, files, exports, logs, backups, and restored backups. Current redaction is not proven irreversible anonymisation.
8. **Resilience:** establish off-host/immutable backups, RTO/RPO, restore monitoring, and periodic witnessed recovery exercises.

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
4. Implement versioned HMAC key rotation before any production HMAC-key replacement.
5. Deploy to staging, run the approved EICAR test, validate certificate/key permissions, and perform a witnessed backup restore.
6. Re-run CI and an independent security review after those actions.

**Final assessment:** substantial source-level hardening has been implemented and the audited CI run passed. Production security and legal/compliance readiness remain conditional on the outstanding release gates above.
