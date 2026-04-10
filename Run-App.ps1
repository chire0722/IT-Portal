param(
    [switch]$Build,
    [switch]$Restore
)

$projectPath = Join-Path $PSScriptRoot 'IT Form.csproj'
$outputPath = Join-Path $PSScriptRoot 'bin\Debug\net10.0\IT Form.dll'

$env:DOTNET_CLI_HOME = $PSScriptRoot

if (-not (Test-Path -LiteralPath $projectPath)) {
    Write-Error "Project file not found: $projectPath"
    exit 1
}

if ($Restore) {
    & dotnet restore $projectPath
    exit $LASTEXITCODE
}

if ($Build -or -not (Test-Path -LiteralPath $outputPath)) {
    & dotnet build $projectPath
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

& dotnet run --project $projectPath --no-build --no-restore
exit $LASTEXITCODE
