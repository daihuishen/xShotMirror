param([string]$Version = '1.0.0')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$install = Join-Path $root 'artifacts\install-test'
$installer = Join-Path $root "dist\xShotMirror-$Version-win-x64-Setup.exe"
$registry = 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{3FFB1718-9BC4-4FC4-9580-55BF58EC7183}_is1'
$shortcut = Join-Path ([Environment]::GetFolderPath('CommonDesktopDirectory')) 'xShot Mirror.lnk'
if (Test-Path $registry) { throw 'A real installation exists. Use a clean test machine.' }
if (Test-Path $shortcut) { throw 'An existing desktop shortcut would be overwritten. Use a clean test machine.' }
if (Test-Path $install) { throw 'The test directory already exists. Inspect it before testing again.' }
if (Get-NetFirewallRule -DisplayName 'xShotMirror.3FFB1718.*' -ErrorAction SilentlyContinue) {
    throw 'Existing release firewall rules found. Use a clean test machine.'
}
$log = Join-Path $root 'artifacts\install-test.log'
$process = Start-Process $installer -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART',
    '/LANG=chinesesimp', ('/DIR="' + $install + '"'), '/GROUP="xShot Mirror Packaging Test"', ('/LOG="' + $log + '"')) `
    -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Installer failed: $($process.ExitCode). See $log" }
try {
    if (-not (Test-Path (Join-Path $install 'xShotMirror.exe'))) { throw 'Installed application missing.' }
    if (-not (Test-Path (Join-Path $install 'coreclr.dll'))) { throw 'Self-contained .NET runtime missing.' }
    if (-not (Test-Path $shortcut)) { throw 'Default desktop shortcut missing.' }
    $shell = New-Object -ComObject WScript.Shell
    if ($shell.CreateShortcut($shortcut).TargetPath -ine (Join-Path $install 'xShotMirror.exe')) { throw 'Shortcut target mismatch.' }
    $rules = @(Get-NetFirewallRule -DisplayName 'xShotMirror.3FFB1718.*')
    if ($rules.Count -ne 2) { throw 'Expected two firewall rules.' }
    foreach ($rule in $rules) {
        if ((Get-NetFirewallApplicationFilter -AssociatedNetFirewallRule $rule).Program -ine (Join-Path $install 'receiver\uxplay.exe')) { throw 'Firewall executable mismatch.' }
        if ((Get-NetFirewallAddressFilter -AssociatedNetFirewallRule $rule).RemoteAddress -ne 'LocalSubnet') { throw 'Firewall remote scope is too broad.' }
    }
    $manifest = Get-Content (Join-Path $install 'runtime-files.json') -Raw | ConvertFrom-Json
    foreach ($file in $manifest) {
        if ((Get-FileHash (Join-Path $install $file.file) -Algorithm SHA256).Hash -ine $file.sha256) { throw "Installed file mismatch: $($file.file)" }
    }
    Write-Host 'PASS: installation, self-contained runtime, default desktop shortcut, scoped firewall rules, and native file hashes.'
    # Repeat the same install to verify the upgrade/repair path does not duplicate rules.
    $process = Start-Process $installer -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', ('/DIR="' + $install + '"')) -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw 'Reinstall failed.' }
    if (@(Get-NetFirewallRule -DisplayName 'xShotMirror.3FFB1718.*').Count -ne 2) { throw 'Reinstall duplicated firewall rules.' }
    Write-Host 'PASS: reinstall.'
} finally {
    $uninstall = Join-Path $install 'unins000.exe'
    if (Test-Path $uninstall) {
        $process = Start-Process $uninstall -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART') -WindowStyle Hidden -Wait -PassThru
        if ($process.ExitCode -ne 0) { throw "Uninstall failed: $($process.ExitCode)" }
    }
}
if (Test-Path $shortcut) { throw 'Desktop shortcut remained after uninstall.' }
if (Test-Path $registry) { throw 'Uninstall registration remained.' }
if (Get-NetFirewallRule -DisplayName 'xShotMirror.3FFB1718.*' -ErrorAction SilentlyContinue) { throw 'Firewall rules remained after uninstall.' }
if (Test-Path (Join-Path $install 'xShotMirror.exe')) { throw 'Executable remained after uninstall.' }
Write-Host 'PASS: uninstall removed application, shortcut, registration, and firewall rules.'
