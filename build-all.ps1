param([string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
& $Dotnet test PingFlud.sln -c Release --nologo
if ($LASTEXITCODE) { exit $LASTEXITCODE }
if (Test-Path artifacts) { Remove-Item artifacts -Recurse -Force }

foreach ($rid in 'win-x86', 'win-x64', 'win-arm64') {
    $platform = $rid.Substring(4)

    # Compact build: requires matching .NET Desktop Runtime and Windows App Runtime.
    & $Dotnet publish src/PingFlud.WinUI/PingFlud.WinUI.csproj -c Release -r $rid "-p:Platform=$platform" -o "artifacts/winui-compact/$rid"
    if ($LASTEXITCODE) { exit $LASTEXITCODE }

    # Portable build: compressed self-contained single executable.
    & $Dotnet publish src/PingFlud.WinUI/PingFlud.WinUI.csproj -c Release -r $rid "-p:Platform=$platform" -p:Portable=true -o "artifacts/winui-portable/$rid"
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
}
Write-Host 'WinUI compact and portable publishes complete.'
