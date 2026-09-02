# Changelog

## Unreleased

- Replaced legacy theme choices with a Light/Dark toggle; Graphite remains the dark default.
- Tightened the workspace wording around reachability testing, latency, packet loss, TTL, and reverse DNS troubleshooting.

## 1.5.2 - 2026-08-30

- Batched streamed result-grid refreshes so large scans do not rebuild the full visible collection for every reply.
- Clamped initial window size to the active display and restored ListView viewport scrolling.
- Exposed persisted target history in the editable target picker and made state saving best-effort at shutdown.

## 1.5.1 - 2026-08-30

- Migrated the release shell to WinUI 3 while keeping the scanner and report domain logic in `PingFlud.Core`.
- Fixed target editing on a new empty scan, and wired Documentation, About, and target-syntax actions.
- Added compact runtime-dependent and compressed portable WinUI releases for x86, x64, and ARM64.
- Hardened report publishing: stage output before publication, remove stale PNG continuation pages, export the visible filtered rows, and neutralize formula prefixes in every spreadsheet-compatible HTML text cell.
- Added regression coverage for export publication, filtering, formula neutralization, and command availability.

## 1.4.5 - 2026-08-27

- **Probe accuracy**: "Don't Fragment" now maps correctly to the ICMP option, every configured ping attempt runs so reply counts and packet-loss percentages are accurate, and DNS/ICMP operations now receive cancellation directly.
- **Export reliability**: Multi-page PNG export now creates the selected first-page file plus numbered continuation files, removes stale continuation pages from older exports, and stages completed reports beside the destination before publication.
- **Release hygiene**: Packaging now derives the application version from the project, includes third-party notices, and archives the complete build metadata.
- **Scanner reliability**: Reverse DNS is now scoped to responding hosts by default (configurable) with a separate DNS timeout, instead of running for every address.
- **Dual-stack support**: The scanner now tries all resolved addresses (IPv4 and IPv6) in order until one responds, instead of using only the first resolved address.
- **Payload safety**: ICMP echo requests now allow fragmentation by default; "Don't Fragment" is available as an explicit MTU-testing option in Settings.
- **UI performance**: Scan progress updates no longer rebuild the entire results grid every 100 ms; change notifications are batched and the `BindingList` is reused.
- **Background exports**: CSV, XML, HTML, PDF, PNG, TXT, and XLS exports now run on a background thread with a cancellation token; PNG page height is capped to prevent large-memory or GDI exhaustion.
- **Testability**: `PingScanner` now accepts injectable `IDnsResolver` and `IPingProbe` interfaces, enabling deterministic unit tests without network I/O.
- **Dependencies**: Updated `xunit` to 2.9.3 in test projects; no vulnerable packages found.
- **Documentation**: Updated README, CHANGELOG, and PRODUCT_SPEC to reflect new settings and behavior.

## 1.4.1

- Fixed rounded-button right and bottom edge clipping by correcting the control region bounds.
- Increased scan-card layout height so the lower action row is fully visible.
- Removed the duplicate header theme selector; themes remain in the Theme menu.
- Wrapped the results grid in a rounded, shadowed gradient card.
- Added a compact version/developer label to the header.

## 1.4.0

- Removed the unnecessary Results-only workspace and redundant Reports sidebar action.
- Added persistent and tooltip documentation for importing `.txt`/`.csv` target lists, plus graceful file-access error handling.
- Added subtle card shadows and gradients plus custom rounded controls throughout the app.
- Added Oceanic, Forest, Amethyst, and Ember themes for seven total themes.
- Results now start sorted by numerical IP address from lowest to highest.
- Enlarged and widened Scan settings so Subtitle content and helper text remain fully visible.

## 1.3.0

- Fixed clipped Subtitle controls and helper text in Scan settings by widening and enlarging the dialog.
- Added custom rounded cards and buttons with explicitly centered action text.
- Made the Results navigation button switch to a dedicated results-only workspace and highlight the active section.
- Results now default to ascending natural IP-address order.
- Added Oceanic, Forest, Amethyst, and Ember themes, for seven total themes.

## 1.2.0

- Redesigned the application around a dark-teal workspace shell with top command bar, left navigation rail, layered content panels, and cyan accents.
- Added Enter-to-start behavior while focus is in the Targets field.
- Fixed `ObjectDisposedException` when choosing a report format from the More formats menu.
- Kept Midnight, Graphite, and Daylight themes and updated Midnight to the new design language.
- Added regression tests for the Enter shortcut and reusable export-menu lifecycle.
- Updated documentation and product metadata.

## 1.1.0

- Added polished cards, persistent themes, expanded syntax documentation, AV-friendly portable/lite packaging, cancellable target expansion, PDF wrapping, and reproducible release validation.
