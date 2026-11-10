# Privacy and Security Compliance Readiness

**Assessment date:** 10 November 2026  
**Repository:** `chire0722/IT-Portal`  
**Assessment branch:** `ci-cd/hardening`  
**Status:** Engineering readiness assessment — **not a certification, legal opinion, or declaration of compliance**.

This document records what can be evidenced from the source tree and what still requires engineering, deployment, organisational, or legal action. A green CI pipeline is not proof of GDPR, Malaysian PDPA, or NIST conformity.

## 1. Applicable framework and scope

### Malaysia PDPA

The primary local reference is the **Personal Data Protection Act 2010 (Act 709)** as amended by the **Personal Data Protection (Amendment) Act 2024 (Act A1727)** and the applicable commencement orders, circulars, regulations, and Commissioner guidelines.

Relevant amendment provisions include:

- **Section 9 / Security Principle:** the amendment expressly extends the Security Principle to data controllers and data processors.
- **Section 12A:** appointment of one or more Data Protection Officers (DPOs), with notification to the Commissioner in the required manner.
- **Section 12B:** notification to the Commissioner when the controller has reason to believe a personal data breach occurred; notification to affected data subjects where significant harm is caused or likely.
- **Section 43A:** data portability, subject to technical feasibility and format compatibility.

The organisation must confirm its controller/processor roles, whether it falls within any controller-registration class, and the current DPO and breach-notification requirements in the Commissioner’s current circulars and guidelines.

The Commissioner’s published DPO FAQ states that appointment is required where processing involves **more than 20,000 data subjects**, **more than 10,000 data subjects whose data includes sensitive personal data (including financial information)**, or **regular and systematic monitoring**. Where a DPO is required, the appointment must be notified within 21 days. Not every organisation automatically meets these criteria; determine this from the actual data inventory and monitoring activities.

### GDPR

GDPR is **not automatically applicable merely because the application is hosted in Malaysia**. Confirm territorial scope under Article 3 (for example, an EU establishment or relevant offering/monitoring of individuals in the EU).

If GDPR applies, the engineering and operational programme must address, among other provisions:

- Articles 5–6: processing principles and lawful basis.
- Articles 12–22: transparent information and data-subject rights.
- Articles 25 and 32: data protection by design/default and security of processing.
- Articles 30, 33–34 and 35: processing records, breach notification, and DPIA where required.

### NIST

Use **NIST Cybersecurity Framework (CSF) 2.0, NIST CSWP 29 (26 February 2024)** as a risk-management framework. Its six concurrent functions are Govern, Identify, Protect, Detect, Respond, and Recover. CSF is outcome-based; it is not a product certification.

## 2. Source-code controls observed

| Area | Current evidence in this branch | Readiness |
|---|---|---|
| Authentication | ASP.NET Identity, password policy, lockout controls, security-stamp validation | Implemented; operational review remains |
| Authorization | Role/claim policies and company-scope filtering exist across application flows | Partially verified; needs route-by-route negative tests |
| Sensitive fields | EF Core value converters encrypt fields marked with `EncryptColumn`; audit changes redact marked values | Implemented for marked fields only; inventory all personal-data fields |
| Sensitive files | Resignation/offboarding files are stored under `App_Data/SecureUploads`, outside the static web root; downloads apply authenticated and company-scoped checks; PDF/image/video uploads require matching MIME and magic-byte signatures; offboarding resignation-letter uploads are scanned through fail-closed ClamAV INSTREAM before persistence | Implemented for resignation-letter scan and supported-format checks; other upload surfaces, scanner deployment/health, filesystem ACL, image re-encoding, and backup access require verification |
| Upload validation | Excel uploads validate extension, MIME declaration, OOXML package structure, ZIP entry limits, macro presence, and endpoint-specific size limits; supported document/image/video paths validate signatures; ClamAV scans resignation-letter uploads | Partial: integrate malware scanning across company logos, spreadsheets and every other upload path; review deeper parsing and safe image re-encoding |
| Audit | Audit payload sanitizer redacts sensitive property names and long/data-URI values; Serilog request logging redacts device-verification bearer tokens and employee IDs embedded in route paths | Partially implemented; test for nested, renamed, and free-text personal data and custom log statements |\n| Digital signature integrity | HMAC verification now rejects synthetic `SYSTEM-IMPORT-` markers; signature metadata guards treat any partial timestamp/signature/HMAC tuple as invalid across onboarding/offboarding detail, PDF, bulk export, and public acknowledgement paths | Implemented in reviewed paths; run full regression tests and review any historical imported records that used synthetic markers |
| Retention | Background service redacts selected identifiers in completed/rejected onboarding/offboarding records, removes offboarding attachments, purges expired tokens, and redacts old audit payloads | Partially implemented; actor references are cleared, but remaining business metadata means irreversible anonymisation is not established |
| Backups | SQLite snapshots are protected using ASP.NET Core Data Protection before writing backup files | Implemented; protection-key custody and restore testing remain critical |
| CI security | Build, tests, Dependency Review, Gitleaks, and CodeQL are in the workflow | Implemented; alerts must still be reviewed and remediated |
| HTTP security headers | CSP, frame denial, MIME sniffing protection, referrer policy, and permissions policy are present | Partial; CSP still contains unsafe directives and internal HTTP origins |
| Cookies | Production cookie policy is hardened in this branch to require Secure cookies; Development retains local HTTP support | Implemented in source; verify production proxy/TLS configuration |

## 3. Gaps requiring work before a compliance claim

### P0 — Organisation and privacy governance

1. Confirm the legal entity acting as data controller and identify all processors/sub-processors.
2. Determine whether the organisation must register as a data controller under the current Malaysian registration rules.
3. Appoint the required DPO(s), confirm competency/independence, and complete Commissioner notification where applicable. Configure a real DPO contact; do not use a generic IT mailbox as a substitute unless formally designated.
4. Maintain a Record of Processing Activities / data inventory: data categories, data subjects, purposes, lawful basis, recipients, systems, locations, retention, and access owners.
5. Confirm whether GDPR applies and document the territorial-scope decision.

### P0 — Data-subject request handling

The current privacy page describes access and correction contact details, but the application does not evidence a complete case-managed workflow for:

- identity verification and request logging;
- access and correction;
- deletion/erasure assessment, including legal-retention exceptions;
- restriction/objection/withdrawal handling where applicable;
- Malaysian data-portability requests under section 43A;
- response deadlines, extension/exception reasons, approval, and evidence of completion.

Implement a controlled DSAR workflow with least-privilege access, audit trail, export format, legal-hold checks, and secure delivery. Do not automatically erase employment or audit records without an approved retention/legal-hold decision.

### P0 — Incident and breach response

The privacy page currently provides a reporting email, but source review did not identify a complete incident case-management and notification workflow. Establish and test:

- 24/7 or otherwise appropriately staffed reporting and escalation route;
- incident record with discovery time, affected systems/data, containment, evidence preservation, risk/harm assessment, and decisions;
- Malaysian Commissioner notification process under the DBN Guideline: notify as soon as practicable and no later than **72 hours** when the guideline's notification criteria are met; assess separately whether affected-person notification is required due to significant harm;
- GDPR supervisory-authority notification within 72 hours where Article 33 applies, and data-subject communication where Article 34 applies;
- processor-to-controller escalation clauses and contact details;
- tabletop exercises and post-incident review.

The Malaysian DBN Guideline specifies a 72-hour maximum for Commissioner notification when its criteria are met. Its assessment includes significant-scale and significant-harm criteria; the Commissioner’s DBN reporting portal identifies more than 1,000 affected data subjects as significant scale. Follow the current guideline/circular for the start of the clock and each notification decision.

### P1 — Data minimisation, accuracy, and retention

1. The current default record/audit retention is 2,555 days (approximately seven years); security-token retention is 30 days. These are application defaults, not proof that seven years is necessary or legally required for every record.
2. Approve a record-class retention schedule with a documented purpose/legal basis and owner for each data category.
3. Add deletion/redaction coverage for every personal-data store, attachments, generated PDFs, exports, temporary files, logs, backups, and downstream systems. The current job clears creator/approver/rejector/HOD references after the configured period, but still retains some dates and business metadata; do not describe the result as irreversible anonymisation without a re-identification assessment.
4. Define backup expiry and deletion behaviour, including how restored backups are brought back under current deletion/retention rules.
5. Test that retention jobs run, report failures, and do not silently leave orphaned files or personal data.

### P1 — Encryption and key management

- The SQLite database itself is not encrypted by the application code. EF Core converters encrypt only properties explicitly marked with `EncryptColumn`; source review found other personal/business-identifying fields (for example names, department/branch/designation, desk details, signer names, free-text remarks, and some audit identifiers) that are not uniformly covered. This is not whole-database or complete field-level encryption.
- Backup files and marked sensitive fields use ASP.NET Core Data Protection. Production startup now requires a valid RSA certificate from the Windows `LocalMachine\My` store and configures certificate protection for newly persisted Data Protection key-ring XML. The certificate private key must be ACL-restricted to the application identity.
- Plaintext SQLite backup snapshots are staged locally under `App_Data/BackupStaging/<machine>/<process-id>`, not in configured remote backup destinations, and stale prior-process staging directories are cleaned after 24 hours. The staging volume still requires encryption and restrictive ACLs.
- This change does **not** encrypt the SQLite database as a whole. EF Core converters still protect only properties explicitly marked with `EncryptColumn`; other personal/business-identifying fields (for example names, department/branch/designation, desk details, signer names, free-text remarks and some audit identifiers) are not uniformly covered.
- Certificate protection does **not** retroactively encrypt existing key-ring XML files. Existing key files, database files, upload directories and logs still require host-volume encryption and restrictive OS ACLs. Historical exposed key material must be assessed as compromised if it was used in a real environment.
- Configure a durable `DataProtection:KeyRingPath`, install the certificate on every application instance, verify private-key ACLs, maintain protected key backups, and test cookie/field/backup decryption and restore. Plan key/certificate rollover and dependent-data re-protection before retiring historical keys.
- The code change is committed to the assessment branch; production certificate provisioning, deployment validation and restore evidence remain outstanding release gates.

### P1 — Processor and cross-border governance

Create and maintain an approved vendor/sub-processor register covering hosting, email/SMTP, backups, monitoring, support, and any integrations. Record data locations, support access locations, transfer mechanism/assessment, security terms, retention/deletion commitments, and breach escalation obligations. Do not claim that data stays in Malaysia until hosting, backups, telemetry, and support locations have been verified.

### P1 — Application hardening

1. CSP no longer trusts hard-coded private HTTP origins and now explicitly restricts objects, base URIs, form actions, and connections. The `unsafe-eval` exception and Tailwind browser CDN are now scoped to the device-verification route only; replace that page's CDN runtime with precompiled local CSS to remove the exception entirely. Reduce the remaining `unsafe-inline` through nonce/hash-based scripts after inventorying inline scripts and testing every view.
3. PDF/image/video uploads validate allowed extensions, MIME types and magic-byte signatures; offboarding resignation-letter uploads are scanned by ClamAV before persistence and fail closed on scanner outage. PDF downloads are forced to attachment. Extend malware scanning to company logos, spreadsheet imports and every other upload path; add safe image re-encoding and deeper file-format parsing. Upload endpoints have explicit request-body limits aligned to per-file limits.
4. Complete a route-by-route authorization matrix and negative tests for cross-company access, exports, downloads, APIs, and signature/token endpoints.
5. Review request/audit/application logs for personal data, tokens, query strings, exception content, and retention. Verification bearer tokens and employee IDs in known route paths are redacted from Serilog request-path logs; custom log statements and other routes still require review.
6. Review Data Protection key permissions and SQLite file permissions on every supported deployment platform.

## 4. NIST CSF 2.0 work programme

| Function | Required evidence for this system |
|---|---|
| Govern | Named risk owner, DPO/privacy owner, policies, legal applicability decision, supplier obligations, risk acceptance |
| Identify | Data inventory, system/data-flow map, asset and dependency inventory, threat/risk assessment, DPIA screening |
| Protect | Least privilege, MFA policy, encryption/key management, secure SDLC, patching, backups, privacy by design |
| Detect | Centralised security logging, alerting, anomaly monitoring, dependency/secret/code scanning, log integrity |
| Respond | Incident playbook, roles, escalation, breach assessment/notification, evidence handling, exercises |
| Recover | Tested restore procedures, RTO/RPO, backup-key recovery, lessons learned, continuity plans |

## 5. Release gates

Do not label the system “GDPR compliant”, “PDPA compliant”, or “NIST certified” until all applicable legal and organisational controls have evidence and independent review.

Before production approval, require:

- all P0 items closed and signed off by the controller/DPO/legal owner;
- all P1 technical controls either implemented or formally risk-accepted;
- CI green on the exact release commit and CodeQL alerts triaged;
- authorization and privacy regression tests passed;
- production TLS, proxy trust, secure cookies, filesystem ACLs, encrypted volumes, and key management verified;
- retention, restore, and incident-response exercises completed;
- privacy notice and processor disclosures reviewed against actual data flows.

## 6. Public primary references

- Malaysia Personal Data Protection Department — [Act 709, amendment, circulars and guidelines](https://www.pdp.gov.my/en/akta/pdp-act-2010-en/).
- Laws of Malaysia — [Personal Data Protection (Amendment) Act 2024, Act A1727](https://www.pdp.gov.my/ppdpv1/wp-content/uploads/2024/11/Act-A1727.pdf).
- Malaysia PDP Commissioner — [DPO appointment circular and guidelines](https://www.pdp.gov.my/en/akta/personal-data-protection-guidelines-on-the-appointment-of-data-protection-officer-dpo/), [DPO FAQ and appointment thresholds](https://www.pdp.gov.my/en/faq/), and [Data Breach Notification guideline (PDF)](https://www.pdp.gov.my/ppdpv1/wp-content/uploads/2025/08/GP_DBN_ENG.pdf).
- European Union — [Regulation (EU) 2016/679 (GDPR), official EUR-Lex text](https://eur-lex.europa.eu/eli/reg/2016/679/oj).
- NIST — [The NIST Cybersecurity Framework (CSF) 2.0, NIST CSWP 29 (2024), DOI: 10.6028/NIST.CSWP.29](https://doi.org/10.6028/NIST.CSWP.29).
