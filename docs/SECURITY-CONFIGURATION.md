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

Do not place these values in GitHub workflow YAML, source files, issue comments, or build artifacts. If deployment later uses GitHub Actions, inject them from repository/environment secrets at deploy time; this repository intentionally has no deployment job yet.

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

The current Gitleaks gate intentionally scans reachable Git history and will remain red while the historical findings exist. Do not add broad allowlists just to make the check green.

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
