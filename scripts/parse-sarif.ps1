# Parse SARIF code inspection results and display issues
# Usage: pwsh scripts/parse-sarif.ps1 [-WarningsOnly]

param(
    [switch]$WarningsOnly
)

$sarifPath = "results.sarif"

if (-not (Test-Path $sarifPath)) {
    Write-Error "SARIF file not found. Run 'jb inspectcode OSDP-Bench.sln -o=results.sarif' first."
    exit 1
}

$sarif = Get-Content $sarifPath | ConvertFrom-Json
$results = $sarif.runs[0].results

if ($WarningsOnly) {
    $results = $results | Where-Object { $_.level -eq 'warning' }
}

$warningCount = ($results | Where-Object { $_.level -eq 'warning' }).Count
$noteCount = ($results | Where-Object { $_.level -eq 'note' }).Count

foreach ($result in $results) {
    $level = $result.level
    $message = $result.message.text
    $file = $result.locations[0].physicalLocation.artifactLocation.uri -replace 'file:///', ''
    $line = $result.locations[0].physicalLocation.region.startLine
    Write-Host "$level : $($file):$line - $message"
}

Write-Host ""
Write-Host "Summary: $warningCount warning(s), $noteCount note(s)"

if ($warningCount -gt 0) {
    exit 1
}
