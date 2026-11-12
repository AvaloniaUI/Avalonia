# Server-side composition visuals

`ServerCompositionVisual` is the render-thread counterpart of `CompositionVisual`. This directory holds the per-frame
tree passes (dirty region collection and rendering), bitmap caches, adorner support, readback for hit-testing and
backdrop effects.

## Design

The update algorithm is close to the one in WPF (`WpfGfx`), with changes for features that WPF does not have. Some
older comments still use WPF's vocabulary. The glossary lists those words as synonyms. New comments use the first
term only.

- A visual does not store a world transform. It stores its own transform (to parent space) and its subtree bounds.
  A walk keeps the transform to the layer on a stack while it goes down the tree.
- A change marks the visual dirty and sets flags up its parent chain (`PropagateFlags`). The update walk visits only
  the parts of the tree that have those flags, so the cost of a frame depends on what changed, not on the tree size.
- The render walk skips a subtree when its bounds do not intersect the dirty region or the clip.
- A bitmap cache draws its subtree into its own surface and keeps its own dirty region. This part follows WPF's
  `BitmapCache` closely.

## Glossary

Synonyms that you can still find in older comments are listed in parentheses. Do not use them in new comments.

### Tree

- **visual** ('node'): a `ServerCompositionVisual`.
- **subtree** ('subgraph', 'sub-graph', 'sub-tree'): a visual and everything below it.
- **parent** ('ancestor', 'ascendant'): the visual's `Parent`. The **parent chain** is every parent up to the root.
- **local space** ('inner space'): the visual's own coordinate space, before its own transform.
- **parent space** ('outer space'): the coordinate space of the visual's parent, after the visual's own transform.
- **bounds** ('bbox', 'bounding box'): an extent in a stated coordinate space. `_ownContentBounds` and `_subTreeBounds`
  are in local space, `_transformedSubTreeBounds` is in parent space.
- **draw order** ('DFS order'): the order in which the render walk draws visuals. A visual earlier in draw order is
  **behind** a later one, a later one is **in front**.
- **dirty** / **clean** ('dirty for render'): whether the visual has to be redrawn this frame. See `PropagateFlags`.

### Passes

- **own properties pass**: `RecomputeOwnProperties` for every visual with changed properties.
- **adorner pass**: `UpdateAdorner` for every adorner whose transform can have changed.
- **update walk**: `UpdateContext`. Recomputes subtree bounds and collects the dirty region. Once per frame and
  target.
- **volatile pass**: `BackdropVolatilePass`. Between the two walks.
- **render walk**: `RenderContext`. Draws the visuals that intersect the dirty region.
- **readback pass**: writes the transforms that the UI thread reads for hit-testing.

See "Frame pipeline" for the order and the details.

### Layers

- **layer** ('host', 'layer host', 'target surface', 'backbuffer'): what a subtree is drawn into. There are two
  kinds:
  - **root layer**: what the `ServerCompositionTarget` draws into. Usually the render target, or an intermediate
    surface when the render target cannot be read back. It is not always a surface: it can be a recording context
    (PDF, picture), which has no pixels to read.
  - **bitmap cache layer**: the surface of a bitmap cache.

  Say which kind you mean. Use the bare word only for something that is true for both kinds.
- **intermediate layer** ('save-layer'): a temporary layer that the drawing backend opens inside a layer for
  opacity, an effect or an opacity mask. Whether its pixels can be read back depends on the backend. Never call it
  just "layer".
- **layer space** ('host space'): the coordinate space of a layer. Device pixels for the root layer, the local space
  of the cached visual for a bitmap cache layer.
- **layer pixels** ('surface pixels', 'tracker space'): pixel coordinates on the layer's surface. Same as layer space
  for the root layer. A bitmap cache layer has its own offset and scale (`DirtyRectSpaceMapping`): the scale covers
  the render scale, and a bitmap cache is also downscaled when the bounds of the cached subtree are too large for a
  surface.
- **bitmap cache** ('cache'): `ServerCompositionVisualCache`, created by `CacheMode="BitmapCache"`. Never call it just
  "cache", because backdrop effects have a cache mode too.
- **pushed** / **skipped** bitmap cache: the update walk pushes a bitmap cache (switches to its dirty region) only if
  something in its subtree is dirty. Otherwise the walk can still pass through it (for example when the cached visual
  itself moved), and the bitmap cache is skipped.

### Dirty region

- **dirty rect**: one rectangle that has to be redrawn.
- **dirty region** ('damage'): all dirty rects of one layer for the current frame. Every layer has its own.
- **working set**: the dirty rects that the update walk has collected so far for the current layer, before they are
  finalized (inflated for antialiasing, merged).
- **collect** ('capture', 'emit', 'deliver'): to add a dirty rect to a dirty region.

### Backdrops

- **backdrop**: the pixels that are already in the layer behind a visual at the moment the visual is drawn. It is
  what a backdrop effect samples. Other things that sample it (backdrop brushes in draw lists) are expected to share
  this infrastructure and the same backdrop.
- **backdrop visual**: a visual with a `BackdropEffect`. The effect is applied to the backdrop, then the visual's own
  content and children are drawn on top.
- **backdrop bounds**: where the effect is visible, in the backdrop visual's local space. The layout rect or the
  subtree bounds, see `BackdropEffectBoundsSource`. The effect is clipped to them, so they follow the visual's
  transform.
- **backdrop AABB**: the pixel-aligned, axis-aligned box around the backdrop bounds, in layer space. Everything that
  needs an axis-aligned area uses it: the backdrop snapshot and the dirty rect tests.
- **input** / **output**: the part of the backdrop that a backdrop visual reads (inside its AABB) and the pixels the
  effect produces from it.
- **sampling radius**: how far the effect reads from a given output pixel (for example the blur radius). In layer
  space it is `LayerSpaceSamplingRadius`.
- **retained** / **volatile**: the two ways a backdrop visual gets its input, selected by `BackdropEffectCache` (the
  **backdrop cache mode**).
  - retained: the input is kept in a backdrop snapshot and only its dirty parts are read again.
  - volatile: the input is read from the layer on every draw.
- **backdrop snapshot** ('store', 'retained state', 'backdrop texture'): `ServerCompositionBackdropSnapshot`. The
  retained copy of a backdrop visual's input. It has the size of the backdrop AABB.
- **refresh** ('ingest', 're-ingest', 'capture'): to copy pixels from the layer into the backdrop snapshot.
- **input dirty rect**: the part of a retained backdrop visual's input that changed and is not in the snapshot yet
  (`InputDirtyRect`). A refresh clears it.
- **output dirty rect** ('halo', 'output inflation'): the part of the output that changes because of an input dirty
  rect. It is the input dirty rect inflated by the sampling radius and clipped to the backdrop AABB. Not related to
  the antialiasing inflation of the dirty region.
- **backdrop state** ('record', 'registration'): `ServerCompositionVisualBackdropState`. Per backdrop visual: layer,
  AABB, retained or volatile, input dirty rect, snapshot.
- **backdrop visual list** ('registry'): `ServerCompositionTarget.BackdropVisuals`. Every backdrop visual attached to
  the target.
- **descent check** ('descent gate'): the test in the update walk that decides to visit the children of a clean
  visual, because a backdrop visual below it may have dirty input.
- **whole-AABB invalidation** ('expansion'): the volatile pass adds the whole AABB of a volatile backdrop visual to
  the dirty region as soon as any dirty rect touches it.

### Words to avoid

- "consume" for "use" or "read". Something that is consumed is gone afterwards. Say what happens: "reads", "uses",
  "clears".
- "gate". Say "check".
- "backdrop" for the visual. The backdrop is what is behind it.

## Frame pipeline

The first three steps run once per frame for the whole compositor (`ServerCompositor.ExecuteGlobalPasses`). Steps 4
to 7 run for every `ServerCompositionTarget` (`Update`, then `Render`). The last step runs once again.

1. **Apply batches**. Deserializes the changes that the UI thread committed and sets the properties of the server
   objects. A visual that leaves its parent adds its bounds to the parent as an extra dirty rect
   (`AddExtraDirtyRect`), because after the removal nothing else knows where it was. Animations are processed after
   the batches.
2. **Own properties pass**. For every visual that was enqueued by a property change: recompute its own transform,
   clip and content bounds, then set the dirty and bounds flags on it and up its parent chain (`PropagateFlags`).
   A transform change also notifies the adorners that depend on the visual.
3. **Adorner pass**. The own transforms are final here, so an adorner can compute where its adorned visual is. See
   "Adorners".
4. **Backdrop state recompute** (`RecomputeBackdropState`). For every backdrop visual in the list: find its layer,
   compute its AABB and decide retained or volatile. If the layer or the AABB changed, the visual is marked dirty.
   This step never adds dirty rects itself.
5. **Update walk**. Goes down the tree only where flags are set. Recomputes subtree bounds and collects the dirty
   region of every layer: the old and the new bounds of a dirty visual, and the extra dirty rects from step 1. Under
   a pushed bitmap cache the dirty rects go to the dirty region of that bitmap cache, and the bounds of the cached
   visual become dirty in the outer layer. For retained backdrop visuals the walk also collects input dirty rects:
   a dirty rect that is already in the working set when the walk reaches a visual belongs to something behind it,
   so it is a change of that visual's backdrop.
6. **Volatile pass**. A volatile backdrop visual cannot be redrawn in part, so whole-AABB invalidation applies. That
   new dirty rect is input for the backdrop visuals in front of it, so the pass repeats until nothing changes.
7. **Render walk**. Draws the dirty region of the root layer. A visual with a bitmap cache first redraws the dirty
   region of its bitmap cache layer (a render walk of its own that starts at the cached visual), then draws the
   bitmap cache layer into the outer layer. A retained backdrop visual first refreshes its snapshot from its input
   dirty rect, then draws the snapshot through the effect. A volatile one filters the layer contents directly.
8. **Readback pass**. See "Readback".

## Adorners

An adorner is a child of the adorner layer, but it has to be drawn where its adorned visual (`AdornedVisual`) is,
and that visual is in a different part of the tree. The adorner code is in `ServerCompositionVisual.Adorners.cs`
and its members have the `AdornerHelper_` prefix.

- **Transform**. In the adorner pass, an adorner sets its own transform to the transform from the adorner layer's
  parent to the adorned visual, combined with the adorner's own offset, scale and rotation
  (`UpdateAdorner`). The `RenderTransform` that the UI thread sets on an adorner is ignored, because it can be behind
  a transform that the compositor animates. This is the only difference in how an adorner is updated: after this
  step it is a normal visual with a normal own transform.
- **Notifications**. An adorner has to run that step again when the transform of the adorned visual, or of any
  visual in its parent chain, changes. `ServerCompositionVisual.Act.cs` tracks that: a visual can subscribe to
  transform changes of the parent chain of another visual.
- **Clip**. With `AdornerIsClipped`, the render walk applies the clips of the adorned visual's parent chain (bounds
  clips and geometry clips, up to the common parent) before it draws the adorner, and removes them after it
  (`AdornerHelper_RenderPreGraphPushAdornerClip`).
- **Limits**. The tree is expected to be: common parent, adorner layer, adorner. The adorned visual has to be in the
  subtree of the common parent. Otherwise the adorner is not drawn.

## Readback

Hit-testing on the UI thread needs the transforms and visibility that the render thread computed. The update walk
only visits a part of the tree, so the tree as a whole has no version number. Each visual has two readback slots
with a revision number instead (`ServerCompositionVisual.Readback.cs`).

- A visual is enqueued when its transform, its visibility or its subtree bounds change.
- The readback pass writes all enqueued visuals as one new revision, under a lock.
- The UI thread takes the same lock only to move to the newest revision. It then reads, for each visual, the newest
  slot that is not newer than its revision. So it always sees values from one consistent revision.
- The writer picks the slot that the UI thread cannot be reading: a slot that is already newer than the reader's
  revision, or else the older one of the two.

The lock is held for a short time on both sides: the UI thread only changes a number, and the render thread writes
values that were collected before. Hit-testing can wait a fraction of a millisecond when a very large update is
written.

## Backdrop rules

- A backdrop visual reads the backdrop only inside its own backdrop bounds. The effect sees the edge pixels repeated
  outside of them. So a change outside of the AABB never changes the output.
- Retained and volatile produce the same pixels, except under an intermediate layer (see below). Apart from that the
  mode is a performance choice only.
- Every layer has its own backdrop. A backdrop visual inside a bitmap cache reads the bitmap cache layer and never
  sees dirty rects of the layer around it. The cached visual's own backdrop effect is the exception: it is drawn into
  the outer layer and reads from it.
- A retained backdrop visual needs its whole input marked dirty when the dirty rects behind it are not in the working
  set yet at the time the update walk reaches it. This happens under a dirty parent (its rect is collected after its
  subtree) and when children were added, removed or reordered in the parent chain.
- The retained path reads the layer directly and cannot read an intermediate layer, so it is skipped while one is
  open (`IsBackdropSamplingSafe`). The volatile path is handled by the backend and reads through intermediate
  layers.
- A root layer without readable pixels (PDF, picture recording) draws no backdrop effects.
