# Refactor To-Do

## Goal

Finish the current Clean Architecture cleanup by removing remaining MVC leakage from application code, reducing entity exposure in Razor views, and consolidating validation/query behavior.

## Priority 1

- Remove remaining `IActionResult` leakage from application handlers that still return `ProcessResult<IActionResult>`.
- Replace remaining entity-shaped Razor page models with dedicated read/edit view models or DTOs.
- Move remaining controller-side validation and error glue into validators, handlers, or shared controller helpers.
- Reduce direct `ApplicationDbContext` usage inside controllers that still perform business/data orchestration.

## Priority 2

- Convert remaining CRUD page read queries that currently build `ViewResult` in application layer into typed result/view-model flows.
- Introduce feature-local page contracts for `Department`, `EmailGroup`, `SharedMailbox`, `ServerFolder`, `StandardSoftware`, `UserManagement`, and `Signature`.
- Add focused handler tests for any newly extracted read flows and file-download flows.
- Review remaining `ModelState.AddModelError(...)` branches and keep only true UI-only feedback in controllers.

## High-Value Remaining Issues

### MVC leakage in application layer

- `Application/Features/UserManagement/UserManagementPageQueries.cs`
- `Application/Features/StandardSoftware/StandardSoftwarePageReadQueries.cs`
- `Application/Features/SharedMailbox/SharedMailboxPageReadQueries.cs`
- `Application/Features/SharedMailbox/SharedMailboxReportQueries.cs`
- `Application/Features/ServerFolder/ServerFolderPageReadQueries.cs`
- `Application/Features/ServerFolder/ServerFolderReportQueries.cs`
- `Application/Features/EmailGroup/EmailGroupPageReadQueries.cs`
- `Application/Features/Department/DepartmentPageQueries.cs`
- `Application/Features/EmployeeTransfer/GetEmployeeTransferConfirmationPage/GetEmployeeTransferConfirmationPageQueryHandler.cs`
- `Application/Features/Signature/SignatureReadQueries.cs`

### Controllers still orchestrating with `ApplicationDbContext`

- `Controllers/AccountController.cs`
- `Controllers/AuditLogController.cs`
- `Controllers/CompanyController.cs`
- `Controllers/EmailContactController.cs`
- `Controllers/EmployeeApiController.cs`
- `Controllers/EmployeeController.cs`
- `Controllers/ITAssetController.cs`
- `Controllers/MaintenanceLogController.cs`
- `Controllers/NotificationController.cs`
- `Controllers/SecureFilesController.cs`
- `Controllers/SlaSettingsController.cs`
- `Controllers/VerificationController.cs`

### Remaining entity-shaped Razor views

- `Views/Department/Create.cshtml`
- `Views/Department/Edit.cshtml`
- `Views/Company/Create.cshtml`
- `Views/Company/Edit.cshtml`
- `Views/Verification/Index.cshtml`
- `Views/EmployeeTransfer/Confirm.cshtml`
- `Views/EmployeeTransfer/Create.cshtml`
- `Views/EmployeeTransfer/Edit.cshtml`
- `Views/EmployeeTransfer/Edit2.cshtml`
- `Views/Onboarding/Delete.cshtml`
- `Views/Offboarding/Step2.cshtml`
- `Views/ITAsset/Create.cshtml`
- `Views/ITAsset/Edit.cshtml`
- `Views/ITAsset/Delete.cshtml`
- `Views/ITAsset/Details.cshtml`
- `Views/EmailGroup/Create.cshtml`
- `Views/EmailGroup/Edit.cshtml`
- `Views/SharedMailbox/Create.cshtml`
- `Views/SharedMailbox/Edit.cshtml`
- `Views/ServerFolder/Create.cshtml`
- `Views/ServerFolder/Edit.cshtml`
- `Views/StandardSoftware/Create.cshtml`
- `Views/StandardSoftware/Edit.cshtml`
- `Views/EmailContact/Create.cshtml`
- `Views/EmailContact/Edit.cshtml`
- `Views/MaintenanceLog/Create.cshtml`
- `Views/MaintenanceLog/Edit.cshtml`
- `Views/MaintenanceLog/Delete.cshtml`
- `Views/Account/Profile.cshtml`

### Remaining controller-side validation glue

- `Controllers/AccountController.cs`
- `Controllers/UserManagementController.cs`

## Recommended Execution Order

1. Finish `Signature` typed read/file flow cleanup.
2. Convert `UserManagement` page queries from `ProcessResult<IActionResult>` to typed page results.
3. Convert `Department`, `EmailGroup`, `SharedMailbox`, and `ServerFolder` CRUD read queries to typed page results.
4. Convert remaining entity-shaped transfer/onboarding/offboarding entry pages.
5. Tackle `AccountController` and `VerificationController` validation + read/write orchestration.
6. Sweep remaining direct-`DbContext` controllers and decide which should become feature handlers versus stay thin MVC endpoints.

## Definition of Done

- Application layer does not construct `ViewResult`, `JsonResult`, `FileContentResult`, or other MVC result types for normal page/query flows.
- Razor views no longer bind directly to EF entities unless the screen is explicitly legacy and documented.
- Validation errors come from FluentValidation or shared controller handling instead of repeated local glue.
- Each extracted flow has at least one handler-level regression test.
