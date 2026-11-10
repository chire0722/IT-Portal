# NIST Cybersecurity Framework 2.0 — IT-Portal Control and Evidence Map

**Repository:** `chire0722/IT-Portal`  
**Assessment branch:** `ci-cd/hardening`  
**Framework:** NIST CSF 2.0, NIST CSWP 29, 26 February 2024, DOI: 10.6028/NIST.CSWP.29  
**Status:** Preliminary engineering mapping. Not a NIST assessment, certification or conformity claim.

## 1. Assessment method

This map distinguishes:
- **Implemented in source:** a control mechanism is visible in the reviewed code or workflow.
- **Partially evidenced:** code exists but coverage, effectiveness or operational configuration is not proven.
- **Not evidenced:** no sufficient implementation evidence was identified in this review.
- **Organisation-owned:** requires a named owner, policy, supplier, legal decision, deployment evidence or exercise.

A source-code feature does not automatically satisfy a CSF outcome. Evidence must be current, scoped, repeatable and linked to an accountable owner.

## 2. Govern (GV)

| CSF 2.0 outcome | IT-Portal evidence / gap | Required evidence and owner |
|---|---|---|
| GV.OC — Organisational Context | Privacy readiness document identifies Malaysian PDPA, conditional GDPR applicability and system scope | Controller-approved applicability decision, business impact and stakeholder register — Controller/Legal |
| GV.RM — Risk Management Strategy | Draft release gates and residual-risk register exist | Approved risk appetite, risk owners, acceptance expiry and escalation — Management |
| GV.PO — Policy | Security configuration and privacy readiness documentation exist | Approved access, encryption, retention, incident, supplier and secure-development policies — Management |
| GV.OV — Oversight | PR and CI evidence provide engineering review points | Periodic management review, metrics, independent audit and tracked corrective actions — Management |
| GV.SC — Cybersecurity Supply Chain Risk Management | CI uses pinned actions and Dependabot; processor register remains outstanding | Supplier inventory, contracts, sub-processor list, data locations, security clauses and incident SLAs — Procurement/Legal |

## 3. Identify (ID)

| CSF 2.0 outcome | IT-Portal evidence / gap | Required evidence and owner |
|---|---|---|
| ID.AM — Asset Management | Repository and dependency inventory; system data inventory not complete | Asset/data-flow map covering database, uploads, logs, key ring, backups, email, processors and environments — IT/Data Owners |
| ID.RA — Risk Assessment | Security audit and CodeQL/secret/dependency scanning workflows exist | Threat model, tenant-boundary abuse cases, DPIA screening, risk register and periodic reassessment — Security/Privacy |
| ID.IM — Improvement | PR remediation and regression tests are tracked | Corrective-action register, retest evidence, post-incident actions and review cadence — Security/Management |

## 4. Protect (PR)

| CSF 2.0 outcome | IT-Portal evidence / gap | Required evidence and owner |
|---|---|---|
| PR.AA — Identity Management, Authentication and Access Control | ASP.NET Identity, Argon2id, lockout/rate-limit controls, role/claim checks and company-scope enforcement exist in source; onboarding deletion now checks HR/IT + company scope in controller and handler; secure downloads authorize before file-existence checks; bulk offboarding export rejects mixed-scope selections | MFA policy, full route-level authorization matrix execution, download/export/signed-link negative tests, privileged access review — IT/Security |
| PR.AT — Awareness and Training | No training evidence in repository | Role-based privacy/security training and completion records — HR/Privacy |
| PR.DS — Data Security | Marked-column Data Protection conversion; protected SQLite backups; private sensitive-upload directory; fail-closed ClamAV scans on resignation-letter and company-logo uploads | Full data classification, upload-scanner coverage for every file entry point, encryption scope, certificate-protected key ring, encrypted volumes, key custody/rotation/recovery and restore test — IT |
| PR.PS — Platform Security | Production secure-cookie and HTTP header hardening; pinned CI actions; dependency checks | Staging/production hardening evidence, patch SLA, secure proxy/TLS, host ACLs, runtime/OS compatibility — IT |
| PR.IR — Technology Infrastructure Resilience | Startup/daily protected backup and restore service exist | Off-host immutable backup, independent key recovery, RTO/RPO, monitored jobs and witnessed restore exercise — IT/Business Continuity |

## 5. Detect (DE)

| CSF 2.0 outcome | IT-Portal evidence / gap | Required evidence and owner |
|---|---|---|
| DE.CM — Continuous Monitoring | Serilog request logging, audit log, CodeQL, Gitleaks and Dependency Review exist | Centralised protected logs, alert coverage, log integrity, alert ownership, retention and tested escalation — Security/IT |
| DE.AE — Adverse Event Analysis | Application logs and audit events exist | Correlation, anomaly baselines, alert triage, incident severity and evidence retention — Security |

## 6. Respond (RS)

| CSF 2.0 outcome | IT-Portal evidence / gap | Required evidence and owner |
|---|---|---|
| RS.MA — Incident Management | Incident response playbook added; no case-managed workflow evidenced | Approved incident register, named commander, on-call process and case evidence — Security/Management |
| RS.AN — Incident Analysis | Technical logs and audit records exist | Forensic procedure, time synchronisation, chain of custody, impact and root-cause analysis — Security |
| RS.CO — Incident Response Reporting and Communication | Draft PDPA/GDPR notification process documented | Approved contact tree, processor escalation, regulator/customer templates and authority — Privacy/Legal |
| RS.MI — Incident Mitigation | Rate limits, token expiry, access controls and security fixes exist | Tested containment playbooks, credential revocation, isolation and remediation validation — IT/Security |

## 7. Recover (RC)

| CSF 2.0 outcome | IT-Portal evidence / gap | Required evidence and owner |
|---|---|---|
| RC.RP — Incident Recovery Plan Execution | Protected SQLite backup and restore code exists | Restore runbook, recovery approval, key availability, integrity validation and test record — IT |
| RC.CO — Incident Recovery Communication | No approved recovery communication evidence | Business/customer/internal communications and status approval — Business/Communications |

## 8. Priority evidence backlog

| Priority | Action | Closure evidence |
|---|---|---|
| P0 | Name controller, DPO/privacy owner, incident commander and legal approver | Signed responsibility matrix and current contact tree |
| P0 | Confirm PDPA registration, DPO criteria and GDPR territorial scope | Legal applicability decision with date and owner |
| P0 | Implement and test DSAR workflow | Case lifecycle, identity verification, export, correction, deletion approval and audit tests |
| P0 | Approve and exercise incident response | Incident register, tabletop report, 72-hour/7-day decision timeline |
| P1 | Complete encryption inventory and Data Protection key custody | Field inventory, encrypted-volume proof, protected key ring, rotation and restore test |
| P1 | Add malware scanning and quarantine | Scanner integration, fail-closed tests, EICAR test evidence and alerting |
| P1 | Complete authorization negative tests | Cross-company, download, API, export and signed-link test results |
| P1 | Verify backup resilience | Off-host/immutable copy, RTO/RPO and restore report |
| P1 | Configure branch protection and triage CodeQL alerts | Ruleset screenshot/API evidence, required checks, alert disposition |
| P2 | Establish continuous metrics and independent review | Monthly security dashboard and periodic independent assessment |

## 9. Evidence register format

For each mapped outcome, maintain:
- CSF function/category/subcategory;
- control owner and accountable approver;
- system/environment and scope;
- implementation reference (path, commit and line range);
- operational evidence (configuration, log, test, ticket or exercise);
- test date, tester, result and exceptions;
- residual risk and acceptance expiry;
- next review date.

## 10. Framework reference

National Institute of Standards and Technology, *The NIST Cybersecurity Framework (CSF) 2.0*, NIST Cybersecurity White Paper 29, 26 February 2024. DOI: https://doi.org/10.6028/NIST.CSWP.29
