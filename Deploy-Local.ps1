<#
.SYNOPSIS
    Build and deploy TodoPal locally for development.

.DESCRIPTION
    Publishes the extension, removes any existing registration, and registers
    the new build with the Windows Package Manager so it appears in CmdPal.

.PARAMETER Configuration
    Build configuration. Defaults to Debug.

.PARAMETER Architecture
    Target architecture. Defaults to the current OS architecture.
#>
[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [ValidateSet("x64", "arm64")]
    [string]$Architecture
)

$ErrorActionPreference = "Stop"

if (-not $Architecture) {
    $Architecture = if ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq [System.Runtime.InteropServices.Architecture]::Arm64) {
        "arm64"
    } else {
        "x64"
    }
}

$rid = "win-$Architecture"
$projectDir = Join-Path $PSScriptRoot "TodoPalExtension"

# Stop any running extension process before publishing (avoids file locks)
$running = Get-Process -Name "TodoPalExtension" -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "Stopping running TodoPalExtension process..." -ForegroundColor Yellow
    $running | Stop-Process -Force
    Start-Sleep -Seconds 1
}

# Remove existing package registration to avoid ghost entries in CmdPal
$existing = Get-AppxPackage -Name "*TodoPal*" -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "Removing existing TodoPal registration..." -ForegroundColor Yellow
    $existing | Remove-AppxPackage
}

Write-Host "Publishing TodoPal ($Configuration, $rid)..." -ForegroundColor Cyan
dotnet publish $projectDir -r $rid -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

$tfm = "net9.0-windows10.0.26100.0"
$manifest = Join-Path $projectDir "bin" $Configuration $tfm $rid "AppxManifest.xml"

if (-not (Test-Path $manifest)) {
    throw "AppxManifest.xml not found at: $manifest"
}

Write-Host "Registering package from $manifest..." -ForegroundColor Cyan
Add-AppxPackage -Register $manifest

Write-Host "Done! TodoPal is registered in CmdPal." -ForegroundColor Green
