param([string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
& $Dotnet test PingFlud.sln -c Release --nologo
if ($LASTEXITCODE) { exit $LASTEXITCODE }
if (Test-Path artifacts) { Remove-Item artifacts -Recurse -Force }

foreach ($rid in 'win-x86', 'win-x64', 'win-arm64') {
    # Portable build: self-contained single-file executable (compressed, ReadyToRun).
    & $Dotnet publish src/PingFlud.App/PingFlud.App.csproj -c Release -r $rid --self-contained true -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -p:PublishReadyToRun=true -o "artifacts/portable/$rid"
    if ($LASTEXITCODE) { exit $LASTEXITCODE }

    # Small build: requires the matching .NET 8 Desktop Runtime.
    & $Dotnet publish src/PingFlud.App/PingFlud.App.csproj -c Release -r $rid --self-contained false -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -p:PublishReadyToRun=true -o "artifacts/lite/$rid"
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
}
Write-Host 'Single-file portable and lite publishes complete.'
