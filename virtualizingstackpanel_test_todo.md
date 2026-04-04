# VirtualizingStackPanel test verification TODO

## Context

All 175 tests in `VirtualizingStackPanelTests.cs` pass (green). However, only a subset could be verified **red-green** — i.e., confirmed to actually fail when the production code they exercise is removed. The remaining tests pass regardless because the deterministic test environment doesn't trigger the code paths they're meant to cover.

## Tests verified red-green

| Test | Code exercised | Verification |
|------|---------------|--------------|
| `Frozen_Extent_Bottom_Boundary_Caps_Even_When_Viewport_Past_Content` | Unconditional bottom cap in RealizeElements | RED when `contentEndU >= viewport.viewportUEnd` guard restored |
| `Frozen_Extent_Grows_Immediately_When_Actual_Extent_Exceeds_It` | Immediate grow path in CompensateForExtentChange | RED when grow path removed |
| `NullifyElement_Returns_Element_And_Clears_Slot` | NullifyElement method | RED when method returns null |
| `Significant_Size_Change_Logs_Warning` | Logger block in ValidateStartU | RED when Logger.TryGet block removed |

## Tests NOT verified — and what's needed

### Group 1: Cycle breaker (6 tests)

**Tests:**
- `Cycle_Breaker_Limits_Measures_Per_Layout_Pass`
- `Mixed_Heights_Scrolling_Does_Not_Cause_Excessive_Measures`
- `Large_Scroll_Jump_With_Mixed_Heights_Does_Not_Cause_Layout_Cycle`
- `Multiple_Scroll_Jumps_Each_Get_Fresh_Measure_Pass`
- `Items_Changed_Resets_Cycle_Breaker`
- `Rapid_Scrolling_With_Mixed_Heights_Does_Not_Cause_Layout_Cycle`

**Why they don't go red:**
The cycle breaker (`_consecutiveMeasureCount > 1`) prevents repeated `MeasureOverride` calls within a single layout pass. In production, non-deterministic measurements (async image loading, text wrapping) cause extent oscillation that triggers the layout manager to re-invoke `MeasureOverride`. In the test environment, measurements are deterministic — the layout manager never re-invokes `MeasureOverride` because the `DesiredSize` is stable. Disabling the cycle breaker has no effect because it's never reached.

**What's needed:**
The test needs the Avalonia layout manager to call `MeasureOverride` multiple times within a single `ExecuteLayoutPass`. This requires:

1. **Non-deterministic element measurement**: Elements whose `DesiredSize` changes each time `Measure()` is called. The `AdjustElementSize` virtual hook was added for this, but it only changes the stored size — not the actual `DesiredSize` seen by the parent `ScrollViewer`. For the parent to re-measure the panel, the panel's `DesiredSize` must actually differ.

2. **Layout manager re-entry**: When `InvalidateMeasure()` is called from within `MeasureOverride`, Avalonia's test layout manager processes it in a subsequent measure-arrange cycle (with `ArrangeOverride` in between, which resets `_consecutiveMeasureCount`). This means the cycle breaker's counter never reaches >1 within a single cycle. To truly test this, the layout manager would need to re-invoke `MeasureOverride` *without* an intervening `ArrangeOverride` — which happens in production when the parent re-measures the panel due to a `DesiredSize` change, but doesn't happen in the test's single-pass layout.

**Possible solutions:**
- Override `MeasureOverride` in a test subclass to call `base.MeasureOverride` twice and check that the second call is a no-op (returns cached `DesiredSize` when count > 1). This tests the breaker directly but bypasses the layout manager.
- Create a custom test layout manager that re-queues measure without arrange when `DesiredSize` changes.
- Accept these as behavior/regression tests that verify "scrolling doesn't produce excessive measures" without isolating the breaker itself.

---

### Group 2: ValidateStartU suppression and sub-pixel tolerance (3 tests)

**Tests:**
- `ValidateStartU_Absorbs_Sub_Pixel_Changes`
- `ValidateStartU_Only_Fires_Once_Per_Arrange_Cycle`
- `Scroll_After_Container_Resize_Does_Not_Use_Stale_Anchor`

**Why they don't go red:**
- **Sub-pixel**: With deterministic measurements, sub-pixel changes never occur. Removing the `>= 1.0` threshold has no effect because all size differences are either 0 (no change) or large (genuine resize).
- **Suppression**: Removing `_suppressValidateStartU = false` from `ArrangeOverride` doesn't change the outcome because ValidateStartU has no effect in subsequent passes when sizes haven't changed.
- **NaN guard**: Removing `!double.IsNaN(startU)` from `CaptureViewportAnchor` doesn't cause the stale anchor to produce wrong results because the estimation is accurate enough with uniform items.

**What's needed:**
- **Sub-pixel**: Use the `AdjustElementSize` hook to add sub-pixel noise (e.g., ±0.3px) to element sizes. This would cause ValidateStartU to see changes < 1px. Without the threshold, these would trigger `hasSignificantChange` and cause instability. With the threshold, they're absorbed.
- **Suppression**: Needs a scenario where ValidateStartU fires on pass 1 (size change >= 1px), then sizes change *again* before the next arrange. Without suppression, ValidateStartU fires again, potentially causing oscillation. With suppression, it's blocked until after arrange.
- **NaN guard**: Needs an item resize that makes `startU` become `NaN` (uniform resize of all realized items), followed by a scroll. Without the NaN guard, `CaptureViewportAnchor` uses the stale cached anchor (from before the resize), producing wrong realized items.

---

### Group 3: Item 0 position correction (2 tests)

**Tests:**
- `Item_Zero_Is_Always_At_Position_Zero`
- `Item_Zero_Not_Forced_To_Zero_During_Frozen_Extent`

**Why they don't go red:**
With mildly-mixed heights (10/50 or 50/50), the estimation error when scrolling back to offset 0 is small. Item 0's estimated position is already at or very close to 0, so the `u = 0` correction has no visible effect. Similarly, during frozen extent with uniform items, the item 0 position doesn't drift enough to break contiguity even without the `IsNaN(_frozenExtentU)` guard.

**What's needed:**
- **Item 0 at zero**: Use extreme height variance (e.g., item 0 = 500px, items 1-99 = 10px). Scroll far down (offset 5000), then back to 0. The estimation error from the large first item would place item 0 at a significantly non-zero position. Without the correction, `Bounds.Top != 0`.
- **Frozen extent guard**: Use `AdjustElementSize` to make items appear larger during the initial scroll (simulating estimation error that pushes item 0 away from position 0). During frozen extent, the guard prevents forcing item 0 to 0, which would create a gap. Without the guard, the gap breaks contiguity.

---

### Group 4: Frozen extent top boundary clamping (1 test)

**Test:** `Frozen_Extent_Top_Boundary_Clamps_When_Viewport_Above_Item_Zero`

**Why it doesn't go red:**
With uniform 50px items scrolled to offset 200 and back, the estimation places item 0 close to position 0. The `firstItemU > viewport.viewportUStart` condition (which triggers the clamping shift) is never true because there's no significant estimation error.

**What's needed:**
Use `AdjustElementSize` to inflate sizes during the initial scroll (so the panel overestimates positions). Then freeze the extent and scroll back to 0. Item 0 would be realized at a position > 0 (above the viewport), and the clamping block would shift items down. Without clamping, empty space would appear above item 0.

---

### Group 5: Estimate cache (2 tests)

**Tests:**
- `Estimate_Cache_Skips_Recalculation_When_Range_Unchanged`
- `Estimate_Cache_Invalidated_After_Genuine_Resize`

**Why they don't go red:**
- **Cache skip**: With deterministic measurements, the estimate average is identical on every pass regardless of whether the cache is used. The smoothing formula `0.7 * old + 0.3 * new` produces the same result when `new == old`. Avalonia's layout rounding (pixel-snaps all sizes to integers) prevents sub-pixel differences from accumulating.
- **Cache invalidation**: After item resize from 40px to 20px, the realized range changes (more items fit in viewport), so the cache naturally misses even without explicit invalidation.

**What's needed:**
- **Cache skip**: Disable `UseLayoutRounding` on test elements, or use `AdjustElementSize` with sub-pixel variation (< 1px so ValidateStartU absorbs it) that changes the average on each pass. The cache would prevent the estimate from drifting; without it, smoothing would produce a different extent.
- **Cache invalidation**: Use very large items (200+ px) with buffer factor 0 so only 1 item is realized. Resize from 200px to 100px. The realized range stays [0..0], so without cache invalidation the old 200px estimate persists. But the current test fails because ValidateStartU with a single item doesn't mark `startU` as unstable (NaN), so the invalidation code path isn't reached. This needs investigation into when exactly ValidateStartU produces NaN with a single realized item.

---

### Group 6: Extent dampening (1 test)

**Test:** `Extent_Dampening_Prevents_Wild_Swings_With_Few_Realized_Items`

**Why it doesn't go red:**
The test only asserts `extent > 0` and `FirstRealizedIndex >= 0` — these are trivially true regardless of dampening.

**What's needed:**
After a large scroll (causing disjunct realization with few items), record the extent. Then do another layout. With dampening, the extent changes gradually (30% of delta). Without dampening, the full extent change is applied immediately. The test should assert that the extent change between two consecutive layouts is bounded (e.g., < 50% of the difference between old and new calculated extent).

---

### Group 7: RetainMatchingContainers / disjunct reuse (3 tests)

**Tests:**
- `Disjunct_Scroll_Reuses_Containers_With_Matching_DataContext`
- `Inserting_Item_Before_Viewport_Reuses_Matching_Containers_Without_Remeasure`
- `Collection_Reset_With_Reorder_Reuses_Matching_Containers_Without_Remeasure`

**Why they don't go red:**
The tests assert that the correct items are realized (correct indices/DataContexts) but don't verify that containers were *reused* (same Control instances). Removing `RetainMatchingContainers` causes containers to be recycled and recreated, but the end result is the same — correct items at correct positions.

**What's needed:**
Capture the `Control` instances before the operation (scroll, insert, reset), then verify after the operation that the *same* Control instances are still realized (reference equality). Without `RetainMatchingContainers`, the old containers are recycled and new ones are created — different instances. Additionally, track `MeasureOverride` call counts on the containers to verify reused containers skip re-measurement.

---

### Group 8: Pre-anchor compensation (2 tests)

**Tests:**
- `Item_Growing_Before_Anchor_While_Scrolling_Up_Preserves_Anchor_Position`
- `Item_Shrinking_Before_Anchor_While_Scrolling_Up_Preserves_Anchor_Position`

**Why they don't go red (not fully verified):**
These were identified as testable by the research agent (flipping the sign of `preDelta` compensation would cause them to fail). They weren't directly verified due to time constraints.

**What's needed:**
Simply change `preDelta += diff` to `preDelta -= diff` (or `_startU -= preDelta` to `_startU += preDelta`) in `ValidateStartU` and run the tests. The scroll offset should jump by ~2x the item size change, failing the `< 50px` assertion.

---

## Integration/behavior tests (5 tests)

These tests are inherently end-to-end and don't isolate a single feature:

- `Scrolling_Down_With_Mixed_Heights_Does_Not_Jump`
- `Scrolling_Up_With_Mixed_Heights_Does_Not_Jump`
- `Scroll_To_End_And_Back_With_Extreme_Height_Variance`
- `Scroll_Down_Then_Back_To_Top_With_Mixed_Heights_Shows_All_Items`
- `Scroll_To_Top_And_Back_Down_With_Frozen_Extent_Preserves_Contiguity`

These verify that the complete scrolling pipeline (estimation, anchoring, compensation, boundary clamping) works together. They're valuable as regression tests but can't be made to fail by removing a single feature — multiple cooperating systems maintain contiguity.

## Common theme

Most unverified tests share the same root cause: **deterministic measurements don't trigger the corrective code paths**. The production code guards against estimation errors and measurement instability that only occur with non-deterministic content (async images, text wrapping, deferred bindings). In the test environment, measurements are perfectly stable, so the guards are never reached.

The `AdjustElementSize` virtual hook in `VirtualizingStackPanel` was added as infrastructure to inject measurement instability from test subclasses. Future work should use this hook to create more extreme scenarios that force the corrective code paths to activate.
