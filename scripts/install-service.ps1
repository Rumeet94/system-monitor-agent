param(
    [string]$ServiceName = "SystemMonitorAgent",
    [string]$DisplayName = "System Monitor Agent",
    [string]$BinaryPath = "",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Run this script from an elevated PowerShell or Command Prompt."
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$publishDirectory = Join-Path $repoRoot "publish"
$defaultBinaryPath = Join-Path $publishDirectory "SystemMonitorAgent.Host.exe"
$binaryPathWasProvided = -not [string]::IsNullOrWhiteSpace($BinaryPath)

if (-not $binaryPathWasProvided) {
    $BinaryPath = $defaultBinaryPath
}

$BinaryPath = [System.IO.Path]::GetFullPath($BinaryPath)

if (-not (Test-Path $BinaryPath)) {
    if ($binaryPathWasProvided) {
        throw "Service binary was not found: $BinaryPath"
    }

    $projectPath = Join-Path $repoRoot "SystemMonitorAgent.Host\SystemMonitorAgent.Host.csproj"

    Write-Host "Service binary was not found. Publishing application to '$publishDirectory'..."
    & dotnet publish $projectPath -c $Configuration -o $publishDirectory

    if ($LASTEXITCODE -ne 0) {
        throw "Application publish failed. Exit code: $LASTEXITCODE"
    }

    if (-not (Test-Path $BinaryPath)) {
        throw "Service binary was not found after publish: $BinaryPath"
    }
}

if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    Write-Host "Service '$ServiceName' already exists."
    Write-Host "Operation completed."
    exit 0
}

New-Service `
    -Name $ServiceName `
    -DisplayName $DisplayName `
    -BinaryPathName "`"$BinaryPath`"" `
    -StartupType Automatic

Start-Service -Name $ServiceName
Write-Host "Service '$ServiceName' was installed and started."
Write-Host "Operation completed."
