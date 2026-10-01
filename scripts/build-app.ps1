$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src\xShotMirror\xShotMirror.csproj'
$config = Join-Path $root 'NuGet.Config'

$env:DOTNET_CLI_HOME = Join-Path $root '.dotnet'
$env:NUGET_PACKAGES = Join-Path $root '.nuget\packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

& dotnet restore $project --configfile $config
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& dotnet build $project -c Release --no-restore
exit $LASTEXITCODE
