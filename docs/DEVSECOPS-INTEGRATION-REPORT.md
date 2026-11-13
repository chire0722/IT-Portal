# DevSecOps Integration Report

**Date:** 13 November 2026
**Repository:** `chire0722/IT-Portal` (branch `ci-cd/hardening`)

## 1. Existing CI/CD baseline (inspected before changes)

`.github/workflows/ci.yml` already included, with pinned commit SHAs and least-privilege permissions:

- .NET 10 build + test matrix (application + audit tests).
- NuGet audit (`-warnaserror:NU1901;NU1902;NU1903;NU1904`).
- CodeQL (C# + Actions, `security-extended`).
- Gitleaks (current-tree blocking + full-history advisory).
- Dependency Review (`fail-on-severity: low`).
- Dependabot (nuget + github-actions).
- `permissions: contents: read`, concurrency group, per-job least-privilege.

## 2. Changes implemented in this iteration

- **SBOM generation** — added an `sbom` job that generates an SPDX 2.2 manifest for the published artifact using Microsoft's precompiled SBOM CLI (`sbom-tool-linux-x64`, v4.1.5) downloaded from the versioned GitHub Release URL and verified against SHA256 `bf5d4f99bc98c119d549d08fc02ae92598a7a42772f17317c01031a92632e05b`. The `build-components` path is the repository source directory. The manifest is validated (`spdxVersion == "SPDX-2.2"`) and uploaded as a 90-day-retained artifact.
  - **Reason for change:** the prior `microsoft/sbom-tool@c83b43de…` Docker action failed building its image (transitive AutoMapper 10.1.1 NU1903 / GHSA-rvv3-g6hj-g44x treated as error). Replaced with the official precompiled CLI to avoid the vulnerable Docker-image build without suppressing NuGet audit.
- **Artifact checksums** — added `dotnet publish` + a deterministic sorted `SHA256SUMS` step to the `build` job, uploading the published output + checksums as a 14-day-retained artifact.

## 3. Checks NOT added (deliberately, to avoid duplication or false automation)

- **Modern Web Guidance**: does not provide a supported automated CI mode. `legacy-scan` is interactive/advisory and requires outbound network. Documented as a manual/advisory workflow (see `docs/MODERN-WEB-GUIDANCE-AUDIT.md`), not wired into CI.
- **EGM**: a methodology, not a CI check. The Goldfish independent review was run manually via fresh subagents and is recorded in `docs/ELEPHANT-GOLDFISH-WORKFLOW.md`. It is **not** represented as an automated CI check.
- **Frontend accessibility/performance checks (axe/Lighthouse)**: not added; would require additional tooling and browser-based runners not present in the current pipeline.

## 4. Dependency & secret scanning status

- NuGet vulnerability scanning: enforced on `restore` (fails on NU1901–NU1904).
- Secret scanning: Gitleaks CLI with SHA256-pinned release download; blocks new secrets in the current tree, surfaces historical findings for remediation.
- SBOM: now generated per-run from the verified precompiled SBOM CLI (v4.1.5). **Verified in CI**: run `43467631782` (head `4bf510b`) completed `Generate SBOM (SPDX)` successfully and uploaded the `sbom-spdx` artifact (50,618 bytes), confirming the SPDX 2.2 manifest was generated and validated (`spdxVersion == "SPDX-2.2"`).
- Artifact integrity: SHA256 checksums now published (new); note these are tamper-evidence-in-retrospect and are not cryptographically signed (see residual risks).

## 5. Least-privilege and safety

- Workflow-level `permissions: contents: read` retained.
- All third-party actions pinned to full commit SHAs.
- No secrets are echoed to logs or artifacts.

## 6. Residual risks

- SBOM/checksums are not cryptographically signed or attested (no `attest-build-provenance`/cosign); a compromised runner could rewrite both binaries and their checksums.
- Historical secrets (per prior reports) require rotation and a Git-history purge handled outside CI.
- Local test execution on this workstation now succeeds (the earlier Windows Application Control `0x800711C7` block cleared); the full suite passes locally (364 application + 55 audit = 419). The uncommitted changes have not yet run in GitHub Actions because they cannot be committed/pushed without approval.
