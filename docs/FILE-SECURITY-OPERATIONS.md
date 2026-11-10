# File Security Operations and Release Gates

**Repository:** `chire0722/IT-Portal`  
**Assessment branch:** `ci-cd/hardening`  
**Status:** Engineering control plan. This document does not claim that malware scanning is currently deployed.

## 1. Current source evidence

| Control | Evidence reviewed | Status |
|---|---|---|
| Sensitive storage outside web root | `Helpers/SensitiveFileStorage.cs` stores resignation/offboarding files under `App_Data/SecureUploads` | Implemented in source; production ACLs still unverified |
| Public legacy path blocked | `Program.cs` returns 404 for legacy sensitive upload URLs | Implemented in source |
| Filename collision protection | Secure storage uses create-new semantics; final move does not overwrite | Implemented in source |
| Partial upload cleanup | `SaveAsync` now writes to a unique temporary file, cleans it on failure/cancellation, and publishes via same-volume move | Implemented in source; new regression tests added, CI result pending |
| Excel parsing | PR #3 description records OOXML structure, required-entry, duplicate-entry, macro, ZIP-entry and size checks | Implemented in reviewed PR scope; verify each import call site |
| Other upload validation | PR #3 description records extension, MIME and magic-byte checks for PDF/image/video uploads | Implemented in reviewed PR scope; call-site coverage still needs proof |
| Malware scanning | ClamAV INSTREAM scanner is implemented and invoked before offboarding resignation-letter requests are persisted; scanner errors and missing configuration fail closed | **Partially implemented** — company logos, spreadsheet imports and other upload surfaces are not yet scanned; production daemon configuration/health still require verification |
| Parser isolation | No isolated parser worker/container was evidenced | **Not verified** |

## 2. Required upload lifecycle

1. Authenticate and authorize the uploader before accepting the upload.
2. Enforce request-body and per-file size limits before buffering or parsing.
3. Validate the allow-listed extension, declared MIME type, detected file signature, and expected format.
4. Store the upload in a quarantine directory outside the web root with restrictive ACLs and a generated storage name.
5. Scan the quarantined bytes using an approved malware-scanning service.
6. Treat scanner timeout, service outage, malformed response, or unknown verdict as **not clean**. Do not publish or serve the file.
7. Parse only after the file is classified clean. Apply parser-specific limits and resource budgets.
8. Move the file to its final private location atomically; store only an opaque internal reference.
9. Record uploader, tenant/company, file hash, detected type, scanner/version, verdict, timestamps and final outcome. Do not log file contents or sensitive filenames unnecessarily.
10. On rejection, timeout, cancellation or parser failure, delete the quarantine/temp object and record cleanup failure for retry.

A file must not become downloadable merely because extension/MIME/magic-byte validation passed. Those checks identify format inconsistencies; they do not establish that the content is benign.

## 3. Malware scanner integration

The application now provides `IFileMalwareScanner` backed by `ClamAvMalwareScanner`. The offboarding resignation-letter workflow scans the uploaded stream before persisting the request, and company create/update workflows scan logo streams before writing them to the public web root. The integration uses ClamAV's NUL-terminated `zINSTREAM` protocol and maps `OK` to clean, `FOUND` to infected, and connection/configuration/timeout/unknown responses to unavailable. Unavailable is rejected, not allowed.

Configuration:
- `IT_FORM__MalwareScanning__ClamAvHost`
- `IT_FORM__MalwareScanning__ClamAvPort` (default 3310)
- `IT_FORM__MalwareScanning__TimeoutSeconds` (default 30; bounded 1–120)

The ClamAV daemon must be deployed on loopback or an isolated private network with firewall rules permitting only the application host. Do not expose clamd's unauthenticated TCP port publicly. Monitor signature freshness and daemon availability.

**Coverage remains partial:** integrate the same scanner into company logo uploads, spreadsheet imports and every other upload/attachment entry point before claiming system-wide malware scanning. Do not silently bypass scanning for a scanner outage.



The deployment owner must select and operate an approved scanner integration (for example, an internally managed ClamAV service or an enterprise ICAP-compatible service). Do not hard-code a scanner hostname or secret in the repository.

Before implementation, document:
- scanner product/version and update cadence;
- service endpoint and authentication method held in the secret store;
- maximum scan size and timeout;
- treatment of encrypted/password-protected archives;
- treatment of unsupported file types and scanner errors;
- quarantine retention and secure deletion;
- availability, alerting, and ownership;
- data-processing and cross-border implications if files leave the hosting environment.

**Fail closed:** an unavailable scanner must never silently turn into an allow decision for sensitive uploads. Provide a clear retry message to the user and an operational alert to IT.

## 4. Parser and archive safety

- Keep allow-lists narrow and endpoint-specific.
- Do not trust user-supplied filenames, MIME headers, archive paths or OOXML relationship targets.
- Reject absolute paths, traversal segments, duplicate normalized archive paths, unexpected package entries and excessive compression ratios.
- Bound archive entry count, expanded size, nesting depth, worksheet dimensions, row/column counts, image dimensions, media duration and parser time.
- Disable macros and external relationships unless a documented business requirement exists.
- Parse in a low-privilege worker with no outbound network access where practical.
- Do not render active content (HTML/SVG) inline. Force downloads for sensitive PDFs and other user-supplied documents unless a safe preview pipeline is proven.
- Re-encode raster images using a maintained library before previewing; strip metadata where it is not required.
- Keep parser exceptions and local filesystem paths out of client responses.

## 5. Required regression and security tests

| Test | Expected result |
|---|---|
| Interrupted upload after partial bytes | Final file absent; temporary file removed |
| Request cancellation during copy | Final file absent; temporary file removed or cleanup failure recorded |
| Existing final filename | Existing file unchanged; new upload rejected or safely assigned a generated name |
| Windows and Unix traversal separators | Stored name remains a single safe filename; no write outside category root |
| MIME/extension/signature mismatch | Rejected |
| Empty, truncated, malformed PDF/OOXML/image | Rejected without parser details in response |
| ZIP bomb, too many entries, duplicate entries, nested archive | Rejected within bounded resource use |
| Macro-enabled or external-link workbook | Rejected unless explicitly approved and safely handled |
| EICAR test file in isolated scanner test environment | Scanner verdict is detected; file never published |
| Scanner timeout/unavailable/unknown verdict | Fail closed; no download |
| Cross-company file identifier | 404/403 according to disclosure policy; no bytes returned |
| Unauthorized signed/download URL | Denied; no-store/no-referrer headers retained |
| Temporary/quarantine cleanup failure | Alert and durable retry record; no silent success |

Never test malware detection using real malware on production systems. Use the EICAR test file only in a controlled test environment.

## 6. Export content safety

CSV exports are an active-content boundary when opened in spreadsheet software. Offboarding CSV fields now pass through `CsvExportSanitizer.EscapeCell`, which prefixes spreadsheet formula-like values (including values beginning with `=`, `+`, `-`, `@` or leading control characters) and escapes embedded double quotes. Regression tests cover formula-like values and ordinary text.

Apply the same sanitizer to every CSV export path. CSV quoting alone is not sufficient to prevent formula injection. Do not assume that a CSV file is passive content simply because it is generated by the server.

## 7. Operational cleanup

The current write path attempts immediate deletion of a failed temporary upload. If deletion itself fails, a `.upload-*.tmp` orphan can remain. Add a scheduled cleanup process that:
- only targets the dedicated upload-temp naming pattern under the known secure root;
- ignores files younger than a conservative in-progress threshold;
- confirms no active upload owns the file;
- records deletion failures without exposing personal data;
- never deletes final stored files or quarantine evidence under legal hold.

## 8. Release gates

Do not claim end-to-end file security until scanner integration, fail-closed behavior, parser limits, quarantine isolation, ACL verification, cleanup monitoring, and negative authorization tests pass in staging. CI unit tests alone do not prove production malware scanning or filesystem isolation.
