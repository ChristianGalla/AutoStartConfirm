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
	   by downloading their license file from the reported License Url while preserving its extension.
	4. Installs Pandoc if it is not already available.
	5. Converts only downloaded HTML license files to plain text using Pandoc and removes the HTML
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

function Get-LicenseFileExtension {
	param(
		[Parameter(Mandatory)] $Response,
		[Parameter(Mandatory)] [uri] $LicenseUri
	)

	$contentDisposition = $Response.Headers['Content-Disposition'] -join ''
	if ($contentDisposition -match 'filename\*?=(?:UTF-8[\x27]{2})?["\x27]?([^;"\x27]+)') {
		$extension = [System.IO.Path]::GetExtension($Matches[1])
		if ($extension) {
			return $extension
		}
	}

	$responseUri = $Response.BaseResponse.RequestMessage.RequestUri
	if (-not $responseUri) {
		$responseUri = $Response.BaseResponse.ResponseUri
	}
	foreach ($uri in @($responseUri, $LicenseUri)) {
		if ($uri) {
			$extension = [System.IO.Path]::GetExtension($uri.AbsolutePath)
			if ($extension) {
				return $extension
			}
		}
	}

	$contentType = ($Response.Headers['Content-Type'] -join '').Split(';')[0].Trim().ToLowerInvariant()
	switch ($contentType) {
		'application/rtf' { return '.rtf' }
		'text/rtf' { return '.rtf' }
		'application/pdf' { return '.pdf' }
		'text/html' { return '.html' }
		'application/xhtml+xml' { return '.html' }
		'text/markdown' { return '.md' }
		default { return '.txt' }
	}
}

Get-ChildItem $OutputDirectory -File | Where-Object { $_.Extension -ne '.json' } | Remove-Item -Force

Write-Host 'Extracting third party license information...'
nuget-license -i $ProjectPath -t -d $OutputDirectory -o JsonPretty -fo $licensesJsonPath
$packages = Get-Content $licensesJsonPath -Raw | ConvertFrom-Json

Write-Host 'Downloading license files for packages without a bundled license file...'
foreach ($package in $packages) {
	$base = "$($package.PackageId)__$($package.PackageVersion)"
	if ($package.LicenseUrl -and -not (Get-ChildItem $OutputDirectory -Filter "$base.*" -File)) {
		Write-Host "Downloading license for $base from $($package.LicenseUrl)..."
		$temporaryPath = Join-Path $OutputDirectory "$base.download"
		$response = Invoke-WebRequest -Uri $package.LicenseUrl -UseBasicParsing -OutFile $temporaryPath -PassThru
		$extension = Get-LicenseFileExtension -Response $response -LicenseUri $package.LicenseUrl
		Move-Item -Path $temporaryPath -Destination (Join-Path $OutputDirectory "$base$extension") -Force
	}
}

$htmlLicenseFiles = @(Get-ChildItem $OutputDirectory -Filter *.html -File)
if ($htmlLicenseFiles.Count -gt 0) {
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
	$htmlLicenseFiles | ForEach-Object {
		$txtPath = [System.IO.Path]::ChangeExtension($_.FullName, '.txt')
		& $pandoc -f html -t plain $_.FullName -o $txtPath
		Remove-Item $_.FullName
	}
}

Write-Host 'Updating Licenses.json with downloaded license file names...'
foreach ($package in $packages) {
	$base = "$($package.PackageId)__$($package.PackageVersion)"
	$licenseFile = Get-ChildItem $OutputDirectory -Filter "$base.*" -File | Select-Object -First 1 -ExpandProperty Name
	$package | Add-Member -NotePropertyName 'LicenseFile' -NotePropertyValue $licenseFile -Force
}
$packages | ConvertTo-Json -Depth 5 | Set-Content -Path $licensesJsonPath -Encoding utf8

Write-Host "Third party licenses were written to $OutputDirectory"
