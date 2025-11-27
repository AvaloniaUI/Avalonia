## Summary

Performance and stability improvements for `VirtualizingStackPanel` when used with heterogeneous item heights. These changes address layout cycles, scroll jumping, and excessive container churn that occur in real-world scenarios with complex item templates (mixed field types, async image loading, text wrapping, deferred bindings).

Relates to #20259

## Changes

### 1. Layout cycle breaker

**Problem:** Complex controls (e.g. controls with async image loading, text wrapping, dynamic visibility bindings) can produce non-deterministic `Measure` results — each call to `element.Measure(sameSize)` returns a slightly different `DesiredSize`. This causes the panel's extent to oscillate between passes, triggering infinite re-measurement by the layout system.

**Scenario:** A `ListBox` with 70+ items of different types (headers, text fields, image fields, checkboxes). Fast-scrolling triggers a viewport jump, the panel realizes new elements, but their measurements fluctuate. The extent changes by 20-400px each pass, the parent re-measures the panel, and Avalonia's layout cycle detector fires after 10 enqueues.

**Fix:** After 1 full `MeasureOverride` pass, subsequent calls return the previous `DesiredSize` and post a `Background`-priority `InvalidateMeasure` via `Dispatcher.UIThread.Post`. This postpones (rather than skips) the measurement, ensuring items eventually settle at correct sizes while preventing tight layout loops. A `_measurePostponed` flag ensures at most one deferred measure is queued. The counter resets in `ArrangeOverride`, `OnEffectiveViewportChanged` (when a new measure is needed), and `OnItemsChanged`, ensuring each legitimate trigger gets a fresh pass.

### 1b. Deferred measure priority

**Problem:** The cycle breaker's deferred `InvalidateMeasure` used `Background` priority, which could fire after the user's next scroll event. This caused the size change to be applied at the wrong scroll position, producing visible scroll jumps during async image loading.

**Fix:** Changed the deferred measure to `Loaded` priority (higher than `Background`, before input), so it runs before the next input/scroll event.

### 2. ValidateStartU tolerance, suppression, and partial-state detection

**Problem:** `ValidateStartU` compares each realized element's current `DesiredSize` to the stored size from the previous pass. With non-deterministic measurements, even sub-pixel differences (< 1px) would mark `StartU` as unstable, invalidate the estimate cache, and trigger a cascade: new estimate → different extent → re-measure → different sizes → repeat.

**Scenario:** A form with 29 realized items where complex controls fluctuate by 0.3-0.8px per element per pass. Across 42 unrealized items, even a 1px/element estimate change produces a ~42px extent swing — enough to trigger re-measurement.

**Fix:**
- **Sub-pixel tolerance:** Changes < 1px are absorbed silently by updating the stored size. Only changes ≥ 1px (genuine resizes like 50px → 25px) mark `StartU` as unstable.
- **One-shot suppression:** After `ValidateStartU` fires once, `_suppressValidateStartU` prevents it from firing again until `ArrangeOverride` resets the flag. This stops repeated instability from non-deterministic measurements while still detecting genuine resizes.
- **Partial layout-manager state detection (`anchorMeasurePending`):** The Avalonia layout manager may re-measure some children before the parent's `MeasureOverride` runs. When items before the anchor have been re-measured (DesiredSize updated) but the anchor element has not yet (`IsMeasureValid == false`), `ValidateStartU` now detects this partial state and marks `StartU` as unstable. Previously, it would incorrectly apply `preDelta` compensation (treating it as async content loading), producing wrong positions when the anchor's size would also change in the next pass.
- **Anchor-aware compensation:** When only items before the viewport anchor changed and the anchor is stable, `ValidateStartU` adjusts `StartU` by the accumulated `preDelta` to preserve the anchor's visual position. This prevents scroll jumping when async content (e.g., images) loads above the viewport.

### 2b. ValidateStartU: preDelta compensation during frozen extent

**Problem:** When `ValidateStartU` runs with `lockSizes=true` (during extent oscillation), the original code skipped *all* processing for changed items via `continue`. This included preDelta tracking for position compensation. When an item before or at the viewport anchor changed size (e.g., a recycled FileFieldViewModel shrinking from 292px to 84px), no `StartU` adjustment happened, causing all visible items to shift by the size delta — a visible scroll jump.

**Scenario:** Scrolling through a form with async image fields. Item 38 (a FileFieldViewModel) shrinks from 292.8px to 84px due to OAPH-backed property propagation. The frozen extent prevents estimate cache invalidation (correct), but the `continue` also prevents preDelta from accumulating (bug). Items 39+ visually jump up by 208.8px.

**Fix:** Two changes in the `lockSizes` block of `ValidateStartU`:
1. **Track preDelta for items at or before the anchor** (`itemIndex <= anchorIndex`): accumulate the size diff into `preDelta` and update the stored size (preventing re-detection). Do *not* set `hasSignificantChange` — that would invalidate the estimate cache and restart oscillation.
2. **Early-return preDelta compensation:** When no significant non-locked changes are detected but `preDelta` accumulated from locked items, adjust `_startU -= preDelta` and return `true`. This keeps visible items at their current positions.

The `<=` (not `<`) in the anchor condition is critical: when the anchor item itself shrinks, all items after it shift. Adjusting `StartU` by the anchor's size delta keeps post-anchor items at the same visual position. The anchor may shift slightly (e.g., from 3410 to 3619), but since it's above or at the viewport edge, this is invisible.

### 3. CaptureViewportAnchor stale-anchor guard

**Problem:** `CaptureViewportAnchor` had a skip-cache optimization that returned early when the viewport hadn't moved significantly. But when `StartU` became `NaN` (unstable from `ValidateStartU`), the cached anchor from a previous pass was stale. `CompensateForExtentChange` then used this stale anchor, creating a positive feedback loop: each pass's drift compensation made the next pass's drift worse (66px → 158px → 234px → ...).

**Scenario:** After a viewport jump, `ValidateStartU` fires, `StartU` becomes `NaN`. On the next pass, `CaptureViewportAnchor` skips re-capture (viewport didn't move), but the old anchor position is invalid. `CompensateForExtentChange` shifts `StartU` by an ever-increasing amount.

**Fix:** Added `!double.IsNaN(startU)` to the skip-cache condition. When `StartU` is unstable, the anchor is always re-evaluated.

### 4. Smart container reuse during disjunct scroll

**Problem:** When the viewport jumps far (fast scrolling), `RecycleAllElements` destroys all containers. Re-creating and preparing containers for the new viewport is expensive — each needs `PrepareItemContainer` + full `Measure`.

**Scenario:** Scrolling quickly through a form with 71 items. The viewport jumps from items [0-28] to items [45-70]. Many of the same data types exist in both ranges (e.g., text fields, headers). Destroying and recreating identical containers wastes time.

**Fix:**
- **`RetainMatchingContainers`**: Before `RecycleAllElements`, walks the realized elements and extracts containers whose `DataContext` matches items in the estimated new viewport. These are stored in `_retainedForReuse` and their slots are nullified (so `RecycleAllElements` skips them).
- **`GetOrCreateElement` integration**: When creating elements for the new viewport, checks `_retainedForReuse` first. Matching containers get a lightweight index update (`ItemContainerIndexChanged`) instead of full `PrepareItemContainer`.
- **`NullifyElement`** in `RealizedStackElements`: New method to remove an element from the realized list without recycling it.
- **`RecycleUnusedRetainedContainers`**: After realization, any retained containers that weren't reused are properly recycled.

### 5. Improved anchor estimation for heterogeneous heights

**Problem:** With items of very different heights (30px headers, 200px image fields), the simple `index = offset / averageHeight` estimation produces large errors. When scrolling to a position where the actual items are much taller or shorter than the average, the anchor element is placed incorrectly, causing visible scroll jumps.

**Scenario:** Average item height is ~110px, but the viewport shows a section of 200px image fields. The estimated anchor index is too high, items appear shifted, and the scroll position jumps after the next Arrange.

**Fix:**
- **Forward/backward extrapolation**: When the viewport is beyond the realized range, the estimation uses the last (or first) realized element's actual position and extrapolates using the estimated size, rather than dividing from zero. This produces better results when there's a mix of large and small items.
- **Item 0 position correction**: In both forward and backward realization loops, if item 0 is realized, it's forced to position `U=0`. This prevents the first item from being clipped off-screen due to estimation errors when scrolling back to the top. **Skipped during frozen extent** — see §10.

### 6. Estimate caching

**Problem:** `EstimateElementSizeU` recalculates the average element size on every `MeasureOverride` call, even when the realized range hasn't changed. With smoothing, this causes the estimate to converge over multiple passes, changing the extent slightly each time and triggering re-measurement.

**Scenario:** The panel measures items [0-28] on pass 1, gets an average of 111.6px. On pass 2 (same items), smoothing adjusts it to 111.0px. On pass 3, it's 111.4px. Each change shifts the extent by ~40px (42 unrealized items × 0.4px), triggering another pass.

**Fix:** Track the realized range (`_lastEstimateFirstIndex`, `_lastEstimateLastIndex`). If the range hasn't changed, return the cached estimate. The cache is invalidated when `ValidateStartU` detects a genuine resize.

### 7. CompensateForExtentChange dampening

**Problem:** When very few items are realized (< 10% of total) and the extent changes dramatically (> 50%), the `ScrollViewer` can overshoot, causing the viewport to jump to a completely wrong position.

**Scenario:** A list with 500 items, 10 realized. The estimate changes from 10px-average to 200px-average items, causing a 20× extent change. The `ScrollViewer` adjusts the scroll thumb position, overshooting past the actual content.

**Fix:** When the extent change ratio exceeds 50% and less than 10% of items are realized, the change is dampened to 30% of the full delta. This prevents wild swings while still converging to the correct extent over multiple passes.

### 8. Skip CompensateForExtentChange after StartU re-estimation

**Problem:** When `ValidateStartU` marks `StartU` as unstable (NaN) due to a uniform resize, positions are re-estimated from scratch (e.g., anchor 3 moves from position 75 to 60 after items shrink from 25px to 20px). `CompensateForExtentChange` then detects this legitimate position change as "anchor drift" and undoes it, pushing `StartU` back to the old value. This prevents the scroll anchor mechanism from adjusting the viewport correctly.

**Scenario:** A `ListBox` with 20 items at 25px each. After resizing all items to 20px, the anchor is re-estimated at `index * newSize = 3 * 20 = 60`. But `CompensateForExtentChange` sees the anchor moved from 75 to 60 and "compensates" by shifting `StartU` back to 75, producing wrong item positions and preventing the `IScrollAnchorProvider` from adjusting the scroll offset.

**Fix:** Track whether `ValidateStartU` marked `StartU` as unstable (`startUAfterValidate` is NaN). When it did, skip `CompensateForExtentChange` — the positions were re-estimated from scratch and the scroll anchor mechanism will adjust the viewport naturally in a subsequent layout pass. The extent tracking (`_lastMeasuredExtentU`) is still updated to maintain a correct baseline.

### 9. NeedsContainer check order fix (ItemsControl)

**Problem:** `NeedsContainer<T>` checked content virtualization logic before checking if the item is already a container of the expected type. This caused items that are their own containers to be incorrectly wrapped.

**Fix:** Moved the `item is T` check before the virtualization logic.

### 10. Extent oscillation detection and frozen extent

**Problem:** When a non-deterministic item template produces alternating sizes across passes (e.g., a FileFieldViewModel measuring as 292px then 84px then 292px due to async OAPH-backed property propagation), the extent oscillates between values. Each swing causes `CompensateForExtentChange` to shift the viewport, triggering another layout cycle with different items realized, perpetuating the oscillation and drifting the scroll position.

**Scenario:** A form with 71 items including FileFieldViewModels. Fast scrolling triggers container recycling. A recycled container's DataContext changes, but the OAPH-backed `ShowImage` property propagates asynchronously. The first measure sees the old value (image visible, 292px), the second sees the new value (image hidden, 84px). The extent swings by 200+px each pass.

**Fix:**
- **Oscillation detection**: Track the sign of extent deltas across passes. When the sign alternates (up-down-up), increment an oscillation counter. For large swings (>100px), freeze on first reversal; for small swings, wait for 2 reversals.
- **Frozen extent (`_frozenExtentU`)**: Lock the extent reported to `ScrollViewer` at the pre-oscillation value. This prevents the scroll anchor mechanism from adjusting the offset in response to oscillating values. The frozen extent converges toward reality: when the actual extent stabilizes within ±5px for 2+ consecutive passes, the frozen value is updated.
- **Frozen extent resets**: `_frozenExtentU` is reset to `NaN` in `OnItemsChanged` (collection changes invalidate all assumptions) and `OnDetachedFromVisualTree`.

### 10b. Item 0 position correction during frozen extent

**Problem:** The item 0 position correction (§5) forces item 0 to `U=0` when realized. During frozen extent, this creates a gap between item 0 (at position 0) and item 1 (at its estimated position, e.g., 2415). The next pass's `GetOrEstimateAnchorElementForViewport` walks cumulative sizes from 0 and finds no item overlapping the viewport (at ~2400), falls through to estimation, and triggers a false disjunct detection. All containers are recycled and the viewport jumps to a completely wrong position.

**Scenario:** User scrolls up toward the top while the extent is frozen. Item 0 gets realized in the backward loop and is forced to position 0. Item 1 remains at its estimated position (2415). Items 0-4 with cumulative sizes from 0 span ~500px. The viewport at ~2400 is beyond the realized range → disjunct → scroll jumps to item 16.

**Fix:** Skip the item 0 position correction in both realization loops (forward and backward) and the post-realization adjustment block when `_frozenExtentU` is not `NaN`. Item 0 keeps its natural position relative to item 1, preserving contiguous positions and preventing false disjunct detection.

### 10c. Frozen extent boundary clamping

**Problem:** When the extent is frozen at an estimated value, it may be larger than the actual content. This allows the user to scroll past the content boundaries: empty space appears above item 0 (the scroll offset goes below item 0's estimated position) or below the last item (the frozen extent exceeds the actual content end).

**Scenario (top):** Item 0 is realized at estimated position 2363 (not corrected to 0 during frozen extent). The ScrollViewer allows offset 0, which puts the viewport at position 0 — 2363px above item 0. The user sees empty space above the first item.

**Scenario (bottom):** The frozen extent is 1500 but actual content is only 1000. The scroll range extends 500px past the last item.

**Fix:**
- **Top boundary**: In the post-realization block, when item 0 is realized during frozen extent and the viewport start is above item 0's position (`firstItemU > viewport.viewportUStart`), shift all items so item 0 aligns with the viewport start (`CompensateStartU(viewportStart - firstItemU)`) and reduce `_frozenExtentU` by the same amount. This gradually converges item 0 toward position 0 as the user scrolls up, with no empty space visible.
- **Bottom boundary**: When the last item is realized (`_hasReachedEnd`) during frozen extent and the frozen extent exceeds the actual content end (`_frozenExtentU > contentEndU` and `contentEndU >= viewportUEnd`), cap `_frozenExtentU` at `contentEndU`. This prevents the scrollbar from allowing scrolling past the last item.

### 11. ControlCatalog: ComplexVirtualizationPage image support

**Problem:** The ComplexVirtualizationPage demo used only text-based item templates with no height variation from images. This didn't exercise the non-deterministic measurement paths that cause real-world scroll jumping.

**Fix:** Enhanced the `PhotoItem` template to display actual images with varying heights:
- `PhotoItem` now has `Image` (Bitmap) and `ImageHeight` (60/100/150/200/250px) properties.
- Images are pre-loaded from 8 sample assets in the ControlCatalog.
- The XAML template shows an `Image` control with `UniformToFill` stretch inside a clipped border, bound to the varying height. This creates heterogeneous container sizes that stress the virtualizing panel.

## Test coverage

Added 22 new tests covering:
- Layout cycle prevention (cycle breaker limits, genuine resize still works)
- ValidateStartU tolerance (sub-pixel absorption) and suppression (one-shot per arrange)
- CaptureViewportAnchor NaN guard (stale anchor not reused)
- NullifyElement (smart reuse infrastructure)
- Disjunct scroll container reuse
- Item 0 position correction
- Estimate caching (skip recalculation, invalidation on resize)
- Extent dampening with few realized items
- End-to-end: scroll down then back to top with mixed heights (contiguity check)
- Rapid scrolling stress test (20 jumps with mixed heights)
- Anchor-self-size-change compensation during frozen extent (preDelta with `<=` anchor)
- Item 0 correction skipped during frozen extent (no false disjunct)
- Frozen extent top boundary clamping (no empty space above item 0)
- Frozen extent bottom boundary capping (no scrolling past last item)
- Scroll-to-top-and-back with frozen extent preserves contiguity

## Files changed

- `src/Avalonia.Controls/VirtualizingStackPanel.cs` — All panel-level changes (cycle breaker, frozen extent, boundary clamping, item 0 correction guard, extent oscillation detection)
- `src/Avalonia.Controls/Utils/RealizedStackElements.cs` — `NullifyElement`, `ValidateStartU` tolerance/suppression/lockSizes preDelta, `CompensateStartU`
- `src/Avalonia.Controls/ItemsControl.cs` — `NeedsContainer` check order
- `tests/Avalonia.Controls.UnitTests/VirtualizingStackPanelTests.cs` — 22 new tests
- `samples/ControlCatalog/Pages/ComplexVirtualizationPage.xaml` — PhotoItem template with image display
- `samples/ControlCatalog/ViewModels/ComplexVirtualizationPageViewModel.cs` — PhotoItem image/height properties, sample image loading
