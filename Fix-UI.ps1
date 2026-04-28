# Project comment: Fix-UI.ps1 contains PowerShell automation for the IT Form project.

$ErrorActionPreference = "Stop"

$viewsPath = "c:\IT_Form_Backup\Views"
$files = Get-ChildItem -Path $viewsPath -Filter "*.cshtml" -Recurse

Write-Host "Starting UI/UX Audit Execution: Enforcing WCAG 2.1 AA and Golden Ratio" -ForegroundColor Cyan
Write-Host "Found $($files.Count) views to process."

$updatedCount = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    
    # 1. ARIA Implementation
    # Adding aria-hidden="true" to Bootstrap and FontAwesome icons that don't have it
    # Regex explicitly targets <i class="...bi... "> ensuring we don't duplicate aria-hidden.
    # We must ensure we inject it BEFORE the closing > bracket.
    $patternAria = '(<i\s+class="[^"]*(?:bi[ -]|fa[A-Za-z0-9 -]*)[^"]*"(?![^>]*aria-hidden)[^>]*)(>)'
    
    # First, let's clean up our old mistake from the corrupted files:
    # " > aria-hidden="true"</i>" -> "></i>"
    $cleanupPattern = '(<i\s+class="[^"]*(?:bi[ -]|fa[A-Za-z0-9 -]*)[^"]*"(?:[^>]*)>)\s*aria-hidden="true"</i>'
    $content = [regex]::Replace($content, $cleanupPattern, '$1</i>')

    # Now apply it correctly
    $content = [regex]::Replace($content, $patternAria, '$1 aria-hidden="true"$2')
    
    # 2. Mathematical Aesthetics (The Golden Ratio Grid)
    # Replaces col-md-6, col-lg-6, col-sm-6 pairs with 5 and 7
    # We use a stateful evaluator to switch between 5 and 7
    $script:isFive = $true
    $matchEvaluator = [System.Text.RegularExpressions.MatchEvaluator] {
        param([System.Text.RegularExpressions.Match]$match)
        $prefix = $match.Groups[1].Value
        # Switch states
        if ($script:isFive) {
            $script:isFive = $false
            return "col-$prefix-5"
        } else {
            $script:isFive = $true
            return "col-$prefix-7"
        }
    }
    
    $gridPattern = 'col-(md|lg|sm)-6\b'
    $content = [regex]::Replace($content, $gridPattern, $matchEvaluator)
    
    # Reset state for next file so we always start with 5 for consistency across different pages
    $script:isFive = $true
    
    if ($content -cne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8
        Write-Host "Updated UI constraints in: $($file.Name)" -ForegroundColor Green
        $updatedCount++
    }
}

Write-Host "Audit Execution Complete. Successfully refactored $updatedCount files." -ForegroundColor Cyan
