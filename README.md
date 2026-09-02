# Ping Flud

[![CI](https://github.com/offbyonehuman/PingFlud/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/offbyonehuman/PingFlud/actions/workflows/ci.yml) [![Latest release](https://img.shields.io/github/v/release/offbyonehuman/PingFlud)](https://github.com/offbyonehuman/PingFlud/releases/latest)

**Ping Flud** is an open-source Windows network reachability scanner by **OffByOneHuman**. It performs bounded, concurrent ICMP checks, resolves reverse DNS names, displays packet statistics, and exports filtered results in several report formats.

> Use Ping Flud only on systems and networks that you own or are explicitly authorized to test.

## Highlights

- IPv4, IPv6, and host-name targets
- Inclusive IPv4 ranges, CIDR, and IPv4 wildcards
- Concurrent ICMP with cancellation and live progress
- Reverse DNS, latency, packet loss, reply count, and TTL
- Natural numerical sorting for target and IP columns
- Search plus Responding / Not responding filters
- CSV, HTML, PDF, PNG, and TXT exports
- Windows 11 WinUI workspace with a left navigation rail and responsive content panels
- Light and dark appearance modes, with Graphite as the dark default
- Compact runtime-dependent builds and portable self-contained builds for Windows x86, x64, and ARM64

## Quick start

1. Enter one or more targets in the **Targets** field.
2. Separate multiple specifications with commas or new lines.
3. Press **Enter** in the Targets field or select **Start scan**.
4. Use **All results**, **Responding**, or **Not responding** and **Search results** to filter the visible report without leaving the scan workspace.
5. Results are initially sorted by **IP address, lowest to highest**. Click any column heading to change the sort; Target and IP columns use numerical network ordering.
6. Select **Export…** to save the currently visible rows.

## Importing target lists

Select **Import list…** to load a `.txt` or `.csv` file. Each line can contain one IP address, host name, inclusive range, CIDR block, or wildcard specification. Blank lines and lines beginning with `#` are ignored. Commas inside a line are treated the same as commas typed in the Targets field, so a CSV row can contain multiple target specifications.

Example:

```text
# Office subnets
192.168.10.0/24
192.168.20.10-192.168.20.40
10.2.?.*
router.example
```

Imported entries populate the Targets field; the scan does not start until you press **Enter** or select **Start scan**.

## Target syntax

| Type | Example | Meaning |
|---|---|---|
| IPv4 | `192.168.1.20` | One IPv4 address |
| IPv6 | `2001:db8::20` | One IPv6 address |
| Host name | `router.example` | Resolve and scan one host |
| Inclusive IPv4 range | `192.168.1.10-192.168.1.25` | Every address from `.10` through `.25`, including both endpoints |
| IPv4 CIDR | `192.168.1.0/24` | Every address in the CIDR block |
| Question-mark wildcard | `192.168.1.1?` | Exactly one decimal digit: `.10` through `.19` |
| Asterisk wildcard | `192.168.1.*` | Any decimal digit sequence that forms a valid octet: `.0` through `.255` |

### `?` and `*` wildcard rules

Wildcards apply to **IPv4 octets only**.

- **`?` matches exactly one decimal digit.**
  - `192.168.1.?` expands to `192.168.1.0` through `192.168.1.9`.
  - `192.168.1.1?` expands to `192.168.1.10` through `192.168.1.19`.
  - `192.168.1.?5` expands to `.15`, `.25`, `.35`, … `.95`.
- **`*` matches zero or more decimal digits**, but the resulting octet must remain between `0` and `255`.
  - `192.168.1.*` expands to the complete last octet, `.0` through `.255`.
  - `10.0.?.*` uses `0–9` for the third octet and `0–255` for the fourth.

Every range, CIDR, and wildcard expansion is constrained by **Expansion safety cap** in Settings. Oversized input is rejected before scanning.

## Scan settings

| Setting | Purpose | Default |
|---|---|---:|
| Max outstanding packets | Maximum concurrent target probes | 64 |
| Timeout | Wait time for each ICMP reply | 1000 ms |
| Pings per target | ICMP attempts per target | 1 |
| Packet TTL | Maximum router hops | 128 |
| Delay between pings | Optional delay between attempts | 0 ms |
| Expansion safety cap | Maximum generated targets | 65,536 |
| ICMP payload | Text placed in each echo request | `Ping Flud` |
| DNS timeout | Milliseconds per reverse DNS lookup | 2000 ms |
| Don't Fragment | Sets the IP don't-fragment flag (MTU testing) | Off |
| Reverse DNS responding only | Skip reverse DNS for non-responding hosts | On |

## Understanding results

- **● Responding** — at least one configured ICMP attempt succeeded.
- **○ Not responding** — no attempt succeeded before the timeout.
- **Latency** — fastest successful round-trip time.
- **Loss %** — unsuccessful attempts divided by total attempts.
- **Replies** — successful attempts.
- **TTL** — TTL from the successful reply when available.
- **Reverse DNS** — name returned by the system resolver. Reachability appears before reverse DNS finishes.

A device can be online while blocking ICMP. “Not responding” means no ICMP response under the selected settings; it does not prove the device is powered off.

## Appearance

Use the **Light mode / Dark mode** toggle to change appearance. Dark mode uses
the neutral Graphite palette; Light mode uses the Daylight palette. The choice
is saved with the scan settings.

The appearance mode, scan settings, custom title/subtitle, and recent target history are stored in:

```text
%LOCALAPPDATA%\PingFlud\settings.json
```

## Reports

Exports contain the currently visible, filtered, and sorted result set.

- **CSV** — UTF-8 report with spreadsheet-formula neutralization
- **HTML** — standalone web report
- **HTML (Excel)** — spreadsheet-compatible HTML with formula-neutralization for untrusted text cells
- **PDF** — paginated summary report
- **PNG** — full result images; large reports split into numbered files
- **TXT** — tab-separated plain text

## Downloads and runtime choices

Each release provides two archive types for Windows x86, x64, and ARM64:

- **Compact** — runtime-dependent multi-file package. Requires the matching **.NET 8 Desktop Runtime** and **Windows App Runtime 1.8** for the selected architecture. It is the smaller download.
- **Portable** — compressed, self-contained single executable. No .NET Desktop Runtime or Windows App Runtime installation is required. It is the offline/clean-machine option.

## Build and test

Requirements: Windows 10 or 11 and the .NET 8 SDK. Python 3.8 or later is needed only to create release archives and checksum manifests.

```bat
dotnet restore PingFlud.sln
dotnet test PingFlud.sln -c Release
build-all.cmd
python package_release.py
```

Build outputs:

```text
artifacts\winui-compact\win-x86
artifacts\winui-compact\win-x64
artifacts\winui-compact\win-arm64
artifacts\winui-portable\win-x86
artifacts\winui-portable\win-x64
artifacts\winui-portable\win-arm64
```

## Developer

**OffByOneHuman**

## Contributing and security

Bug reports and focused pull requests are welcome. Please do not use real private-network details in issues or test fixtures. Report security concerns using the guidance in [SECURITY.md](SECURITY.md).

## License

MIT — see [LICENSE](LICENSE). Runtime and development dependency notices are documented in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
