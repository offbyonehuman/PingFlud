# Credits and provenance

## Ping Flud source

Ping Flud application source, tests, build scripts, and project documentation are maintained by **OffByOneHuman** and are licensed under the repository's [MIT License](LICENSE). Files under `third_party/` retain the licenses and notices of their original components.

The project source is intended to contain Ping Flud's own implementation. Any copied, adapted, generated, or otherwise externally sourced material must be identified and documented according to [CONTRIBUTING.md](CONTRIBUTING.md).

## Bundled production components

The following components may be present in release archives. Their license and notice files are preserved in the indicated repository paths and are included by `package_release.py`.

| Component | Version | License/terms | Repository notice |
|---|---:|---|---|
| .NET runtime | 8.0.30 | MIT plus notices for included third-party material | `third_party/dotnet/runtime/` |
| Windows Forms runtime components | 8.0.30 | MIT plus third-party notices | `third_party/dotnet/winforms/` |
| WPF runtime components | 8.0.30 | MIT plus third-party notices | `third_party/dotnet/wpf/` |
| Microsoft.Windows.SDK.NET.Ref | 10.0.19041.56 | Microsoft Windows SDK license terms | `third_party/windows-sdk-net-ref/10.0.19041.56/` |
| System.Drawing.Common | 8.0.5 | MIT | `third_party/dotnet/system.drawing.common/` |
| Microsoft.Win32.SystemEvents | 8.0.0 | MIT | `third_party/dotnet/systemevents/` |
| System.Numerics.Tensors | 9.0.0 | MIT | `third_party/dotnet/system.numerics.tensors/` |
| Microsoft.Web.WebView2 | 1.0.3179.45 | BSD-style Microsoft license and third-party notices | `third_party/webview2/` |
| Microsoft Windows App SDK | 1.8.260804001 | Microsoft Software License Terms and package notices | `third_party/windowsappsdk/` |
| Microsoft.WindowsAppSDK.AI | 1.8.79 | Microsoft Software License Terms | `third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.AI/1.8.79/` |
| Microsoft.WindowsAppSDK.Base | 1.8.251216001 | Microsoft Software License Terms and package notices | `third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.Base/1.8.251216001/` |
| Microsoft.WindowsAppSDK.DWrite | 1.8.25122902 | Microsoft Software License Terms | `third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.DWrite/1.8.25122902/` |
| Microsoft.WindowsAppSDK.Foundation | 1.8.260803002 | Microsoft Software License Terms | `third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.Foundation/1.8.260803002/` |
| Microsoft.WindowsAppSDK.InteractiveExperiences | 1.8.260708001 | Microsoft Software License Terms | `third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.InteractiveExperiences/1.8.260708001/` |
| Microsoft.WindowsAppSDK.ML | 1.8.2197 | Microsoft Software License Terms and third-party notices | `third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.ML/1.8.2197/` |
| Microsoft.WindowsAppSDK.Runtime | 1.8.260804001 | Microsoft Software License Terms and package notices | `third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.Runtime/1.8.260804001/` |
| Microsoft.WindowsAppSDK.Widgets | 1.8.251231004 | Microsoft Software License Terms | `third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.Widgets/1.8.251231004/` |
| Microsoft.WindowsAppSDK.WinUI | 1.8.260803003 | Microsoft Software License Terms and package notices | `third_party/windowsappsdk/packages/Microsoft.WindowsAppSDK.WinUI/1.8.260803003/` |

Upstream projects:

- [.NET runtime](https://github.com/dotnet/runtime/tree/v8.0.30)
- [Windows Forms](https://github.com/dotnet/winforms/tree/v8.0.30)
- [WPF](https://github.com/dotnet/wpf/tree/v8.0.30)
- [Microsoft.Windows.SDK.NET.Ref 10.0.19041.56](https://aka.ms/WinSDKProjectURL) — license terms: https://aka.ms/WinSDKLicenseURL
- [System.Drawing.Common](https://github.com/dotnet/winforms)
- [System.Numerics.Tensors](https://github.com/dotnet/runtime)
- [Microsoft.Web.WebView2 1.0.3179.45](https://www.nuget.org/packages/Microsoft.Web.WebView2/1.0.3179.45)
- [Microsoft Windows App SDK](https://github.com/microsoft/WindowsAppSDK)

See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for the complete license scope and notice inventory.

## Development and test components

These packages are restored for development or testing and are not bundled as application dependencies:

- [Microsoft.NET.Test.Sdk](https://github.com/microsoft/vstest) 18.9.0 — MIT
- [xUnit.net](https://github.com/xunit/xunit) 2.9.3 — Apache License 2.0
- xUnit Visual Studio runner 4.0.0 — Apache License 2.0
- [Microsoft.Windows.SDK.BuildTools](https://aka.ms/WinSDKLicenseURL) 10.0.26100.9169 — Microsoft Windows SDK license terms
