# WDAC / Smart App Control — Diagnostic Report

**Date:** 13–14 November 2026
**Machine:** Windows (VBS/HVCI enabled)
**Affected assembly:** `IT_Form.Tests\bin\Release\net10.0\IT_Form.Tests.dll`
**Error:** `0x800711C7` (`ERROR_INSTALL_POLICY_FAILURE`) — "An Application Control policy has blocked this file."

## 1. Root cause (definitive)

The block is caused by **Smart App Control (SAC)** — Microsoft's "Verified and Reputable" code-integrity policy — running in **Enforcement mode**, **not** a custom enterprise WDAC/AppLocker policy.

Evidence:

1. **Code Integrity Operational log** (`Microsoft-Windows-CodeIntegrity/Operational`) recorded paired events 3077 + 3033:
   > "Code Integrity determined that a process (…`testhost.exe`) attempted to load `…\IT_Form.Tests.dll` that did not meet the **Enterprise signing level requirements** or violated code integrity policy (Policy ID: `{0283ac0f-fff1-49ae-ada1-8a933130cad6}`)."

2. **Policy identity** (event 3099 "Refreshed and activated Code Integrity policy"):
   > `{0283ac0f-fff1-49ae-ada1-8a933130cad6}` = **`VerifiedAndReputableDesktop`** — the Smart App Control policy.

3. **Policy state** (`HKLM\SYSTEM\CurrentControlSet\Control\CI\Policy`):
   > `VerifiedAndReputablePolicyState = 1` → **Enforcement** (0 = Off/Evaluation, 1 = On).

4. **DeviceGuard / VBS** (`Win32_DeviceGuard`):
   > `VirtualizationBasedSecurityStatus = 2` (running); `SecurityServicesRunning = {2}` (HVCI active).

5. **The blocked binary is unsigned:**
   > `Get-AuthenticodeSignature IT_Form.Tests.dll` → `Status: NotSigned`.
   > `testhost.exe` → `Status: Valid` (that is why the host process can start, then fails to load the unsigned test assembly).

## 2. Why it is intermittent

`IT_Form.AuditTests.dll` (also `NotSigned`) currently runs; `IT_Form.Tests.dll` is blocked. Both are unsigned local build artifacts. SAC's "Verified and Reputable" classification is **hash/reputation-based**: a locally compiled, unsigned .NET assembly has no publisher reputation, and its classification can vary between binaries and over time as the file hash changes on each rebuild. This produces the observed intermittency — some unsigned assemblies are transiently allowed (e.g., previously seen hashes or a reputation grace window) while freshly recompiled ones are blocked.

## 3. What this is NOT

- Not an enterprise WDAC policy requiring a specific signer (the policy is the stock `VerifiedAndReputableDesktop`).
- Not AppLocker (no AppLocker-specific 8003/8004 block events for this DLL).
- Not a defect in the IT-Portal code or the test assemblies themselves (they compile cleanly, 0 warnings/errors; the same DLL ran 364 passing tests earlier in the session).

## 4. Verification (exact counts, this session)

| Project | Result |
|---|---|
| `IT_Form.AuditTests` | **55 passed / 0 failed / 0 skipped** (runs successfully) |
| `IT_Form.Tests` | **Blocked** — `0x800711C7` (assembly cannot load under SAC) |

The expected baseline (364 application tests) cannot be re-run locally while SAC enforcement blocks the freshly compiled `IT_Form.Tests.dll`. **No blocked test is reported as passed.**

## 5. Environment summary

- .NET SDKs: 9.0.316, 10.0.302.
- No `<SignAssembly>` / `<AssemblyOriginatorKeyFile>` / strong-name signing configured in any `.csproj`/`.props`/`.targets` (grep confirmed only code identifiers matched `Sign*`, not MSBuild signing properties).
- `testhost.exe` (xUnit runner, from the SDK) is Microsoft-signed (`Valid`).

## 6. Security posture

No security control was disabled or weakened during diagnosis. All actions were read-only evidence collection (`Get-WinEvent`, `Get-AuthenticodeSignature`, `Get-FileHash`, `Get-CimInstance`, registry read). No policy modification, no elevation, no bypass.
