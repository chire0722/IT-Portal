# File security route inventory

Verified from the `ci-cd/hardening` source tree on 2026-11-10. This is the coding inventory for binary ingress/egress; production ClamAV availability and operator EICAR proof remain external gates.

## Upload and import ingress

| Path/action | Input | Validation and malware gate | Storage/processing | Negative authorization coverage |
|---|---|---|---|---|
| `CompanyController.Create` / `CompanyController.Edit` | Company logo | `CreateCompanyCommandValidator` / `UpdateCompanyCommandValidator`, signature/content-type checks, `MalwareScanHelper`, deep ImageSharp decode and re-encode | Managed `wwwroot/uploads/companies/logos`, unique name, atomic move, old logo cleanup | Company write handlers enforce current company scope; controller authorization tests remain required for every role permutation |
| `AccountController.Profile` → `UpdateProfile` | Email signature image | Validator, signature/content-type checks, `MalwareScanHelper`, deep ImageSharp decode and re-encode | Data URI stored in the user record; stripped metadata and canonical PNG/JPEG bytes | Self-service profile authorization is bound to the authenticated user; cross-user command tests remain required |
| `OffboardingController.Create` | Resignation letter | Extension/signature/size validation and mandatory malware scan | `SensitiveFileStorage` under `App_Data/SecureUploads/resignations`; safe filename, atomic write, cleanup on delete | `SecureFilesController.Download` checks workflow ownership and company scope |
| `OffboardingController.Step1` | Hardware-damage proof and hardware-return proof | Size, extension, deep signature check, mandatory malware scan; raster images are decoded and re-encoded | `SensitiveFileStorage` under `offboarding`; non-raster PDF/video inputs remain signature-checked originals | Request-level authorization is checked before storage; secure download negative tests cover owner/role/company boundaries |
| `DepartmentController.ImportExcel` | Department workbook | OOXML ZIP validation, size limit, malware scan | Parsed in memory; no uploaded workbook is persisted | Endpoint requires the existing privileged policy and company-scoped write path |
| `EmailContactController.BulkImport` | Email contact workbook | OOXML ZIP validation, size limit, malware scan | Parsed in memory | Privileged endpoint and company-scoped handler |
| `EmailGroupController.ImportExcel` | Email group workbook | OOXML ZIP validation, size limit, malware scan | Parsed in memory | Privileged endpoint and company-scoped handler |
| `ServerFolderController.BulkImport` | Server-folder workbook | OOXML ZIP validation, 5 MB limit, malware scan | Parsed in memory | Privileged endpoint and company-scoped handler |
| `SharedMailboxController.ImportExcel` | Shared-mailbox workbook | OOXML ZIP validation, size limit, malware scan | Parsed in memory | Privileged endpoint and company-scoped handler |
| `StandardSoftwareController.ImportExcel` | Standard-software workbook | OOXML ZIP validation, size limit, malware scan | Parsed in memory | Privileged endpoint and company-scoped handler |
| `EmployeeApiController.Import` | Employee workbook | OOXML ZIP validation, size limit, malware scan, request cancellation | Parsed in memory | API authorization and company-scope negative tests remain part of the release matrix |
| `ITAssetController.BulkImport` | IT asset workbook | OOXML ZIP validation, size limit, malware scan, request cancellation | Parsed in memory | API authorization and company-scope negative tests remain part of the release matrix |

## Download, preview, export, and signed-link egress

| Path/action | Protection verified in source | Remaining evidence |
|---|---|---|
| `/secure-files/{category}/{fileName}` | Authenticated request; category allow-list; exact workflow record match; company scope; owner/HOD/HR/IT check; `NotFound` for out-of-scope records; no-store and `nosniff` | Execute the full cross-company/cross-role HTTP matrix against a running environment |
| `OnboardingController` bulk PDF export | Query company scope; mixed-scope selection fails closed; maximum 100 records/100 MB; signature integrity checks; safe ZIP names | Execute HTTP matrix for HR, IT, HOD, creator and unrelated company |
| `OffboardingController` bulk PDF export | HR role; company scope; mixed-scope selection fails closed; maximum 100 records/100 MB; signature integrity checks; safe ZIP names | Execute HTTP matrix for HR, IT, HOD, creator and unrelated company |
| `AuditLogController` export | Existing protected audit-log query and authorization path | Execute cross-company and cross-role HTTP matrix |
| `ITAssetController` CSV export | Existing controller authorization and company-scoped query | Execute cross-company and cross-role HTTP matrix |
| `SignatureController` PDF / signed acknowledgement | Token and request workflow authorization | Execute invalid, expired, replayed, cross-company and cross-role token matrix |
| `VerificationController` device ZIP | Token-bound verification flow | Execute invalid, expired, replayed and cross-tenant token matrix |
| Notification PDF/email preview | Confirmation integrity token binds request/type/actor/body; recipient and header validation | Execute role/company and tampered-confirmation matrix |

## Cleanup and path safety

Sensitive file writes use a private managed root, `Path.GetFileName`, category allow-lists, temporary files, atomic moves, and post-commit deletion of replaced records. Secure-file downloads never accept arbitrary filesystem paths. Retention cleanup currently covers terminal email-outbox rows and managed sensitive files referenced by deleted offboarding records; backup/log retention and production storage lifecycle still require operational policy and evidence.

## Required release-gate tests still external to source-only verification

The repository contains focused negative tests for the implemented scope guards and signing/export limits. A complete HTTP matrix must still run with two companies and at least HR, IT, HOD, creator, unrelated authenticated user, and anonymous requests. It must cover every endpoint above, every attachment, every export, every signed link, and both `404`/`403` behavior where applicable. No test or report should claim that matrix is complete until that run is recorded.

