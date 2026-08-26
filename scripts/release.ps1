#!/usr/bin/env pwsh

# OSDP-Bench Release Script
# Creates a version tag to trigger CI release pipeline and opens a matching
# draft release on GitHub

param(
    [string]$Version,
    [string]$NotesFile
)

Write-Host "OSDP-Bench Release Process" -ForegroundColor Cyan
Write-Host "==========================" -ForegroundColor Cyan
Write-Host ""

# Check the GitHub CLI before anything is changed, so a missing tool costs nothing
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Host "Error: GitHub CLI (gh) is not installed. Install it from https://cli.github.com/" -ForegroundColor Red
    exit 1
}

gh auth status *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: GitHub CLI is not authenticated. Run 'gh auth login' first." -ForegroundColor Red
    exit 1
}

if ($NotesFile -and -not (Test-Path $NotesFile)) {
    Write-Host "Error: Release notes file not found: $NotesFile" -ForegroundColor Red
    exit 1
}

# Ensure we have latest changes
Write-Host "Fetching latest changes..." -ForegroundColor Yellow
git fetch --all

# Check if there are uncommitted changes
$uncommittedChanges = git status -s
if ($uncommittedChanges) {
    Write-Host "Error: You have uncommitted changes. Please commit or stash them before releasing." -ForegroundColor Red
    exit 1
}

# Ensure we're on main branch
$currentBranch = git rev-parse --abbrev-ref HEAD
if ($currentBranch -ne "main") {
    Write-Host "Error: You must be on the main branch to release. Currently on: $currentBranch" -ForegroundColor Red
    exit 1
}

# Pull latest main
Write-Host "Updating main branch..." -ForegroundColor Yellow
git pull origin main

# Get current version from Directory.Build.props
$propsFile = "Directory.Build.props"
$propsContent = Get-Content $propsFile -Raw
if ($propsContent -match '<VersionPrefix>(.*?)</VersionPrefix>') {
    $currentVersion = $matches[1]
    Write-Host "Current version: $currentVersion" -ForegroundColor Green
} else {
    Write-Host "Error: Could not find VersionPrefix in $propsFile" -ForegroundColor Red
    exit 1
}

# Prompt for new version if not provided
if (-not $Version) {
    Write-Host ""
    $Version = Read-Host "Enter new version (e.g., 3.0.14)"
}

# Validate version format (semantic versioning)
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Write-Host "Error: Invalid version format. Use semantic versioning (e.g., 1.2.3)" -ForegroundColor Red
    exit 1
}

# Check if version is different from current
if ($Version -eq $currentVersion) {
    Write-Host "Error: New version must be different from current version ($currentVersion)" -ForegroundColor Red
    exit 1
}

# Check if tag already exists
$existingTag = git tag -l "v$Version"
if ($existingTag) {
    Write-Host "Error: Tag v$Version already exists" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Release Summary:" -ForegroundColor Green
Write-Host "  Current version: $currentVersion"
Write-Host "  New version:     $Version"
Write-Host "  Tag to create:   v$Version"
Write-Host ""

$confirm = Read-Host "Do you want to proceed with the release? (y/n)"
if ($confirm -ne "y") {
    Write-Host "Release cancelled." -ForegroundColor Yellow
    exit 0
}

# Write the release notes before anything is pushed, so backing out stays cheap
$previousTag = git describe --tags --abbrev=0
$repositoryUrl = (git remote get-url origin) -replace '\.git$', ''

if ($NotesFile) {
    $notesSource = $NotesFile
} else {
    $notesSource = Join-Path ([System.IO.Path]::GetTempPath()) "osdp-bench-release-v$Version.md"
    $commits = git log --pretty=format:'- %s' "$previousTag..HEAD"
    $template = @"
## OSDP-Bench $Version

<!--
Write the release notes above and below, then save the file and close the editor.
Anything inside an HTML comment is stripped before the release is created.

Lead with a short paragraph on what this release is about, then group the details
under headings such as "### New", "### Changes" or "### Fixes". Write for someone
using OSDP-Bench, not for someone reading the commit log.

Style reference: https://github.com/Z-bit-Systems-LLC/OSDP.Net/releases

The commits since $previousTag are listed below as a starting point. Rewrite them
into user-facing notes and delete the ones that do not matter to a user.
-->

### Changes

$($commits -join "`n")

**Full changelog:** $repositoryUrl/compare/$previousTag...v$Version
"@
    Set-Content -Path $notesSource -Value $template

    # Split the configured editor into an executable and its arguments and start it
    # with -Wait, because PowerShell does not wait for a GUI editor on its own
    $editor = (git var GIT_EDITOR) -replace '\\\\', '\'
    if ($editor -match '^\s*"([^"]+)"\s*(.*)$' -or $editor -match '^\s*(\S+)\s*(.*)$') {
        $editorExe = $matches[1]
        $editorArgs = @($matches[2].Split(' ') | Where-Object { $_ }) + $notesSource
    } else {
        $editorExe = $editor
        $editorArgs = @($notesSource)
    }

    Write-Host ""
    Write-Host "Opening the release notes in $editorExe..." -ForegroundColor Yellow
    Write-Host "  $notesSource"
    try {
        Start-Process -FilePath $editorExe -ArgumentList $editorArgs -Wait -ErrorAction Stop
    } catch {
        Write-Host "Could not start the editor. Edit the file listed above by hand." -ForegroundColor Yellow
    }
    Read-Host "Press Enter once the notes are saved" | Out-Null
}

# Strip the instructions and keep the result in its own file, so a supplied
# notes file is never rewritten
$notes = ((Get-Content $notesSource -Raw) -replace '(?s)<!--.*?-->', '') -replace '(\r?\n){3,}', "`n`n"
$notes = $notes.Trim()
if (-not $notes) {
    Write-Host "Error: The release notes are empty. Release cancelled." -ForegroundColor Red
    exit 1
}

$notesPath = Join-Path ([System.IO.Path]::GetTempPath()) "osdp-bench-release-v$Version-final.md"
Set-Content -Path $notesPath -Value $notes -NoNewline

Write-Host ""
Write-Host "Release notes:" -ForegroundColor Green
Write-Host "----------------------------------------"
Write-Host $notes
Write-Host "----------------------------------------"
Write-Host ""

$confirmNotes = Read-Host "Release with these notes? (y/n)"
if ($confirmNotes -ne "y") {
    Write-Host "Release cancelled. Notes kept at $notesSource" -ForegroundColor Yellow
    exit 0
}

# Update version in Directory.Build.props
Write-Host "Updating version in $propsFile..." -ForegroundColor Yellow
$propsContent = $propsContent -replace '<VersionPrefix>.*?</VersionPrefix>', "<VersionPrefix>$Version</VersionPrefix>"
Set-Content -Path $propsFile -Value $propsContent -NoNewline

# Commit version change
Write-Host "Committing version change..." -ForegroundColor Yellow
git add $propsFile
git commit -m "Bump version to $Version"

# Create version tag
Write-Host "Creating tag v$Version..." -ForegroundColor Yellow
git tag "v$Version"

# Push commit and tag
Write-Host "Pushing to remote..." -ForegroundColor Yellow
git push origin main
git push origin "v$Version"

# Open the draft release for the tag that was just pushed
Write-Host "Creating draft GitHub release..." -ForegroundColor Yellow
gh release create "v$Version" --draft --verify-tag --title "v$Version" --notes-file $notesPath
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Error: The tag was pushed but the GitHub release could not be created." -ForegroundColor Red
    Write-Host "Retry with:" -ForegroundColor Yellow
    Write-Host "  gh release create v$Version --draft --verify-tag --title v$Version --notes-file `"$notesPath`"" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Release process completed successfully!" -ForegroundColor Green
Write-Host "The CI pipeline will automatically:" -ForegroundColor Green
Write-Host "  1. Run build and tests" -ForegroundColor Green
Write-Host "  2. Run code inspection" -ForegroundColor Green
Write-Host "  3. Run the full UI test suite" -ForegroundColor Green
Write-Host ""
Write-Host "The GitHub release is a draft. Review it and publish when the pipeline is green:" -ForegroundColor Green
Write-Host "  $repositoryUrl/releases" -ForegroundColor Green
Write-Host ""
Write-Host "You can monitor the release progress in Azure DevOps." -ForegroundColor Green
