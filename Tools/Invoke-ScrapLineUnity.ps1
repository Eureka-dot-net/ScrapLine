<#
.SYNOPSIS
Runs ScrapLine's standard Unity command-line checks.

.EXAMPLE
.\Tools\Invoke-ScrapLineUnity.ps1 -Command Validate

.EXAMPLE
.\Tools\Invoke-ScrapLineUnity.ps1 -Command Test -TestFilter ProgressionFrameworkTests
#>
[CmdletBinding()]
param(
    [ValidateSet("Version", "Compile", "Validate", "Test")]
    [string] $Command = "Test",

    [ValidateSet("EditMode", "PlayMode")]
    [string] $TestPlatform = "EditMode",

    [string] $TestFilter,

    [string] $UnityPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot "ScrapLine"
$projectVersionFile = Join-Path $projectPath "ProjectSettings\ProjectVersion.txt"

if (-not (Test-Path -LiteralPath $projectVersionFile)) {
    throw "Unity project not found at '$projectPath'."
}

$versionLine = Select-String -LiteralPath $projectVersionFile -Pattern '^m_EditorVersion:\s*(.+)$'
if (-not $versionLine) {
    throw "Could not read the Unity version from '$projectVersionFile'."
}

$editorVersion = $versionLine.Matches[0].Groups[1].Value.Trim()

if (-not $UnityPath) {
    $UnityPath = $env:SCRAPLINE_UNITY_PATH
}

if (-not $UnityPath) {
    $UnityPath = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$editorVersion\Editor\Unity.com"
}

if (-not (Test-Path -LiteralPath $UnityPath)) {
    throw "Unity $editorVersion was not found at '$UnityPath'. Set SCRAPLINE_UNITY_PATH or pass -UnityPath."
}

if ($Command -eq "Version") {
    & $UnityPath -version
    return
}

$logsPath = Join-Path $projectPath "Logs"
New-Item -ItemType Directory -Path $logsPath -Force | Out-Null
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$name = "$($Command.ToLowerInvariant())-$($TestPlatform.ToLowerInvariant())-$timestamp"
$logFile = Join-Path $logsPath "$name.log"

$unityArguments = @(
    "-batchmode"
    "-nographics"
    "-projectPath", $projectPath
)

switch ($Command) {
    "Compile" {
        $unityArguments += @("-quit")
    }
    "Validate" {
        $unityArguments += @(
            "-quit"
            "-executeMethod", "ScrapLine.Editor.ContentValidation.ContentValidationMenu.ValidateContent"
        )
    }
    "Test" {
        $resultsFile = Join-Path $logsPath "$name.xml"
        $unityArguments += @(
            "-runTests"
            "-testPlatform", $TestPlatform
            "-testResults", $resultsFile
        )

        if ($TestFilter) {
            $unityArguments += @("-testFilter", $TestFilter)
        }

        Write-Host "Test results: $resultsFile"
    }
}

$unityArguments += @("-logFile", $logFile)

Write-Host "Unity $editorVersion — $Command"
Write-Host "Log: $logFile"
& $UnityPath @unityArguments

if ($LASTEXITCODE -ne 0) {
    throw "Unity $Command failed with exit code $LASTEXITCODE. See '$logFile'."
}

Write-Host "Unity $Command completed successfully."
