// Project comment: VerificationController.cs is part of the IT Form application source code.

using Microsoft.AspNetCore.Mvc;
using IT_Form.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.IO;
using System.IO.Compression;
using IT_Form.Helpers;
using IT_Form.Models;

namespace IT_Form.Controllers
{
    [Authorize(Policy = "RequireIT")]
    /// <summary>
    /// Handles web requests for the Verification area.
    /// </summary>
    public class VerificationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public VerificationController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [EnableRateLimiting("public-verification")]
        [HttpGet("/verify/{token}")]
        /// <summary>
        /// Runs the Index operation.
        /// </summary>
        public async Task<IActionResult> Index(string token)
        {
            SetPrivateNoStoreHeaders();
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest("Invalid token.");
            }

            var dbToken = await _context.DeviceVerificationTokens
                .Include(t => t.Employee)
                .FirstOrDefaultAsync(t => t.Token == token);

            if (dbToken == null)
            {
                return NotFound("Verification link not found or invalid.");
            }

            if (IsExpired(dbToken))
            {
                return BadRequest("Verification link expired. Generate a new device verification token.");
            }

            return View(dbToken);
        }

        [HttpPost("/api/inventory/generate-token/{employeeId}")]
        /// <summary>
        /// Runs the GenerateToken operation.
        /// </summary>
        public async Task<IActionResult> GenerateToken(string employeeId)
        {
            return await GenerateTokenCoreAsync(employeeId);
        }

        [HttpPost("/api/inventory/generate-token")]
        /// <summary>
        /// Runs the GenerateTokenWithoutEmployee operation.
        /// </summary>
        public async Task<IActionResult> GenerateTokenWithoutEmployee()
        {
            return await GenerateTokenCoreAsync(null);
        }

        /// <summary>
        /// Runs the GenerateTokenCoreAsync operation.
        /// </summary>
        private async Task<IActionResult> GenerateTokenCoreAsync(string? employeeId)
        {
            ApplicationUser? user = null;
            if (!string.IsNullOrWhiteSpace(employeeId))
            {
                user = await _context.Users.FindAsync(employeeId);
                if (user == null)
                {
                    return NotFound("Employee not found.");
                }
            }

            if (user != null)
            {
                // Invalidate old tokens for this employee
                var oldTokens = _context.DeviceVerificationTokens.Where(t => t.EmployeeId == employeeId);
                _context.DeviceVerificationTokens.RemoveRange(oldTokens);
            }

            var newToken = new DeviceVerificationToken
            {
                EmployeeId = user?.Id,
                IsVerified = false
            };

            _context.DeviceVerificationTokens.Add(newToken);
            await _context.SaveChangesAsync();

            string link = Url.Action("Index", "Verification", new { token = newToken.Token }, Request.Scheme) ?? string.Empty;
            return Ok(new
            {
                token = newToken.Token,
                link = link,
                expiresAtUtc = HasExpiringEmployeeContext(newToken)
                    ? newToken.CreatedAt.AddMinutes(GetTokenLifetimeMinutes())
                    : (DateTime?)null,
                assignmentMode = user == null ? "asset-only" : "employee-specific",
                employeeName = user?.FullName
            });
        }

        [AllowAnonymous]
        [EnableRateLimiting("public-verification")]
        [HttpGet("/api/inventory/status/{token}")]
        /// <summary>
        /// Gets the data required by the GetStatus operation.
        /// </summary>
        public async Task<IActionResult> GetStatus(string token)
        {
            SetPrivateNoStoreHeaders();
            var dbToken = await _context.DeviceVerificationTokens.FirstOrDefaultAsync(t => t.Token == token);
            if (dbToken == null)
            {
                return NotFound();
            }

            if (IsExpired(dbToken))
            {
                return BadRequest(new { isVerified = false, expired = true });
            }

            return Json(new
            {
                isVerified = dbToken.IsVerified,
                fingerprintMismatch = dbToken.HasHardwareFingerprintMismatch,
                warningMessage = dbToken.HardwareFingerprintWarning,
                assetTag = dbToken.VerifiedAssetTag
            });
        }

        [HttpGet("/api/inventory/download-universal")]
        /// <summary>
        /// Runs the DownloadUniversalTool operation.
        /// </summary>
        public IActionResult DownloadUniversalTool()
        {
            return NotFound("Universal inventory downloads are disabled. Generate an employee-specific verification token instead.");
        }

        [AllowAnonymous]
        [EnableRateLimiting("public-verification")]
        [HttpGet("/api/inventory/download/{token}")]
        /// <summary>
        /// Runs the DownloadTool operation.
        /// </summary>
        public async Task<IActionResult> DownloadTool(string token)
        {
            SetPrivateNoStoreHeaders();
            var dbToken = await _context.DeviceVerificationTokens.FirstOrDefaultAsync(t => t.Token == token);
            if (dbToken == null || dbToken.IsVerified)
            {
                return BadRequest("Token invalid or already verified.");
            }

            if (IsExpired(dbToken))
            {
                return BadRequest("Verification token expired. Generate a new device verification token.");
            }

            var request = HttpContext.Request;
            var apiUrls = BuildInventorySyncUrls(request);

            // Create ZIP in memory
            using var ms = new MemoryStream();
            using (var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
            {
                AddToolScriptsToArchive(
                    archive,
                    apiUrls,
                    token: token);
            }

            ms.Position = 0;
            return File(ms.ToArray(), "application/zip", "Device_Verification.zip");
        }

        /// <summary>
        /// Runs the AddToolScriptsToArchive operation.
        /// </summary>
        private static void AddToolScriptsToArchive(System.IO.Compression.ZipArchive archive, IReadOnlyList<string> apiUrls, string token)
        {
            var psEntry = archive.CreateEntry("Verify_Device.ps1");
            using (var writer = new StreamWriter(psEntry.Open()))
            {
                writer.Write(BuildPowerShellAuditScript(apiUrls, token));
            }

            var batEntry = archive.CreateEntry("Verify_Device.bat");
            using (var writer = new StreamWriter(batEntry.Open()))
            {
                writer.Write(BuildBatchWrapperScript());
            }
        }

        /// <summary>
        /// Builds the value returned by the BuildInventorySyncUrls operation.
        /// </summary>
        private IReadOnlyList<string> BuildInventorySyncUrls(HttpRequest request)
        {
            var urls = new List<string>();

            AppendInventorySyncUrl(urls, ConfigurationValueNormalizer.NormalizeBaseUrl(_configuration["AppDomain"]));
            AppendInventorySyncUrl(urls, ConfigurationValueNormalizer.NormalizeBaseUrl($"{request.Scheme}://{request.Host}"));

            return urls;
        }

        /// <summary>
        /// Runs the AppendInventorySyncUrl operation.
        /// </summary>
        private static void AppendInventorySyncUrl(List<string> urls, string? baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return;
            }

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var parsed))
            {
                return;
            }

            var isLoopback = parsed.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                             parsed.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                             parsed.Host.Equals("::1", StringComparison.OrdinalIgnoreCase);

            if (!parsed.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) && !isLoopback)
            {
                return;
            }

            var normalized = $"{baseUrl.TrimEnd('/')}/api/inventory/sync";
            if (!urls.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                urls.Add(normalized);
            }
        }

        /// <summary>
        /// Gets the data required by the GetTokenLifetimeMinutes operation.
        /// </summary>
        private int GetTokenLifetimeMinutes()
        {
            return int.TryParse(_configuration["SecuritySettings:DeviceVerificationExpiryMinutes"], out var minutes) && minutes > 0
                ? minutes
                : 30;
        }

        /// <summary>
        /// Runs the HasExpiringEmployeeContext operation.
        /// </summary>
        private static bool HasExpiringEmployeeContext(DeviceVerificationToken token)
            => !string.IsNullOrWhiteSpace(token.EmployeeId);

        /// <summary>
        /// Runs the IsExpired operation.
        /// </summary>
        private void SetPrivateNoStoreHeaders()
        {
            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";
            Response.Headers.ReferrerPolicy = "no-referrer";
        }

        private bool IsExpired(DeviceVerificationToken token)
            => HasExpiringEmployeeContext(token) &&
               token.CreatedAt.AddMinutes(GetTokenLifetimeMinutes()) <= DateTime.UtcNow;

        /// <summary>
        /// Builds the value returned by the BuildPowerShellAuditScript operation.
        /// </summary>
        private static string BuildPowerShellAuditScript(IReadOnlyList<string> apiUrls, string token)
        {
            var escapedApiUrls = string.Join(
                Environment.NewLine,
                apiUrls.Select(url => $"    '{EscapeForPowerShellSingleQuotedString(url)}'"));
            var escapedToken = EscapeForPowerShellSingleQuotedString(token);

            return $$"""
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$Token = '{{escapedToken}}'
$ApiUrls = @(
{{escapedApiUrls}}
)

$scriptDirectory = Split-Path -Path $MyInvocation.MyCommand.Path -Parent
if (-not [string]::IsNullOrWhiteSpace($scriptDirectory) -and (Test-Path -LiteralPath $scriptDirectory)) {
    Set-Location -LiteralPath $scriptDirectory
}

function Normalize-Serial {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return 'N/A'
    }

    $normalized = $Value.Trim()
    $invalidSerials = @(
        'To be filled by O.E.M.',
        'To Be Filled By O.E.M.',
        'To Be Filled By OEM',
        'To be filled by OEM',
        'System Serial Number',
        'Default string',
        'None',
        'Not Specified',
        'N/A'
    )

    foreach ($invalid in $invalidSerials) {
        if ($normalized.Equals($invalid, [System.StringComparison]::OrdinalIgnoreCase)) {
            return 'N/A'
        }
    }

    return $normalized
}

function Safe-Text {
    param([object]$Value)

    if ($null -eq $Value) {
        return 'N/A'
    }

    $text = $Value.ToString().Trim()
    if ([string]::IsNullOrWhiteSpace($text)) {
        return 'N/A'
    }

    return $text
}

function Get-CimSafe {
    param(
        [Parameter(Mandatory = $true)][string]$ClassName,
        [string]$Filter
    )

    try {
        if ([string]::IsNullOrWhiteSpace($Filter)) {
            return @(Get-CimInstance -ClassName $ClassName -ErrorAction Stop)
        }

        return @(Get-CimInstance -ClassName $ClassName -Filter $Filter -ErrorAction Stop)
    }
    catch {
        return @()
    }
}

function Convert-BytesToGB {
    param([object]$Value)

    if ($null -eq $Value) {
        return $null
    }

    try {
        return [math]::Round(([double]$Value / 1GB), 2)
    }
    catch {
        return $null
    }
}

function Get-ErrorSummary {
    param([System.Management.Automation.ErrorRecord]$Record)

    $details = @()
    if ($null -ne $Record.Exception -and -not [string]::IsNullOrWhiteSpace($Record.Exception.Message)) {
        $details += $Record.Exception.Message.Trim()
    }

    if ($Record.ErrorDetails -and -not [string]::IsNullOrWhiteSpace($Record.ErrorDetails.Message)) {
        $details += $Record.ErrorDetails.Message.Trim()
    }

    return ($details | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique) -join [Environment]::NewLine
}

Write-Host '==================================' -ForegroundColor Cyan
Write-Host '   IT Device Verification' -ForegroundColor Cyan
Write-Host '==================================' -ForegroundColor Cyan
Write-Host ''
Write-Host '[1/3] Capturing hardware fingerprints...'

# Core hardware sources via CIM (Get-WmiObject is intentionally not used).
$computerSystem = (Get-CimSafe -ClassName 'Win32_ComputerSystem' | Select-Object -First 1)
$motherboard = (Get-CimSafe -ClassName 'Win32_BaseBoard' | Select-Object -First 1)
$bios = (Get-CimSafe -ClassName 'Win32_BIOS' | Select-Object -First 1)
$cpu = (Get-CimSafe -ClassName 'Win32_Processor' | Select-Object -First 1)
$os = (Get-CimSafe -ClassName 'Win32_OperatingSystem' | Select-Object -First 1)
$ramModulesRaw = Get-CimSafe -ClassName 'Win32_PhysicalMemory'
$disksRaw = Get-CimSafe -ClassName 'Win32_DiskDrive'
$networkRaw = Get-CimSafe -ClassName 'Win32_NetworkAdapterConfiguration' -Filter 'IPEnabled = TRUE'

$ramModules = @()
foreach ($module in ($ramModulesRaw | Sort-Object BankLabel, DeviceLocator)) {
    $slotLabel = if (-not [string]::IsNullOrWhiteSpace($module.DeviceLocator)) {
        $module.DeviceLocator
    } elseif (-not [string]::IsNullOrWhiteSpace($module.BankLabel)) {
        $module.BankLabel
    } else {
        'Unknown'
    }

    $ramModules += [pscustomobject]@{
        Slot = $slotLabel
        CapacityGB = Convert-BytesToGB $module.Capacity
        SpeedMHz = if ($null -ne $module.Speed) { [int]$module.Speed } else { $null }
        SerialNumber = Normalize-Serial $module.SerialNumber
    }
}

$disks = @()
foreach ($disk in ($disksRaw | Sort-Object Index)) {
    $indexValue = $null
    if ($null -ne $disk.Index) {
        $indexValue = [int]$disk.Index
    }

    $disks += [pscustomobject]@{
        Index = $indexValue
        Model = Safe-Text $disk.Model
        SizeGB = Convert-BytesToGB $disk.Size
        SerialNumber = Normalize-Serial $disk.SerialNumber
    }
}

$networkAdapters = @()
foreach ($adapter in $networkRaw) {
    $ipAddresses = @()
    if ($null -ne $adapter.IPAddress) {
        foreach ($ip in $adapter.IPAddress) {
            if (-not [string]::IsNullOrWhiteSpace($ip)) {
                $ipAddresses += $ip.Trim()
            }
        }
    }

    if ($ipAddresses.Count -eq 0) {
        $ipAddresses += 'N/A'
    }

    $networkAdapters += [pscustomobject]@{
        Adapter = Safe-Text $adapter.Description
        MACAddress = Safe-Text $adapter.MACAddress
        IPAddress = $ipAddresses
    }
}

$computerName = Safe-Text $env:COMPUTERNAME
$currentUser = Safe-Text $computerSystem.UserName
if ($currentUser -eq 'N/A') {
    try {
        $currentUser = Safe-Text ([System.Security.Principal.WindowsIdentity]::GetCurrent().Name)
    }
    catch {
        $currentUser = 'N/A'
    }
}

$motherboardSerial = Normalize-Serial $motherboard.SerialNumber
$biosSerial = Normalize-Serial $bios.SerialNumber
$cpuId = Normalize-Serial $cpu.ProcessorId

$disk0 = $disks | Where-Object { $_.Index -eq 0 } | Select-Object -First 1
if ($null -eq $disk0) {
    $disk0 = $disks | Select-Object -First 1
}
$disk0Serial = if ($null -ne $disk0) { Normalize-Serial $disk0.SerialNumber } else { 'N/A' }

# Requested machine identifier: concatenated motherboard serial + CPU ID + Disk 0 serial.
$hardwareHash = '{0}|{1}|{2}' -f $motherboardSerial, $cpuId, $disk0Serial

$osInstallDate = 'N/A'
if ($os -and $os.InstallDate) {
    try {
        if ($os.InstallDate -is [DateTime]) {
            $osInstallDate = $os.InstallDate.ToUniversalTime().ToString('o')
        }
        else {
            $osInstallDate = ([System.Management.ManagementDateTimeConverter]::ToDateTime([string]$os.InstallDate)).ToUniversalTime().ToString('o')
        }
    }
    catch {
        try {
            $osInstallDate = (Get-Date $os.InstallDate).ToUniversalTime().ToString('o')
        }
        catch {
            $osInstallDate = 'N/A'
        }
    }
}

$totalRamGb = $null
if ($ramModules.Count -gt 0) {
    $sumRam = ($ramModules | Where-Object { $_.CapacityGB -ne $null } | Measure-Object -Property CapacityGB -Sum).Sum
    if ($null -ne $sumRam) {
        $totalRamGb = [math]::Round([double]$sumRam, 2)
    }
}
if ($null -eq $totalRamGb) {
    $totalRamGb = Convert-BytesToGB $computerSystem.TotalPhysicalMemory
}
$totalRamText = if ($null -ne $totalRamGb) { '{0} GB' -f $totalRamGb } else { 'N/A' }

$totalStorageGb = $null
if ($disks.Count -gt 0) {
    $sumStorage = ($disks | Where-Object { $_.SizeGB -ne $null } | Measure-Object -Property SizeGB -Sum).Sum
    if ($null -ne $sumStorage) {
        $totalStorageGb = [math]::Round([double]$sumStorage, 2)
    }
}
$totalStorageText = if ($null -ne $totalStorageGb) { '{0} GB' -f $totalStorageGb } else { 'N/A' }

$primaryMemoryModule = $ramModules | Select-Object -First 1
$ddrTypeTelemetry = if ($primaryMemoryModule -and $primaryMemoryModule.SpeedMHz) {
    '{0} MHz' -f $primaryMemoryModule.SpeedMHz
} else {
    'N/A'
}

$primarySerial = $biosSerial
if ($primarySerial -eq 'N/A' -and $motherboardSerial -ne 'N/A') {
    $primarySerial = $motherboardSerial
}
if ($primarySerial -eq 'N/A' -and $disk0Serial -ne 'N/A') {
    $primarySerial = $disk0Serial
}
if ($primarySerial -eq 'N/A') {
    $primarySerial = $computerName
}

$auditObject = [pscustomobject]@{
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    HardwareHash = $hardwareHash
    Motherboard = [pscustomobject]@{
        Manufacturer = Safe-Text $motherboard.Manufacturer
        Product = Safe-Text $motherboard.Product
        SerialNumber = $motherboardSerial
    }
    BIOS = [pscustomobject]@{
        Manufacturer = Safe-Text $bios.Manufacturer
        Version = Safe-Text $bios.SMBIOSBIOSVersion
        SerialNumber = $biosSerial
    }
    CPU = [pscustomobject]@{
        Name = Safe-Text $cpu.Name
        NumberOfCores = if ($null -ne $cpu.NumberOfCores) { [int]$cpu.NumberOfCores } else { $null }
        ProcessorId = $cpuId
    }
    RAM = $ramModules
    Storage = $disks
    Network = $networkAdapters
    OS = [pscustomobject]@{
        Caption = Safe-Text $os.Caption
        Version = Safe-Text $os.Version
        InstallDate = $osInstallDate
    }
    Identity = [pscustomobject]@{
        ComputerName = $computerName
        CurrentUser = $currentUser
    }
}

$osTelemetry = Safe-Text $os.Caption
$osVersion = Safe-Text $os.Version
if ($osVersion -ne 'N/A') {
    $osTelemetry = '{0} ({1})' -f $osTelemetry, $osVersion
}

$assetType = 'DesktopPC'
if ($osTelemetry -match 'Server') {
    $assetType = 'Others'
} elseif ((Safe-Text $computerSystem.Model) -match 'Laptop|Book') {
    $assetType = 'Laptop'
}

$ramDisplay = $totalRamText
if ($ddrTypeTelemetry -ne 'N/A') {
    $ramDisplay = '{0} ({1})' -f $totalRamText, $ddrTypeTelemetry
}

$reportFileName = 'HardwareInfo_{0}.txt' -f $computerName
$reportFilePath = Join-Path -Path (Get-Location).Path -ChildPath $reportFileName
$reportContent = @"
Hardware Information
--------------------
Asset Tag       : $computerName
Type            : $assetType
Status          : Issued
Model / Make    : $(Safe-Text $computerSystem.Model)
Serial Number   : $primarySerial
RAM             : $ramDisplay
Storage         : $totalStorageText
Windows Version : $osTelemetry
Hardware Hash   : $hardwareHash
Last Seen       : $(Get-Date -Format 'dd/MM/yyyy HH:mm:ss')
Purchase Date   : -
Warranty Expiry : -
Warranty Plan   : Extend / Monitor
"@
$reportContent | Out-File -LiteralPath $reportFilePath -Encoding UTF8
Write-Host ('[2/3] Hardware report saved: {0}' -f $reportFilePath)

$syncLogFileName = 'SyncStatus_{0}.log' -f $computerName
$syncLogFilePath = Join-Path -Path (Get-Location).Path -ChildPath $syncLogFileName

$payload = @{
    ComputerName = $computerName
    SN = $primarySerial
    Model = Safe-Text $computerSystem.Model
    CPU = Safe-Text $cpu.Name
    RAM_GB = $totalRamText
    Storage = $totalStorageText
    DDRType = $ddrTypeTelemetry
    OS = $osTelemetry
    HardwareHash = $hardwareHash
    Audit = $auditObject
}

if (-not [string]::IsNullOrWhiteSpace($Token)) {
    $payload.Token = $Token
}

$candidateApiUrls = @($ApiUrls | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
if ($candidateApiUrls.Count -eq 0) {
    $candidateApiUrls = @('http://localhost:5063/api/inventory/sync')
}

$syncFailures = @()
foreach ($candidateApiUrl in $candidateApiUrls) {
    Write-Host ('[3/3] Sending telemetry to IT infrastructure via: {0}' -f $candidateApiUrl)
    try {
        Invoke-RestMethod -Uri $candidateApiUrl -Method Post -Body ($payload | ConvertTo-Json -Depth 12) -ContentType 'application/json' | Out-Null

        @(
            ('TimestampUtc={0}' -f (Get-Date).ToUniversalTime().ToString('o'))
            ('Status=Success')
            ('Endpoint={0}' -f $candidateApiUrl)
            ('AuditFile={0}' -f $reportFilePath)
        ) | Out-File -LiteralPath $syncLogFilePath -Encoding UTF8

        Write-Host '[OK] Verification completed.' -ForegroundColor Green
        Write-Host ('[OK] Sync details saved: {0}' -f $syncLogFilePath)
        Start-Sleep -Seconds 2
        exit 0
    }
    catch {
        $message = Get-ErrorSummary $_
        if ([string]::IsNullOrWhiteSpace($message)) {
            $message = 'Unknown sync failure.'
        }

        $syncFailures += @(
            ('TimestampUtc={0}' -f (Get-Date).ToUniversalTime().ToString('o'))
            ('Status=Failure')
            ('Endpoint={0}' -f $candidateApiUrl)
            ('Message={0}' -f $message.Replace([Environment]::NewLine, ' | '))
            ''
        )

        Write-Host '[WARN] Server sync failed for this endpoint. Local hardware report is still available.' -ForegroundColor Yellow
        Write-Host $message -ForegroundColor Red
    }
}

$syncFailures | Out-File -LiteralPath $syncLogFilePath -Encoding UTF8
Write-Host '[WARN] Verification could not reach a valid inventory endpoint.' -ForegroundColor Yellow
Write-Host ('[WARN] Sync details saved: {0}' -f $syncLogFilePath)
Start-Sleep -Seconds 5
exit 2
""";
        }

        /// <summary>
        /// Builds the value returned by the BuildBatchWrapperScript operation.
        /// </summary>
        private static string BuildBatchWrapperScript()
        {
            return """
@echo on
setlocal EnableExtensions

if /i "%~1" neq "__keepopen" (
    start "Device Verification" "%ComSpec%" /k ""%~f0" __keepopen"
    exit /b
)

shift /1
title Device Verification
cd /d "%~dp0"
set "TOOL_VERSION=2026.04.05.5"

echo ==================================
echo IT Device Verification Tool
echo Version: %TOOL_VERSION%
echo ==================================

REM Ensure the PowerShell script exists in the extracted ZIP folder.
if not exist "Verify_Device.ps1" (
    echo [ERROR] Verify_Device.ps1 not found.
    echo Please extract the ZIP file before running this batch file.
    pause
    exit /b 1
)

echo [INFO] Starting hardware verification audit...

set "AuditFile=HardwareInfo_%COMPUTERNAME%.txt"
set "SyncLogFile=SyncStatus_%COMPUTERNAME%.log"
if exist "%AuditFile%" (
    echo [INFO] Existing audit file will be overwritten: %CD%\%AuditFile%
)
if exist "%SyncLogFile%" (
    echo [INFO] Existing sync log will be overwritten: %CD%\%SyncLogFile%
)

echo [INFO] Launching PowerShell script...
echo [INFO] PowerShell will remain open after completion for troubleshooting.
call powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -NoExit -File ".\Verify_Device.ps1"
set "ScriptExit=%errorlevel%"

REM Confirm that the expected report artifact exists.
if exist "%AuditFile%" (
    echo [OK] Hardware report created: %CD%\%AuditFile%
) else (
    echo [ERROR] Hardware report was not created.
    if "%ScriptExit%"=="0" set "ScriptExit=1"
)

if exist "%SyncLogFile%" (
    echo [INFO] Sync log: %CD%\%SyncLogFile%
)

if not "%ScriptExit%"=="0" (
    echo [INFO] Verification did not fully complete.
)

echo [INFO] Review the output above if anything failed.
echo [INFO] Script finished. This window will remain open.
echo [INFO] Hardware report path: %CD%\%AuditFile%
echo [INFO] Sync status path: %CD%\%SyncLogFile%
exit /b %ScriptExit%
""";
        }

        /// <summary>
        /// Runs the EscapeForPowerShellSingleQuotedString operation.
        /// </summary>
        private static string EscapeForPowerShellSingleQuotedString(string value)
        {
            return value.Replace("'", "''");
        }
    }
}
