$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$app = Join-Path $root 'src\xShotMirror\bin\Release\net10.0-windows\xShotMirror.exe'

if (-not (Test-Path -LiteralPath $app)) {
    throw 'Desktop app is missing. Run scripts/build-app.ps1 first.'
}
$localDotnet = Join-Path $root '.tools\dotnet\dotnet.exe'
if (Test-Path -LiteralPath $localDotnet) {
    & $localDotnet ([IO.Path]::ChangeExtension($app, '.dll'))
} else {
    & $app
}
exit $LASTEXITCODE
