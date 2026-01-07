#!/usr/bin/env pwsh

# OSDP-Bench Release Script
# Creates a version tag to trigger CI release pipeline

param(
    [string]$Version
)

Write-Host "OSDP-Bench Release Process" -ForegroundColor Cyan
Write-Host "==========================" -ForegroundColor Cyan
Write-Host ""

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

Write-Host ""
Write-Host "Release process completed successfully!" -ForegroundColor Green
Write-Host "The CI pipeline will automatically:" -ForegroundColor Green
Write-Host "  1. Run build and tests" -ForegroundColor Green
Write-Host "  2. Run code inspection" -ForegroundColor Green
Write-Host "  3. Create Windows binaries (x64 and ARM64)" -ForegroundColor Green
Write-Host "  4. Create NuGet package" -ForegroundColor Green
Write-Host ""
Write-Host "You can monitor the release progress in Azure DevOps." -ForegroundColor Green
