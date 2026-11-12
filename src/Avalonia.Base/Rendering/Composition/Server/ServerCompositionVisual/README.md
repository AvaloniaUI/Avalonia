# Server-side composition visuals

`ServerCompositionVisual` is the render-thread counterpart of `CompositionVisual`. This directory holds the per-frame
tree passes (dirty region collection and rendering), bitmap caches and backdrop effects.

The dirty region logic is a port of WPF's (`WpfGfx`), so some older comments still use its vocabulary. The glossary
lists those words as synonyms. New comments use the first term only.

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

- **update walk**: `UpdateContext`. Recomputes subtree bounds and collects the dirty region. Runs once per frame.
- **render walk**: `RenderContext`. Draws the visuals that intersect the dirty region.
- **volatile pass**: `BackdropVolatilePass`. Runs between the two walks, see below.

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

For a `ServerCompositionTarget`, in this order:

1. **Backdrop state recompute** (`RecomputeBackdropState`). For every backdrop visual in the list: find its layer,
   compute its AABB and decide retained or volatile. If the layer or the AABB changed, the visual is marked dirty.
   This step never adds dirty rects itself.
2. **Update walk**. Recomputes bounds and collects the dirty region of every layer. For retained backdrop visuals it
   also collects input dirty rects: a dirty rect that is already in the working set when the walk reaches a visual
   belongs to something behind it, so it is a change of that visual's backdrop.
3. **Volatile pass**. A volatile backdrop visual cannot be redrawn in part, so whole-AABB invalidation applies. That
   new dirty rect is input for the backdrop visuals in front of it, so the pass repeats until nothing changes.
4. **Render walk**. Draws the dirty region. A retained backdrop visual first refreshes its snapshot from its input
   dirty rect, then draws the snapshot through the effect. A volatile one filters the layer contents directly.

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
