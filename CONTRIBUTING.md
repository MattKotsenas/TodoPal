# Contributing to TodoPal

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (or later)
- Windows 10/11 with [CmdPal](https://github.com/microsoft/devhome) installed
- PowerShell 5.1+

## Build and test

```powershell
dotnet build
dotnet test
```

## Local deployment

A `Deploy-Local.ps1` script handles the full publish → unregister → register cycle:

```powershell
.\Deploy-Local.ps1                        # Debug build, auto-detect architecture
.\Deploy-Local.ps1 -Configuration Release # Release build
.\Deploy-Local.ps1 -Architecture arm64    # Force ARM64
```

The script:

1. Publishes the extension with `dotnet publish`
2. Stops any running `TodoPalExtension` process
3. Removes the existing AppxPackage registration (prevents ghost entries in CmdPal)
4. Registers the new build via `Add-AppxPackage -Register`

After running, open CmdPal and search for "TodoPal".

## Iterating

After making code changes, re-run `.\Deploy-Local.ps1` to redeploy. There's no hot-reload; CmdPal loads the extension from the published output directory.

## CI

CI runs on GitHub Actions (`.github/workflows/ci.yml`). It builds, tests, and does a Release publish to verify trimming and AOT compatibility.
