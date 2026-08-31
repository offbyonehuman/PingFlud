# Contributing

Thanks for taking the time to look at Ping Flud.

## Before you start

- Open an issue for a bug or a change that affects the user interface.
- Do not include real private-network addresses, host names, scan results, or other sensitive data in issues or pull requests.
- Keep changes focused. A small pull request is easier to review and backport.

## Local checks

On Windows with the .NET 8 SDK installed:

```bat
dotnet restore PingFlud.sln
dotnet test PingFlud.sln -c Release
dotnet build PingFlud.sln -c Release --no-restore --nologo
```

For release artifacts, use `build-all.cmd` and then `python package_release.py`. The release script publishes compact and portable WinUI builds for x86, x64, and ARM64.

## Pull requests

Explain what changed and why. Include the test command you ran and call out anything you could not test locally. UI changes should include a short screen recording or screenshots when they affect layout or interaction.

By submitting a contribution, you agree that it will be released under the repository's MIT license.
