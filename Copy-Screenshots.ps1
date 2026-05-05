# Create directory
$dir = "c:\IT_Form v1.92\docs\screenshots"
if (-not (Test-Path $dir)) {
    New-Item -ItemType Directory -Force -Path $dir
}

# Copy files from artifacts
Copy-Item "C:\Users\it.dept\.gemini\antigravity\brain\b02d163a-be77-42f7-baf5-90320dde8f08\dashboard_screenshot_1777527193101.png" -Destination "$dir\dashboard.png" -Force
Copy-Item "C:\Users\it.dept\.gemini\antigravity\brain\b02d163a-be77-42f7-baf5-90320dde8f08\onboarding_screenshot_1777527230943.png" -Destination "$dir\onboarding.png" -Force
Copy-Item "C:\Users\it.dept\.gemini\antigravity\brain\b02d163a-be77-42f7-baf5-90320dde8f08\offboarding_screenshot_1777527256095.png" -Destination "$dir\offboarding.png" -Force
Copy-Item "C:\Users\it.dept\.gemini\antigravity\brain\b02d163a-be77-42f7-baf5-90320dde8f08\it_assets_screenshot_1777527279496.png" -Destination "$dir\it_assets.png" -Force
Copy-Item "C:\Users\it.dept\.gemini\antigravity\brain\b02d163a-be77-42f7-baf5-90320dde8f08\software_screenshot_1777527301690.png" -Destination "$dir\software.png" -Force
Copy-Item "C:\Users\it.dept\.gemini\antigravity\brain\b02d163a-be77-42f7-baf5-90320dde8f08\configuration_screenshot_1777527335959.png" -Destination "$dir\configuration_dashboard.png" -Force
Copy-Item "C:\Users\it.dept\.gemini\antigravity\brain\b02d163a-be77-42f7-baf5-90320dde8f08\profile_screenshot_1777527620319.png" -Destination "$dir\user_profile.png" -Force
Copy-Item "C:\Users\it.dept\.gemini\antigravity\brain\b02d163a-be77-42f7-baf5-90320dde8f08\login_screenshot_1777527658453.png" -Destination "$dir\login_page.png" -Force

# Duplicate the module screenshots for the specific steps since we only took one per module
Copy-Item "C:\Users\it.dept\.gemini\antigravity\brain\b02d163a-be77-42f7-baf5-90320dde8f08\onboarding_screenshot_1777527230943.png" -Destination "$dir\onboarding_hr_step1.png" -Force
Copy-Item "C:\Users\it.dept\.gemini\antigravity\brain\b02d163a-be77-42f7-baf5-90320dde8f08\onboarding_screenshot_1777527230943.png" -Destination "$dir\onboarding_hr_step2.png" -Force
Copy-Item "C:\Users\it.dept\.gemini\antigravity\brain\b02d163a-be77-42f7-baf5-90320dde8f08\onboarding_screenshot_1777527230943.png" -Destination "$dir\onboarding_it_setup.png" -Force
Copy-Item "C:\Users\it.dept\.gemini\antigravity\brain\b02d163a-be77-42f7-baf5-90320dde8f08\onboarding_screenshot_1777527230943.png" -Destination "$dir\onboarding_employee_sign.png" -Force

Copy-Item "C:\Users\it.dept\.gemini\antigravity\brain\b02d163a-be77-42f7-baf5-90320dde8f08\offboarding_screenshot_1777527256095.png" -Destination "$dir\offboarding_create.png" -Force
Copy-Item "C:\Users\it.dept\.gemini\antigravity\brain\b02d163a-be77-42f7-baf5-90320dde8f08\offboarding_screenshot_1777527256095.png" -Destination "$dir\offboarding_it_setup.png" -Force

Copy-Item "C:\Users\it.dept\.gemini\antigravity\brain\b02d163a-be77-42f7-baf5-90320dde8f08\it_assets_screenshot_1777527279496.png" -Destination "$dir\asset_add.png" -Force
Copy-Item "C:\Users\it.dept\.gemini\antigravity\brain\b02d163a-be77-42f7-baf5-90320dde8f08\software_screenshot_1777527301690.png" -Destination "$dir\software_add.png" -Force
Copy-Item "C:\Users\it.dept\.gemini\antigravity\brain\b02d163a-be77-42f7-baf5-90320dde8f08\dashboard_screenshot_1777527193101.png" -Destination "$dir\signature_pdf_export.png" -Force

# Same for wwwroot to ensure web server renders them
$wwwdir = "c:\IT_Form v1.92\wwwroot\docs\screenshots"
if (-not (Test-Path $wwwdir)) {
    New-Item -ItemType Directory -Force -Path $wwwdir
}
Copy-Item "$dir\*.png" -Destination $wwwdir -Force

Write-Host "Screenshots copied successfully to $dir and $wwwdir!"
