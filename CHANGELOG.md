# Changelog

## Unreleased

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
