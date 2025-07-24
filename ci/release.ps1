#!/usr/bin/env pwsh

# OSDP-Bench Release Script
# Merges develop into main to trigger CI version bump and release

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

# Ensure we're on develop branch
$currentBranch = git rev-parse --abbrev-ref HEAD
if ($currentBranch -ne "develop") {
    Write-Host "Error: You must be on the develop branch to release. Currently on: $currentBranch" -ForegroundColor Red
    exit 1
}

# Pull latest develop
Write-Host "Updating develop branch..." -ForegroundColor Yellow
git pull origin develop

# Check if develop is ahead of main
$aheadCount = git rev-list --count origin/main..origin/develop
if ($aheadCount -eq 0) {
    Write-Host "Error: develop branch is not ahead of main. Nothing to release." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Changes to be released:" -ForegroundColor Green
git log --oneline --no-merges origin/main..origin/develop

Write-Host ""
$confirm = Read-Host "Do you want to proceed with the release? (y/n)"
if ($confirm -ne "y") {
    Write-Host "Release cancelled." -ForegroundColor Yellow
    exit 0
}

# Checkout main
Write-Host "Checking out main branch..." -ForegroundColor Yellow
git checkout main

# Pull latest main
Write-Host "Updating main branch..." -ForegroundColor Yellow
git pull origin main

# Merge develop
Write-Host "Merging develop into main..." -ForegroundColor Yellow
git merge --no-ff develop -m "Release: Merge develop into main for automated release"

# Push to remote
Write-Host "Pushing to remote..." -ForegroundColor Yellow
git push origin main

# Switch back to develop
Write-Host "Switching back to develop branch..." -ForegroundColor Yellow
git checkout develop

Write-Host ""
Write-Host "Release process completed successfully!" -ForegroundColor Green
Write-Host "The CI pipeline will automatically:" -ForegroundColor Green
Write-Host "1. Run tests" -ForegroundColor Green
Write-Host "2. Bump version number" -ForegroundColor Green
Write-Host "3. Create a tagged release" -ForegroundColor Green
Write-Host ""
Write-Host "You can monitor the release progress in Azure DevOps." -ForegroundColor Green