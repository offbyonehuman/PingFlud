# Ping Flud

**Ping Flud** is an open-source Windows network reachability scanner by **OffByOneHuman**. It performs bounded, concurrent ICMP checks, resolves reverse DNS names, displays packet statistics, and exports filtered results in several report formats.

> Use Ping Flud only on systems and networks that you own or are explicitly authorized to test.

## Highlights

- IPv4, IPv6, and host-name targets
- Inclusive IPv4 ranges, CIDR, and IPv4 wildcards
- Concurrent ICMP with cancellation and live progress
- Reverse DNS, latency, packet loss, reply count, and TTL
- Natural numerical sorting for target and IP columns
- Search plus Responding / Not responding filters
- CSV, XML, HTML, PDF, PNG, TXT, and XLS-compatible exports
- Polished dark-teal workspace with a left navigation rail and responsive content panels
- Seven persisted themes: Midnight, Graphite, Oceanic, Forest, Amethyst, Ember, and Daylight
- Self-contained portable builds for Windows x86, x64, and ARM64
- Small framework-dependent builds for the same architectures

## Quick start

1. Enter one or more targets in the **Targets** field.
2. Separate multiple specifications with commas or new lines.
3. Press **Enter** in the Targets field or select **Start scan**.
4. Use **Show** and **Search** to filter the visible report without leaving the scan workspace.
5. Results are initially sorted by **IP address, lowest to highest**. Click any column heading to change the sort; Target and IP columns use numerical network ordering.
6. Select **Export CSV** or **More formats** to save the currently visible rows.

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

## Understanding results

- **● Responding** — at least one configured ICMP attempt succeeded.
- **○ Not responding** — no attempt succeeded before the timeout.
- **Latency** — fastest successful round-trip time.
- **Loss %** — unsuccessful attempts divided by total attempts.
- **Replies** — successful attempts.
- **TTL** — TTL from the successful reply when available.
- **Reverse DNS** — name returned by the system resolver. Reachability appears before reverse DNS finishes.

A device can be online while blocking ICMP. “Not responding” means no ICMP response under the selected settings; it does not prove the device is powered off.

## Themes

Choose a theme from the header or **Theme** menu:

- **Midnight** — dark teal surfaces with cyan accents
- **Graphite** — neutral charcoal with blue accents
- **Oceanic** — deep blue with bright cyan accents
- **Forest** — dark green with mint accents
- **Amethyst** — deep purple with violet accents
- **Ember** — warm charcoal with amber accents
- **Daylight** — light surfaces with Windows-blue accents

The selected theme, scan settings, custom title/subtitle, and recent target history are stored in:

```text
%LOCALAPPDATA%\PingFlud\settings.json
```

## Reports

Exports contain the currently visible, filtered, and sorted result set.

- **CSV** — UTF-8 report with spreadsheet-formula neutralization
- **XML** — structured `PingFludResults` document
- **HTML** — theme-aware standalone report
- **PDF** — paginated summary report
- **PNG** — full result images; large reports split into numbered files
- **TXT** — tab-separated plain text
- **XLS-compatible HTML** — HTML table readable by spreadsheet applications

## Downloads and runtime choices

Each release provides two package types:

- **Portable** — self-contained; no .NET installation required. This is the recommended package.
- **Lite** — much smaller, but requires the matching **.NET 8 Desktop Runtime** for x86, x64, or ARM64.

Both package types use a normal unpacked .NET publish layout instead of a compressed single-file wrapper.

## Antivirus and trust

An older unsigned, compressed single-file archive received one heuristic detection on VirusTotal. A single detection can be a false positive, but it should still be investigated rather than ignored.

The current release reduces common heuristic triggers by:

- removing compressed single-file bundling;
- publishing normal .NET files that antivirus engines can inspect directly;
- including complete source, deterministic build instructions, version metadata, and SHA-256 checksums;
- running the automated test suite and a local Microsoft Defender scan before release.

The binaries are still **unsigned**. The strongest reputation improvement would be an Authenticode code-signing certificate owned by OffByOneHuman. If a vendor flags a current checksum, submit that exact file to the vendor’s false-positive portal and include the source repository and checksums.

VirusTotal report supplied for the older archive:

```text
SHA-256: af36fa2473d9de1ca7230a7aac492cc81ae46f8aa7068f2001e0c17d5b03ea7c
```

## Build and test

Requirements: .NET 8 SDK on Windows.

```bat
dotnet restore PingFlud.sln
dotnet test PingFlud.sln -c Release
build-all.cmd
python package_release.py
```

Build outputs:

```text
artifacts\portable\win-x86
artifacts\portable\win-x64
artifacts\portable\win-arm64
artifacts\lite\win-x86
artifacts\lite\win-x64
artifacts\lite\win-arm64
```

## Developer

**OffByOneHuman**

## License

MIT — see [LICENSE](LICENSE).
