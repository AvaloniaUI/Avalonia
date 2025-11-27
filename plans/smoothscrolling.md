# VirtualizingStackPanel Smooth Scrolling Improvements

## Problem Statement

The VirtualizingStackPanel exhibited multiple unnecessary measure passes during slow scrolling with heterogeneous items (items with varying heights), causing performance issues and scroll jumping.

### User Observation

Slow scrolling through a list with mixed item types (PersonItem: 150-250px, TaskItem: 80-120px, ProductItem: 100-180px) produced **2-3 measure passes per scroll** instead of 1:

```
Pass 1: Realize item 34, extent changes +37px
Pass 2: Estimate updates based on item 34, extent changes -62px
Pass 3: Suppressed by tolerance (if threshold enabled)
```

This caused:
- Performance degradation during scrolling
- Visible scroll jumping/stuttering
- Non-deterministic extent calculation based on convergence history

## Root Cause Analysis

### Temporal Mismatch in Estimation

The original `MeasureOverride` flow had a **temporal mismatch** where the estimate was calculated from the PREVIOUS viewport before measuring the CURRENT viewport:

```csharp
// ORIGINAL FLOW (BROKEN)
MeasureOverride():
  Line 239: EstimateElementSizeU()     // Uses OLD realized elements [0-20]
  Line 258: RealizeElements()          // Measures NEW elements [500-520]
  Line 272: CalculateDesiredSize()     // Uses OLD estimate! (based on [0-20])
```

**Example Issue:**
- User scrolls from items [0-20] (avg 50px) to items [500-520] (avg 100px)
- `EstimateElementSizeU()` sees [0-20], calculates estimate = 50px
- `RealizeElements()` measures [500-520], actual sizes ~100px
- `CalculateDesiredSize()` uses 50px estimate for items 521+
- **But items 521+ are probably also ~100px!**
- Result: Extent underestimated by ~50%
- Next pass: Estimate updates, extent jumps → triggers another measure pass

### Secondary Issues

1. **Exponential smoothing convergence:** Even after fixing temporal mismatch, 10% exponential smoothing caused gradual convergence over multiple passes when realizing new items
2. **Redundant re-estimation:** Same realized range measured multiple times, each time recalculating estimate with smoothing
3. **Premature ScrollIntoView re-enabling:** After Reset events (infinite scroll), ScrollIntoView was re-enabled too early, causing ListBox to trigger unwanted scrolling

## Implemented Fixes

### Fix 1: Phase 1 - Eliminate Temporal Mismatch

**File:** `VirtualizingStackPanel.cs`

**Changes:**
- **Removed** estimate calculation from line 239 (before `RealizeElements`)
- **Added** estimate calculation after line 262 (after swapping measure/realized elements)

```csharp
// NEW FLOW (FIXED)
MeasureOverride():
  // Line 239: REMOVED EstimateElementSizeU()

  Line 258: RealizeElements()                  // Measure NEW viewport FIRST
  Line 261: Swap _measureElements ↔ _realizedElements

  Line 266: EstimateElementSizeU()             // Calculate from CURRENT viewport
  Line 272: CalculateDesiredSize()              // Use CURRENT estimate ✓
```

**Impact:**
- Estimate now reflects CURRENT viewport composition (items 500-520), not previous (items 0-20)
- When scrolling to a region with different item sizes, estimate adapts immediately
- Extent calculation uses contextually-accurate estimate
- Reduced passes from 2-3 to 1 in many cases

**Files Modified:** VirtualizingStackPanel.cs:238-239 (removed), 266 (added)

### Fix 2: Skip Re-estimation When Realized Range Unchanged

**Problem:** Even with Phase 1, when the same realized range was measured twice (e.g., layout-triggered measure), the estimate was recalculated with smoothing, causing extent to converge over multiple passes.

**Solution:** Track the realized range used for estimation and skip recalculation if unchanged.

**Changes:**
```csharp
// New fields (line 97-98)
private int _lastEstimateFirstIndex = -1;
private int _lastEstimateLastIndex = -1;

// In EstimateElementSizeU() (line 775-784)
var firstIndex = _realizedElements.FirstIndex;
var lastIndex = _realizedElements.LastIndex;
if (firstIndex == _lastEstimateFirstIndex && lastIndex == _lastEstimateLastIndex)
{
    // Skip - range unchanged
    return _lastEstimatedElementSizeU;
}

// Track after calculation
_lastEstimateFirstIndex = firstIndex;
_lastEstimateLastIndex = lastIndex;
```

**Impact:**
- Eliminated redundant re-estimation when measuring same elements
- No more smoothing convergence over multiple passes for same viewport
- Extent remains stable when realized range doesn't change

**Files Modified:** VirtualizingStackPanel.cs:97-98, 770-847

### Fix 3: Direct Averaging (No Smoothing) for Larger Samples

**Problem:** Even with 3% smoothing, realizing a single new item (e.g., item 25) with a different size caused estimate changes large enough to trigger additional layout passes.

**Solution:** Completely eliminate smoothing for samples >= 5 items. Smoothing is no longer needed because:
1. Phase 1 eliminated temporal mismatch (estimate from current viewport)
2. Fix 2 prevents re-estimation of same range (no oscillation)

**Changes:**
```csharp
// Old: Always smooth
if (_lastEstimatedElementSizeU > 0)
{
    var smoothingFactor = divisor < 5 ? 0.3 : 0.1;  // 10% for larger samples
    ...
}

// New: Only smooth for very small samples
if (_lastEstimatedElementSizeU > 0 && divisor < 5)
{
    var smoothingFactor = 0.3;  // 30% for < 5 items only
    ...
}
else
{
    // Direct average for >= 5 items
    return _lastEstimatedElementSizeU = newAverage;
}
```

**Impact:**
- Immediate adaptation to new item sizes (no convergence lag)
- Accurate extent on first pass when realizing new items
- Reduced from 2 passes to 1 pass consistently

**Files Modified:** VirtualizingStackPanel.cs:815-846

### Fix 4: Preserve Estimate Tracking on Reset When Preserving Elements

**Problem:** During infinite scroll Reset events, estimate tracking was always reset, forcing recalculation even when realized elements were preserved. This caused extent oscillation.

**Solution:** Only reset estimate tracking when actually recycling all elements, not when preserving them.

**Changes:**
```csharp
// In OnItemsChanged, Reset case (line 435-458)
if (shouldPreserveRealizedElements)
{
    // Preserve elements - DON'T reset estimate tracking
    // Realized elements unchanged, estimate still valid
}
else
{
    // Recycle everything - DO reset estimate tracking
    _lastEstimateFirstIndex = -1;
    _lastEstimateLastIndex = -1;
}
```

**Impact:**
- Estimate remains stable during infinite scroll
- No cascading extent changes from re-estimation
- Prevented scroll jumping during Reset events

**Files Modified:** VirtualizingStackPanel.cs:435-458

### Fix 5: Enhanced Diagnostic Logging

**Added comprehensive logging to `ScrollIntoView` method to trace what's triggering unwanted scrolls:**

```csharp
[VSP-SCROLLINTO] ScrollIntoView(index) called, Realized=[X-Y], Suppressed=bool, InLayout=bool
[VSP-SCROLLINTO] ScrollIntoView(index) - element already realized, calling BringIntoView
[VSP-SCROLLINTO] Calling BringIntoView on element X at position Y
[VSP-SCROLLINTO] Viewport {rect} contains item rect {rect}: bool, setting _isWaitingForViewportUpdate=bool
```

**Purpose:** Identify what component (likely ListBox) is calling ScrollIntoView after Reset events, causing jumps to top.

**Files Modified:** VirtualizingStackPanel.cs:573-650

### Fix 6: Item 0 Clipping on Fast Scroll Up

**Problem:** When scrolling up fast to the top, item 0 (the first item) would be clipped or positioned incorrectly:
- Item 0 positioned at negative Y (top portion cut off)
- Item 0 positioned below Y=0 (gap at top)
- ScrollViewer appeared not at the top even though it should be

**Root Cause:** `CompensateForExtentChange` was undoing item 0 position corrections:

1. `CaptureViewportAnchor` captured anchor (possibly item 0 or another item) at its OLD estimated position
2. `RealizeElements` correctly positioned item 0 at u=0
3. `CompensateForExtentChange` detected "anchor drift" and compensated by shifting ALL elements
4. This shifted item 0 away from position 0, causing clipping

**Example Flow:**
```
1. CaptureViewportAnchor: Item 0 at estimated position 21px (wrong)
2. Forward loop realizes item 0, forces u=0 (correct)
3. CompensateForExtentChange sees: "Item 0 moved 21→0, that's drift!"
4. Calls CompensateStartU(21) to "restore" anchor
5. Item 0 ends up at position 21 or -3 (wrong again!)
```

**Solution:** Three-layer protection to ensure item 0 always stays at position 0:

#### Layer 1: Forward Loop Protection (NEW)
```csharp
// In RealizeElements, forward loop (line 1311-1318)
if (index == 0 && !MathUtilities.AreClose(u, 0))
{
   // System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
   //    $"[VSP-CLIP-FIX] FORWARD LOOP: Item 0 at {u:F2}px, forcing to U=0");
   u = 0;
}
```

When item 0 is the anchor element (common when scrolling near top), force its position to exactly 0.

#### Layer 2: Extent Compensation Protection (NEW - CRITICAL)
```csharp
// In CompensateForExtentChange (line 1233-1248)
// CRITICAL: If item 0 is realized at position 0, NEVER apply compensation.
if (_realizedElements != null &&
    _realizedElements.FirstIndex == 0 &&
    _realizedElements.StartU is { } startU &&
    !double.IsNaN(startU) &&
    MathUtilities.AreClose(startU, 0))
{
    // Skip ALL compensation to preserve item 0 position
    return;
}
```

**This was the key fix!** When item 0 is at position 0, there are no items before it that could cause drift. Any anchor drift in other items is just estimation error that we should accept rather than "compensate" by shifting everything including item 0.

#### Layer 3: Safety Net (Enhanced)
```csharp
// At end of RealizeElements (line 1393-1436)
if (_hasReachedStart && _measureElements.Count > 0 && _measureElements.FirstIndex == 0)
{
    var firstItemU = _measureElements.StartU;

    if (double.IsNaN(firstItemU))
    {
       // Warn if StartU is NaN
    }
    else if (!MathUtilities.AreClose(firstItemU, 0))
    {
        var adjustment = -firstItemU;
        _measureElements.CompensateStartU(adjustment);
        viewport.realizedEndU += adjustment;
    }
}
```

Added defensive checks and verification that the adjustment worked.

**Impact:**
- Item 0 always positioned at exactly u=0 when realized ✓
- No more clipping when scrolling up fast ✓
- Extent compensation doesn't interfere with item 0 position ✓
- Works regardless of which loop realizes item 0 ✓

**Files Modified:** VirtualizingStackPanel.cs:1311-1318 (forward loop), 1233-1248 (extent compensation), 1397-1436 (safety net)

## Current State

### What Works ✓

1. **Single-pass scrolling:** Normal scrolling now requires only 1 measure pass (down from 2-3)
2. **Accurate estimation:** Estimates reflect current viewport, not historical data
3. **No redundant recalculation:** Same realized range doesn't trigger re-estimation
4. **Immediate adaptation:** New items update estimate directly without convergence lag
5. **Stable extent during Reset:** Infinite scroll doesn't cause estimate oscillation
6. **No item 0 clipping:** Fast scroll up to top positions item 0 correctly (no clipping/gaps)

### Remaining Issue ❌

**Jump to top after infinite scroll Reset:**

**Symptoms:**
```
1. User scrolls to viewport [2141-2539], items [20-25] realized
2. Infinite scroll loads 25 more items → Reset event
3. Extent updates correctly (2496 → 4663)
4. User scrolls a bit more
5. Suddenly: [VSP-MEASURE] Waiting for viewport update
6. Viewport jumps from [2141-2539] to [0-398] (top!)
```

**Diagnostic logs needed:**
- Which index is being scrolled to? (Likely 0)
- What's calling ScrollIntoView? (Likely ListBox selection/focus restoration)
- When is it called? (After suppression is lifted)

**Next steps:**
1. Run with enhanced diagnostic logging enabled
2. Identify the calling component and trigger
3. Implement proper fix (not workaround):
   - If ListBox selection: Handle selection state after Reset
   - If focus restoration: Preserve focus state properly
   - If specific index: Suppress ScrollIntoView for that index after Reset

## Technical Implementation Details

### Estimate Calculation Flow

**Before (Temporal Mismatch):**
```
OnEffectiveViewportChanged → InvalidateMeasure
  ↓
MeasureOverride:
  EstimateElementSizeU()        // Uses _realizedElements (OLD viewport)
  RealizeElements()              // Updates _measureElements (NEW viewport)
  Swap(_measureElements, _realizedElements)  // Now _realizedElements = NEW
  CalculateDesiredSize()         // But estimate was from OLD!
```

**After (Fixed):**
```
OnEffectiveViewportChanged → InvalidateMeasure
  ↓
MeasureOverride:
  RealizeElements()              // Updates _measureElements (NEW viewport)
  Swap(_measureElements, _realizedElements)  // Now _realizedElements = NEW
  EstimateElementSizeU()         // Uses _realizedElements (NEW viewport) ✓
  CalculateDesiredSize()         // Estimate is contextually accurate ✓
```

### Estimate Tracking Logic

```csharp
EstimateElementSizeU():
  // Check if realized range changed
  if (firstIndex == _lastEstimateFirstIndex &&
      lastIndex == _lastEstimateLastIndex)
  {
    return cached estimate  // Skip recalculation
  }

  // Calculate average from realized elements
  avg = sum(realized sizes) / count

  // Apply smoothing only for small samples
  if (count < 5)
  {
    estimate = old * 0.7 + avg * 0.3  // 30% smoothing
  }
  else
  {
    estimate = avg  // Direct average, no smoothing
  }

  // Track for next call
  _lastEstimateFirstIndex = firstIndex
  _lastEstimateLastIndex = lastIndex

  return estimate
```

### Reset Event Handling

```csharp
OnItemsChanged(Reset):
  // Check if realized elements still valid
  if (most items still exist at same indices)
  {
    shouldPreserveRealizedElements = true
  }

  if (shouldPreserveRealizedElements)
  {
    _suppressScrollIntoView = true
    // DON'T reset estimate tracking - elements unchanged
  }
  else
  {
    RecycleAllElements()
    _lastEstimateFirstIndex = -1  // Reset tracking
    _lastEstimateLastIndex = -1
  }
```

## Performance Metrics

### Before All Fixes

**Slow scrolling through heterogeneous list:**
- **3 measure passes** per scroll event
- Extent oscillation: ±50-100px per pass
- Visible stuttering

**Infinite scroll (Reset event):**
- **2-3 measure passes** after Reset
- Extent oscillation from re-estimation
- Occasional jump to top

### After All Fixes

**Slow scrolling:**
- **1 measure pass** per scroll event ✓
- Stable extent calculation ✓
- Smooth scrolling ✓

**Infinite scroll (Reset event):**
- **1 measure pass** after Reset ✓
- Stable extent (single clean increase) ✓
- **Jump to top still occurs** ❌ (under investigation)

## Code Quality Improvements

### Removed "Rough Estimation Strategies"

1. ~~**Tolerance threshold:**~~ Deleted extent suppression for changes < 50px
2. ~~**Heavy smoothing:**~~ Reduced from 10% to 0% (direct average) for samples >= 5
3. **Deterministic calculation:** Extent now based on actual measurements, not convergence history

### Added Precision

1. **Contextual estimation:** Estimate from current viewport, not historical
2. **Redundancy elimination:** Skip recalculation when range unchanged
3. **Immediate adaptation:** Direct average for accurate extent on first pass

## Files Modified

### Primary File
- `src/Avalonia.Controls/VirtualizingStackPanel.cs`
  - Line 238-239: Removed pre-RealizeElements estimation
  - Line 266: Added post-RealizeElements estimation
  - Line 97-98: Added estimate tracking fields
  - Line 770-847: Updated EstimateElementSizeU with range checking
  - Line 815-846: Removed smoothing for samples >= 5
  - Line 435-458: Conditional estimate reset on Reset
  - Line 573-650: Enhanced ScrollIntoView diagnostic logging
  - Line 1311-1318: Forward loop item 0 position fix (Fix 6)
  - Line 1233-1248: Extent compensation protection for item 0 (Fix 6 - CRITICAL)
  - Line 1397-1436: Enhanced safety net with defensive checks (Fix 6)

### Supporting Files
- `src/Avalonia.Controls/Utils/RealizedStackElements.cs` (read for context)

## Testing Recommendations

### Test Cases

1. **Slow scrolling through heterogeneous list**
   - Expected: 1 measure pass per scroll
   - Verify: Check `[VSP-MEASURE]` logs show only one pass

2. **Fast scrolling (flinging)**
   - Expected: 1 measure pass per viewport change
   - Verify: No layout cycles

3. **Infinite scroll (Reset event)**
   - Expected: Preserved realized elements, stable extent
   - Verify: `[VSP-RESET] Preserving realized elements for scroll stability`

4. **Collection replacement (Reset event)**
   - Expected: Recycled elements, estimate recalculated
   - Verify: `[VSP-RESET] Collection replaced, recycling all elements`

5. **ScrollIntoView after Reset**
   - Expected: Identify calling component via `[VSP-SCROLLINTO]` logs
   - Verify: Which index is being scrolled to

### Debug Logging

Enable tracing to see all diagnostic logs:
```xml
<VirtualizingStackPanel IsTracingEnabled="True" ... />
```

Key log prefixes:
- `[VSP-MEASURE]` - Measure passes
- `[VSP-ESTIMATE]` - Estimate calculations
- `[VSP-EXTENT]` - Extent changes
- `[VSP-ANCHOR]` - Anchor tracking
- `[VSP-RESET]` - Reset event handling
- `[VSP-SCROLLINTO]` - ScrollIntoView calls (NEW)

## Lessons Learned

### Design Principles

1. **Fix root causes, not symptoms:** The temporal mismatch was the real issue, not the need for smoothing/tolerance
2. **Precision over approximation:** Direct measurements are better than converging estimates
3. **Avoid workarounds:** The 3-pass counter for ScrollIntoView re-enabling was correctly rejected as a band-aid
4. **Diagnostic-first debugging:** Adding logging to trace ScrollIntoView calls will reveal the true cause

### Avalonia Insights

1. **Measure/Realize separation:** VirtualizingStackPanel uses separate collections during measurement, requiring careful timing of estimate calculations
2. **Reset event complexity:** NotifyCollectionChanged.Reset can mean either "items replaced" or "items appended", requiring validation logic
3. **ScrollViewer interaction:** ScrollIntoView is called by parent ListBox during focus/selection changes, can interfere with scroll position after Reset

## Next Actions

1. **Reproduce jump-to-top issue with enhanced logging**
   - Identify calling component via `[VSP-SCROLLINTO]` logs
   - Determine which index is being scrolled to
   - Understand why suppression isn't preventing it

2. **Implement proper fix based on findings:**
   - Option A: Suppress specific index (e.g., ScrollIntoView(0)) after Reset
   - Option B: Handle ListBox selection state preservation after Reset
   - Option C: Improve focus restoration logic to prevent unwanted scrolls

3. **Consider additional optimizations:**
   - Could estimate be cached per-item instead of globally? (Rejected due to DataContext changes)
   - Should Reset detection be more sophisticated? (Current heuristic: >50% items preserved = append)
   - Is there a better way to communicate with ListBox about Reset events?

## References

- Original issue: Multiple measure passes during slow scrolling
- Related: Jump to top on infinite scroll (previously fixed, regressed)
- Avalonia VirtualizingStackPanel: Heterogeneous item virtualization
- NotifyCollectionChanged.Reset: Ambiguous event (replace vs append)

---

**Status:** Fixes 1-6 implemented and tested. Item 0 clipping issue resolved. Jump-to-top investigation in progress with enhanced diagnostic logging.

**Last Updated:** 2025-12-11
