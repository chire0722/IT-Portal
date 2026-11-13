# Authorization Negative-Test Matrix

**Repository:** `chire0722/IT-Portal`  
**Assessment branch:** `ci-cd/hardening`  
**Status:** Test specification. This is not evidence that every scenario has already been executed.

## 1. Test identities and fixtures

Create isolated test identities and records in two companies:
- Company A and Company B;
- HR user scoped to Company A;
- IT user scoped to Company A;
- IT user scoped to Company B;
- unscoped/global IT user;
- Super Admin;
- employee in Company A;
- employee in Company B;
- HOD/Manager assigned only to Company A;
- anonymous client.

Create matching records in both companies: employee, onboarding request, offboarding request, attachment, software license, license key, asset allocation, export row, signature record and signed URL. Keep test data synthetic.

The test must distinguish explicitly global roles from scoped roles. Never infer that an IT role alone grants global access.

## 2. Expected response policy

Use the application's approved disclosure policy consistently:
- 404 when revealing object existence would create an IDOR/discovery oracle;
- 403 where the endpoint intentionally communicates authenticated-but-forbidden access;
- 401 or redirect for unauthenticated access, according to endpoint type;
- no response may contain protected bytes, partial exports, secret fields, internal paths, stack traces or another company's metadata.

Tests should assert both status and absence of sensitive content.

## 3. Negative-test matrix

| Surface | Actor / attack | Expected security property |
|---|---|---|
| Employee details | Company A HR/IT requests Company B employee ID | Denied; no employee fields or relationship metadata |
| Employee directory/search | Company A user manipulates company, department, branch or employee filter | Query remains scoped to Company A; no cross-company counts or autocomplete |
| Onboarding details | Company A user substitutes Company B request ID | Denied at controller and handler/query layer |
| Offboarding details | Company A user substitutes Company B request ID | Denied; attachment and signature metadata not disclosed |
| Approval mutation | Unassigned HOD/Manager submits approve/reject for Company B request | Denied before state mutation; audit records failure without sensitive payload |
| API delete | Employee, anonymous or unrelated-company user calls delete endpoint | Denied at controller and command handler; record remains unchanged |
| Token regeneration | Non-HR or out-of-scope HR requests token regeneration | Denied; no new token created or emailed |
| Bulk export | Scoped HR submits mixed Company A + Company B identifiers | Reject the entire request; never return a partial unauthorized selection |
| Bulk export | User manipulates company IDs in request body/query | Server derives scope from authenticated principal; client company IDs are not trusted |
| Sensitive file download | Company A user requests Company B file path/identifier | Denied before file bytes are opened or streamed |
| Sensitive file download | Anonymous user requests a `/secure-files` route | Denied; no static-file fallback |
| Legacy upload URL | User requests old `/uploads/resignations` or `/uploads/offboarding` path | 404; no file bytes or redirect to a public location |
| Filename/path | URL-encoded traversal, backslash, repeated separators or encoded dot segments | Cannot escape secure category root |
| Signed acknowledgement | Token from another request/employee is substituted | HMAC/purpose/request binding fails; no acknowledgement mutation |
| Signed PDF | Expired, malformed, altered or revoked token | Denied; no PDF generated or cached |
| Signed PDF | Valid token for Company A used to request Company B document | Denied; token scope is bound to the correct record |
| License list/read | Scoped IT from Company A enumerates Company B license IDs | No record, key count, metadata or decrypted key disclosure |
| License key read/decrypt | Company A scoped IT requests Company B key ID | Denied before decrypting or returning the key |
| License key create/update/delete | Request body changes company/license ownership fields | Server validates ownership and scope; no cross-company mutation |
| License import | Scoped IT imports keys into a license outside assigned companies | Reject; no partial key insert |
| License assignment/seat allocation | Scoped IT assigns Company B license to Company A employee | Denied; database remains unchanged |
| Global access | Scoped IT is accidentally treated as unscoped due empty/missing company mapping | Fail closed; only explicit global role/assignment can access all companies |
| CSRF | Cookie-authenticated mutation omits or forges anti-forgery token | Rejected; no mutation |
| Rate limiting | Repeated token, OTP, login or export requests | Throttled with no bypass through alternate route or forwarded IP spoofing |

## 4. Test method

For every endpoint:
1. Authenticate as the least-privileged actor.
2. Create a legitimate Company A request and confirm the normal operation works.
3. Substitute Company B object IDs in route, query, body and nested object references.
4. Repeat with a valid Company A ID but a Company B relationship field.
5. Attempt direct API calls, alternate HTTP methods and bulk/multi-select forms.
6. Capture response status, body length, returned headers, database before/after state and audit event.
7. Confirm the request does not create side effects, send email, generate a file, mutate status or expose timing/existence details.
8. Repeat with anonymous, inactive, expired-session and revoked-token actors.
9. Confirm explicitly global roles still perform intended global tasks; avoid over-restricting legitimate administrators.

## 5. Evidence required for each test

Record:
- test ID and scenario;
- commit SHA and environment;
- actor role and company scope;
- endpoint, method and redacted request;
- expected and actual status;
- response content assertion;
- database/file/email side-effect assertion;
- audit event and correlation ID;
- tester, date, result and linked defect;
- retest commit and result.

Do not place live bearer tokens, employee identifiers or personal-data payloads in test reports.

## 6. Remediation already added to the assessment branch

- `OnboardingApiController.Delete` now requires the `RequireHROrIT` policy.
- `OnboardingRequestAuthorizationHelper.CanDelete` independently checks authentication, HR/IT role, company scope and the request's permitted deletion state. The command handler calls this helper, so direct MediatR invocation does not bypass the policy.
- `SecureFilesController.Download` authorizes against the owning offboarding record before checking physical file existence. Out-of-scope/missing records use the same not-found response, and successful responses set no-store/no-referrer/nosniff headers.
- `DownloadOffboardingResignationQueryHandler` returns not-found for out-of-scope records; the controller adds no-store/no-referrer/nosniff headers.
- Bulk offboarding PDF export now requires HR in the handler, rejects invalid IDs and mixed/out-of-scope selections rather than exporting a partial subset, caps selection at 100 requests and total generated PDF bytes at 100 MiB, and fails the entire export if a PDF cannot be generated.
- Offboarding CSV export now rechecks HR role in the handler and uses `CsvExportSanitizer` to neutralize spreadsheet formula-like cell values.
- New authorization helper and CSV sanitizer regression tests have been added. Their CI execution is still pending.

## 7. Release gate

The authorization gate is not closed by unit tests alone. Require route-by-route regression tests, API integration tests and authenticated browser testing in staging. A failed cross-company test is a release blocker. Keep a named reviewer independent of the code author for high-impact authorization changes.
