# Ping Flud — Windows 11 Native UI Redesign

## Problem
The current UI looks like a legacy WinForms app. It needs to look like a native Windows 11 application with proper:
- Mica/Acrylic background materials
- Windows 11 rounded corners (2px radius for controls, 8-12px for cards)
- Proper Windows 11 color palette
- Fluent Design System shadows
- Modern typography (Segoe UI Variable)
- Tighter spacing and proper padding

## Design Direction: Windows 11 Fluent Design

### Key Principles
1. **Mica material** — Background with tinted, noise-textured acrylic effect
2. **Windows 11 corner radius** — 2px for buttons, 8-12px for cards/surfaces
3. **Proper elevation** — Multiple shadow layers with proper opacity
4. **Segoe UI Variable** — Modern typography with better weight distribution
5. **Windows 11 color palette** — Reference Microsoft's official design tokens

### Windows 11 Design Tokens

| Token | Light | Dark |
|-------|-------|------|
| Layer 0 (background) | #FFFFFF | #0F1011 (Mica base) |
| Layer 1 (surface) | #F9F9FC | #1A1A1D |
| Layer 2 (elevated) | #FFFFFF | #222227 |
| Border | #E6E6E6 | RGBA(255,255,255,0.14) |
| Outline (hover) | #D0D0D0 | RGBA(255,255,255,0.24) |
| Accent | #0078D4 | #00BCF3 (cyan) |
| On-accent | #FFFFFF | #000000 |
| Text primary | #212121 | #F3F3F3 |
| Text secondary | #6C6C6C | #B3B3B3 |
| Text disabled | #A19FA3 | #8B8B8B |

### Implementation Approach

#### Option A: Pure WinForms (Recommended for compatibility)
- Use `DwmApi` to enable Mica/Acrylic backgrounds
- Set `Form.TransparencyKey` for acrylic effects
- Use `Color.FromArgb(10, ...)` for semi-transparent overlays
- Apply Windows 11 corner rounding via DWM
- Use proper Fluent Design spacing (16px base padding, 8px grid)

#### Option B: Modernize with Windows API Code Pack
- Enable Mica via `DwmSetWindowAttribute` with `DWMA_USE_IMMERSIVE_DARK_MODE`
- Use `AccentPolicy` for blur effects
- Requires P/Invoke but achieves true Windows 11 look

## Specific Changes Needed

### 1. Form Styling
```csharp
// Enable Windows 11 rounded corners and shadows
internal static partial class DwmApi
{
    [DllImport("dwmapi.dll")]
    internal static extern int DwmSetWindowAttribute(
        IntPtr hwnd, int attr, ref int attrValue, int attrSize);
}

// In MainForm constructor after InitializeComponent():
// Enable rounded corners: DWM_WINDOW_CORNER_PREFERENCE = 3 (RCW_USE_HOSTBACKCOLOR)
// Enable drop shadow: DWM_WINDOW_CORNER_PREFERENCE
```

### 2. Color Palette Rewrite
Replace current themes with Windows 11 FUI tokens:
- `WindowBackground` → Mica base (semi-transparent)
- `Surface` → Layer 1 (elevated surface)
- `SurfaceRaised` → Layer 2 (card elevation)
- `Border` → Windows 11 subtle border
- `Accent` → Windows 11 blue (#0078D4)

### 3. CardPanel Redesign
- **Corner radius**: 12px (Windows 11 large surface radius)
- **Shadow**: Multiple layers at 12%, 8%, 4% opacity (simulating Fluent elevation)
- **Background**: Semi-transparent with mica-like noise texture
- **Border**: 1px at RGBA(255,255,255,0.14)

### 4. Button Redesign
- **Corner radius**: 8px (Windows 11 control radius)
- **Padding**: 12px horizontal, 8px vertical (proper Fluent spacing)
- **Height**: 32px (Windows 11 baseline button height)
- **States**: 
  - Default: Transparent with border
  - Hover: Subtle acrylic overlay (RGBA(255,255,255,0.08))
  - Pressed: Slightly darker overlay
  - Accent buttons: Solid accent with hover tint

### 5. Typography
- **App title**: Segoe UI Variable Semibold, 24pt
- **Card headings**: Segoe UI Variable Semibold, 13pt
- **Body text**: Segoe UI Variable Regular, 9pt
- **Caption/hint**: Segoe UI Variable Regular, 8.5pt (0.875rem)

### 6. Grid Improvements
- **Row height**: 32px (Fluent baseline)
- **Header height**: 40px (taller for touch)
- **Alternating rows**: Subtle (5% opacity of accent)
- **Hover**: 4% opacity overlay
- **Selection**: 8% opacity accent tint
- **Border**: None (seamless) or 1px at layer 0 border color
