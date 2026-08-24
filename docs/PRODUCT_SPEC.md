# Ping Flud — Product Specification

## Clean-room scope

Ping Flud is an independent, MIT-licensed Windows network utility built from generally documented network protocols and behavior. It must not contain third-party proprietary code, artwork, logos, product names, screenshots, or copied text. Its visual identity uses dark teal layered surfaces, cyan accents, a left navigation rail, and the name **Ping Flud**.

## Functional requirements

### Target input
- Accept comma-delimited and line-delimited entries.
- Accept individual IPv4 and IPv6 addresses and resolvable host names.
- Accept inclusive IPv4 ranges (`192.168.1.1-192.168.1.254`).
- Accept IPv4 CIDR (`192.168.1.0/24`).
- Accept IPv4 wildcard octets (`*`) and decimal digit wildcards (`?`) with a safe expansion limit.
- Load a text file containing one or more target specifications per line.
- Retain a bounded list of recent target specifications.
- Reject malformed input and expansion beyond the configurable safety limit with a clear message.

### Sweep engine
- Send ICMP echo requests concurrently, bounded by **Max outstanding packets**.
- Expose ICMP timeout, pings per node, packet TTL, delay between pings, and payload.
- Support cancellation and deterministic progress reporting.
- Resolve reverse DNS names without blocking result delivery.
- Record address, host name, state, response time, successes/attempts, packet loss, TTL when available, and error/status detail.
- Filter results by Responding, Not Responding, or All.

### Desktop interface
- Windows desktop executable with a distinct professional, responsive GUI.
- Structured single-workspace app shell with top command bar, compact left rail, scan controls, syntax guide, results grid, and status sections.
- Rounded cards and buttons with centered action text, compact typography, and subtle borders.
- Persistent Midnight, Graphite, Oceanic, Forest, Amethyst, Ember, and Daylight themes.
- Title/subtitle, targets history box, From File, scan filter, Start/Stop, and Settings controls.
- Sortable results table, text search, copy selected rows, clear, and row-state styling.
- Persistent target-syntax legend plus detailed in-app help defining ranges, CIDR, `?`, and `*`.
- Progress bar and summary counts.
- Persist window-independent settings, theme, and history under the current user's AppData.
- About dialog and authorization warning.

### Export
- CSV
- XML
- HTML
- plain text
- PDF
- PNG image

Exports must represent the currently visible result set and escape user/network-derived text correctly.

## Packaging
- .NET 8 Windows desktop application.
- AV-friendly self-contained portable outputs for `win-x86`, `win-x64`, and `win-arm64` using a normal unpacked publish layout.
- Small framework-dependent outputs for the same architectures, requiring the matching .NET 8 Desktop Runtime.
- No compressed single-file bundling in release packages.
- Reproducible build script, SHA-256 manifest, source archive, and documented commands.

## Safety

Users must scan only networks and systems they own or are explicitly authorized to test. A conservative default expansion cap prevents accidental Internet-scale scans. The app must not request elevation merely to perform ordinary ICMP requests.
