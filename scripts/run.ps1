$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$receiver = Join-Path $root 'third_party\UxPlay\build-bonjour\uxplay.exe'
$runtime = 'C:\msys64\ucrt64\bin'

if (-not (Test-Path -LiteralPath $receiver)) {
    throw 'Receiver binary is missing. Run scripts/build-uxplay.ps1 first.'
}
if (-not (Test-Path -LiteralPath (Join-Path $runtime 'libgstreamer-1.0-0.dll'))) {
    throw 'GStreamer runtime is missing from MSYS2 UCRT64. See README.md.'
}
if ((Get-Service -Name 'Bonjour Service' -ErrorAction SilentlyContinue).Status -ne 'Running') {
    throw 'Bonjour Service is not running. Start or install Bonjour before launching xShot Mirror.'
}

$env:PATH = "$runtime;C:\msys64\usr\bin;$env:PATH"
Write-Host 'xShot Mirror is starting. Open iPhone Control Center > Screen Mirroring.'
Write-Host 'The live picture appears in a separate native video window after connection.'
Write-Host 'The terminal may show a pairing PIN. Press Ctrl+C here to stop receiving.'
& $receiver -n 'xShot Mirror' -nh -as wasapisink -pin
exit $LASTEXITCODE
