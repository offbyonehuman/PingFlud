# Contributing

Thanks for taking the time to look at Ping Flud.

Please read the [Code of Conduct](CODE_OF_CONDUCT.md) before participating.

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

For release artifacts, stage or commit all intended source and notice files first, then use `build-all.cmd` and `python package_release.py`. The release script publishes compact and portable WinUI builds for x86, x64, and ARM64, validates the resolved NuGet notice graph, and rejects release packaging when required legal files are not tracked.

## Pull requests

Explain what changed and why. Include the test command you ran and call out anything you could not test locally. UI changes should include a short screen recording or screenshots when they affect layout or interaction.

## Source provenance and attribution

Only submit code, documentation, data, fonts, icons, or other assets that you created or are authorized to redistribute.

If a contribution copies or adapts material from another source, include the upstream project or URL, version or commit when known, original copyright holder, license, and the location of the retained notice. Mark material that has been modified. Do not copy code or assets that have no license permitting redistribution.

If generated content or code is included, disclose the generator or upstream source and review its terms before submitting it. Keep third-party notices separate from Ping Flud's own MIT-licensed source.

By submitting a contribution, you confirm that you have the necessary rights to submit it and agree that your contribution may be released under the repository's MIT license, subject to any clearly identified third-party terms.
