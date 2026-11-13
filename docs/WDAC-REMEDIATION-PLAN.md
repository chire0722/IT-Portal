# WDAC / Smart App Control — Remediation Plan

**Date:** 13–14 November 2026
**Related:** `docs/WDAC-DIAGNOSTIC-REPORT.md`

## 1. Objective

Restore reliable local execution of the .NET test suite (`IT_Form.Tests`, 364 tests) without disabling or weakening Smart App Control, WDAC, HVCI, Secure Boot, or Windows Defender.

## 2. Remediation options (prioritized)

### Option A — CI execution (recommended, no local policy change)

Run tests in GitHub Actions. The committed head `2ee6742` is already green (run `43247025395`, 7/7 jobs including `Test - IT_Form.Tests`). This is the authoritative, policy-independent verification path.

- Requires: user authorization to commit/push the uncommitted changes so CI runs against them.
- No change to the local machine's security posture.

### Option B — Sign the test assemblies with a trusted certificate (proper fix)

Strong-name sign and/or Authenticode-sign the test project so the binaries meet SAC's "Verified and Reputable" criteria.

- Add a code-signing certificate (org EV/OV code-signing cert, or a locally-trusted dev cert installed to the Trusted Root/Trusted Publisher store).
- Configure signing in `IT_Form.Tests.csproj` (or a shared `Directory.Build.props`) via `<SignAssembly>`, `<AssemblyOriginatorKeyFile>`, and Authenticode signing in the build.
- Requires: a valid signing certificate and org policy. Not applied automatically.

### Option C — SAC-compatible developer workflow (approved process)

SAC provides a sanctioned way for a user to allow a specific blocked app without turning SAC off:

- Run the blocked binary; when SAC shows the "Windows protected your PC" prompt, the user (with administrative rights) may choose to allow the app. This adds the binary's publisher/hash to the SAC allowed list.
- This is the Microsoft-supported per-app exception path and does **not** disable SAC globally.

### Option D — Evaluate SAC mode (org decision, requires admin)

Switch Smart App Control to **Evaluation** (or Off) via Windows Security UI. This is a **security-posture reduction** and must be an explicit organizational decision by an administrator — **not** something to be done during routine development, and **not** performed by this agent.

## 3. What was deliberately NOT done

- Did not disable SAC/WDAC/HVCI/Secure Boot/Defender.
- Did not modify `HKLM\SYSTEM\CurrentControlSet\Control\CI\Policy` or any CI policy.
- Did not use undocumented bypasses.
- Did not change production code to evade the block.
- Did not run `CiTool` to add/remove policies (requires elevation; not authorized).

## 4. Residual risks

- Local `IT_Form.Tests` execution remains blocked until one of the above is chosen.
- The intermittency means local results are unreliable: the same binary may pass or be blocked across rebuilds. Any local-only "green" should not be treated as authoritative while SAC enforcement is active.

## 5. Recommendation

1. **Immediate:** use GitHub Actions as the authoritative test runner (Option A); authorize a commit/push so CI validates the uncommitted security changes.
2. **Medium term:** adopt Option B (signing) or Option C (sanctioned per-app allow) for stable local development, per organizational policy.
3. **Do not** choose Option D without explicit management sign-off, as it weakens the machine's security posture.

## 6. Verification

- `IT_Form.AuditTests`: 55 passed (verified this session).
- `IT_Form.Tests`: blocked (`0x800711C7`); 364 tests pending a chosen remediation path.
