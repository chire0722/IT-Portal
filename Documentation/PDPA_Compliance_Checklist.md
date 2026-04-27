# PDPA Compliance Checklist

Last reviewed: 2026-04-27

This checklist maps IT Portal controls to Malaysia PDPA obligations. It is an engineering control record, not a legal opinion.

## Official References

- Malaysia Personal Data Protection Department, "Introduction": https://www.pdp.gov.my/ppdpv1/en/introduction/
- Malaysia Personal Data Protection Department, "Principles of Personal Data Protection": https://www.pdp.gov.my/ppdpv1/en/principles-of-personal-data-protection/
- Malaysia Personal Data Protection Department, "Personal Data Protection Standard 2015": https://www.pdp.gov.my/ppdpv1/en/personal-data-protection-standard-2015/
- Malaysia Personal Data Protection Department, "Circular No. 1/2025 Data Breach Notification": https://www.pdp.gov.my/ppdpv1/en/akta/circular-of-personal-data-protection-commissioner-no-1-2025-data-breach-notification/

## Current Technical Controls

| PDPA area | IT Portal control |
| --- | --- |
| General / consent | Employee acknowledgement requires an explicit PDPA checkbox before signature submission. Consent timestamp is stored on onboarding and offboarding records. |
| Notice and choice | `/Home/Privacy` explains controller, purposes, data categories, disclosure, security, retention, access/correction, and incident contacts. |
| Disclosure | Role-based controller policies restrict access to IT, HR, HOD, manager, and authenticated users by module. |
| Security | Sensitive model fields use `[EncryptColumn]`; cookies are HTTP-only and SameSite; CSRF protection, rate limiting, signature HMAC checks, secure upload storage, and audit logging are enabled. |
| Storage / retention | `DataRetentionService` anonymises completed or rejected onboarding/offboarding records after the configured retention window, deletes related sensitive uploads, purges expired tokens, and redacts old audit payloads. |
| Data integrity | Edit flows, validation, status checks, signature integrity checks, and audit trails support accuracy and tamper detection. |
| Access / correction | Privacy notice provides a request channel. Role-based views and admin edit flows allow authorised correction of active records. |
| Breach notification readiness | Privacy notice includes an incident contact. Operational DBN assessment and regulator submission remain an organisation process. |

## Configuration

`appsettings.json` contains a `PdpaCompliance` section:

```json
{
  "PdpaCompliance": {
    "DataControllerName": "YTC / PYT Group",
    "PrivacyContactEmail": "itdept@ytcprecast.com",
    "BreachResponseEmail": "itdept@ytcprecast.com",
    "RecordRetentionDays": 2555,
    "SecurityTokenRetentionDays": 30,
    "AuditLogRetentionDays": 2555
  }
}
```

Override these values using environment variables in production where needed, for example `IT_FORM__PdpaCompliance__PrivacyContactEmail`.

## Operational Items To Confirm

- Confirm whether the organisation must register as a Data Controller with JPDP for its processing activities.
- Confirm whether a Data Protection Officer must be appointed and registered.
- Maintain a written breach-response procedure, including who decides whether a Data Breach Notification report is required.
- Review third-party processors and cross-border transfer arrangements outside this codebase.
- Review retention periods with HR/legal before changing `RecordRetentionDays` or `AuditLogRetentionDays`.
