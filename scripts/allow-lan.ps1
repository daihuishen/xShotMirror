$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$receiver = Join-Path $root 'third_party\UxPlay\build-bonjour\uxplay.exe'
$oldReceiver = Join-Path $root 'third_party\UxPlay\build\uxplay.exe'

if (-not (Test-Path -LiteralPath $receiver)) {
    throw 'Receiver binary is missing. Build UxPlay first.'
}

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Open PowerShell as Administrator to add the LAN-only firewall rules.'
}

foreach ($protocol in @('TCP', 'UDP')) {
    $name = "xShot Mirror (LAN $protocol)"
    $existing = Get-NetFirewallRule -DisplayName $name -ErrorAction SilentlyContinue
    if ($existing) {
        $existingProgram = (Get-NetFirewallApplicationFilter -AssociatedNetFirewallRule $existing).Program
        if ($existingProgram -ine $receiver) {
            if ($existingProgram -ieq $oldReceiver) {
                Get-NetFirewallApplicationFilter -AssociatedNetFirewallRule $existing |
                    Set-NetFirewallApplicationFilter -Program $receiver | Out-Null
                Write-Host "Updated $name for the Bonjour build."
                continue
            }
            throw "An existing rule named $name points to $existingProgram. No rule was changed."
        }
        Write-Host "$name already exists."
        continue
    }
    New-NetFirewallRule -DisplayName $name -Direction Inbound -Action Allow `
        -Program $receiver -Protocol $protocol -Profile Public,Private `
        -RemoteAddress LocalSubnet | Out-Null
    Write-Host "Added $name, limited to LocalSubnet."
}
