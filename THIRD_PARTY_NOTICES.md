# Third-party notices

Ping Flud uses the Microsoft Windows App SDK for its WinUI 3 shell. Portable release archives redistribute the Microsoft .NET 8 runtime and Windows App SDK components; compact archives rely on the corresponding installed runtimes.

## Microsoft .NET 8 runtime

The exact MIT licenses and third-party notices for the .NET 8.0.30 source components used by the self-contained packages are preserved verbatim in this repository and included in every binary archive:

- [`third_party/dotnet/runtime`](third_party/dotnet/runtime)
- [`third_party/dotnet/winforms`](third_party/dotnet/winforms)
- [`third_party/dotnet/wpf`](third_party/dotnet/wpf)

Those files govern the redistributed runtime components and contain notices for components under additional licenses. They must remain with self-contained distributions. The notices are pinned to 8.0.30, matching the runtime version recorded in the portable packages.

Upstream provenance: [dotnet/runtime v8.0.30](https://github.com/dotnet/runtime/tree/v8.0.30), [dotnet/winforms v8.0.30](https://github.com/dotnet/winforms/tree/v8.0.30), and [dotnet/wpf v8.0.30](https://github.com/dotnet/wpf/tree/v8.0.30).

## Microsoft Windows App SDK

The portable archives also include the license and notice files from the restored Microsoft Windows App SDK package. `package_release.py` copies them into each binary archive under `third_party/windowsappsdk/`.

## Development and test dependencies

These packages are restored from NuGet for testing and are not bundled as application dependencies:

- Microsoft.NET.Test.Sdk and Microsoft Test Platform components — MIT License — https://github.com/microsoft/vstest
- xUnit.net packages and Visual Studio runner — Apache License 2.0 — https://github.com/xunit/xunit

Package versions are declared in the two test project files. NuGet supplies each package's license metadata during restore.

## Fonts and graphics

Ping Flud bundles no fonts, icons, photographs, or other third-party visual assets. It requests Segoe UI Variable from Windows and uses only programmatically drawn interface elements.
