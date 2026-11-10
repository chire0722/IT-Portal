# Encryption and Key Management Standard — IT-Portal

**Repository:** `chire0722/IT-Portal`  
**Assessment branch:** `ci-cd/hardening`  
**Status:** Engineering standard and deployment gate; production key custody and restore evidence remain outstanding.

## 1. Cryptographic assets and purposes

| Asset | Current use | Protection boundary | Owner / requirement |
|---|---|---|---|
| ASP.NET Core Data Protection key ring | Cookies, marked sensitive fields, software-license key values and protected SQLite backups | Shared application name `IT_Form`; purpose isolation for `IT_Form.SoftwareLicenseKeys.v2` and `IT_Form.DatabaseBackups.v1` | IT owns durable storage, certificate private key, access, backup and recovery |
| Data Protection certificate | Encrypts newly persisted key-ring XML in Production | Valid RSA certificate (minimum 2048-bit private key) in Windows `LocalMachine\\My` | Platform/IT owns issuance, renewal, private-key ACL and recovery |
| Signature HMAC key | Integrity validation for signed workflow records/documents | Separate configuration secret `IT_FORM__SignatureHmac__Key`; at least 32 random bytes | Security/IT owns generation, custody and versioned migration |
| SMTP credential | Outbound notification authentication | Secret/configuration store only | IT owns rotation and provider revocation |
| Inventory API key | Optional legacy inventory integration | Secret/configuration store only | Integration owner; disable if no longer required |
| SQLite database | Application persistence | SQLite file is not encrypted as a whole by the application code | Host volume encryption and restrictive filesystem ACLs required |
| Sensitive uploads | Resignation/offboarding documents | Stored outside web root under `App_Data/SecureUploads` | Host volume encryption, restrictive ACLs, scanning and backup protection required |
| Protected backups | SQLite snapshots protected with Data Protection | Encrypted backup bytes written to configured backup destinations | IT owns off-host copies, expiry, restore and key availability |

Never reuse one key for HMAC, Data Protection, SMTP or unrelated application purposes.

## 2. Data Protection implementation

Production startup now:
- requires `DataProtection:CertificateThumbprint`;
- searches the Windows `LocalMachine\\My` certificate store;
- requires a valid certificate with an accessible RSA private key of at least 2048 bits;
- configures `ProtectKeysWithCertificate` for newly persisted Data Protection keys;
- supports an explicit persistent directory through `DataProtection:KeyRingPath`;
- fails startup if the production certificate is missing or invalid.

Development keeps the local development key-ring behavior. Do not copy development keys into production.

All application instances that must share authentication cookies, encrypted field values or protected backups must use the same application name, compatible key ring and corresponding private key. Restrict key-ring directory and certificate private-key permissions to the application identity and approved administrators.

**Important limitation:** certificate protection applies to newly persisted key-ring XML. It does not retroactively encrypt old XML files. Existing key files must be protected by volume encryption and ACLs until safely migrated or retired.

## 3. Backup snapshot confidentiality

The backup service creates a consistent SQLite snapshot using SQLite's backup API and protects the snapshot using Data Protection before writing to configured backup destinations.

The plaintext temporary snapshot is now staged under the local private application directory:
`App_Data/BackupStaging/<machine>/<process-id>/`

It is no longer staged inside a configured additional/off-host backup destination. The service deletes the temporary snapshot after reading it and attempts to remove abandoned staging directories older than 24 hours from prior processes.

The staging directory still contains plaintext database bytes while the snapshot is being created. Therefore:
- place `App_Data` on an encrypted host volume;
- grant access only to the application identity and approved administrators;
- exclude `BackupStaging` from web/static serving and public artifacts;
- monitor disk space and stale-file cleanup failures;
- do not copy staging files to remote backup destinations;
- verify that backups remain encrypted at every destination.

The current service keeps the newest ten matching backups per destination. This is a code default, not an approved retention policy or immutable backup guarantee. Configure retention, off-host immutability, geographic separation and deletion with the controller/IT owner.

## 4. Key lifecycle

### Generate and provision
- Generate keys/certificates in the approved platform or secret-management environment.
- Do not paste private keys, HMAC values, passwords or API keys into chat, Git, issues, build logs or reports.
- Use a certificate with a documented owner, purpose, expiry, renewal lead time and recovery method.
- Record only non-secret identifiers (certificate thumbprint, key version, creation date and owner) in the inventory.

### Access and use
- Separate duties between certificate/key custodians and application deployers.
- Restrict private-key access to the application identity and a minimal recovery group.
- Audit access to the key-ring directory and secret store.
- Never log plaintext sensitive fields, decrypted license keys, HMAC secrets or protected backup contents.

### Rotate
- Rotate compromised or expired credentials immediately after assessing dependencies.
- HMAC rotation requires a versioned signature migration and controlled handling of historical signatures; do not simply replace the configured key.
- Data Protection rotation must preserve required decryption access to old protected data until data has been safely re-protected or intentionally invalidated.
- Certificate renewal must account for old key-ring entries that still require the previous certificate's private key.
- Test new-key encryption and old-data decryption in staging before retiring old keys.

### Revoke and retire
- Revoke external credentials with their issuer/provider.
- Remove old private keys only after dependent cookies, protected fields, backups and documents have been migrated or intentionally invalidated.
- Record approvals, dates, affected data, migration evidence and recovery outcome.
- If a key was publicly exposed, treat the affected confidentiality/integrity guarantees as compromised; a source-file deletion is not remediation.

## 5. Historical exposure and migration

The PR history records historical Gitleaks matches, including repeated generated development configuration and a Data Protection key XML file. Determine whether each value/key was used in a real environment. If so:
1. identify the exact credential/key and every system/environment that used it;
2. revoke or rotate the credential where possible;
3. assess whether data encrypted/signed with it may have been exposed or forged;
4. preserve a controlled mirror backup before any history rewrite;
5. plan HMAC signature and Data Protection data migration;
6. coordinate any history rewrite with repository maintainers and clone/fork owners;
7. rescan reachable history and verify that no current-tree secret remains.

Do not force-push or rewrite `main` as part of routine remediation. Historical key removal is a separate, coordinated maintenance operation.

## 6. Required production verification

| Test | Pass condition |
|---|---|
| Missing certificate | Production startup fails with a clear configuration error |
| Invalid/expired certificate | Production startup fails; no plaintext key-ring fallback |
| Private-key ACL | Application identity can use certificate; unrelated service identities cannot |
| Key-ring persistence | Keys survive application restart and deployment to a new release |
| Multi-instance compatibility | All authorised instances can read shared protected data |
| Cookie continuity | Existing valid cookies work during planned migration or users are intentionally forced to re-authenticate |
| Field encryption | Marked fields are encrypted at rest and can be read by authorised code |
| Key mismatch | Protected ciphertext fails closed; it is not returned as a successful plaintext value |
| Backup confidentiality | Backup destinations contain protected bytes, not SQLite headers/plaintext |
| Staging cleanup | Failed/crashed snapshot files are removed according to cleanup policy |
| Backup restore | A clean environment restores with the approved key ring/certificate and verifies SQLite integrity |
| Key loss simulation | Recovery process and documented impact are demonstrated |
| HMAC rotation | New signatures validate; old signatures follow the approved review/re-signing plan |

## 7. Remaining limitations and release gates

- SQLite is not encrypted as a whole. Decide whether host-volume encryption is sufficient or whether a database-encryption architecture is required.
- `EncryptColumn` coverage is selective. Complete a field-level data inventory before claiming field-level encryption.
- Existing Data Protection key-ring XML is not retroactively protected by the new certificate configuration.
- The production certificate, key-ring directory, volume encryption and filesystem ACLs have not been provisioned or verified in this repository audit.
- Backup retention is not immutable off-site backup. Confirm RPO/RTO and run witnessed restore tests.
- Historical Data Protection/HMAC exposures require environment-specific impact assessment and migration.
- New encryption/decryption tests have been added, but the latest code has not yet received a verified CI run.

Do not merge or deploy until the production certificate and key-ring path are provisioned, all affected data remains recoverable, the backup restore test passes, and the controller/IT owner signs off the residual risk.
