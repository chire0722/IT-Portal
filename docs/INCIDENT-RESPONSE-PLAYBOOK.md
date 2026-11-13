# Security Incident and Personal Data Breach Response Playbook

**Repository:** `chire0722/IT-Portal`  
**Assessment branch:** `ci-cd/hardening`  
**Status:** Operational playbook template requiring controller, DPO/privacy, legal and IT approval. The application does not yet evidence a complete incident case-management workflow.

## 1. Activation

Activate this playbook for suspected or confirmed events affecting confidentiality, integrity or availability, including:
- unauthorised access to employee or business data;
- cross-company data exposure;
- lost or stolen devices/media;
- misdirected email or exports;
- exposed credentials, HMAC keys or Data Protection keys;
- ransomware, exfiltration, destructive changes or backup loss;
- public exposure of private uploads or signed documents;
- processor or supplier incidents.

A security incident may or may not be a notifiable personal data breach. Record and assess every suspected incident; do not wait for legal classification before containment.

## 2. Roles

| Role | Responsibility |
|---|---|
| Incident Commander | Coordinates response, severity, resources and timeline |
| IT/Security Lead | Containment, investigation, evidence preservation, eradication and recovery |
| Privacy/DPO Owner | Personal-data assessment, controller advice and regulator/data-subject coordination |
| Legal/Compliance Owner | Applicable-law assessment, privilege, contractual and regulator obligations |
| Business/Data Owner | Data impact, affected persons, operational decisions |
| Communications Owner | Approved internal, customer, regulator and public communications |
| Supplier/Processor Contact | Promptly escalates incidents and supplies evidence and impact details |

Record named primary and backup contacts, 24/7 escalation method, decision authority and alternates before production approval.

## 3. First response

1. Open an incident record immediately; record who reported it and the earliest known occurrence/awareness time.
2. Assign an incident ID, commander, severity and response channel.
3. Preserve evidence: relevant logs, cloud audit events, database snapshots, file hashes, access records, email headers and system clocks. Record chain of custody.
4. Contain proportionately: isolate affected hosts, disable compromised accounts/tokens, revoke sessions/keys, block malicious routes, stop affected processing or sharing, and protect unaffected backups.
5. Do not destroy or overwrite evidence during containment. Coordinate emergency changes and document every action.
6. Determine whether the incident is ongoing, what data/system is affected, likely attack path, and whether the attacker retains access.
7. Engage external forensic, legal or privacy support where internal capability is insufficient.

Avoid copying sensitive records into tickets, chat or general-purpose email. Use the restricted incident evidence repository.

## 4. Personal data breach assessment — Malaysia PDPA

The controller must assess whether a personal data breach occurred and whether notification criteria are met under Act 709 section 12B and the Commissioner's current DBN Guideline/Circular.

The Commissioner's DBN Guideline (Version 1.0, 25 February 2025) describes significant harm criteria including:
- likely physical harm, financial loss, adverse credit impact or property loss;
- potential misuse for illegal purposes;
- sensitive personal data;
- combinations of personal data/information that could enable identity fraud;
- significant scale.

The Guideline defines significant scale for Commissioner notification as more than 1,000 affected data subjects. The significant-scale criterion does not apply when assessing whether affected individuals themselves must be notified.

**Commissioner notification:** where the Guideline's criteria are met, notify as soon as practicable and no later than 72 hours. Record the earliest awareness/confirmation timeline carefully; the Guideline's examples explain when the clock begins for different scenarios. If all details are not available, submit the initial notification within the required period and provide additional information in phases as permitted by the Guideline.

**Affected individuals:** where significant harm results or is likely, notify without unnecessary delay and no later than 7 days after the initial notification to the Commissioner. Communicate directly and individually where practicable, in clear language, with the nature of the breach, likely consequences, mitigation, recommended protective steps and a contact point.

**Processor incident:** the DBN Guideline states that section 12B's mandatory notification obligation applies to the data controller, not directly to the processor. Contracts must require processors to notify the controller promptly and provide evidence and assistance. The response team must start assessment as soon as the controller receives the processor's notice or has clear evidence, whichever is earlier under the Guideline.

The legal/privacy owner must confirm the current Act, circulars, sectoral rules and facts before making the final notification decision.

## 5. GDPR assessment, where applicable

First determine territorial scope under GDPR Article 3. If GDPR applies:
- controller notifies the competent supervisory authority without undue delay and, where feasible, within 72 hours after becoming aware, unless the breach is unlikely to result in a risk to individuals' rights and freedoms (Article 33);
- processor notifies controller without undue delay (Article 33(2));
- communicate to affected individuals without undue delay where the breach is likely to result in a high risk, subject to Article 34 exceptions;
- document all personal-data breaches, including facts, effects and remedial action (Article 33(5)).

Do not assume GDPR applies solely because the application is hosted online or because users may travel.

## 6. Required incident record

Maintain a restricted incident register with:
- incident ID, report time, discovery time, occurrence window and controller-awareness time;
- reporter, incident commander, owners and decision makers;
- affected systems, environments, processors and locations;
- data categories, sensitivity, approximate records and affected-person counts;
- confidentiality/integrity/availability impact;
- attack vector, root cause, timeline and whether compromise is ongoing;
- containment, evidence, eradication and recovery actions;
- harm assessment and rationale for notification/no-notification;
- regulator and affected-person notification dates, content and delivery evidence;
- processor communications and contractual escalation;
- remediation owner, due date, risk acceptance and lessons learned.

The Malaysian DBN Guideline requires a breach register to be maintained for at least two years and includes incidents that did not meet notification criteria. Retain longer where other law, investigation, litigation hold or approved records schedules require it.

## 7. Recovery and closure

- Rebuild or restore from known-good sources; do not restore a compromised backup blindly.
- Validate Data Protection key availability before restoring protected backups.
- Rotate credentials and keys according to a documented dependency and migration plan; preserve required historical data access without reactivating compromised keys.
- Patch the root cause and verify the attack path is closed.
- Monitor for persistence, repeat access and data misuse.
- Obtain business, IT and privacy sign-off before resuming affected processing.
- Conduct a post-incident review with timeline, control failures, decisions, lessons and tracked actions.

## 8. Exercises

At least annually, and after material architecture changes, conduct tabletop exercises for:
1. cross-company employee data exposure;
2. compromised HMAC/Data Protection key;
3. ransomware with database and backup access;
4. accidental export/email to the wrong recipient;
5. processor-reported breach.

Exercise the 72-hour and 7-day Malaysia notification timelines, decision ownership, evidence capture, supplier escalation and backup recovery. Record attendance, scenario, decisions, timing gaps and corrective actions.

## 9. Release gates

Before production approval:
- name incident commander, DPO/privacy owner, legal owner and backups;
- approve on-call and supplier escalation contacts;
- approve regulator/data-subject notification decision templates;
- establish the restricted incident register and evidence store;
- test alerting and evidence preservation;
- run and document a tabletop exercise;
- confirm contractual processor notification obligations.

A source-code audit cannot prove that the organisation is staffed, trained or legally ready to respond.

## 10. Primary references

- Personal Data Protection Commissioner Malaysia, *Personal Data Protection Guideline: Data Breach Notification*, Version 1.0, 25 February 2025: https://www.pdp.gov.my/ppdpv1/wp-content/uploads/2025/08/GP_DBN_ENG.pdf
- Personal Data Protection Act 2010 [Act 709], section 12B, as amended by Act A1727: https://www.pdp.gov.my/en/akta/pdp-act-2010-en/
- Regulation (EU) 2016/679 (GDPR), Articles 3 and 33–34: https://eur-lex.europa.eu/eli/reg/2016/679/oj
