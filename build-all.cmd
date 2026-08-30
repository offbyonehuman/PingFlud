@echo off
setlocal
if defined DOTNET_EXE (set "DOTNET=%DOTNET_EXE%") else (set "DOTNET=dotnet")

"%DOTNET%" test PingFlud.sln -c Release --nologo || exit /b 1
if exist artifacts rmdir /s /q artifacts

for %%R in (win-x86 win-x64 win-arm64) do (
  for /f "tokens=2 delims=-" %%P in ("%%R") do (
    rem Compact build: requires matching .NET Desktop Runtime and Windows App Runtime.
    "%DOTNET%" publish src\PingFlud.WinUI\PingFlud.WinUI.csproj -c Release -r %%R -p:Platform=%%P -o artifacts\winui-compact\%%R || exit /b 1

    rem Portable build: compressed self-contained single executable.
    "%DOTNET%" publish src\PingFlud.WinUI\PingFlud.WinUI.csproj -c Release -r %%R -p:Platform=%%P -p:Portable=true -o artifacts\winui-portable\%%R || exit /b 1
  )
)

echo WinUI compact and portable publishes complete.
