<#
.SYNOPSIS
	Extracts third party license information for AutoStartConfirmLib and stores it under AutoStartConfirmLib\Licenses.

.DESCRIPTION
	This script:
	1. Installs the nuget-license dotnet tool if it is not already available.
	2. Runs nuget-license against AutoStartConfirmLib.csproj to determine all used packages
	   (including their authors and project urls) as JSON, and downloads license files for
	   packages that provide one directly.
	3. Fills in the remaining packages (those that only specify a license expression, e.g. MIT)
	   by downloading their license text from the reported License Url.
	4. Installs Pandoc if it is not already available.
	5. Converts all downloaded HTML license files to plain text using Pandoc and removes the HTML
	   files, since HTML must not be rendered inside the app for security reasons.
	6. Writes AutoStartConfirmLib\Licenses\Licenses.json, derived directly from the nuget-license
	   JSON output, containing for every package its id, version, authors, project url and the
	   file name of its downloaded license text (if any). This file is read by the about page to
	   render a real table instead of a markdown table dumped into a plain text block.

.PARAMETER ProjectPath
	Path to the project for which to extract licenses. Defaults to AutoStartConfirmLib\AutoStartConfirmLib.csproj
	relative to the repository root.

.PARAMETER OutputDirectory
	Directory in which to store the extracted license files. Defaults to AutoStartConfirmLib\Licenses
	relative to the repository root.

.EXAMPLE
	.\Build\Export-ThirdPartyLicenses.ps1
#>
[CmdletBinding()]
param(
	[string]$ProjectPath = (Join-Path $PSScriptRoot '..\AutoStartConfirmLib\AutoStartConfirmLib.csproj'),
	[string]$OutputDirectory = (Join-Path $PSScriptRoot '..\AutoStartConfirmLib\Licenses')
)

$ErrorActionPreference = 'Stop'

$ProjectPath = (Resolve-Path $ProjectPath).Path
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$licensesJsonPath = Join-Path $OutputDirectory 'Licenses.json'

if (-not (Test-Path $OutputDirectory)) {
	New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
}

if (-not (Get-Command nuget-license -ErrorAction SilentlyContinue)) {
	Write-Host 'Installing nuget-license dotnet tool...'
	dotnet tool install --global nuget-license
}

Write-Host 'Extracting third party license information...'
nuget-license -i $ProjectPath -t -d $OutputDirectory -o JsonPretty -fo $licensesJsonPath
$packages = Get-Content $licensesJsonPath -Raw | ConvertFrom-Json

Write-Host 'Downloading license texts for packages without a bundled license file...'
foreach ($package in $packages) {
	$base = "$($package.PackageId)__$($package.PackageVersion)"
	if ($package.LicenseUrl -and -not (Test-Path (Join-Path $OutputDirectory "$base.*"))) {
		Write-Host "Downloading license for $base from $($package.LicenseUrl)..."
		Invoke-WebRequest -Uri $package.LicenseUrl -UseBasicParsing -OutFile (Join-Path $OutputDirectory "$base.html")
	}
}

$pandoc = (Get-Command pandoc -ErrorAction SilentlyContinue).Source
if (-not $pandoc) {
	$localAppDataPandoc = "$env:LOCALAPPDATA\Pandoc\pandoc.exe"
	if (Test-Path $localAppDataPandoc) {
		$pandoc = $localAppDataPandoc
	}
}
if (-not $pandoc) {
	Write-Host 'Installing Pandoc...'
	winget install --id JohnMacFarlane.Pandoc -e --accept-source-agreements --accept-package-agreements
	$pandoc = (Get-Command pandoc -ErrorAction SilentlyContinue).Source
	if (-not $pandoc) {
		$pandoc = "$env:LOCALAPPDATA\Pandoc\pandoc.exe"
	}
}

Write-Host 'Converting HTML license files to plain text...'
Get-ChildItem $OutputDirectory -Filter *.html | ForEach-Object {
	$txtPath = [System.IO.Path]::ChangeExtension($_.FullName, '.txt')
	& $pandoc -f html -t plain $_.FullName -o $txtPath
	Remove-Item $_.FullName
}

Write-Host 'Updating Licenses.json with downloaded license file names...'
foreach ($package in $packages) {
	$base = "$($package.PackageId)__$($package.PackageVersion)"
	$licenseFile = $null
	if (Test-Path (Join-Path $OutputDirectory "$base.txt")) {
		$licenseFile = "$base.txt"
	}
	$package | Add-Member -NotePropertyName 'LicenseFile' -NotePropertyValue $licenseFile -Force
}
$packages | ConvertTo-Json -Depth 5 | Set-Content -Path $licensesJsonPath -Encoding utf8

Write-Host "Third party licenses were written to $OutputDirectory"
