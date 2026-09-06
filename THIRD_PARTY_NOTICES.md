# Third-party notices

## License scope

The root [MIT License](LICENSE) applies to Ping Flud's own source, tests, build scripts, and documentation. Files and components under `third_party/` retain the licenses and notices of their original authors. A release archive is therefore a mixed-license distribution, not an all-MIT distribution.

## Microsoft .NET 8 runtime

The exact MIT licenses and third-party notices for the .NET 8.0.30 source components used by the self-contained packages are preserved verbatim in this repository and included in every binary archive:

- [`third_party/dotnet/runtime`](third_party/dotnet/runtime)
- [`third_party/dotnet/winforms`](third_party/dotnet/winforms)
- [`third_party/dotnet/wpf`](third_party/dotnet/wpf)

Those files govern the redistributed runtime components and contain notices for components under additional licenses. They must remain with self-contained distributions. The notices are pinned to 8.0.30, matching the runtime version recorded in the portable packages.

Upstream provenance: [dotnet/runtime v8.0.30](https://github.com/dotnet/runtime/tree/v8.0.30), [dotnet/winforms v8.0.30](https://github.com/dotnet/winforms/tree/v8.0.30), and [dotnet/wpf v8.0.30](https://github.com/dotnet/wpf/tree/v8.0.30).

## Shipped .NET package dependencies

The Windows application references `System.Drawing.Common` 8.0.5. The published output also contains its transitive `Microsoft.Win32.SystemEvents` 8.0.0 and `System.Numerics.Tensors` 9.0.0 assemblies. Their exact package license and notice files are committed and included in binary archives:

- `System.Drawing.Common` 8.0.5 — MIT — [`third_party/dotnet/system.drawing.common`](third_party/dotnet/system.drawing.common) — upstream [dotnet/winforms](https://github.com/dotnet/winforms)
- `Microsoft.Win32.SystemEvents` 8.0.0 — MIT — [`third_party/dotnet/systemevents`](third_party/dotnet/systemevents) — upstream [dotnet/runtime](https://github.com/dotnet/runtime)
- `System.Numerics.Tensors` 9.0.0 — MIT — [`third_party/dotnet/system.numerics.tensors`](third_party/dotnet/system.numerics.tensors) — upstream [.NET runtime](https://github.com/dotnet/runtime)

## Microsoft Windows App SDK and WebView2

The application uses [`Microsoft.WindowsAppSDK`](https://github.com/microsoft/WindowsAppSDK) 1.8.260804001. The package and its runtime package components have Microsoft Software License Terms rather than Ping Flud's MIT license. The resolved runtime graph is recorded below; each package-specific file is preserved under [`third_party/windowsappsdk`](third_party/windowsappsdk):

- `Microsoft.WindowsAppSDK` 1.8.260804001 — `LICENSE.txt`, `NOTICE.txt` — [`third_party/windowsappsdk`](third_party/windowsappsdk)
- `Microsoft.WindowsAppSDK.AI` 1.8.79 — `license.txt` — [`third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.AI/1.8.79`](third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.AI/1.8.79)
- `Microsoft.WindowsAppSDK.Base` 1.8.251216001 — `license.txt`, `NOTICE.txt` — [`third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.Base/1.8.251216001`](third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.Base/1.8.251216001)
- `Microsoft.WindowsAppSDK.DWrite` 1.8.25122902 — `license.txt` — [`third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.DWrite/1.8.25122902`](third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.DWrite/1.8.25122902)
- `Microsoft.WindowsAppSDK.Foundation` 1.8.260803002 — `license.txt` — [`third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.Foundation/1.8.260803002`](third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.Foundation/1.8.260803002)
- `Microsoft.WindowsAppSDK.InteractiveExperiences` 1.8.260708001 — `license.txt` — [`third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.InteractiveExperiences/1.8.260708001`](third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.InteractiveExperiences/1.8.260708001)
- `Microsoft.WindowsAppSDK.ML` 1.8.2197 — `license.txt`, `ThirdPartyNotices.txt` — [`third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.ML/1.8.2197`](third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.ML/1.8.2197)
- `Microsoft.WindowsAppSDK.Runtime` 1.8.260804001 — `license.txt`, `NOTICE.txt` — [`third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.Runtime/1.8.260804001`](third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.Runtime/1.8.260804001)
- `Microsoft.WindowsAppSDK.Widgets` 1.8.251231004 — `license.txt` — [`third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.Widgets/1.8.251231004`](third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.Widgets/1.8.251231004)
- `Microsoft.WindowsAppSDK.WinUI` 1.8.260803003 — `license.txt`, `NOTICE.txt`, `tools/NOTICE.txt` — [`third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.WinUI/1.8.260803003`](third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.WinUI/1.8.260803003)

The published output also contains `Microsoft.Web.WebView2` 1.0.3179.45. Its Microsoft package license and notices are preserved under [`third_party/webview2`](third_party/webview2).

The package notice files are copied from the resolved NuGet packages without content changes. `package_release.py` reads the restored WinUI asset graph, honors `NUGET_PACKAGES`, and fails if any resolved package notice is absent or differs from the committed version. `Microsoft.Windows.SDK.BuildTools` and its MSIX helper, along with `Microsoft.NET.ILLink.Tasks`, are build-time packages and are not bundled.

## Development and test dependencies

These packages are restored from NuGet for testing and are not bundled as application dependencies. They are referenced by all three test project files:

- Microsoft.NET.Test.Sdk and Microsoft Test Platform components — MIT License — https://github.com/microsoft/vstest
- xUnit.net packages and Visual Studio runner — Apache License 2.0 — https://github.com/xunit/xunit

Package versions are declared in the three test project files. NuGet supplies each package's license metadata during restore.

## Fonts and graphics

Ping Flud bundles no fonts, icons, photographs, or other third-party visual assets. It requests Segoe UI Variable from Windows and uses only programmatically drawn interface elements.
