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
| Move / Reset | `Clear()` — index→item mapping is no longer trustworthy |
| Replace | The replaced indices are dropped; the rest stand |
| Detach from visual tree | **Nothing.** The record survives detach |

Insert/remove **remap in place**: the moving entries are snapshotted, removed, then re-inserted at their
new keys — removing before re-inserting is what stops a shifted key landing on one that has not moved
yet, and it needs no sort (dictionary order is unspecified). An insert at or past the highest recorded
index allocates nothing at all. A prepend is still inherently O(N): every entry is re-keyed.

The running sum `_measuredSizesSum` is maintained **incrementally** at the single upsert site
(`RecordMeasuredSize`), never by sweeping the record — a sweep per measure pass would be O(items ever
measured), strictly worse than stock's O(realized window). An upsert whose value is bit-identical
writes nothing, so a scrolling session over settled content accumulates no rounding error at all;
genuine deltas go through Neumaier compensated summation, which keeps the incremental sum within a
rounding of a freshly computed one however many updates it has seen. Read it through
`MeasuredSizesSum`, never the field. This is why there is no periodic re-sum: the interval would be a
tuning constant.

**Surviving detach is deliberate.** On detach/re-attach — a `TabControl` page, a virtualized control
inside another virtualizing panel — the collection has not changed, so every recorded size is still
valid and throwing them away would only re-introduce estimation error on the way back. If the
collection *did* change while detached, the ordinary lifecycle rows above still apply, and anything
missed self-heals per the staleness contract below. An earlier version of this table claimed detach
cleared the record; the code never did.

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
2. IVirtualizingDataTemplate.GetKey(item)     → that key            (the only opt-in)
3. otherwise                                  → DefaultRecycleKey   (stock behaviour)
```

The `item is T` check **must come first** — otherwise items that are their own containers get
incorrectly wrapped. That ordering was a real bug fix. Covered by
`Item_That_Is_Its_Own_Container_Is_Not_Wrapped_When_Virtualization_Enabled`.

Step 1 is unconditional; step 2 applies only when
`ContentVirtualizationDiagnostics.IsEnabled && Presenter?.Panel is VirtualizingStackPanel`, and it
resolves the template through `GetEffectiveItemTemplate()` — the same path everything else keys off.
Stock behaviour is one shared pool under `DefaultRecycleKey`; an opted-in template gets one pool per
key.

**Virtualization is opt-in, and the code now says so.** There were once two more branches — automatic
`ITypedDataTemplate with DataType != null → DataType`, and an `item.GetType()` fallback — which meant
type-aware pools and skipped content-clearing applied to *every* `ItemsControl` over a
`VirtualizingStackPanel`, including plain XAML `<DataTemplate DataType="local:Foo">`. That silently
imposed the §9 view-lifecycle trade on every Avalonia user and contradicted the PR's own breaking-changes
text. Both are gone: nothing changes for anyone who has not asked for it.

`ContentVirtualizationDiagnostics.IsEnabled` (default `true`) is therefore a **kill switch, not the
opt-in** — it forces every `ItemsControl` back to stock recycling, which is how you establish whether a
layout problem comes from virtualization. Covered by
`IsEnabled_False_Forces_Default_Recycle_Key_And_Clears_Content`.

`MaxPoolSizePerKey` is honoured only for keys an `IVirtualizingDataTemplate` actually handed out
(`ItemsControl.GetMaxPoolSizePerKey`); `DefaultRecycleKey` pooling is uncapped. This matters because the
XAML `DataTemplate` implements `IVirtualizingDataTemplate` *unconditionally* with `MaxPoolSizePerKey = 5`,
so reading the cap off the template type alone capped stock pooling at 5 containers per key for every
`ItemTemplate` in XAML — a perf regression in the common case, inside a change whose headline claim is
perf. Covered by `Plain_DataTemplate_Does_Not_Cap_Recycle_Pool` and
`MaxPoolSizePerKey_Is_Respected_For_DataTemplate_With_EnableVirtualization`.

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
  / `DisplayMemberBinding` change, and on mutation of this control's own `DataTemplates` collection —
  the subscription is created lazily the first time the cache is, which is only on the
  `DataTemplates`-collection path, so an `ItemsControl` that never takes that path neither caches nor
  subscribes. Covered by `Mutating_DataTemplates_Invalidates_Template_Cache`.

  Two things a reviewer will ask about, both known:

  1. **`FindDataTemplate` walks *up* the tree**, so a template added to or removed from an *ancestor's*
     `DataTemplates`, or to `Application.DataTemplates`, is not picked up for a type already cached —
     nor is a change of resolution caused by reparenting the control. Hooking the whole ancestor chain
     for the lifetime of the control is not worth it; the supported way to change templates at runtime
     is to mutate the collection on the `ItemsControl` itself. Already-realized containers keep the
     template they were prepared with either way, exactly as in stock, where `ContentPresenter`
     resolves the template once when it builds its child.
  2. **On the `DataTemplates`-collection path the cache currently memoizes `null` and achieves
     nothing.** `VirtualizingStackPanel.CreateElement` calls `PrepareItemContainer` *before*
     `AddInternalChild`, so on an item type's first realization `container.FindDataTemplate(item, null)`
     runs on an **unparented** container, finds nothing, and that `null` is cached permanently.
     `ContentTemplate` is then never set on the container and the `ContentPresenter` does its own tree
     walk on every `CreateChild` — i.e. the layout-cycle motivation for the cache is not actually being
     served on the one path that needs it. The obvious fix is to add the child before preparing it, but
     that reorders container lifecycle relative to stock. **Open**: either fix the ordering or delete
     the cache as dead weight; do not describe it as working until one of those happens.
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

**A `Refresh()` is not a preservable Reset.** `ItemsControl.RefreshContainers()` — raised on an
`ItemTemplate`, `ItemContainerTheme` or `DisplayMemberBinding` change — reaches the panel as a
*synthetic* `Reset` via `ItemsPresenter.Refresh()` → `VirtualizingPanel.Refresh()`. The collection has
not changed, so *every* realized element matches its item, preservation always kicks in and
`PrepareItemContainer` is never called: on a virtualized `ItemsControl` those three properties had no
effect on already-realized containers. `VirtualizingPanel.Refresh()` is therefore `virtual`, and
`VirtualizingStackPanel` overrides it to recycle every realized element *before* delegating to the
base Reset path — which then sees an empty realized set, so neither preservation nor
`RetainMatchingContainers` (§5, also skips `PrepareItemContainer`) can hold a container back. Covered
by `ItemsControlTests.ItemContainerTheme_Can_Be_Changed_Virtualizing` and
`ItemsControlTests.ItemTemplate_Can_Be_Changed_Virtualizing`.

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
them or add virtualization-aware equivalents. Since virtualization became opt-in this trade is only
paid by templates that asked for it, which is what makes it acceptable at all.

**`Panel.Children` retains invisible pooled containers.** Stock's `RecycleElementOnItemRemoved`
unparented the container (`RemoveInternalChild`); this fork pools it instead, so it stays in
`Children` with `IsVisible = false`. That is the point — unparenting is exactly the detach/reattach
churn container-level virtualization exists to avoid, and §12 records that separate content pooling
failed partly because the child was still being detached. Note stock *already* retained the parent on
the ordinary scroll-recycle path; only the item-removed path changed.

It is not a ghost: invisible and absent from the realized set means nothing renders it, and focus
navigation filters on `IsEffectivelyVisible` so nothing navigates to it. But it *is* publicly
observable — anything enumerating `Panel.Children` and assuming every child is a live item will now
see extras. `ListBoxVirtualizationIssueTests.GhostItemTest_FocusManagement` asserted the old contract
and was updated to the new one.

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

**Making the recycle pool observable is harder than it looks.** Two approaches that do *not* work:

- **Shrinking the viewport** does not reduce the realized count. `root.ClientSize` shrink, with or
  without an explicit `InvalidateMeasure()`, leaves all realized elements in place — the extended
  viewport (`CacheLength`) swallows it.
- **Scrolling** drains the pool in the same measure pass that fills it, so the pool is ~0 whenever you
  look at it.

What works is **shrinking the item collection** so fewer containers are needed than are currently
realized; the surplus lands in the pool and stays there.

Also: with `DisplayMemberBinding`, items whose display value is an empty string collapse the container
width to 0, after which the panel realizes exactly one item. Give test items non-empty display values.

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

**How this inventory was wrong, so the next audit is done right.** It claimed completeness while a
fourth constant was still live: the `remainingItems <= 3` tail-realization block in `RealizeElements`
(§12). It was missed because the audit grepped for the *named* fields and constants the removal work
had catalogued — `_frozenExtentU`, `WarmupSampleSize`, and so on — and an inline literal in the middle
of a method has no name to grep for. Worse, it had a fork-added test asserting its behaviour, so the
suite was green and read as confirmation.

Auditing constants means **reading the layout methods**, not grepping for known names. And a green
suite is not evidence: a damper added together with a test that asserts the damper's own threshold will
always look correct. When a test's failure message quotes a threshold (this one said "since the last 2
were within the `<=3` threshold"), that is the tell that it guards a mechanism rather than a behaviour.

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
| Tail-realization heuristic in `RealizeElements` (realize all remaining items when the forward loop stopped within a few of the collection end) | `3` remaining items | By its own comment it existed so "the extent is based on actual measured sizes rather than estimates" — exactly the §2 root cause. Subsumed: scrolling to the end measures the tail, `unknownCount` reaches 0 and `CacheBasedExtentU` returns the exact total, so the last item is reachable in full. Covered by `Last_Item_Is_Reachable_And_Extent_Is_Exact_After_Scrolling_To_End` (red/green verified). It also over-realized on every viewport that ended near the collection end, which is what `ListBoxTests.Handles_Resetting_Items_With_Existing_Selection_And_AutoScrollToSelectedItem` caught. Its guard test `Last_Item_Not_Clipped_When_Few_Remaining_Items_Are_Larger_Than_Estimate` went with it: it demanded a measurement-accurate extent *before any scrolling*, which no virtualizing panel (stock included) can deliver. |

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
- **"`ITypedDataTemplate` auto-recycling was removed."** This claim was false for a long time and
  survived several rounds of review — the branch was still live in both `NeedsContainer<T>` and
  `ClearContainerForItemOverride`, which is how virtualization ended up on by default while the PR
  text advertised it as opt-in. It is *now* genuinely removed (§4), deliberately and with tests. Both
  states of this claim were once wrong; check the code, not the doc.

- **"These three test failures are pre-existing."** They were not. `ListBoxTests.Handles_Resetting_Items_With_Existing_Selection_And_AutoScrollToSelectedItem`,
  `ListBoxVirtualizationIssueTests.GhostItemTest_FocusManagement` and
  `ItemsControlTests.ItemContainerTheme_Can_Be_Changed_Virtualizing` were all introduced by this
  branch's first commit (`6812547229`) and all three pass at the merge-base. The verification that
  produced the wrong answer was `git stash` + re-run — which only removes *uncommitted* work, leaving
  every branch commit in place. **To check a claim about baseline behaviour, use a worktree at
  `git merge-base master HEAD`, not a stash.** (A fresh worktree needs
  `git submodule update --init --recursive` for `external/XamlX` before it will build.)
