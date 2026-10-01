$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$target = Join-Path $root 'third_party\UxPlay'
$commit = '1ad348a890f48e4178099a697445a02e920af259'

if (Test-Path -LiteralPath $target) {
    if (-not (Test-Path -LiteralPath (Join-Path $target '.git'))) {
        throw "Existing directory is not a Git checkout: $target"
    }
    $current = git -c "safe.directory=$target" -C $target rev-parse HEAD
    if ($LASTEXITCODE -ne 0 -or $current -ne $commit) {
        throw "Existing UxPlay checkout has commit $current, expected $commit. No files were changed."
    }
    Write-Host "UxPlay source is already at $commit"
    exit 0
}

New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
git clone https://github.com/FDH2/UxPlay.git $target
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
git -c "safe.directory=$target" -C $target checkout --detach $commit
exit $LASTEXITCODE
