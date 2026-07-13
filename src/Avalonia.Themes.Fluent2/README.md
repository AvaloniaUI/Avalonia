# Avalonia.Themes.Fluent2

A modern Fluent theme for Avalonia implementing the **Fluent 2 / WinUI 3 visual
language** (the design system Microsoft shipped with Windows 11), built as a
**drop-in replacement** for `Avalonia.Themes.Fluent`.

Reference implementation: the WinUI 3 resource dictionaries (release 1.5.2
baseline, individual controls up to 1.8.x) as ported by Uno Platform's
`Uno.UI.FluentTheme.v2`.

## Usage

```xml
<Application.Styles>
    <Fluent2Theme />
</Application.Styles>
```

Migrating from `Avalonia.Themes.Fluent`: change the package reference and swap
`<FluentTheme />` for `<Fluent2Theme />`. Everything else keeps working:

- **Resource key overrides** — every v1 resource key (`ButtonBackground`,
  `TextControlBorderBrushFocused`, `ControlCornerRadius`, …) still resolves
  with a compatible runtime type. Values changed; names did not.
- **`Classes="accent"`**, pseudo-class selectors, template part names and named
  control themes are unchanged.
- **`Fluent2Theme.Palettes`** keeps the v1 `ColorPaletteResources` API. `Accent`
  flows into all the new accent tokens. Non-accent legacy colors
  (`RegionColor`, `BaseHigh`, `BaseMediumHigh`, `BaseMedium`, `BaseLow`,
  `ChromeMedium`, `ErrorText`) heuristically drive the nearest Fluent 2 tokens;
  for exact control override the token keys directly
  (`SolidBackgroundFillColorBase`, `TextFillColorPrimary`, …).
- **`DensityStyle="Compact"`** is preserved and re-derived against the new
  metrics.

## What changed visually (intentional)

| Default | v1 | Fluent2 |
|---|---|---|
| `ControlCornerRadius` | 3 | **4** |
| `OverlayCornerRadius` | 5 | **8** |
| `ButtonPadding` | 8,5,8,6 | **11,5,11,6** |
| `TextControlThemePadding` | 10,6,6,5 | **10,5,6,6** |
| Focused text border | 2 uniform | **1,1,1,2** + accent underline |
| Button press | scale(0.98) | **fill change + 83 ms cross-fade** (WinUI has no press scale) |
| Control borders | flat | **gradient elevation borders** (darker bottom edge) |
| List/tree/combo selection | full-row accent | **subtle fill + 3×16 accent pill** |
| Scroll bars | 16 px rail | **12 px rail, 2 px collapsed thumb** |
| Window background | AltHigh | **SolidBackgroundFillColorBase** (#F3F3F3/#202020) |
| Default accent shades | HSL-computed | **WinUI static values** (when no OS accent) |
| OS accent shades (Windows) | HSL-computed | **read from the OS** (`UISettings` Light1–3/Dark1–3) |

One deliberate deviation from WinUI (following FluentAvalonia's lead): CheckBox
and RadioButton do **not** get WinUI's forced 120 px `MinWidth` — they size to
content plus 8 px trailing padding, as in v1. Set the `CheckBoxMinWidth` /
`RadioButtonMinWidth` resources to restore the WinUI metric.

The full WinUI 3 token family (`TextFillColor*`, `ControlFillColor*`,
`SubtleFillColor*`, `ControlStrokeColor*`, `CardBackgroundFillColor*`,
`SolidBackgroundFillColor*`, `SystemFillColor*`, `AccentFillColor*`, elevation
border brushes, acrylic fallbacks) is available for app use alongside the
legacy `SystemControl*` aliases.

## Known scope limitations

- Acrylic/Mica surfaces ship as their solid fallback colors (WinUI's own
  fallback mode); real translucency is a possible future enhancement.
- `TabControl` maps to the WinUI **Pivot** header look (Avalonia's TabItem
  semantics); the TabView document-tab look is out of scope.
- No HighContrast variant yet (same as v1).
- AnimatedIcon glyph animations are approximated with static glyphs and simple
  transitions.
- On Windows the six accent shades (`SystemAccentColorDark1`…`Light3`) come
  straight from the OS; on platforms that report only a base accent color
  (macOS, Linux) the shades are HSL-computed and can deviate slightly from
  Windows' palette algorithm.

## Compatibility tests

`tests/Avalonia.Themes.Fluent2.UnitTests` enforces the drop-in contract against
the live v1 theme: key parity in both variants with compatible runtime types,
implicit `ControlTheme` coverage, palette semantics, compact-density key
parity, and an instantiation/render smoke test over every themed control.
