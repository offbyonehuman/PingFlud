# Feature Matrix

| Area | Publicly documented reference behavior | Ping Flud implementation target |
|---|---|---|
| Address input | Lists, ranges, CIDR, `*` and `?` wildcards, files | Equivalent interoperable syntax with bounded expansion and IPv6 single targets |
| Result selection | Responding, Not Responding, All | Live filter and post-scan filter |
| Discovery | Concurrent ICMP echo | Bounded asynchronous ICMP engine with cancellation |
| Naming | DNS/WINS lookup | Reverse DNS through the Windows/.NET resolver; no proprietary WINS component |
| Results | Table | Sortable/searchable grid with status, timing, loss, and detail |
| ICMP settings | Outstanding packets, timeout, pings/node, TTL, delay, payload | All six settings persisted per user |
| Titles | Custom title/subtitle | Persisted title and subtitle |
| Export | CSV, XML, HTML, PDF, image | CSV, XML, HTML, PDF, PNG, plus plain text |
| Profiles/history | Previously entered lists | Bounded recent-input history and JSON settings |
| Documentation | Basic target examples | Persistent syntax legend and detailed help for ranges, CIDR, `?`, and `*` |
| Themes | Not applicable | Seven persisted palettes: Midnight, Graphite, Oceanic, Forest, Amethyst, Ember, and Daylight |
| Packaging | Desktop installer/toolset | AV-friendly portable and small runtime-dependent builds for x86/x64/ARM64 |

## Deliberate clean-room differences

- No third-party vendor name, logo, icons, source code, assets, internal formats, or proprietary integrations.
- No Workspace Studio drag-and-drop integration.
- The UI uses an original layout and branding while retaining the familiar workflow of target entry → scan controls → result table.
- The network engine uses only documented .NET APIs.
