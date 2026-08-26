# Ping Flud — UI/UX Design Improvements

## Overview
This document outlines concrete UI/UX improvements for Ping Flud based on design-system best practices and the user's preferences for polished, modern desktop experiences with rounded controls, layered surfaces, subtle gradients/shadows, and keyboard-friendly behavior.

## Current State Analysis

### Strengths
- Dark-teal dashboard aesthetic with 7 theme options
- Card-based layout with rounded corners and shadows
- Keyboard navigation (Enter to start scan, Ctrl+F, etc.)
- Natural numeric sorting for IP addresses
- Consistent typography hierarchy (Segoe UI)

### Issues Identified

| # | Severity | Issue | Impact |
|---|----------|-------|--------|
| 1 | Medium | Scan card layout breaks on resize | The `importHint` label uses absolute positioning with a manual `Resize` handler; doesn't adapt cleanly to narrow windows |
| 2 | Medium | Results toolbar hint also uses manual absolute positioning | Same fragile pattern as scan card — breaks layout on resize |
| 3 | Low | NumericUpDown controls lack hover/focus state styling | Inconsistent with the custom RoundedButton's visual feedback |
| 4 | Low | Export menu doesn't support keyboard Enter | Requires mouse click on submenu item |
| 5 | Low | No visual feedback during background exports | Progress indicator would improve perceived performance |
| 6 | Low | Search results highlight not persistent | After filtering, the search box doesn't show match count |

## Proposed Improvements

### 1. Layout Resilience (Scan Card & Results Toolbar)

**Problem:** Manual absolute positioning with Resize handlers breaks on window resizes.

**Fix:** Replace fragile `Location`-based positioning with `FlowLayoutPanel` or `TableLayoutPanel`-based layouts that naturally adapt. Specifically:
- Scan card: Use a `TableLayoutPanel` with the title in one cell and the import hint in the next, using `SizeType.Percent` for flexible spacing.
- Results toolbar: Same approach — use FlowLayoutPanel for the hint label so it auto-wraps.

### 2. Numeric Input Hover/Focus States

**Problem:** `NumericUpDown` controls use default Windows styling with no visual feedback consistent with RoundedButton.

**Fix:** Create a `RoundedNumericUpDown` control that subclasses `NumericUpDown` with:
- `FlatStyle.Flat` with custom border colors from the theme
- Hover state (`MouseEnter`/`MouseLeave`) that lightens the background
- Focus cues matching `RoundedButton.FocusCuesVisible`
- Consistent `CornerRadius` of 6 (slightly less rounded than buttons for visual hierarchy)

### 3. Keyboard-Friendly Export Menu

**Problem:** The "More formats ▾" button opens a `ContextMenuStrip` which requires a mouse click to select an item.

**Fix:** Add `KeyDown` handling on the export button to open the menu with `Down`/`Space`/`Enter`. Also add access keys (e.g., "CSV" → `Alt+C`).

### 4. Export Progress Feedback

**Problem:** After moving exports to background tasks, there's no progress indicator.

**Fix:** Add a `ToolStripProgressBar` to the status strip that activates during export, with a `ToolStripLabel` showing "Exporting (3/7)..." status. Use `IProgress<int>` to report progress from the background task.

### 5. Search Match Count

**Problem:** The search box doesn't show how many matches were found.

**Fix:** After `ApplyFilter()`, update the search box placeholder to show the match count (e.g., "Search… (12 of 45 shown)"). Use a `Label` next to the search box for the count.

### 6. Results Grid Accessibility

**Problem:** The `DataGridView` uses default cell formatting; no row count in the status.

**Fix:** 
- Add row count to the status bar summary (e.g., "245 targets · 12 responding")
- Ensure grid has `AccessibleName = "Scan results grid"` (already partially done)
- Add alternating row colors using `_theme.GridAlternate`

## Implementation Plan

### Phase 1: Layout Resilience (3 files, ~30 min)
1. `MainForm.cs` — Replace `BuildScanCard` positioning with `TableLayoutPanel`
2. `MainForm.cs` — Replace `BuildResultsToolbar` positioning with `FlowLayoutPanel`
3. Add tests for resize behavior

### Phase 2: Input Control Styling (2 files, ~20 min)
1. `Controls.cs` — Add `RoundedNumericUpDown` class
2. `SettingsDialog.cs` — Use `RoundedNumericUpDown` instead of `NumericUpDown`

### Phase 3: Keyboard Navigation & Feedback (2 files, ~25 min)
1. `MainForm.cs` — Add keyboard handling for export menu
2. `MainForm.cs` — Add export progress to status strip

### Phase 4: Search & Grid Enhancements (1 file, ~15 min)
1. `MainForm.cs` — Add match count display and grid accessibility improvements

### Phase 5: Tests (1 file, ~20 min)
1. `SettingsDialogTests.cs` — Add assertions for `RoundedNumericUpDown`
2. Add `MainFormTests.cs` for layout resilience

## Color Palette Reference
- Surface background: `Color.FromArgb(18, 35, 43)` (Midnight)
- Surface raised: `Color.FromArgb(31, 50, 59)`
- Accent: `Color.FromArgb(20, 157, 178)` (teal/cyan)
- Foreground: `Color.FromArgb(240, 245, 247)`
- Muted foreground: `Color.FromArgb(148, 165, 173)`
- Border: `Color.FromArgb(42, 77, 88)`
- Success: `Color.FromArgb(69, 211, 139)` (green)
