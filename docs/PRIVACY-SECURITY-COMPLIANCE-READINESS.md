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
| Sensitive files | Resignation/offboarding files are stored under `App_Data/SecureUploads`, outside the static web root; download endpoint applies authenticated and company-scoped checks | Implemented; filesystem ACL and backup access require deployment verification |
| Upload validation | Excel uploads now validate extension, MIME declaration, OOXML package structure, ZIP entry limits, macro presence, and endpoint-specific size limits | Implemented for Excel path; other upload types need content-signature validation review |
| Audit | Audit payload sanitizer redacts sensitive property names and long/data-URI values | Partially implemented; test for nested, renamed, and free-text personal data |
| Retention | Background service anonymises completed/rejected onboarding/offboarding records, removes offboarding attachments, purges expired tokens, and redacts old audit payloads | Implemented but retention periods and deletion coverage need policy approval |
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
- Malaysian Commissioner and affected-person notification decision process under Act 709 as amended and the current DBN guideline/circular;
- GDPR supervisory-authority notification within 72 hours where Article 33 applies, and data-subject communication where Article 34 applies;
- processor-to-controller escalation clauses and contact details;
- tabletop exercises and post-incident review.

Do not infer a single universal notification clock for all Malaysian incidents from the Act text alone; follow the current Commissioner guideline/circular and obtain legal advice for each incident.

### P1 — Data minimisation, accuracy, and retention

1. The current default record/audit retention is 2,555 days (approximately seven years); security-token retention is 30 days. These are application defaults, not proof that seven years is necessary or legally required for every record.
2. Approve a record-class retention schedule with a documented purpose/legal basis and owner for each data category.
3. Add deletion/anonymisation coverage for every personal-data store, attachments, generated PDFs, exports, temporary files, logs, backups, and downstream systems.
4. Define backup expiry and deletion behaviour, including how restored backups are brought back under current deletion/retention rules.
5. Test that retention jobs run, report failures, and do not silently leave orphaned files or personal data.

### P1 — Encryption and key management

- The SQLite database itself is not encrypted by the application code. Some fields are encrypted through EF Core converters; this is not whole-database encryption.
- Backup files are protected by ASP.NET Core Data Protection, but the Data Protection key ring is persisted to a local filesystem path without an explicit at-rest key-encryption provider in the reviewed startup code.
- Require host-volume encryption, restrictive OS ACLs for database/uploads/logs/key rings, protected key backups, key rotation/recovery procedures, and tested restore access.
- For production, use an approved key-protection mechanism (for example, platform-managed key storage or a certificate/managed key service) and document key custodians and recovery procedures.

### P1 — Processor and cross-border governance

Create and maintain an approved vendor/sub-processor register covering hosting, email/SMTP, backups, monitoring, support, and any integrations. Record data locations, support access locations, transfer mechanism/assessment, security terms, retention/deletion commitments, and breach escalation obligations. Do not claim that data stays in Malaysia until hosting, backups, telemetry, and support locations have been verified.

### P1 — Application hardening

1. Reduce CSP use of `unsafe-eval` and `unsafe-inline`; move toward nonce/hash-based scripts after inventorying inline scripts and testing every view.
2. Remove unused hard-coded private HTTP origins from CSP after verifying no supported deployment depends on them.
3. Validate file signatures/content for PDF/image uploads, not just client-supplied extension and MIME type; consider safe re-encoding for images and attachment disposition for sensitive documents.
4. Complete a route-by-route authorization matrix and negative tests for cross-company access, exports, downloads, APIs, and signature/token endpoints.
5. Review request/audit/application logs for personal data, tokens, query strings, exception content, and retention.
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
- Malaysia PDP Commissioner — [DPO appointment circular and guidelines](https://www.pdp.gov.my/en/akta/personal-data-protection-guidelines-on-the-appointment-of-data-protection-officer-dpo/) and [Data Breach Notification guidelines](https://www.pdp.gov.my/en/akta/personal-data-protection-guidelines-on-data-breach-notification-dbn/).
- European Union — [Regulation (EU) 2016/679 (GDPR), official EUR-Lex text](https://eur-lex.europa.eu/eli/reg/2016/679/oj).
- NIST — [The NIST Cybersecurity Framework (CSF) 2.0, NIST CSWP 29 (2024), DOI: 10.6028/NIST.CSWP.29](https://doi.org/10.6028/NIST.CSWP.29).
