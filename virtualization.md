# Virtualization in this fork — design knowledge

Everything durable we learned building `#20259` (branch `feature/20259_virtualizingdatatemplate_master`,
upstream draft PR [AvaloniaUI/Avalonia#20993](https://github.com/AvaloniaUI/Avalonia/pull/20993)).

This is the *knowledge* document: how the design works, why it works, and which ideas were tried
and rejected. Open work lives in `handoff_pr.md`.

Line numbers drift — anchor on method names. Paths are relative to the repo root.

---

## 1. The problem this fork solves

Stock `VirtualizingStackPanel` assumes item containers **measure deterministically**: the same item
measures to the same size on every pass. Our form templates violate that:

- async image / plan loading (a container is 84px as a placeholder, then 292px once loaded),
- text wrapping and deferred bindings that change desired size across passes,
- `IVirtualizingDataTemplate`s that swap content by key,
- OAPH-backed properties that propagate a pass late, so pass 1 sees the old value and pass 2 the new.

That drives a feedback loop: extent changes → `ScrollViewer` re-measures the panel → different items
realize → different sizes → extent changes again → **oscillation / scroll drift / infinite layout
passes**.

## 2. The one root cause, and the fix

`EstimateElementSizeU` estimated the size of un-realized items from an average over the
**currently-realized** set. Because the realized set's *membership* changes every scroll pass, the
scalar estimate swings → reported extent swings → the loop above. Every damper this fork used to
carry was fighting that single loop.

**The fix is a persistent per-item size record.** `_measuredSizes` (`Dictionary<int, double>`,
last-measured `sizeU` per item index) plus a published running sum `_measuredSizesSum`:

- `EstimateElementSizeU` upserts every currently-realized, measure-valid element's size, then
  returns the **mean over all recorded sizes** — a function of *everything ever measured*, not of the
  current window.
- `CacheBasedExtentU(itemCount)` returns `knownSum + unknownCount * mean`. When every item has been
  measured, `unknownCount == 0` and the extent is *exactly* the true total — the correct bottom edge.

Why this is upstreamable where the dampers were not:

- **Provable no-op for uniform items.** Every recorded size is equal, so the mean equals that size —
  identical to stock's realized average. Covered by `Adversarial_Uniform_*`.
- **Distribution-agnostic.** No constant assumes a height distribution. Verified against uniform,
  bimodal, extreme outliers (20px rows + occasional 2000px), monotonic ramp, async-grow (84→292),
  and cross-region shapes.
- **Reproducible.** Revisiting an offset reports the same extent, because the estimate no longer
  depends on which window happens to be realized. Covered by
  `Reported_Extent_Is_Reproducible_When_Revisiting_An_Offset_With_Few_Realized_Items`.
- Realizing more items only *sharpens* the estimate; it can never oscillate. Cross-region estimate
  spread went 26000px → 1567px when this landed.

### Record lifecycle

| Event | Action |
|---|---|
| Insert *n* at *i* | Entries `>= i` shift up by *n*; inserted slots left unrecorded (unknown) |
| Remove *n* at *i* | Entries in range dropped; entries after shift down |
| Move / Reset / Replace | `Clear()` — index→item mapping is no longer trustworthy |
| Detach from visual tree | `Clear()` |

Insert/remove **remap by allocating a fresh dictionary**. See `handoff_pr.md` §2d for the cost.

### Cache staleness contract — do not "fix" this as a bug

While an item is **out of view** its `_measuredSizes` entry can go stale if its VM data changes (no
container exists to re-measure it). This is **safe by construction**: the record is consulted *only*
to estimate items that are **not currently realized** (the extent tail + un-realized positions). A
realized/visible item is *always* sized by its live measure, never the record. On re-realization the
container is re-measured and the upsert overwrites the stale entry, so the extent self-heals.

Worst case = a transient scrollbar-length error for off-screen mutations, corrected on scroll-in.
Inherent to all virtualization, and strictly better than stock, which remembers nothing.

**The one real risk** is that the self-heal depends on re-realized containers actually being
re-measured — especially via `RetainMatchingContainers` reuse, which deliberately skips
`PrepareItemContainer`. Guarded by three tests:
`Off_View_Height_Change_Is_Remeasured_On_Scroll_Back`,
`Retained_Container_Reuse_Remeasures_Changed_Item`,
`Visible_Item_Height_Change_Uses_Live_Measure`.

## 3. Invariants that survive

**Item 0 is at `u == 0`.** Realization walks backwards from an *estimated* anchor, so it can arrive at
item 0 with a non-zero `u` — that is accumulated estimation error, not a real offset. When
realization reaches item 0 (`_hasReachedStart`), the realized block is re-based to `StartU == 0`.
Covered by `Item_Zero_Is_Always_At_Position_Zero`.

Two earlier in-loop `if (index == 0) u = 0` clamps in `RealizeElements` were redundant with this and
are gone.

**Pre-anchor resize compensation is arithmetic, not a heuristic.** The anchor sits at
`StartU + Σ sizes before it`. If that sum grew by `preDelta`, `StartU` must shrink by the same amount
or the anchor visually jumps. Hence `ValidateStartU`'s `StartU -= preDelta`. No constants.

Two related hazards were fixed along the way:

- Sizes are recorded *and* re-checked through **one** accessor (`GetElementSizeU`), so the two can
  never disagree and read as a resize on every pass.
- Resize detection uses `MathUtilities.AreClose(..., LayoutHelper.LayoutEpsilon)` — Avalonia's own
  layout-significance epsilon — so only floating-point noise is absorbed, not real sub-pixel change.
  Covered by `Sub_Pixel_Pre_Anchor_Resize_Is_Compensated_Not_Absorbed`.

`ValidateStartU`'s signature is now
`bool ValidateStartU(int anchorIndex, Func<Control, int, double> getSizeU, out double preDelta)`.

## 4. Container-level virtualization

**Core principle: container + child = one reusable unit.** Stock recycles containers but destroys
their content. This fork keeps the child attached and pools the pair.

Flow when an item scrolls out:

1. `VSP.RecycleElement` → `ItemsControl.ClearContainerForItemOverride`
2. Virtualization detected → **skip** clearing `Content` / `ContentTemplate`; the child stays attached
3. `IsVisible = false`, container pushed to `VSP._recyclePool` under its recycle key

And back in:

1. `VSP.GetRecycledElement` finds a container for that key — child still attached
2. `PrepareContainerForItemOverride` sets `Content` to the new item
3. `ContentPresenter.CreateChild` passes the attached `oldChild` to the template's `Build`, which
   returns it unchanged → `newChild == oldChild` → **no visual tree mutation**
4. `DataContext` changes, bindings update, layout refreshes with new data

### Recycle-key selection (`ItemsControl.NeedsContainer<T>`)

```
1. item is T                                  → recycleKey = null   (item is its own container)
2. IVirtualizingDataTemplate.GetKey(item)     → that key            (explicit)
3. ITypedDataTemplate with DataType != null   → DataType            (automatic)
4. otherwise                                  → item.GetType()      (type-aware fallback)
```

The `item is T` check **must come first** — otherwise items that are their own containers get
incorrectly wrapped. That ordering was a real bug fix.

Step 1 is unconditional; steps 2–4 apply only when
`ContentVirtualizationDiagnostics.IsEnabled && Presenter?.Panel is VirtualizingStackPanel`. Stock
behaviour is one shared pool under `DefaultRecycleKey`; this fork gives one pool per key.

> `ContentVirtualizationDiagnostics.IsEnabled` currently defaults to **`true`**, so this is on by
> default for every `ItemsControl` over a `VirtualizingStackPanel`, not opt-in. See `handoff_pr.md`
> §2b — this is unresolved and contradicts the PR text.

### `IVirtualizingDataTemplate`

```csharp
public interface IVirtualizingDataTemplate : IRecyclingDataTemplate
{
    object? GetKey(object? data);   // null = no pooling for this data
    int MaxPoolSizePerKey { get; } // container-pool cap
    int MinPoolSizePerKey { get; } // warmup target; only consulted when warmup is enabled
}
```

It extends `IRecyclingDataTemplate` (**not** `IDataTemplate`), so `Build(data, existing)` comes from
the base interface. XAML `DataTemplate` implements it with `EnableVirtualization` (default `false`),
`MaxPoolSizePerKey = 5`, `MinPoolSizePerKey = 2`; `GetKey` returns `null` unless
`EnableVirtualization` is set.

### Supporting machinery in `ItemsControl`

- **`_templateCache`** (`Dictionary<Type, IDataTemplate?>`) — memoizes `FindDataTemplate` per item
  type so measure doesn't repeatedly walk the tree. Necessary for `DataTemplates` *collections*;
  repeated tree walks during measure were themselves a layout-cycle source. Cleared on `ItemTemplate`
  / `DisplayMemberBinding` change — **but not on `DataTemplates` mutation**, see `handoff_pr.md` §2e.
- **`SetIfUnsetOrDifferent`** — unlike `SetIfUnset`, forces the update when a recycled container
  already holds a (different) value. Without it a reused container keeps the previous item.
- **`ContentPresenter.BeginBatchUpdate` / `EndBatchUpdate`** — `Content` and `ContentTemplate` must
  land together, or `UpdateChild` runs once with a mismatched pair and rebuilds the child for
  nothing. `_deferUpdateChild` suppresses `UpdateChild` in between.

### Why `IRecyclingDataTemplate` alone was not enough

- **Instance-equality constraint.** `ContentPresenter.CreateChild` only recycles when
  `rdt == _recyclingDataTemplate` — the *same template instance*.
- **No pool.** `ClearItemContainer` clears `ContentProperty`, destroying the content before anything
  could save it.
- **No cross-container reuse.** Nothing survives container recycling.

## 5. `RetainMatchingContainers` (disjunct-scroll reuse)

On a viewport jump, `CalculateMeasureViewport` marks the viewport **disjunct**
(`anchorIndex < FirstIndex || anchorIndex > LastIndex`) and everything is recycled. Before that,
`RetainMatchingContainers` walks the realized elements and pulls out any whose `DataContext` matches
an item in the estimated new viewport, into `_retainedForReuse`; their slots are nullified
(`RealizedStackElements.NullifyElement`) so `RecycleAllElements` skips them. `GetOrCreateElement`
checks `_retainedForReuse` first and gives a match a lightweight `ItemContainerIndexChanged` instead
of a full `PrepareItemContainer`. `RecycleUnusedRetainedContainers` cleans up the rest.

**Correctness-safe by construction**: keyed on the item reference, so a container is only ever reused
for the *same* item it already held. It cannot create a wrong-index mapping. It does depend on the
viewport *estimate* to pick the retain range, and on `_scrollAnchorProvider` unregister/re-register
bookkeeping.

Because it skips `PrepareItemContainer`, it is the one path that can defeat the staleness self-heal
of §2 — hence `Retained_Container_Reuse_Remeasures_Changed_Item`.

## 6. Reset handling and the coalesced-edit bug

Stock treats `NotifyCollectionChangedAction.Reset` as a full rebuild. This fork preserves realized
containers across a `Reset` for scroll stability (the infinite-scroll / append case).

**The bug that made this dangerous** (fixed in `bb50f4b60e`): the gate was a *bare majority* —
`preservedCount > _realizedElements.Count / 2`. A mid-list insert or remove coalesced into a single
`Reset` (DynamicData's `Bind` reset-threshold does this) leaves the prefix matching but shifts
everything after the edit point. A bare majority therefore preserved the whole **stale** mapping →
shifted items pinned to the wrong containers → children rendered under a later headline. This is the
visible-corruption class of bug.

**The fix:** preserve only when *every* realized element is still valid at its index
(`preservedCount == realizedCount`); any partial match falls through to the full-reset path.

Test note worth keeping: an edit must land **past the middle of the realized window** for a bare
majority to still match, which is why the `LateInWindow` position in
`Collection_Edit_Keeps_Every_Container_On_Its_Own_Item` (7 edit kinds × 4 positions) is the case that
actually catches this. An edit near the front does not reproduce it.

Whether upstream wants Reset-preservation *at all* is still open.

## 7. The zero-viewport guard, and the story that justifies it

`OnEffectiveViewportChanged` returns immediately when the incoming effective viewport (intersected
with `Bounds.Size`) is empty (`Width <= 0 || Height <= 0`), without touching `_viewport`, the extended
viewports, the extent, or invalidating measure.

This looks like a defensive nicety. It is not — it is load-bearing, and here is the on-device trace
that proves it. Symptom: in a heterogeneous `ListBox`, a photo field is ~50px while it has no image;
the user scrolls to it, taps its button, takes a picture, returns — and **the scroll position has
jumped up**. The cause is not the photo's 50px→large resize. It is the window viewport collapsing to
0×0 while the camera activity is in front:

| Seq | Event |
|---|---|
| `#01478` | Baseline: `vpY=983.8 startU=435.9 realized=[2..9] anchor=item3@821.7` |
| `#01499` | Camera launches → `OnEffectiveViewportChanged: effVp=0,983.8,0,0` (**0×0**) → needsMeasure |
| `#01504` | Empty-viewport measure → treated as **disjunct** → recycles everything → `realized=[0..0] startU=0` (**anchor lost**) |
| `#01532` | Return from camera: viewport correctly restored to `vpY=983.8` |
| `#01535` | But panel state is item0/startU=0 → `CaptureViewportAnchor: anchorIdx=-1` (unrecoverable) |
| `#01544` | ScrollViewer clamps the now-out-of-range offset → **`effVp` jumps `983.8 → 123.7`** |

Chain: hidden window → empty viewport → disjunct recycle resets `StartU=0` and drops all realized
items → on restore the ScrollViewer clamps the out-of-range offset → scroll jumps up.

There were once **two** mutually-redundant guards, one here and one in `MeasureOverride`. Either
alone preserved the scroll position; only removing both broke it. We kept this one because it
rejects the meaningless viewport *at the source*, so no downstream state is polluted, and deleted the
`MeasureOverride` duplicate. Covered by
`Collapsing_Viewport_To_Empty_And_Restoring_Preserves_Scroll_Position` (red/green verified: with the
guard disabled `FirstRealizedIndex` collapses 20 → 0 across the round-trip).

Any tab switch, minimise, or foreign activity produces the same 0×0 viewport — this is not
camera-specific.

## 8. Warmup (opt-in, default off)

`EnableWarmup` (default `false`) pre-creates and pre-measures containers on a background dispatcher
tick so the first scroll doesn't pay for container construction.

The pool grows off the template keys the panel has **actually needed a container for**
(`_encounteredRecycleKeys`), scheduling a top-up when a new key appears and forgetting keys the
collection no longer contains. Depth per key is `IVirtualizingDataTemplate.MinPoolSizePerKey`, else
`DefaultWarmupPoolSizePerKey` (`3`).

The original design sampled the **first N items** (`WarmupSampleSize`, default 50) to discover the
key set. That assumes the head of the collection represents the whole collection's key distribution —
false for any grouped or sorted list whose kinds are not all present at its head (perf miss, not a
correctness bug). Removed. Covered by
`Warmup_Pools_Keys_First_Encountered_Outside_The_Head_Of_The_Collection` and
`Warmup_Forgets_Template_Keys_The_Collection_No_Longer_Contains`.

Historical bug worth not re-introducing: `PerformWarmup` once used `alreadyRealized` (the total
realized count across **all** types) as the start index into `matchingItems` (a list for **one**
recycle key), so with 10 realized elements and 5 matching items the loop `for (i = 9; i < 5; i++)`
never executed and warmup silently created nothing.

## 9. Accepted trades — document these in any upstream PR

**Templates that never settle now iterate to the layout manager's cap.** Removing the layout-cycle
breaker means a template reporting a *different size on every measure* drives repeated measure passes
until Avalonia's `LayoutManager` hits its own iteration cap, instead of being hard-capped at one pass
per layout cycle by the panel.

This is deliberate. "One pass suffices" was an assumption; the cap also dropped legitimate work
(a second resize inside one measure→arrange cycle never reached `ValidateStartU`); and the deferred
re-measure lagged real size changes by a dispatcher tick, applying a size change at the *next* scroll
position and producing the very jump it was meant to prevent. Templates that *settle* — async
images, deferred bindings, text that wraps once it knows its width, i.e. essentially all real
content — converge, which is what the tests assert. A template that never settles is a defect in the
template, and it behaves the same way in a plain `StackPanel`.

**View lifecycle events do not fire as templates expect.** Because the child stays attached across
recycling, `Loaded`/`Unloaded` and added/removed-to-visual/logical-tree do not fire per item. A
template that forwards those to its view model will not see them. Unresolved: whether to synthesise
them or add virtualization-aware equivalents.

## 10. Test harness knowledge

`protected internal virtual double AdjustElementSize(int index, double measuredSizeU)` is the seam
tests use to inject non-deterministic measurement. It is called wherever the panel needs an element's
size.

Test subclasses in `VirtualizingStackPanelTests.cs`: `VirtualizingStackPanelCountingMeasureArrange`,
`VirtualizingStackPanelWithInstability`, `VirtualizingStackPanelWithSubPixelNoise`,
`VirtualizingStackPanelAsyncGrow`.

**Two harness bugs made tests pass for the wrong reason. Don't reintroduce either.**

1. `VirtualizingStackPanelWithInstability` flipped element sizes by **measure-pass parity**, which
   never settles. No panel can converge against that, so such a model can only ever demonstrate that
   *some* hard cap exists — it cannot show that layout converges. It now perturbs each item once and
   settles, like real async content.
2. The "does not cause layout cycle" tests bounded **`AdjustElementSize` call counts** as a proxy for
   layout work. That hook is consulted wherever a size is needed, not only during realization, so it
   never measured layout cost. They now bound actual container measures (`ContainerMeasures`).

**Deterministic measurement doesn't reach corrective paths.** This is why so much of the original
test suite was green-but-toothless: the guards exist for estimation error and measurement
instability, which a stable test environment never produces. Every removal in this work was verified
**red→green** — re-introduce the heuristic, confirm a *specific* named test catches it, and confirm no
other test does. Do the same for anything added here.

Internal test seams: `TryGetMeasuredSizeForTesting(int, out double)`, `RecyclePoolForTesting`.

Run the suite with:

```
dotnet test tests/Avalonia.Controls.UnitTests/Avalonia.Controls.UnitTests.csproj -c Debug \
  -- --filter-class "Avalonia.Controls.UnitTests.VirtualizingStackPanelTests"
```

## 11. Constants

Every fork-added tuning constant is gone. Two values remain, neither a tuning constant:

| Value | Location | Why it is not a magic number |
|---|---|---|
| `25` | `_lastEstimatedElementSizeU` init | **Stock Avalonia**, not fork-added (`master:VirtualizingStackPanel.cs:73`). Only consulted before any item has been measured. |
| `3` | `DefaultWarmupPoolSizePerKey` | Pool depth used *only* when the template does not implement `MinPoolSizePerKey`. Affects first-scroll performance only — never layout or correctness — and only when warmup is explicitly enabled. |

`CacheLength` is **not** part of this work: it already exists on this repo's `master` via `#18626`
(commit `df1816bde5`). The branch diff against it is a single trailing space.

## 12. Removed — and why, so nobody re-adds it

All of these were symptom-level dampers with hard-coded magic numbers tuned against traces from our
proprietary UI. They are overfit by construction: they encode a particular height distribution. With
the §2 root cause fixed, each turned out to be either dead or replaceable by constant-free
arithmetic.

| Mechanism | Constants it carried | Why it went |
|---|---|---|
| EMA smoothing in `EstimateElementSizeU` | `0.3` smoothing, `> 50%` overlap gate, realized-range skip | The oscillation made visible in the estimate. Replaced by the `_measuredSizes` mean. |
| Extent-oscillation freezing (`CompensateForExtentChange`, `_frozenExtentU`, oscillation counters, `_frozenStableCount`) | `100px`, `2` reversals, `2px` noise floor, `5px`/`2` passes, and an undocumented `0.5`/`10%`/`>10`/`0.3` dampening branch | Froze the extent reported to `ScrollViewer` — i.e. made the scrollbar **wrong on purpose**. Probing showed the whole method was *dead*: disabling it changed no test outcome, because its anchor-drift compensation duplicated `ValidateStartU`'s constant-free `StartU -= preDelta`. The dampening branch's only real effects were skipping compensation and recording a fabricated extent. |
| Layout-cycle breaker (`_consecutiveMeasureCount`, `_measurePostponed`, deferred `Dispatcher.Post(InvalidateMeasure)`) | `> 1` | Also swallowed legitimate work — a second resize within one measure→arrange never reached `ValidateStartU`. See §9. |
| `ValidateStartU` bespoke logic (`lockSizes`, `_suppressValidateStartU`) | `1px` "real resize" threshold, once-per-arrange suppression | Reduced to constant-free arithmetic + `LayoutHelper.LayoutEpsilon`. See §3. |
| Warmup head sampling (`WarmupSampleSize`) | `50`, validated `1..1000` | Assumed the head represents the collection. See §8. |
| Duplicate zero-viewport guard in `MeasureOverride` | — | Redundant with the `OnEffectiveViewportChanged` guard. See §7. |
| In-loop `if (index == 0) u = 0` clamps (×2) | — | Redundant with the `_hasReachedStart` re-basing invariant. See §3. |
| Separate **content** pooling (`ItemsControl._contentRecyclePool`, `ReturnContentToPool`, `GetRecycledContent`, `DataTypeRecyclingMarker`, `_recycledContentToUse`, `PrepareRecycledContent`) | pool cap `5` | Two levels of pooling (container in VSP, content in ItemsControl) could not be reconciled: the child was pooled while still attached to its old container, so reattaching threw *"The control Border already has a visual parent"*. Superseded by container+child-as-one-unit (§4). Profiling had also shown ~no gain, because the child was still detached/reattached from the visual tree — full layout invalidation, nearly the cost of building new. |
| Distance-based disjunct gap tolerance (`GapBefore`/`GapAfter` pixel thresholds vs. viewport size) | viewport-relative thresholds | Replaced by the plain index test `anchorIndex < FirstIndex \|\| anchorIndex > LastIndex`. |
| `[VSP-*]` trace logging + `IsTracingEnabled` + the `ScrollTrace` static hook | — | Diagnostic scaffolding. The trace in §7 came from it; it lives on `release/12.0.3.1-optiq01` if a fresh device trace is ever needed. |

## 13. Decisions that reversed

Worth knowing, because the reasoning that produced the *wrong* answer is still tempting:

- **"Could the estimate be cached per-item instead of globally? — Rejected, because DataContext
  changes."** This is now the shipped design. The objection was real but has a narrow answer: the
  record is keyed on **item index**, remapped on structural change and cleared when the mapping
  becomes untrustworthy, and it is never consulted for a realized item. See §2.
- **"Freezing the extent is the fix for oscillation."** It was a symptom damper, and the mechanism
  turned out to be dead code once the estimate stopped swinging.
- **"One measure pass per layout cycle suffices."** It dropped legitimate work and lagged real size
  changes by a tick. See §9.
- **"Smoothing is needed, only the factor needs tuning."** No amount of smoothing fixes a
  window-dependent estimate; it only slows the swing down.
- **"`ITypedDataTemplate` auto-recycling was removed."** It was *not* — that branch is still live in
  both `NeedsContainer<T>` and `ClearContainerForItemOverride`. Recorded here because an earlier doc
  claimed otherwise and the claim survived several rounds of review.
