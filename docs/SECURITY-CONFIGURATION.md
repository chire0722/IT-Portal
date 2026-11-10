# Security-sensitive configuration

The application loads environment variables with the `IT_FORM__` prefix in `Program.cs`. Double underscores map to nested configuration sections. Keep credentials, hostnames, network shares, and machine-specific paths out of tracked JSON files.

## Required production settings

Configure these in the hosting platform's secret/configuration store, then restart the application:

| Environment variable | Purpose |
|---|---|
| `IT_FORM__SignatureHmac__Key` | Base64-encoded HMAC key, at least 32 random bytes |
| `IT_FORM__InventorySync__ApiKey` | Inventory integration key, only if the legacy key endpoint is required |
| `IT_FORM__SmtpSettings__Host` | SMTP host |
| `IT_FORM__SmtpSettings__Port` | SMTP port |
| `IT_FORM__SmtpSettings__Username` | SMTP account |
| `IT_FORM__SmtpSettings__Password` | SMTP credential/app password |
| `IT_FORM__SmtpSettings__FromName` | Sender display name |
| `IT_FORM__SmtpSettings__EnableSsl` | Whether SMTP TLS is enabled |
| `IT_FORM__AllowedHosts` | Production host names accepted by ASP.NET Core |
| `IT_FORM__AppDomain` | Public HTTPS application URL |
| `IT_FORM__DatabaseBackup__BackupPath` | Local backup directory |
| `IT_FORM__DatabaseBackup__AdditionalBackupPath` | Optional additional backup destination |
| `IT_FORM__DatabaseBackup2__AdditionalBackupPath` | Optional second backup destination |
| `IT_FORM__DataProtection__KeyRingPath` | Persistent directory for the ASP.NET Core Data Protection key ring |
| `IT_FORM__DataProtection__CertificateThumbprint` | Thumbprint of a valid RSA certificate in `LocalMachine\My`; its private key must be readable by the application identity |
| `IT_FORM__DataProtection__UnprotectCertificateThumbprints` | Optional comma/semicolon-separated prior certificate thumbprints retained only to decrypt historical key-ring entries during rollover |

Do not place these values in GitHub workflow YAML, source files, issue comments, or build artifacts. If deployment later uses GitHub Actions, inject them from repository/environment secrets at deploy time; this repository intentionally has no deployment job yet.

## ASP.NET Core Data Protection key-ring protection

Production startup now fails closed unless `IT_FORM__DataProtection__CertificateThumbprint` is configured and resolves to a valid certificate in the Windows `LocalMachine\My` store with an accessible RSA private key of at least 2048 bits. The application uses that certificate to protect newly persisted Data Protection key-ring XML.

Set `IT_FORM__DataProtection__KeyRingPath` to the **full directory** used for the persistent key ring. It must be on durable storage, outside the publish directory and web root. Every application instance that must read the same cookies, protected fields or backups must have access to the same key ring and the corresponding certificate private key.

Before enabling this requirement in production:
1. Back up the existing key ring and database/backup files using an access-controlled process.
2. Install the active certificate on every host and grant the application identity read access to its private key only.
3. Configure the same durable key-ring directory and active certificate thumbprint on each instance.
4. During rollover, add prior certificate thumbprints to `IT_FORM__DataProtection__UnprotectCertificateThumbprints` before changing the active certificate. The application uses these certificates only for decryption; expired prior certificates are accepted when their private keys remain available.
5. Test application startup, cookie validation, encrypted software-key reads/writes and protected-backup restore in staging, including data protected with the previous certificate.
6. Remove a prior certificate/private key only after all dependent key-ring entries and protected data have been migrated or intentionally invalidated.
7. Review existing key XML. Enabling certificate protection does **not retroactively encrypt existing key files**; protect their storage separately and migrate/retire exposed historical keys only after dependent data has been safely re-protected or deliberately invalidated.

The application deliberately does not generate, install, or commit a production certificate. A missing/invalid certificate blocks production startup; this is a release gate, not a reason to disable the check.

## Malware scanning for sensitive uploads

The offboarding resignation-letter upload path requires a clean ClamAV scan before the request is persisted. Configure the scanner endpoint through the secret/environment configuration provider:

| Environment variable | Purpose | Default |
|---|---|---|
| `IT_FORM__MalwareScanning__ClamAvHost` | Hostname/IP of the isolated ClamAV daemon | No default; scanning fails closed |
| `IT_FORM__MalwareScanning__ClamAvPort` | ClamAV TCP port | `3310` |
| `IT_FORM__MalwareScanning__TimeoutSeconds` | Maximum scan time | `30` seconds; bounded to 1–120 |

The application uses ClamAV's `zINSTREAM` protocol. Keep the daemon on loopback or a private, isolated network segment; do not expose the unauthenticated ClamAV TCP service to the public internet. Apply network ACLs so only the application host can connect, keep signature databases updated, and monitor scanner health.

A malware verdict of `FOUND` rejects the upload. Missing configuration, timeout, connection failure, malformed/unknown scanner response or daemon error also rejects the upload; it never falls back to allowing the file.

**Coverage limitation:** this integration currently protects the sensitive offboarding resignation-letter upload path. It does not establish that every upload surface (for example company logos or spreadsheet imports) is scanned. Those paths remain a release-gated backlog item until integrated and tested.

## HMAC key generation and rotation

Generate a new standard Base64 key outside the repository, for example:

```bash
openssl rand -base64 32
```

Store the result as `IT_FORM__SignatureHmac__Key`. Never commit the generated value.

**Important:** the current signature-integrity implementation uses one HMAC key for both creating and verifying signatures. Replacing it can make previously stored signature HMACs fail verification. Before rotating an existing production key, plan and test a controlled migration/re-signing strategy; do not simply replace the key during a routine deployment.

## Previously committed credentials and key material

Treat any real SMTP password, inventory API key, HMAC key, or ASP.NET Data Protection master key that appeared in public Git history as compromised. Revoke/rotate provider credentials where possible. Data Protection key rotation can invalidate existing protected cookies/tokens, requiring users to sign in again.

Deleting a file from the current tree does not erase old Git commits. A complete history purge requires a coordinated history rewrite and force-push; this cleanup does not rewrite `main` or any existing history.

## Build outputs

The `.codexbuild/` and `.build-verify/` directories are generated workspaces, not source. They are removed from the current branch and ignored to prevent binaries, copied configuration, and stale runtime files from being committed again.


## Coordinated Git-history cleanup (manual maintenance window)

The Gitleaks workflow has two distinct checks: the current working-tree scan is blocking and must report no findings; the reachable-history scan is non-blocking but reports redacted finding metadata so historical exposures remain visible without blocking unrelated security fixes. A non-blocking historical scan is not evidence that old secrets are safe. Do not add broad allowlists just to make the report appear clean. Investigate each match, rotate any real exposed credential/key, and handle history removal through the coordinated process below.

Before any history rewrite:

1. Revoke/rotate the SMTP password and Inventory API key with their providers. Treat the previously committed HMAC key and Data Protection master key as compromised if they were used in a real environment.
2. For HMAC, first plan the signature-record migration described above. Do not rotate blindly if existing signatures must remain verifiable.
3. Freeze repository pushes, create and verify a mirror backup, and notify anyone with a clone or fork.
4. In a separate mirror clone, use `git-filter-repo` (version 2.47 or later with `--sensitive-data-removal`) to remove the generated workspace paths, all historical `appsettings.Development.json` copies, and `App_Data/DataProtection-Keys/`.
5. Use `git-filter-repo --replace-text` with a **private local replacement file outside the repository** to scrub any previously committed values from historical `appsettings.json` versions. Never commit that replacement file.
6. Run the full-history Gitleaks scan again and inspect the changed refs before publishing the rewrite.
7. Only after explicit maintainer approval, force-push the rewritten refs and coordinate fresh clones/rebases. Contact GitHub Support if cached views or pull-request references need removal.

A history rewrite changes commit IDs and can affect pull requests, forks, tags, and existing clones. This PR deliberately does not perform a force-push or rewrite `main`.

Reference: [GitHub Docs — Removing sensitive data from a repository](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/removing-sensitive-data-from-a-repository).


## DPO contact details for the privacy notice

If the organisation is required to appoint a DPO under the current Commissioner criteria, configure the appointed officer's business contact details. The privacy notice displays the DPO card only when an official contact email is configured.

- `IT_FORM__PdpaCompliance__DpoName`
- `IT_FORM__PdpaCompliance__DpoContactEmail`

Do not set these values to a generic IT mailbox unless that mailbox is the formally designated DPO contact.
