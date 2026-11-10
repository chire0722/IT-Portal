# Privacy Operations Runbook — Data Subject Requests

**Repository:** `chire0722/IT-Portal`  
**Assessment branch:** `ci-cd/hardening`  
**Status:** Operational procedure pending formal controller/DPO approval. The application does not yet evidence a complete case-managed DSAR workflow.

## 1. Purpose and ownership

This runbook defines a controlled interim process for access, correction, data-portability and deletion/erasure-related requests. It is not a legal opinion and does not create rights or deadlines beyond applicable law.

The organisation acting as data controller must appoint:
- a privacy/DPO owner (where appointment is legally required);
- an HR/business data owner for employment records;
- an IT system owner for extraction and technical deletion;
- a legal/records owner for statutory retention, litigation hold and refusal decisions;
- an approver independent of the person executing a high-impact export or deletion.

The generic IT mailbox must not be represented as the DPO unless it is formally designated.

## 2. Intake and case registration

Accept requests through the privacy contact published in the approved privacy notice. Create a case with:
- unique request ID and received timestamp (including time zone);
- request type and requested outcome;
- requestor identity and verification method/status;
- data subject and relevant organisation/company scope;
- systems and processors likely to hold data;
- applicable jurisdiction and legal regime decision;
- statutory/target due date and extension reason, if applicable;
- assigned owner, reviewer and approver;
- communications, decisions, export hash and completion evidence.

Do not put identity documents, full personal-data exports, passwords, signatures or unnecessary sensitive data into ordinary email or general audit logs.

## 3. Identity and authority verification

1. Verify the requester using a proportionate method already associated with the account or employment relationship.
2. For an agent/representative, verify authority and the data subject's consent/mandate where required.
3. Do not disclose whether a record exists until identity and authority checks are sufficient.
4. Request only the minimum additional evidence necessary. Store verification evidence separately with restricted access and a defined retention period.
5. Record failed verification and close or pause the case using an approved response template.

## 4. Scope discovery

Search all relevant stores, not only the primary SQLite database:
- Identity and account records;
- onboarding/offboarding requests, comments, approvals and workflow history;
- signatures, signature metadata and generated PDFs;
- attachments and private uploads;
- software/asset allocations and related records;
- audit logs, security logs and exports;
- temporary files, email/outbox records and notifications;
- backup sets and restore locations;
- processors, hosting, email, monitoring and support systems.

Use a data inventory and record-of-processing map. Record systems searched, search keys, time range, exclusions and any inaccessible processor/system.

## 5. Access and correction requests

### Malaysia PDPA
Act 709 sections 30–32 govern data access requests and related exceptions. Section 31 generally requires compliance within 21 days of receipt. If the controller cannot comply within that period, it must notify the requester in writing before the period expires, explain why, comply to the extent possible, and complete the request within the additional period stated in section 31. The privacy/legal owner must confirm the current consolidated text and any applicable exceptions before communicating a final deadline.

Correction requests must be assessed under the Act's applicable correction provisions. Preserve an audit trail of the prior value, corrected value, approver, reason and effective time without unnecessarily duplicating sensitive values in logs.

### GDPR, where Article 3 applies
Under Article 12(3), respond without undue delay and normally within one month. The period may be extended by up to two further months where necessary because of complexity or number of requests; notify the requester within the initial month and explain the reason. Apply the other Article 12 rules, including identity checks, transparent communication and reasons/complaint rights where refusing.

## 6. Data portability

For Malaysia, assess requests under section 43A of Act 709, subject to technical feasibility and format compatibility. Do not promise portability where the statutory conditions are not met.

Where GDPR Article 20 applies, assess its scope and conditions, including data provided by the data subject, processing based on consent or contract, and processing by automated means.

For an approved export:
- export only the requester's data within the verified scope;
- exclude other employees' personal data, privileged/security data and legally restricted information unless disclosure is authorised;
- use a documented, machine-readable format where appropriate (for example CSV/JSON plus a manifest);
- include a data dictionary and export timestamp;
- calculate a cryptographic hash for integrity;
- encrypt the export and deliver through an authenticated, expiring channel;
- record recipient, delivery time, expiry and deletion of the temporary export.

Never use a public or guessable download URL.

## 7. Deletion and erasure assessment

Do not treat every deletion request as an unconditional instruction to delete employment, accounting, security or audit records.

Before deletion:
1. Identify the exact data subject and all linked records.
2. Determine the applicable law and whether an erasure right exists (for example GDPR Article 17 only where GDPR applies).
3. Check statutory retention, employment, tax, legal claims, investigation and litigation-hold requirements.
4. Identify controller/processor copies, attachments, generated documents, logs, exports, temporary files and backup copies.
5. Obtain approval from the privacy/legal owner and data owner.
6. Prefer deletion or irreversible anonymisation where lawful and technically safe; document any data retained, purpose, legal basis and review date.
7. Propagate deletion instructions to processors where required by contract/law.
8. Record completion evidence and any exceptions.

Malaysia PDPA section 10 requires personal data not to be retained longer than necessary and reasonable steps to destroy or permanently delete data when no longer required. This is not the same as assuming a universal GDPR-style erasure right.

Backups require a documented expiry/restore process. If a backup is restored after deletion, the current deletion/retention rules must be re-applied before normal access is restored.

## 8. Approval and audit

Use separation of duties:
- requester intake and identity verification;
- technical data discovery/export/deletion;
- independent privacy/legal approval;
- final communication and case closure.

Record decisions, legal exceptions, partial fulfilment, processor responses, data categories disclosed/removed, and completion timestamps. Audit records must not themselves reproduce the full sensitive data being processed.

## 9. Current application gap and implementation backlog

The reviewed source tree does not evidence a full DSAR case-management feature. Until implemented, this procedure must be run using an access-controlled case register and approved secure export tooling.

Required application capabilities:
- DSAR case entity with status, due date, owner, reviewer, jurisdiction and decision;
- immutable event/audit history;
- role-restricted case access;
- identity verification workflow;
- data inventory-driven search/export;
- encrypted export staging and expiring delivery;
- deletion approval, legal-hold and processor propagation;
- retryable deletion jobs and failure reporting;
- backup restore reconciliation;
- metrics for overdue, paused, rejected and completed requests.

Do not store DSAR case attachments or exports in ordinary public uploads.

## 10. Primary references

- Malaysia Personal Data Protection Act 2010 [Act 709], including sections 10, 30–32 and 43A, and Personal Data Protection (Amendment) Act 2024 [Act A1727]. Official source: https://www.pdp.gov.my/en/akta/pdp-act-2010-en/
- Regulation (EU) 2016/679 (GDPR), Articles 3, 12, 15–22 and 20: https://eur-lex.europa.eu/eli/reg/2016/679/oj
