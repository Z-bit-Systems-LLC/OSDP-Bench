#!/bin/bash

# OSDP-Bench Release Script
# Merges develop into main to trigger CI version bump and release

echo "OSDP-Bench Release Process"
echo "=========================="
echo ""

# Ensure we have latest changes
echo "Fetching latest changes..."
git fetch --all

# Check if there are uncommitted changes
if [[ -n $(git status -s) ]]; then
  echo "Error: You have uncommitted changes. Please commit or stash them before releasing."
  exit 1
fi

# Ensure we're on develop branch
CURRENT_BRANCH=$(git rev-parse --abbrev-ref HEAD)
if [[ "$CURRENT_BRANCH" != "develop" ]]; then
  echo "Error: You must be on the develop branch to release. Currently on: $CURRENT_BRANCH"
  exit 1
fi

# Pull latest develop
echo "Updating develop branch..."
git pull origin develop

# Check if develop is ahead of main
AHEAD_COUNT=$(git rev-list --count origin/main..origin/develop)
if [[ "$AHEAD_COUNT" -eq 0 ]]; then
  echo "Error: develop branch is not ahead of main. Nothing to release."
  exit 1
fi

echo ""
echo "Changes to be released:"
git log --oneline --no-merges origin/main..origin/develop

echo ""
read -p "Do you want to proceed with the release? (y/n) " CONFIRM
if [[ "$CONFIRM" != "y" ]]; then
  echo "Release cancelled."
  exit 0
fi

# Checkout main
echo "Checking out main branch..."
git checkout main

# Pull latest main
echo "Updating main branch..."
git pull origin main

# Merge develop
echo "Merging develop into main..."
git merge --no-ff develop -m "Release: Merge develop into main for automated release"

# Push to remote
echo "Pushing to remote..."
git push origin main

# Switch back to develop
echo "Switching back to develop branch..."
git checkout develop

echo ""
echo "Release process completed successfully!"
echo "The CI pipeline will automatically:"
echo "1. Run tests"
echo "2. Bump version number"
echo "3. Create a tagged release"
echo ""
echo "You can monitor the release progress in Azure DevOps."