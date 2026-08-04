# Handoff — what's still open on #20259

Branch `feature/20259_virtualizingdatatemplate_master` → upstream draft PR
[AvaloniaUI/Avalonia#20993](https://github.com/AvaloniaUI/Avalonia/pull/20993) (base `main`).

Design knowledge lives in `virtualization.md`. This file is only the open work.

## 1. Status

Heuristic removal is **complete and verified**: grep across `src/` returns zero hits for
`_frozenExtentU`, `_extentOscillation*`, `CompensateForExtentChange`, `_consecutiveMeasureCount`,
`_measurePostponed`, `_suppressValidateStartU`, `lockSizes`, `WarmupSampleSize`,
`_lastEstimateFirstIndex/LastIndex`, `_lastMeasuredExtentU`, `_viewportAnchorU`, `IsTracingEnabled`,
`_contentRecyclePool`, `DataTypeRecyclingMarker`, `_recycledContentToUse`. The constants inventory in
`virtualization.md` §11 has no open row.

`VirtualizingStackPanelTests.cs`: 73 → 139 test methods (+66; ~226 cases with `Theory` expansion).
Full `Avalonia.Controls.UnitTests` green apart from three failures confirmed pre-existing by stashing
this work and re-running: `ItemsControlTests.ItemContainerTheme_Can_Be_Changed_Virtualizing`,
`ListBoxTests.Handles_Resetting_Items_With_Existing_Selection_And_AutoScrollToSelectedItem`,
`ListBoxVirtualizationIssueTests.GhostItemTest_FocusManagement`.

What is **not** done is everything outside `VirtualizingStackPanel.cs`: the `ItemsControl` /
`ContentPresenter` / `DataTemplate` half of the PR has five unaddressed defects and no test coverage
at all.

## 2. Defects to fix before the PR leaves draft

Ordered by severity. (a) and (b) are the two that must not ship as-is.

### (a) Every plain XAML `DataTemplate` caps the container pool at 5 — even with `EnableVirtualization="False"`

`src/Avalonia.Controls/VirtualizingStackPanel.cs`, `PushToRecyclePool`:

```csharp
if (ItemsControl?.ItemTemplate is Templates.IVirtualizingDataTemplate vdt &&
    pool.Count >= vdt.MaxPoolSizePerKey)
    return;
```

`DataTemplate` now implements `IVirtualizingDataTemplate` with `MaxPoolSizePerKey = 5`
*unconditionally* — the property does not consult `EnableVirtualization`. So any `ListBox` with an
`ItemTemplate` set in XAML silently drops containers past 5 per key. With a 20-row viewport that is
~15 container constructions per screen of scrolling where stock pooled all of them: a **perf
regression in the common case**, inside a PR whose headline claim is perf.

Two problems, one fix each:

1. Gate the cap on virtualization actually being active for this item — test the resolved recycle key
   / `GetKey(item) != null`, not merely "the template type implements the interface".
2. It reads `ItemsControl?.ItemTemplate` directly while key selection uses
   `GetEffectiveItemTemplate()`. A `DisplayMemberBinding` template or a `DataTemplates` collection
   therefore keys one way and caps another. Use the same resolution path.

No test catches this: `MaxPoolSizePerKey_Is_Respected` uses `FuncVirtualizingDataTemplate`, an
explicit `IVirtualizingDataTemplate`. Add the plain-`DataTemplate` case.

### (b) `ContentVirtualizationDiagnostics.IsEnabled` defaults to `true`, so nothing here is opt-in

`src/Avalonia.Controls/ItemsControl.cs:1033` — `public static bool IsEnabled { get; set; } = true;`

With the default, for **every** `ItemsControl` over a `VirtualizingStackPanel`:

- `NeedsContainer<T>` returns `recycleKey = item.GetType()` instead of `DefaultRecycleKey` — one pool
  per type instead of one shared pool;
- `<DataTemplate DataType="local:Foo">` hits the `ITypedDataTemplate` branch of
  `ClearContainerForItemOverride`, so `Content` / `ContentTemplate` are **no longer cleared** on
  recycle.

The PR's *Breaking changes* section says "Virtualization is opt-in via `EnableVirtualization="True"`".
That is contradicted by its own *Automatic virtualization* section and by the code. Pick one story:

- **Default-on** (current behaviour): then the view-lifecycle caveat in `virtualization.md` §9 is a
  breaking change for **every** Avalonia user, and the PR must say so plainly.
- **Default-off**: flip to `false` and the "automatic virtualization for templates with `DataType`"
  feature effectively disappears — decide whether it is worth keeping at all.

This decision blocks the PR text, so make it first.

### (c) Dead public API and a dead sample

`ContentVirtualizationDiagnostics.GetPoolStats` returns `null` unconditionally (`ItemsControl.cs:1040`,
comment: *"Content pooling has been removed"*), `ClearPools` is a no-op, and `ContentPoolStats` /
`PoolEntry` are public types nothing can populate.

`samples/ControlCatalog/Pages/ListBoxPage.xaml.cs` runs a 1-second `DispatcherTimer` forever to render
`"Cache: Empty"` from that null, and mutates the process-global `IsEnabled` from a page constructor —
a side effect that leaks into every other ControlCatalog page for the rest of the session.

Either delete all four types plus the timer, or repoint stats at `VirtualizingStackPanel._recyclePool`
(which is what actually pools now). Shipping unpopulatable public API into an API review is the worst
of the three options.

### (d) Unflagged cost of the root-cause fix

Not bugs, but the first things a reviewer will profile, and none of it is currently disclosed:

- `EstimateElementSizeU` sums the **whole** record on every measure pass
  (`foreach (var size in _measuredSizes.Values)`), so it is O(items ever measured) per pass, not
  O(realized window) like stock. Browse 100k items and every subsequent pass adds 100k doubles.
  The field comment nearby claims *"no separate sweep"* — the code contradicts it; fix the comment
  either way.
- `_measuredSizes` is cleared only on move/reset/replace/detach, never trimmed. One entry per item
  ever measured, unbounded in the browse-everything case.
- Insert/remove **allocate a fresh `Dictionary`** to remap indices — O(N) per edit. A prepend in an
  infinite-scroll list pays this on every batch.

Cheapest credible fixes: maintain `_measuredSizesSum` incrementally at the upsert site instead of
re-summing (the upsert already knows the old value); remap in place for the common append/prepend
shapes. Then state the residual cost in the PR rather than letting a reviewer find it.

### (e) `_templateCache` is never invalidated on `DataTemplates` mutation

`ItemsControl._templateCache` (`Dictionary<Type, IDataTemplate?>`, memoizes `FindDataTemplate` per item
type) is cleared only when `ItemTemplate` or `DisplayMemberBinding` changes
(`ItemsControl.cs:732`, `:740`). But `FindDataTemplate` walks **up the tree**, so a template added or
removed at runtime — on the `ItemsControl`, on any ancestor, or on `Application.DataTemplates` — is
ignored forever for any type already cached. The cache is also unconditional, not gated on
`IsEnabled`, so it affects every `ItemsControl` in the framework.

Hook the `DataTemplates` collection (and document the ancestor case as a known limitation if hooking
ancestors is too invasive).

## 3. Missing test coverage

**All 66 new tests are in `VirtualizingStackPanelTests.cs`.** Grep for
`IVirtualizingDataTemplate|ContentVirtualizationDiagnostics|EnableVirtualization|MinPoolSizePerKey`
across `tests/` outside that file returns nothing. The `ItemsControl` / `ContentPresenter` /
`DataTemplate` changes — +249, +32 and +39 lines — are untested.

| Gap | Why it matters |
|---|---|
| `IsEnabled = false` | The documented kill switch. No test proves fallback to stock (`DefaultRecycleKey`, content cleared). Existing tests set it `true` and "restore" it to `true` in `finally` — they never exercise `false`, and they leak global state if they throw. |
| `MaxPoolSizePerKey` with a plain XAML `DataTemplate` | Hides defect (a). |
| `DataTemplate.GetKey` / `Build(data, existing)` / `EnableVirtualization` | +38 lines, zero tests. The `EnableVirtualization && existing != null → return existing` branch is the whole feature. |
| `BeginBatchUpdate` / `EndBatchUpdate` / `_deferUpdateChild` | New internal API with an early return in `ContentChanged` — exactly where a missed `UpdateChild` yields a blank row. |
| `ITypedDataTemplate` skip-clear branch | Fires for every typed XAML template (see defect (b)); only the `IVirtualizingDataTemplate` branch is covered. |
| `NeedsContainer<T>` ordering (`item is T` first) | Called out as a fix in the PR. Nothing asserts a `Control` item isn't wrapped while virtualization is on. |
| `SetIfUnsetOrDifferent` | The reason a reused container picks up the new item at all. |
| `_templateCache` invalidation | Would catch defect (e). |
| **Horizontal orientation** | All 10 `Orientation.Horizontal` occurrences are in pre-existing stock tests. Zero horizontal coverage for the size record, adversarial shapes, warmup, collection matrix, or pre-anchor compensation. `_measuredSizes` is orientation-blind by design, so this is cheap — and a certain reviewer question. |
| Nested / recursive virtualization | Skip-clear on a `ContentPresenter` whose child is itself a virtualizing `ItemsControl`. |
| Warmup + `OnDetachedFromVisualTree` | Warmup posts dispatcher work; nothing asserts detaching before the tick neither leaks nor throws. |
| `_measuredSizes` growth / per-pass cost | Nothing bounds either. Would pin defect (d). |

Per `virtualization.md` §10, verify each new test **red→green**: break the specific code it covers,
confirm *that* test fails and no other does.

## 4. Rewriting the PR description

The current body is the most obsolete artifact in the whole change: **it advertises as features
exactly what we spent four phases deleting.** A reviewer reading it and then the diff will not find
the same PR twice.

### Delete

- *Updated/expected behavior* bullets: layout cycle breaker, sub-pixel tolerance (`< 1px`), extent
  oscillation detection + frozen extent, frozen-extent boundary clamping, stale anchor guard,
  item-0 correction, estimate caching, dampened extent compensation,
  skip-compensate-after-re-estimation.
- *Phase 4* items **2, 3, 4, 5, 6, 7, 8, 11, 12, 13, 15** — all deleted code. Items 1 (temporal
  mismatch) and 10 (anchor extrapolation) are *superseded* by the size record; fold them into it
  rather than listing them.
- *Phase 1*'s interface signature — it says `IVirtualizingDataTemplate : IDataTemplate`; it is
  `: IRecyclingDataTemplate`.
- *Phase 5*'s XAML sample — `WarmupSampleSize="100"` no longer exists.
- *Test coverage — "Added 22 new tests"* and its whole bullet list: it enumerates tests for removed
  heuristics (cycle breaker, frozen-extent clamping, estimate caching, extent dampening, NaN guard).
  Real figure: **66 new test methods, ~226 cases**.
- `MaxPoolSizePerKey="10"` from the adoption snippet, until defect (a) is fixed.
- The self-deprecating `ContentVirtualizationDiagnostics` note ("naming is terrible") — it invites a
  bikeshed instead of the API decision in defect (b). Replace with the named property you actually
  want reviewed.

### Fix in *Files changed*

- `ComplexVirtualizationPage.xaml` / `ComplexVirtualizationPageViewModel.cs` **do not exist**. The
  sample is `samples/ControlCatalog/Pages/ListBoxComplexLayoutPage.{xaml,xaml.cs}` +
  `FieldTemplateSelector.cs` + `Converter/MarkdownToInlinesConverter.cs` +
  `ViewModels/ListBoxComplexLayoutPageViewModel.cs`.
- `RealizedStackElements.cs`: no tolerance / suppression / `lockSizes`. What changed is
  `NullifyElement` plus `ValidateStartU`'s new signature
  `(int anchorIndex, Func<Control,int,double> getSizeU, out double preDelta)`.
- `ItemsControl.cs` is **+249 lines**, not "`NeedsContainer` check order": recycle keys, conditional
  clearing, `_templateCache`, `SetIfUnsetOrDifferent`, `GetEffectiveItemTemplate`,
  `ContentVirtualizationDiagnostics`.
- Drop `CacheLength` entirely — already on this repo's `master` via `#18626`; the branch diff is one
  trailing space.

### Keep — the honest story, and a better PR than the current one

1. **Root cause, not dampers.** Persistent per-item size record replaces the realized-window average;
   extent becomes reproducible across revisits and a provable no-op for uniform items. Lead with the
   constants-inventory table (`virtualization.md` §11) — it is the single strongest asset in this
   change: *every fork-added tuning constant is gone.*
2. **Tier A correctness fix.** Reset-preservation only when *every* realized element still validates;
   fixes wrong-index render on a mid-list edit coalesced into a `Reset`. Cite
   `Reset_With_MidList_Insert_Realizes_Shifted_Items_At_Correct_Index` and the 28-case
   `Collection_Edit_Keeps_Every_Container_On_Its_Own_Item` matrix.
3. **Constant-free anchor compensation** in `ValidateStartU` (`StartU -= preDelta`,
   `LayoutHelper.LayoutEpsilon` for float noise, one `GetElementSizeU` accessor for record-and-recheck).
4. **Container-level virtualization** — `IVirtualizingDataTemplate`, type-aware recycle keys, child
   stays attached. State the `IsEnabled` default honestly per defect (b).
5. **`RetainMatchingContainers`** + `NullifyElement` — keyed on item identity, so correctness-safe;
   flag that it wants prepare-count benchmarks.
6. **Opt-in warmup**, default off, pool grows off encountered keys, no head sampling.
7. **The zero-viewport guard**, with the camera trace from `virtualization.md` §7 as justification —
   it reads as a defensive nicety otherwise, and a reviewer will ask to delete it.
8. **Accepted trades**, verbatim from `virtualization.md` §9: never-settling templates iterate to the
   `LayoutManager` cap; view lifecycle events don't fire per item.

## 5. Still open before upstream will take this

- **Benchmarks.** Prepare/measure counts and scroll smoothness vs. stock are asserted as bounds in
  tests, never measured. `RetainMatchingContainers` in particular needs before/after prepare-count
  numbers, and defect (d) needs a large-collection profile.
- **Scope split.** The fork carries several separable features: Reset-preservation for the append
  case, `RetainMatchingContainers`, warmup, and `IVirtualizingDataTemplate`. Each is defensible alone
  but they are separate PRs. Landing the §4 item-1 root-cause fix + item-2 correctness fix on their
  own would be a far easier review than the current bundle.
- **Is Reset-preservation wanted at all?** Stock treats `Reset` as a full rebuild. This whole concept
  is non-upstream; the scroll-anchor system may be the right owner instead.
- **`AdjustElementSize` is a test seam** on a `protected internal virtual` member. Justify as public
  API or replace with an internals-visible hook.
- **View lifecycle events.** Synthesise `Loaded`/`Unloaded` for recycled containers, or add
  virtualization-aware equivalents? Currently an unanswered question in the PR body.
- **API review / naming** — `ContentVirtualizationDiagnostics`, `EnableVirtualization`,
  `MaxPoolSizePerKey` / `MinPoolSizePerKey`, `EnableWarmup`.
- **Docs PR** to `AvaloniaUI/avalonia-docs`, including the lifecycle-event caveat.
- Optional, from the existing checklist: implement for `VirtualizingPanel` too; add virtualization
  support to `FuncDataTemplate`.

## 6. Repo hygiene

Done: the nine AI-generated markdown files (`VARIABLE_HEIGHT_TEST.md`, `handoff_heuristics.md`,
`handoff_takepicture.md`, `heuristics_removal_plan.md`, `virtualizingstackpanel_perf.md`,
`virtualizingstackpanel_test_todo.md`, `plans/smoothscrolling.md`, `plans/virt_impro.md`,
`plans/virtualizingdatatemplate.md`, `plans/virtualizingdatatemplate_warmup.md`) are deleted; their
durable content is in `virtualization.md`.

Still to do:

- `plans/virtualizingdatatemplate_memory_{enabled,disabled}.png` are now orphaned — nothing references
  them. Delete, or attach them to the PR as the before/after memory evidence they were meant to be.
- Decide whether `virtualization.md` and `handoff_pr.md` themselves belong in the upstream PR. They
  probably do not — consider keeping them fork-only, or moving `virtualization.md` next to the code as
  XML docs / a `docs/` note before the PR leaves draft.
- Unrelated to this PR but sitting in the working tree: `Directory.Build.props`,
  `nukebuild/BuildParameters.cs`, `external/Avalonia.Controls.DataGrid/`, `external/Numerge/`.
