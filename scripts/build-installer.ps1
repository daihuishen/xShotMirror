param(
    [ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version = '1.0.0',
    [string]$MsysRoot = 'C:\msys64',
    [string]$Iscc,
    [switch]$SkipSources
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$publish = Join-Path $root 'artifacts\publish'
$dotnet = Join-Path $root '.tools\dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = (Get-Command dotnet -ErrorAction Stop).Source }
if (-not $Iscc) { $Iscc = Join-Path $root '.tools\InnoSetup\ISCC.exe' }
if (-not (Test-Path $Iscc)) { throw 'Install Inno Setup 6.7+, then pass -Iscc <path to ISCC.exe>.' }
$sdkVersion = & $dotnet --version
if ($LASTEXITCODE -ne 0 -or [version]$sdkVersion -lt [version]'10.0.100') { throw '.NET SDK 10 or later is required.' }
if (Test-Path $publish) {
    $resolved = (Resolve-Path $publish).Path
    $expected = [IO.Path]::GetFullPath((Join-Path $root 'artifacts\publish'))
    if ($resolved -ine $expected) { throw 'Refusing to clean an unexpected publish directory.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
$env:DOTNET_CLI_HOME = Join-Path $root '.dotnet'
$env:NUGET_PACKAGES = Join-Path $root '.nuget\packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
& (Join-Path $PSScriptRoot 'build-uxplay.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Receiver build failed.' }
& $dotnet publish (Join-Path $root 'src\xShotMirror\xShotMirror.csproj') -c Release -r win-x64 `
    --self-contained true -o $publish "-p:Version=$Version" -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { throw 'Application publish failed.' }
& python (Join-Path $PSScriptRoot 'collect-runtime.py') --msys $MsysRoot --output $publish `
    --receiver (Join-Path $root 'third_party\UxPlay\build-bonjour\uxplay.exe')
if ($LASTEXITCODE -ne 0) { throw 'Runtime collection failed.' }
Copy-Item (Join-Path $root 'LICENSE') $publish
foreach ($notice in @('THIRD_PARTY_NOTICES.md', 'DISTRIBUTION.md')) {
    if (Test-Path (Join-Path $root $notice)) { Copy-Item (Join-Path $root $notice) $publish }
}
$guide = (Get-Content (Join-Path $root 'installer\user-guide.txt') -Raw).Replace('1.0.0', $Version)
Set-Content (Join-Path $publish '使用说明.txt') $guide -Encoding utf8BOM
$englishGuide = (Get-Content (Join-Path $root 'installer\user-guide.en.txt') -Raw).Replace('1.0.0', $Version)
Set-Content (Join-Path $publish 'User Guide.txt') $englishGuide -Encoding utf8BOM
foreach ($name in @('dotnet', 'UxPlay', 'bonjour-header', 'InnoSetup')) {
    New-Item -ItemType Directory -Force (Join-Path $publish "licenses\$name") | Out-Null
}
$runtimeVersion = (Get-Content (Join-Path $publish 'xShotMirror.runtimeconfig.json') -Raw | ConvertFrom-Json).runtimeOptions.includedFrameworks |
    Where-Object name -eq 'Microsoft.NETCore.App' | Select-Object -ExpandProperty version
$runtimePackage = Join-Path $env:NUGET_PACKAGES "microsoft.netcore.app.runtime.win-x64\$runtimeVersion"
$desktopPackage = Join-Path $env:NUGET_PACKAGES "microsoft.windowsdesktop.app.runtime.win-x64\$runtimeVersion"
foreach ($package in @($runtimePackage, $desktopPackage)) {
    if (-not (Test-Path $package)) { throw "Runtime package notices unavailable: $package" }
    $dest = Join-Path $publish ('licenses\dotnet\' + (Split-Path (Split-Path $package -Parent) -Leaf))
    New-Item -ItemType Directory -Force $dest | Out-Null
    Get-ChildItem $package -File | Where-Object Name -Match 'LICENSE|THIRD.?PARTY.?NOTICES' | Copy-Item -Destination $dest
}
Copy-Item (Join-Path $root 'third_party\UxPlay\LICENSE') (Join-Path $publish 'licenses\UxPlay')
Copy-Item (Join-Path $root 'third_party\bonjour-header\dns_sd.h') (Join-Path $publish 'licenses\bonjour-header')
Copy-Item (Join-Path $root 'installer\InnoSetup-LICENSE.txt') (Join-Path $publish 'licenses\InnoSetup')
$receiverSource = Join-Path $root 'third_party\UxPlay'
$receiverCommit = & git -c "safe.directory=$($receiverSource.Replace('\','/'))" -C $receiverSource rev-parse HEAD
@{ version=$Version; sdk=$sdkVersion; runtime=$runtimeVersion; architecture='win-x64';
   uxplay_commit=$receiverCommit; uxplay_patch='patches/uxplay-bonjour-no-import-lib.patch';
   decoder='avdec_h264'; video_sink='d3d11videosink'; signed=$false } |
    ConvertTo-Json | Set-Content (Join-Path $publish 'release-build.json') -Encoding utf8
if (-not $SkipSources) {
    & python (Join-Path $PSScriptRoot 'package-sources.py') --version $Version --publish $publish
    if ($LASTEXITCODE -ne 0) { throw 'Corresponding-source packaging failed; no release installer was compiled.' }
    & python (Join-Path $PSScriptRoot 'collect-source-notices.py') --publish $publish --msys $MsysRoot
    if ($LASTEXITCODE -ne 0) { throw 'Third-party notice collection failed.' }
}
& $Iscc "/DAppVersion=$Version" "/DPublishDir=$publish" (Join-Path $root 'installer\xShotMirror.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
$files = @(Join-Path $root "dist\xShotMirror-$Version-win-x64-Setup.exe")
if (-not $SkipSources) { $files += Join-Path $root "dist\xShotMirror-$Version-sources.zip" }
$lines = foreach ($file in $files) { '{0}  {1}' -f (Get-FileHash $file -Algorithm SHA256).Hash.ToLowerInvariant(), (Split-Path $file -Leaf) }
$lines | Set-Content (Join-Path $root 'dist\SHA256SUMS.txt') -Encoding ascii
if ($SkipSources) { Write-Warning 'LOCAL TEST BUILD: source archive was skipped. Do not publish without matching sources.' }
Write-Host "Installer and SHA-256 checksums are in $root\dist"
