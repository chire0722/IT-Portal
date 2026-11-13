# Database field-encryption inventory

Verified from model attributes and `ApplicationDbContext` converter registration on 2026-11-10. `EncryptColumn` uses the application Data Protection provider through EF Core value converters. This is selective field protection, not whole-database encryption.

## Fields currently protected by `[EncryptColumn]`

| Entity | Property | Evidence |
|---|---|---|
| `OnboardingRequest` | `EmployeeID` | `Models/OnboardingRequest.cs:53-54` |
| `OnboardingRequest` | `EmailAddress` | `Models/OnboardingRequest.cs:72-73` |
| `OnboardingRequest` | `ContactNumber` | `Models/OnboardingRequest.cs:76-77` |
| `OnboardingRequest` | `HODEmail` | `Models/OnboardingRequest.cs:80-81` |
| `OffboardingRequest` | `EmployeeID` | `Models/OffboardingRequest.cs:34-35` |
| `OffboardingRequest` | `EmailAddress` | `Models/OffboardingRequest.cs:50-51` |
| `OffboardingRequest` | `HODEmail` | `Models/OffboardingRequest.cs:54-55` |
| `OffboardingRequest` | `ForwardToEmail` | `Models/OffboardingRequest.cs:61-62` |
| `EmailContact` | `EmailAddress` | `Models/EmailContact.cs:26-27` |
| `SoftwareKey` | `KeyValue` | `Models/SoftwareKey.cs:22-23` |
| `DataSubjectRequest` | `SubjectIdentifier` | `Models/DataSubjectRequest.cs` |
| `DataSubjectRequest` | `Description` | `Models/DataSubjectRequest.cs` |

`ApplicationDbContext` discovers the attribute by reflection and installs converters for marked string properties. The inventory is intentionally generated from source evidence rather than inferred from database column names.

## Identified unprotected personal/business data requiring a product decision

The source model set also contains names, department/branch/designation, desk and asset details, signer names/signatures, free-text remarks, user profile fields, audit actor/entity identifiers, workflow status/timestamps, file paths, email-outbox fields, and device/verification identifiers. They are not uniformly marked with `EncryptColumn`. Some must remain queryable, indexed, or interoperable, so blanket encryption would change search, authorization, reporting, migrations, and retention behavior.

## Coding conclusion

The exact protected-field set is now documented. Adding more encrypted fields safely requires a field-by-field classification, query/index impact review, migration/backfill plan, key-rotation/re-protection plan, and tests for old/new ciphertext. Those decisions are not safely inventable from the current architecture. Production volume encryption, certificate/key custody, backup copies, and restore evidence remain external release gates.

