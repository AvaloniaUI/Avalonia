# Handoff: Scroll jump after taking a picture in a virtualized ListBox

## Symptom

In a heterogeneous (variable-height, mixed-type) `ListBox` using content virtualization,
a photo/file field is short (~50px) while it has no image. The user scrolls down so the
field is visible, taps its button, takes a picture, and returns to the list. On return the
**scroll position has jumped up significantly** — the photo field is no longer visible.

Regression was introduced by the content-virtualization performance work in commits:
- `b2a07506` — *VirtualizingStackPanel warmup / variable heights / IVirtualizingDataTemplate*
- `abc1c225` — *scrolling down fast sometimes clips the last element*

## Root cause (proven from an on-device trace)

The jump is **not** caused by the photo item's 50px→large resize. It is caused by the
**window viewport collapsing to 0×0 while the camera activity is in front**, which destroys
the panel's virtualization anchor.

Trace evidence (panel `E23F8D5C`, viewport was at Y≈984 before the picture):

| Seq | Event |
|-----|-------|
| `#01478` | Baseline: `vpY=983.8 startU=435.9 realized=[2..9] anchor=item3@821.7 frozenExtentU=2108.1` |
| `#01499/1500` | Camera launches → `OnEffectiveViewportChanged: effVp=0,983.8,0,0` (**width=0, height=0**) → needsMeasure=TRUE |
| `#01504–1506` | Empty-viewport measure → treated as **disjunct** → recycles everything → `realized=[0..0] startU=0` (**anchor lost**) |
| `#01532/1533` | Return from camera: viewport correctly restored to `vpY=983.8` (ScrollViewer kept offset because extent stayed frozen at 2108) |
| `#01535` | But panel state is now item0/startU=0 → `CaptureViewportAnchor: anchorIdx=-1` (unrecoverable) |
| `#01537–1540` | Re-anchors by estimate to item 9, tiny realization, `frozenExtentU` collapses 2108 → 775.5 |
| `#01544` | **`effVp` jumps `983.8 → 123.7`** — ScrollViewer clamps offset to `775.5 − 651.7 ≈ 123.8` because extent shrank ← **the visible scroll jump** |
| `#01562+` | Extent later regrows to ~2976 but offset already lost at ~182; never recovers |

**Chain:** hidden window → empty viewport → disjunct recycle resets `StartU=0` and drops all
realized items → frozen extent converges to the tiny corrupted realization → on restore the
ScrollViewer clamps the (now out-of-range) offset → scroll jumps up.

## Fix

Make the panel ignore the empty (0×0) viewport that occurs while it is hidden, preserving all
measure-driving state so the scroll position survives the round-trip. Two guards in
`src/Avalonia.Controls/VirtualizingStackPanel.cs`:

1. **`OnEffectiveViewportChanged`** — if the incoming effective viewport (intersected with
   `Bounds.Size`) is empty (`Width <= 0 || Height <= 0`), return immediately without updating
   `_viewport`, the extended viewports, extent, or invalidating measure. *(Primary fix —
   prevents `_lastMeasuredExtendedViewport` from being overwritten with `(0,0,0,0)`.)*

2. **`MeasureOverride`** — defensive early-out next to the `_isWaitingForViewportUpdate` guard:
   if `_viewport` is empty **and** content is already realized (`_realizedElements.Count > 0`)
   **and** `DesiredSize != default`, return the cached `DesiredSize` instead of recycling.
   Skipped on first realization so startup is unaffected.

Net effect against the log: `startU=435.9 / realized=[2..9] / frozenExtentU=2108` stay intact
across the camera trip; on restore the anchor is found normally, the extent doesn't collapse,
and the ScrollViewer never clamps `984 → 123`.

## Test (red-green verified)

`tests/Avalonia.Controls.UnitTests/VirtualizingStackPanelTests.cs` →
`Collapsing_Viewport_To_Empty_And_Restoring_Preserves_Scroll_Position`:

- 100 items × 10px, viewport 100px. Scroll to offset 200 (first realized = item 20).
- Collapse `root.ClientSize` to `(0,0)`, layout. Restore to `(100,100)`, layout.
- Assert `scroll.Offset.Y` and `FirstRealizedIndex` are unchanged.

**Red** (guards disabled): `FirstRealizedIndex` collapses `20 → 0` after the round-trip.
**Green** (guards enabled): passes.

## On-device reproduction / verification

1. Enable the diagnostic hook once at startup (only present on the debugging branch; removed
   from the committed fix):
   ```csharp
   VirtualizingStackPanel.ScrollTrace = s => System.Diagnostics.Debug.WriteLine(s);
   ```
2. Scroll so the empty (~50px) photo field is visible.
3. Tap its button → take a picture → return to the list.
4. Confirm the field is still visible and the scroll position is unchanged.

## Notes

- The `ScrollTrace` diagnostic instrumentation (static hook in `VirtualizingStackPanel`, the
  `Trace(...)` call sites, and the trace calls in `RealizedStackElements.ValidateStartU` /
  `CompensateStartU`) was used only for diagnosis. **It is removed from the committed fix** on
  `feature/20259_virtualizingdatatemplate_master`. It still exists on the debugging branch
  `release/12.0.3.1-optiq01` (stashed) if a fresh device trace is ever needed.
- Pre-existing unrelated failure: `Focused_Container_Is_Positioned_Correctly_when_Container_Size_Change…(bufferFactor: 0.5)`
  fails on committed HEAD of `release/12.0.3.1-optiq01` independently of this fix. Verify
  separately whether it also fails on `feature/20259_virtualizingdatatemplate_master`.
