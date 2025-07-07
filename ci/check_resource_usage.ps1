# OSDP-Bench Resource Usage Checker (With Progress Indicator)

Write-Host "=== OSDP-Bench Resource Usage Analysis ===" -ForegroundColor Cyan
Write-Host

$ResourceFile = "src/Core/Resources/Resources.resx"

# Phase 1: Extract resource keys
Write-Host "Phase 1: Extracting resource keys from Resources.resx..." -ForegroundColor Yellow

if (-not (Test-Path $ResourceFile)) {
    Write-Host "Error: Resources.resx not found at $ResourceFile" -ForegroundColor Red
    exit 1
}

[xml]$resourceXml = Get-Content $ResourceFile
$resourceKeys = $resourceXml.root.data | Where-Object { $_.name } | Select-Object -ExpandProperty name | Sort-Object

Write-Host "Found $($resourceKeys.Count) resource keys" -ForegroundColor Green
Write-Host

# Phase 2: Check usage with progress
Write-Host "Phase 2: Checking usage of each resource key..." -ForegroundColor Yellow

$usedKeys = @()
$unusedKeys = @()
$total = $resourceKeys.Count
$current = 0

# Check if ripgrep is available for faster processing
$rgAvailable = $null -ne (Get-Command "rg" -ErrorAction SilentlyContinue)
if ($rgAvailable) {
    Write-Host "Using ripgrep for fast searching..." -ForegroundColor Gray
} else {
    Write-Host "Using PowerShell Select-String (slower fallback)..." -ForegroundColor Gray
}

foreach ($key in $resourceKeys) {
    $current++
    $percentComplete = [math]::Round(($current / $total) * 100, 1)
    
    # Update progress bar
    Write-Progress -Activity "Checking Resource Usage" -Status "Processing $key ($current of $total)" -PercentComplete $percentComplete
    
    $found = $false
    
    if ($rgAvailable) {
        # Use ripgrep patterns - simplified for speed
        try {
            & rg -q $key "src" "test" --glob="*.cs" --glob="*.xaml" 2>$null
            if ($LASTEXITCODE -eq 0) {
                $found = $true
            }
        }
        catch {
            # Continue if rg fails
        }
    }
    else {
        # Fallback to Select-String
        $files = Get-ChildItem -Path "src", "test" -Recurse -Include "*.cs", "*.xaml" -ErrorAction SilentlyContinue
        
        if ($files | Select-String -Pattern $key -Quiet) {
            $found = $true
        }
    }
    
    if ($found) {
        $usedKeys += $key
        Write-Host "✓ $key" -ForegroundColor Green
    }
    else {
        $unusedKeys += $key
        Write-Host "✗ $key" -ForegroundColor Red
    }
}

# Clear progress bar
Write-Progress -Activity "Checking Resource Usage" -Completed

Write-Host
Write-Host "=== SUMMARY ===" -ForegroundColor Cyan
Write-Host "Used resource keys: $($usedKeys.Count)" -ForegroundColor Green
Write-Host "Unused resource keys: $($unusedKeys.Count)" -ForegroundColor Red

if ($unusedKeys.Count -gt 0) {
    Write-Host
    Write-Host "=== UNUSED RESOURCE KEYS ===" -ForegroundColor Yellow
    
    foreach ($key in $unusedKeys) {
        $dataNode = $resourceXml.root.data | Where-Object { $_.name -eq $key }
        $value = if ($dataNode.value) { $dataNode.value } else { "" }
        $comment = if ($dataNode.comment) { $dataNode.comment } else { "" }
        
        Write-Host $key -ForegroundColor Red
        Write-Host "  Value: `"$value`""
        if ($comment) {
            Write-Host "  Comment: $comment"
        }
        Write-Host
    }
    
    Write-Host "Consider removing these unused resource strings to clean up the codebase." -ForegroundColor Yellow
}
else {
    Write-Host "All resource strings are being used! 🎉" -ForegroundColor Green
}

Write-Host

# Phase 3: Check for missing resource definitions
Write-Host "Phase 3: Checking for resource lookups without definitions..." -ForegroundColor Yellow

$missingKeys = @()
$missingDetails = @()

# Get all source files
$allFiles = Get-ChildItem -Path "src", "test" -Recurse -Include "*.cs", "*.xaml" -ErrorAction SilentlyContinue

Write-Host "Found $($allFiles.Count) source files to scan..." -ForegroundColor Gray
Write-Progress -Activity "Phase 3: Scanning for Missing Definitions" -Status "Initializing scan..." -PercentComplete 0

$fileCount = 0
$totalFiles = $allFiles.Count

foreach ($file in $allFiles) {
    $fileCount++
    $percentComplete = [math]::Round(($fileCount / $totalFiles) * 100, 1)
    
    Write-Progress -Activity "Phase 3: Scanning for Missing Definitions" -Status "Scanning $($file.Name) ($fileCount of $totalFiles) - $($percentComplete)%" -PercentComplete $percentComplete
    
    # Show periodic status updates
    if ($fileCount % 10 -eq 0 -or $fileCount -eq $totalFiles) {
        Write-Host "  Processed $fileCount of $totalFiles files..." -ForegroundColor Gray
    }
    
    try {
        $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
        if (-not $content) { continue }
        
        # Pattern 1: {markup:Localize KeyName}
        $matches = [regex]::Matches($content, '\{markup:Localize\s+([^}]+)\}')
        foreach ($match in $matches) {
            $key = $match.Groups[1].Value.Trim()
            if ($key -notin $resourceKeys -and $key -notin $missingKeys) {
                $missingKeys += $key
                $missingDetails += @{
                    Key = $key
                    File = $file.Name
                    Pattern = "{markup:Localize $key}"
                }
            }
        }
        
        # Pattern 2: GetString("KeyName")
        $matches = [regex]::Matches($content, 'GetString\s*\(\s*"([^"]+)"\s*\)')
        foreach ($match in $matches) {
            $key = $match.Groups[1].Value
            if ($key -notin $resourceKeys -and $key -notin $missingKeys) {
                $missingKeys += $key
                $missingDetails += @{
                    Key = $key
                    File = $file.Name
                    Pattern = "GetString(`"$key`")"
                }
            }
        }
        
        # Pattern 3: Resources.KeyName (exclude common properties)
        $matches = [regex]::Matches($content, 'Resources\.([A-Za-z_][A-Za-z0-9_]*)')
        foreach ($match in $matches) {
            $key = $match.Groups[1].Value
            if ($key -notin @("ResourceManager", "Culture", "GetString", "Resources") -and 
                $key -notin $resourceKeys -and $key -notin $missingKeys) {
                $missingKeys += $key
                $missingDetails += @{
                    Key = $key
                    File = $file.Name
                    Pattern = "Resources.$key"
                }
            }
        }
    }
    catch {
        # Skip files that can't be processed
        continue
    }
}

# Clear progress bar
Write-Progress -Activity "Phase 3: Scanning for Missing Definitions" -Completed
Write-Host "Completed scanning $totalFiles files for missing definitions." -ForegroundColor Gray

# Filter out common false positives
$actualMissingKeys = $missingKeys | Where-Object { 
    $_ -notin @("AssemblyAssociatedContentFileAttribute", "GetString", "Resources") -and
    $_ -ne ""
} | Sort-Object -Unique

if ($actualMissingKeys.Count -gt 0) {
    Write-Host
    Write-Host "=== MISSING RESOURCE DEFINITIONS ===" -ForegroundColor Yellow
    Write-Host "Found $($actualMissingKeys.Count) resource lookups without definitions:" -ForegroundColor Red
    Write-Host
    
    foreach ($key in $actualMissingKeys) {
        Write-Host "Missing: $key" -ForegroundColor Red
        # Show where it's referenced
        $keyDetails = $missingDetails | Where-Object { $_.Key -eq $key }
        foreach ($detail in $keyDetails) {
            Write-Host "  Used in: $($detail.File)" -ForegroundColor Blue -NoNewline
            Write-Host " as $($detail.Pattern)"
        }
        Write-Host
    }
    
    Write-Host "These resource keys are referenced in code but not defined in Resources.resx" -ForegroundColor Yellow
}
else {
    Write-Host "All resource lookups have corresponding definitions! ✓" -ForegroundColor Green
}

Write-Host
Write-Host "=== FINAL SUMMARY ===" -ForegroundColor Cyan
Write-Host "Used resource keys: $($usedKeys.Count)" -ForegroundColor Green
Write-Host "Unused resource keys: $($unusedKeys.Count)" -ForegroundColor Red
Write-Host "Missing definitions: $($actualMissingKeys.Count)" -ForegroundColor Red

if ($unusedKeys.Count -gt 0 -or $actualMissingKeys.Count -gt 0) {
    Write-Host
    Write-Host "⚠️  Resource issues found. Consider cleaning up unused resources and adding missing definitions." -ForegroundColor Yellow
}
else {
    Write-Host
    Write-Host "✅ All resource strings are properly managed!" -ForegroundColor Green
}

Write-Host
Write-Host "Analysis complete." -ForegroundColor Cyan