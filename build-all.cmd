@echo off
setlocal
if defined DOTNET_EXE (set "DOTNET=%DOTNET_EXE%") else (set "DOTNET=dotnet")

"%DOTNET%" test PingFlud.sln -c Release --nologo || exit /b 1
if exist artifacts rmdir /s /q artifacts

for %%R in (win-x86 win-x64 win-arm64) do (
  rem AV-friendly portable build: normal, unpacked .NET runtime files.
  "%DOTNET%" publish src\PingFlud.App\PingFlud.App.csproj -c Release -r %%R --self-contained true -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false -p:PublishReadyToRun=false -o artifacts\portable\%%R || exit /b 1

  rem Small build: requires the matching .NET 8 Desktop Runtime.
  "%DOTNET%" publish src\PingFlud.App\PingFlud.App.csproj -c Release -r %%R --self-contained false -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false -p:PublishReadyToRun=false -o artifacts\lite\%%R || exit /b 1
)

echo AV-friendly portable and lite publishes complete.
