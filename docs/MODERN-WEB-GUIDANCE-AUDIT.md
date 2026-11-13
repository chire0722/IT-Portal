# Modern Web Guidance — Audit Report

**Date:** 13 November 2026
**Tool:** Google Chrome `modern-web-guidance` (installed via `npx modern-web-guidance@latest install`)
**Repository:** `chire0722/IT-Portal` (branch `ci-cd/hardening`)

## 1. Tool installation and version

- Installed `modern-web-guidance` skill to `C:\Users\tan18\Documents\IT Portal\IT-Portal\.agents\skills\modern-web-guidance` (copied from `GoogleChrome/modern-web-guidance`).
- A `skills-lock.json` manifest was created at the repository root recording the skill source and content hash (`9e69ce00dc314b64f6664031b70295ea46c7de93ac682c624808df6e48e08b66`).
- The tool is a **guide corpus + interactive CLI** (`search`, `retrieve`, `legacy-scan`), not a self-contained CI scanner. It is designed for interactive use at the start of frontend work; it does not ship a standalone automated CI runner.

## 2. Capability verification

Verified actual commands against the installed `SKILL.md`:

- `npx modern-web-guidance@latest legacy-scan <path> --json` — detects a curated subset (~20% of the guidance corpus; 35 rules across 33 guides) of legacy web-platform patterns.
- `npx modern-web-guidance@latest retrieve <guide-id>` — returns full guide text.
- `npx modern-web-guidance@latest search "<query>"` — search for feature-specific guidance.

## 3. Scan coverage and results

Frontend surface: `Views/` (88 `.cshtml`), `wwwroot/js/` (site.js, table-sort.js, csp-events.js), `wwwroot/css/` (site.css, verification.css). Vendor libraries under `wwwroot/lib/` (Bootstrap 5, jQuery, jquery-validation) were excluded from the active-rule scan as third-party code.

| Target | Result |
|---|---|
| `Views/` | 0 findings (33 guides / 35 rules) |
| `wwwroot/js/` | 0 findings |
| `wwwroot/css/` | 5 findings (1 rule: `customize-scrollbar-color-and-thickness`) |

## 4. Findings (all resolved)

**Rule:** `customize-scrollbar-color-and-thickness` — vendor `::-webkit-scrollbar` pseudo-elements used to customize scrollbars without standard `scrollbar-color`/`scrollbar-width`.

- **Severity:** Low (progressive enhancement). Impact: custom scrollbar styling was invisible in Firefox and Safari; non-standard WebKit-only CSS.
- **Files:** `wwwroot/css/site.css`
  - `.sidebar-nav::-webkit-scrollbar` (sidebar scrollbar)
  - global `::-webkit-scrollbar` rules (SCROLLBAR section)
- **Fix:** Added standard `scrollbar-color` / `scrollbar-width` on the scrollable containers and wrapped the `::-webkit-scrollbar` rules in `@supports not (scrollbar-color: auto)` fallback blocks, per the retrieved guide. Used CSS custom properties for thumb/track colors.
- **Verification:** Re-ran `legacy-scan wwwroot/css --json` → **0 findings**.

### Modern Web Guidance

- **`customize-scrollbar-color-and-thickness`**: Added standard `scrollbar-color`/`scrollbar-width` and wrapped the legacy `::-webkit-scrollbar` rules in an `@supports not (scrollbar-color: auto)` fallback so browsers supporting the standard property ignore the vendor rules ("Wrap legacy fallbacks in an `@supports not (scrollbar-color: auto)` block to prevent conflicts between standard properties and legacy WebKit selectors").

## 5. What the scanner does NOT cover

`legacy-scan` only checks the deterministic subset (~20%) of the guidance corpus. The broader audit areas requested (accessibility, performance, unnecessary JS, dependency staleness, browser compatibility) are **not** covered by the automated legacy scan and would require `search`/`retrieve`-driven manual review plus browser/Lighthouse/axe tooling. No claims are made about those areas from the legacy scan alone.

## 6. CI/CD suitability

`modern-web-guidance` does **not** provide a documented, supported automated CI mode. `legacy-scan` requires outbound network access (fetches the package via `npx`) and is designed as an interactive, advisory tool. Recommendation: do **not** wire `legacy-scan` into CI as a blocking check. A safe manual/advisory workflow is to run `legacy-scan <path>` periodically on `Views/` and `wwwroot/` and triage findings against the `keepWhen`/`dropWhen` rubrics.
