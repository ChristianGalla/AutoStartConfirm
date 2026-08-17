<#
.SYNOPSIS
	Generates "What's Changed" release notes grouped by conventional commit type.

.DESCRIPTION
	Scans the git log (either since the given tag, or the full history if no tag
	is given) for commits following the Conventional Commits format
	(https://www.conventionalcommits.org/). Only commits with a "feat", "fix" or
	"refactor" type are included. Commits marked as breaking changes (either via
	a "!" after the type/scope, or a "BREAKING CHANGE:" footer) are additionally
	listed under a "Breaking changes" section.

	GitHub issue references (e.g. "#94") found anywhere in the commit message are
	appended to the rendered line as links.

	A "New Contributors" section is appended, listing authors whose first-ever
	commit falls within the generated range, linking to the pull request
	referenced in their commit (e.g. "(#99)") when available.

.PARAMETER LastTag
	The previous release tag to generate notes since. If omitted or empty, the
	full commit history is used.

.PARAMETER RepoUrl
	The base URL of the GitHub repository (e.g. https://github.com/owner/repo).
	If omitted, it is derived from the "origin" git remote.

.PARAMETER NewTag
	The tag of the release being generated. If provided (together with
	LastTag), a "Full Changelog" compare link from LastTag to NewTag is
	appended to the release notes.
#>
param(
	[string]$LastTag,
	[string]$RepoUrl,
	[string]$NewTag
)

$ErrorActionPreference = "Stop"

if (-not $RepoUrl) {
	$remoteUrl = git config --get remote.origin.url
	$RepoUrl = $remoteUrl -replace '\.git$', ''
	$RepoUrl = $RepoUrl -replace '^git@github\.com:', 'https://github.com/'
}
$RepoUrl = $RepoUrl.TrimEnd('/')

$commitSeparator = "---AUTOSTARTCONFIRM-COMMIT-END---"

$gitLogArgs = @("--no-merges", "--pretty=format:%B$commitSeparator")
if ($LastTag) {
	$gitLogArgs += "$LastTag..HEAD"
}

$rawLog = git log @gitLogArgs
if (-not $rawLog) {
	$rawLog = ""
}

$commitMessages = $rawLog -split [regex]::Escape($commitSeparator) | Where-Object { $_.Trim() -ne "" }

$addedLines = New-Object System.Collections.Generic.List[string]
$removedLines = New-Object System.Collections.Generic.List[string]
$fixedLines = New-Object System.Collections.Generic.List[string]
$refactoredLines = New-Object System.Collections.Generic.List[string]
$breakingLines = New-Object System.Collections.Generic.List[string]

$conventionalCommitRegex = '^(?<type>\w+)(?<scope>\([^)]*\))?(?<breaking>!)?:\s*(?<description>.+)$'
$issueRegex = '#(\d+)'

foreach ($rawMessage in $commitMessages) {
	$message = $rawMessage.Trim("`r", "`n", " ")
	if (-not $message) {
		continue
	}

	$messageLines = $message -split "`n"
	$firstLine = $messageLines[0].Trim()

	$match = [regex]::Match($firstLine, $conventionalCommitRegex)
	if (-not $match.Success) {
		continue
	}

	$type = $match.Groups["type"].Value.ToLowerInvariant()
	if ($type -ne "feat" -and $type -ne "fix" -and $type -ne "refactor") {
		continue
	}

	$description = $match.Groups["description"].Value.Trim()
	$hasBreakingMarker = $match.Groups["breaking"].Success

	# Collect all referenced issue numbers from the whole commit message
	$issueNumbers = [regex]::Matches($message, $issueRegex) | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique
	$issueSuffix = ""
	if ($issueNumbers.Count -gt 0) {
		$issueLinks = $issueNumbers | ForEach-Object { "($RepoUrl/issues/$_)" }
		$issueSuffix = " " + ($issueLinks -join " ")
	}

	if ($type -eq "feat") {
		if ($description -match '^remove\s+') {
			$removedDescription = $description -replace '^remove\s+', ''
			$removedLines.Add("- $removedDescription$issueSuffix")
		} else {
			$addedDescription = $description -replace '^add\s+', ''
			$addedLines.Add("- $addedDescription$issueSuffix")
		}
	} elseif ($type -eq "fix") {
		$fixedLines.Add("- $description$issueSuffix")
	} elseif ($type -eq "refactor") {
		$refactoredLines.Add("- $description$issueSuffix")
	}

	$breakingFooterMatch = [regex]::Match($message, 'BREAKING CHANGE:\s*(?<text>.+?)(\r?\n\r?\n|$)', [System.Text.RegularExpressions.RegexOptions]::Singleline)
	if ($hasBreakingMarker -or $breakingFooterMatch.Success) {
		if ($breakingFooterMatch.Success) {
			$breakingText = ($breakingFooterMatch.Groups["text"].Value -replace '\s+', ' ').Trim()
		} else {
			$breakingText = $description
		}
		$breakingLines.Add("- $breakingText$issueSuffix")
	}
}

function Get-DedupedLines {
	param([System.Collections.Generic.List[string]]$Lines)
	$seen = New-Object System.Collections.Generic.HashSet[string]
	$result = New-Object System.Collections.Generic.List[string]
	foreach ($line in $Lines) {
		if ($seen.Add($line)) {
			$result.Add($line)
		}
	}
	, $result
}

$addedLines = Get-DedupedLines -Lines $addedLines
$removedLines = Get-DedupedLines -Lines $removedLines
$breakingLines = Get-DedupedLines -Lines $breakingLines
$fixedLines = Get-DedupedLines -Lines $fixedLines
$refactoredLines = Get-DedupedLines -Lines $refactoredLines

$notes = New-Object System.Collections.Generic.List[string]
$notes.Add("## What's Changed")

if ($addedLines.Count -gt 0) {
	$notes.Add("### Added")
	$notes.AddRange($addedLines)
}

if ($removedLines.Count -gt 0) {
	$notes.Add("### Removed")
	$notes.AddRange($removedLines)
}

if ($breakingLines.Count -gt 0) {
	$notes.Add("### Breaking changes")
	$notes.AddRange($breakingLines)
}

if ($fixedLines.Count -gt 0) {
	$notes.Add("### Fixed")
	$notes.AddRange($fixedLines)
}

if ($refactoredLines.Count -gt 0) {
	$notes.Add("### Refactored")
	$notes.AddRange($refactoredLines)
}

# Detect first-time contributors within the range and link to their pull request
# %aN/%aE (rather than %an/%ae) are used so that mailmap-based identity mapping
# (e.g. when a contributor commits with a normal, non-GitHub-noreply email
# address that is mapped to their canonical identity via a .mailmap file) is
# taken into account when grouping commits by author.
$fieldSeparator = "`u{001F}"
$rawCommitInfos = git log @gitLogArgs --pretty=format:"%H$fieldSeparator%aN$fieldSeparator%aE$fieldSeparator%ad$fieldSeparator%s" --date=iso-strict
if (-not $rawCommitInfos) {
	$rawCommitInfos = @()
}

$commitInfos = @()
foreach ($line in $rawCommitInfos) {
	if (-not $line) {
		continue
	}
	$fields = $line -split $fieldSeparator
	if ($fields.Count -lt 5) {
		continue
	}
	$commitInfos += [PSCustomObject]@{
		Hash    = $fields[0]
		Author  = $fields[1]
		Email   = $fields[2]
		Date    = $fields[3]
		Subject = $fields[4]
	}
}

$prRegex = '\(#(\d+)\)\s*$'
$newContributorLines = New-Object System.Collections.Generic.List[string]

$commitsByAuthor = $commitInfos | Group-Object -Property Email
foreach ($authorGroup in $commitsByAuthor) {
	$firstInRange = $authorGroup.Group | Sort-Object -Property Date | Select-Object -First 1

	$priorCommits = git log --before="$($firstInRange.Date)" --pretty=format:"%H$fieldSeparator%aE" |
		Where-Object {
			$priorFields = $_ -split $fieldSeparator
			$priorFields[1] -eq $firstInRange.Email -and $priorFields[0] -ne $firstInRange.Hash
		}
	if ($priorCommits) {
		continue
	}

	$prMatch = [regex]::Match($firstInRange.Subject, $prRegex)
	if ($prMatch.Success) {
		$link = "$RepoUrl/pull/$($prMatch.Groups[1].Value)"
	} else {
		$link = "$RepoUrl/commit/$($firstInRange.Hash)"
	}

	$noreplyMatch = [regex]::Match($firstInRange.Email, '^(?:\d+\+)?(?<username>[^@]+)@users\.noreply\.github\.com$')
	if ($noreplyMatch.Success) {
		$displayName = $noreplyMatch.Groups["username"].Value
	} else {
		$displayName = $firstInRange.Author
	}

	$newContributorLines.Add("* @$displayName made their first contribution in $link")
}
$newContributorLines = Get-DedupedLines -Lines $newContributorLines

if ($newContributorLines.Count -gt 0) {
	$notes.Add("")
	$notes.Add("## New Contributors")
	$notes.AddRange($newContributorLines)
}

if ($LastTag) {
	$compareTo = if ($NewTag) { $NewTag } else { "master" }
	$notes.Add("")
	$notes.Add("**Full Changelog**: $RepoUrl/compare/$LastTag...$compareTo")
}

$notes -join "`n"
