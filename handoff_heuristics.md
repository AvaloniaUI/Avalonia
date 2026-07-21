# VirtualizingStackPanel — Heuristics Handoff

**Status:** internal fork `12.0.5.1-optiq02` / `feature/20259_virtualizingdatatemplate_master`
**Purpose of this doc:** enumerate every non-upstream heuristic the fork added to `VirtualizingStackPanel`, explain why each exists, why it blocks an Avalonia-UI PR in its current form, and what "fixed" means. This is the tech-debt checklist that must be worked down before any of this can be proposed upstream.

> Line numbers are approximate (they drift with edits) — anchor on the **method name**. All references are to `src/Avalonia.Controls/VirtualizingStackPanel.cs` unless stated.

---

## Background: why these heuristics exist

Stock Avalonia's `VirtualizingStackPanel` assumes item containers **measure deterministically** — the same item measures to the same size every pass. This fork is used with form item templates that do **not** hold that assumption:

- async image / plan loading (a container is 84px as a placeholder, then 292px once loaded),
- text wrapping and deferred bindings that change desired size across passes,
- `IVirtualizingDataTemplate`s that swap content by key.

Non-deterministic measurement drives a feedback loop: extent changes → `ScrollViewer` re-measures the panel → different items realize → different sizes → extent changes again → **oscillation / scroll drift / infinite layout passes**. Nearly every heuristic below is a *damper* bolted on to break that loop, plus a container-warmup/pooling layer for first-scroll performance.

**The upstreaming problem in one sentence:** these are symptom-level dampers with hard-coded magic numbers, not root-caused fixes, and several change observable behavior for the *deterministic* common case that upstream must not regress.

---

## Tiers at a glance

| Tier | What it governs | Can it misrender items? | Upstream blocker severity |
|------|-----------------|--------------------------|---------------------------|
| **A** | container ↔ index mapping (keep/reuse/recycle decisions) | **Yes** | High — correctness |
| **B** | size / position estimation (scrollbar extent, scroll feel) | No | Medium — behavior/quality |
| **C** | anti-oscillation & loop-breaking dampers | No | High — magic numbers, non-determinism, hides root cause |

Only **Tier A** can produce a wrong-index / stale-content render (the class of bug we just fixed). Tier B/C affect scrollbar accuracy and scroll smoothness, but they are the most *un-upstreamable* because they are tuning constants.

---

## Tier A — container ↔ index decisions (correctness)

These decide which realized container maps to which item index. A wrong decision here renders the wrong item at a position — the visible-corruption class of bug.

### A1. Reset "preserve realized elements" heuristic — ✅ FIXED (reference case)
- **Where:** `OnItemsChanged`, `NotifyCollectionChangedAction.Reset` branch.
- **Was:** `shouldPreserveRealizedElements = preservedCount > _realizedElements.Count / 2;` — keep all realized containers if a bare majority still match their index by `DataContext`.
- **Bug:** a mid-list insert/remove coalesced into a single `Reset` (DynamicData `Bind` reset-threshold) leaves the prefix matching but everything after the edit point shifted. A bare majority preserved the whole **stale** mapping → shifted items pinned to the wrong containers → children rendered under a later headline.
- **Fix (shipped, commit `bb50f4b60e`):** preserve **only** when *every* realized element is still valid at its index (`preservedCount == realizedCount`); any partial match falls through to the full-reset path. Regression test: `Reset_With_MidList_Insert_Realizes_Shifted_Items_At_Correct_Index`.
- **Upstream note:** this whole "preserve on Reset for scroll stability" concept is itself non-upstream. Stock VSP treats `Reset` as a full rebuild. Before a PR, decide whether the append/infinite-scroll optimization is worth carrying at all, or whether the scroll-anchor system should handle it instead (see B/C).

### A2. `RetainMatchingContainers` + disjunct-recycle reuse
- **Where:** `RetainMatchingContainers` (~2013); callers in `MeasureOverride` disjunct branch (~386) and the `Reset` else-branch (~653).
- **What:** on a recycle-all, walk realized elements, and for any whose `DataContext` matches an item in the estimated new viewport, pull it out and re-key it for reuse instead of recycling+re-preparing.
- **Risk assessment:** **correctness-safe by construction** — keyed on the item reference, so a container is only ever reused for the *same* item it already held. It cannot create a wrong-index mapping.
- **Upstream blocker:** it depends on the viewport *estimate* (A3/B) to pick the retain range, and on `_scrollAnchorProvider` unregister/re-register bookkeeping. It's an optimization layered on the estimation stack; upstream will want it justified with before/after prepare-count benchmarks (the existing tests `Inserting_Item_Before_Viewport_Reuses_Matching_Containers_Without_Remeasure` and `Collection_Reset_With_Reorder_Reuses_Matching_Containers_Without_Remeasure` are a start but assert only loose `<= 3` bounds).

### A3. `viewportIsDisjunct`
- **Where:** `CalculateMeasureViewport` (~1129): `disjunct = anchorIndex < FirstIndex || anchorIndex > LastIndex`.
- **What:** if the new anchor falls outside the realized range, recycle everything and re-anchor.
- **Risk:** trivial boolean, low risk on its own — but it is the gate that triggers A2 and the estimate-driven retain range, so its correctness is coupled to the estimate being sane.

**Tier A definition of done:** every keep/reuse/recycle decision is either (a) keyed on item identity (safe), or (b) provably total (handles insert, remove, move, replace, reorder, and coalesced Reset). Add adversarial tests for each `NotifyCollectionChangedAction` **and** for the DynamicData "edit coalesced into Reset" shape.

---

## Tier B — size / position estimation (extent & scroll feel)

Inherent to any virtualizing panel, but this fork's variants add smoothing/caching constants that change behavior.

### B1. `EstimateElementSizeU` with EMA smoothing
- **Where:** `EstimateElementSizeU` (~1175).
- **What:** average `DesiredSize` of realized+measured elements; **then** blend with the previous estimate using exponential smoothing `smoothingFactor = 0.3` **when** the realized range overlaps the previous range by `> 50%`. Skips smoothing when the range is "mostly new" to adapt fast on big jumps. Also short-circuits re-estimation when the realized range is unchanged.
- **Why:** without smoothing, one outlier item (async image 292px vs placeholder 84px) entering/leaving the realized set on alternating passes swings the estimate ~50% and causes ~2000px extent oscillation.
- **Upstream blocker:** `0.3` and the `> 50%` overlap gate are unexplained magic numbers. Initial seed `_lastEstimatedElementSizeU = 25` is also arbitrary. Upstream will ask: why EMA and not median-of-realized? why these constants? is behavior for uniform-size items (the 99% case) *identical* to stock?
- **Fix direction:** justify or eliminate. Prefer a scheme that is provably a no-op for deterministic uniform items, and make any smoothing constant a documented, ideally non-magic derivation (or a property with a sane default).

### B2. Anchor / unrealized-position estimation
- **Where:** `GetOrEstimateElementU`, `GetOrEstimateAnchorElementForViewport` (~1113–1150), `EstimateDesiredSize` (~1156).
- **What:** position/size of not-yet-realized items ≈ `index × estimate`; anchor recovered from offset ÷ estimate.
- **Upstream status:** this is close to what stock VSP does; the concern is only that it consumes B1's fork-specific estimate. Mostly acceptable once B1 is cleaned up.

**Tier B definition of done:** estimation is deterministic and a verified no-op vs. stock behavior for uniform items; every constant is derived or documented; mixed-height behavior covered by tests asserting extent bounds (not just "doesn't crash").

---

## Tier C — anti-oscillation & loop-breaking dampers (the biggest upstream blocker)

These exist purely to survive non-deterministic measurement. They are the most fragile and the least upstreamable because they are tuned against specific in-house symptoms.

### C1. Extent-oscillation freezing
- **Where:** `CompensateForExtentChange` (~1401); state fields `_frozenExtentU`, `_extentOscillationSign/Count`, `_frozenStableCount`; a second freeze site ~1790 (`_frozenExtentU = contentEndU`).
- **What:** detect the extent alternating up/down across measure→arrange cycles; once confirmed, **freeze** the extent reported to the `ScrollViewer` so the scroll-anchor machinery stops chasing it; then slowly converge the frozen value back to reality.
- **Magic numbers:** freeze after `1` reversal if `|delta| > 100px` else `2` reversals; ignore `|delta| < 2px` as noise; grow immediately, shrink only after `≥ 2` stable passes within `±5px`.
- **Upstream blocker:** freezing the reported extent means the scrollbar can be **wrong on purpose**. That is a hard sell upstream. All five constants (100, 2, 5, 2, 2px) are symptom-tuned.
- **Fix direction:** the real fix is to stop the oscillation at its source — deterministic measurement, or measure-once/cache-desired-size per item, or an opt-in "assume templates may resize" mode with a documented contract. Freezing should be a last resort behind a flag, not default behavior.

### C2. Layout-cycle breaker
- **Where:** `MeasureOverride` (~309–333).
- **What:** after `_consecutiveMeasureCount > 1`, return the previous `DesiredSize` without work and schedule a deferred `InvalidateMeasure` at `Loaded` priority. Counter reset in `ArrangeOverride`, `OnEffectiveViewportChanged`, `OnItemsChanged`.
- **Why:** bounds the "measure → different size → re-measure" loop to one pass.
- **Upstream blocker:** "one pass suffices" is an assumption; legitimate multi-pass layout (nested virtualization, deferred content that genuinely needs two passes) could be cut off, and deferring to `Loaded` can visibly lag a size change by a frame. Needs justification that it never drops a *legitimate* pass.

### C3. Zero-viewport guard
- **Where:** `MeasureOverride` (~303–307).
- **What:** when viewport collapses to 0×0 but elements are realized, return cached `DesiredSize` instead of recycling — preserves the scroll anchor across transient 0-size measures (e.g. tab switch).
- **Upstream status:** small and defensible, but still a behavior change vs. stock; document the scenario and cover with a test.

### C4. `ValidateStartU` resize compensation + `lockSizes`
- **Where:** `MeasureOverride` (~346–370) calling `RealizedStackElements.ValidateStartU`; `_suppressValidateStartU`.
- **What:** treat a `≥ 1px` change in a realized element's desired size as a real resize; update stored sizes in place and compensate `StartU` for items before the anchor to prevent scroll jump. `lockSizes` (active while extent is frozen) suppresses those updates. Fires at most once per measure→arrange.
- **Upstream blocker:** the `1px` threshold, the once-per-pass suppression, and the freeze-coupled `lockSizes` are all bespoke. This is tightly entangled with C1 — they must be redesigned together.

### C5. Container warmup / template-key sampling
- **Where:** `EnableWarmup` (default **false**, opt-in), `WarmupSampleSize` (default **50**, validated 1–1000), `PerformWarmup` (~2506), `DiscoverTemplateKeys` (~2459), `ClearObsoleteWarmupContainers`.
- **What:** sample the first `WarmupSampleSize` items to discover the set of template keys in use, then pre-instantiate a recycle pool per key on a background dispatcher tick to speed up first scroll. Re-tops-up after a `Reset` if new keys appear.
- **Upstream blocker:** sampling the first N items **assumes the head represents the whole collection's key distribution** — a collection whose first 50 items are one template but later items use another will be under-warmed (perf miss, not correctness). It also posts background work from a layout method. Opt-in default softens this, but upstream will want the pooling strategy separated from the sampling guess.
- **Fix direction:** drive the pool off actually-encountered keys (lazily grow) rather than a head sample; keep warmup strictly opt-in and side-effect-free w.r.t. layout correctness.

**Tier C definition of done:** either (a) the underlying non-determinism is fixed / given an explicit opt-in contract so these dampers can be deleted, or (b) each surviving damper is behind a documented property (off by default), every magic number is justified, and stock-VSP behavior for deterministic templates is proven unchanged by tests.

---

## Recommended order of attack (before PR)

1. **Lock down Tier A** — done for A1; add the exhaustive collection-change test matrix (insert/remove/move/replace/reorder + coalesced-Reset) so no other keep/recycle path can regress.
2. **Root-cause Tier C1/C4** — the extent-freeze + `ValidateStartU` pair is the heart of the fork and the biggest blocker. Decide the contract: does upstream VSP support resizing containers at all? If yes, design it deterministically (measure-once cache or explicit invalidation) so C1/C4 disappear.
3. **Clean Tier B1** — replace/justify the EMA constants; prove no-op for uniform items.
4. **Isolate Tier C2/C3/C5** behind documented, default-off properties (or remove) once C1 is root-caused; most of them are only needed *because* of the oscillation.
5. **Characterization tests + benchmarks** — before touching anything, capture current behavior (extent stability, prepare counts, scroll position) so the redesign can prove parity for deterministic content and improvement for non-deterministic content.

## Constants inventory (all currently hard-coded)

| Constant | Location | Meaning |
|----------|----------|---------|
| `0.3` | `EstimateElementSizeU` | EMA smoothing factor |
| `> 50%` overlap | `EstimateElementSizeU` | when to smooth vs. adapt |
| `25` | `_lastEstimatedElementSizeU` init | seed element size |
| `100px` | `CompensateForExtentChange` | swing size → freeze after 1 reversal |
| `2` reversals | `CompensateForExtentChange` | freeze threshold for small swings |
| `2px` | `CompensateForExtentChange` | extent-delta noise floor |
| `5px` / `2` passes | `CompensateForExtentChange` | frozen-value convergence band/dwell |
| `1px` | `ValidateStartU` | "real resize" threshold |
| `> 1` | `MeasureOverride` | consecutive-measure loop breaker |
| `50` / `1..1000` | `WarmupSampleSize` | template-key sample size + bounds |

Every row above must be derived, documented, made configurable, or eliminated before this is upstream-ready.
