$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root 'third_party\UxPlay'
$build = Join-Path $source 'build-bonjour'
$header = Join-Path $root 'third_party\bonjour-header'
$patches = @((Join-Path $root 'patches\uxplay-bonjour-no-import-lib.patch'))
$msysBin = 'C:\msys64\ucrt64\bin'

if (-not (Test-Path -LiteralPath (Join-Path $source 'CMakeLists.txt'))) {
    throw 'UxPlay source is missing from third_party/UxPlay.'
}
if (-not (Test-Path -LiteralPath (Join-Path $msysBin 'cmake.exe'))) {
    throw 'MSYS2 UCRT64 toolchain is missing. See README.md.'
}
if (-not (Test-Path -LiteralPath (Join-Path $header 'dns_sd.h'))) {
    throw 'Apple dns_sd.h header is missing. See README.md.'
}
$expectedHeaderHash = '5D0CA50F207F6EB02E09D743F9B65D2ADE65E8F81862DCD3703845B4FE87A9C1'
$actualHeaderHash = (Get-FileHash -LiteralPath (Join-Path $header 'dns_sd.h') -Algorithm SHA256).Hash
if ($actualHeaderHash -ne $expectedHeaderHash) {
    throw 'Apple dns_sd.h differs from the pinned upstream file. Check its origin and license before building.'
}
if (Get-Process -Name uxplay -ErrorAction SilentlyContinue) {
    throw 'UxPlay is running. Stop the receiver before rebuilding its executable.'
}

$env:PATH = "$msysBin;C:\msys64\usr\bin;$env:PATH"
foreach ($patch in $patches) {
    & git -c "safe.directory=$($source.Replace('\','/'))" -C $source apply --check $patch 2>$null
    if ($LASTEXITCODE -eq 0) {
        & git -c "safe.directory=$($source.Replace('\','/'))" -C $source apply $patch
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    } else {
        & git -c "safe.directory=$($source.Replace('\','/'))" -C $source apply --reverse --check $patch 2>$null
        if ($LASTEXITCODE -ne 0) { throw "UxPlay patch cannot be applied: $patch. Check source revision." }
    }
}
& (Join-Path $msysBin 'cmake.exe') -S $source -B $build -G Ninja -DCMAKE_BUILD_TYPE=Release -DUSE_DNS_SD=ON -DUSE_MDNS=OFF -DNO_MARCH_NATIVE=ON "-DDNSSD_INCLUDE_DIR=$($header.Replace('\','/'))"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& (Join-Path $msysBin 'cmake.exe') --build $build --parallel 4
exit $LASTEXITCODE
