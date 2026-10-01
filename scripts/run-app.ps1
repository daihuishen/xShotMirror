$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$app = Join-Path $root 'src\xShotMirror\bin\Release\net6.0-windows\xShotMirror.exe'

if (-not (Test-Path -LiteralPath $app)) {
    throw 'Desktop app is missing. Run scripts/build-app.ps1 first.'
}
& $app
exit $LASTEXITCODE
