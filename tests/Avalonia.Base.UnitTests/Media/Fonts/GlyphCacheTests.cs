using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts
{
    /// <summary>
    /// Unit tests for the unified, bounded glyph cache: each entry holds a cheap, retained ink box and
    /// a heavy, lazily-built, evictable geometry. Exercised in isolation — geometry payloads are plain
    /// objects, so no font, geometry backend, or render interface is needed.
    /// </summary>
    public class GlyphCacheTests
    {
        private static BuiltGeometry Built(int cost, object? payload = null,
            GlyphPayloadKind kind = GlyphPayloadKind.Outline, ushort[]? dependencies = null)
            => new(payload ?? new object(), cost, kind, dependencies ?? Array.Empty<ushort>(),
                default, hasBounds: false);

        // A successful build whose result is "this glyph has no payload" (malformed / no colour data).
        private static BuiltGeometry BuiltNull(int cost, GlyphPayloadKind kind = GlyphPayloadKind.Outline)
            => new(null, cost, kind, Array.Empty<ushort>(), default, hasBounds: false);

        // Builds and caches a geometry for `glyph`, returning the payload that was stored.
        private static object BuildGeometry(GlyphCache cache, ushort glyph, int cost,
            GlyphPayloadKind kind = GlyphPayloadKind.Outline, ushort[]? dependencies = null)
        {
            var payload = new object();
            var entry = cache.GetEntry(glyph);
            cache.GetOrBuildGeometry(entry, _ => Built(cost, payload, kind, dependencies));
            return payload;
        }

        [Fact]
        public void New_Entry_Has_Neither_Bounds_Nor_Geometry()
        {
            var entry = new GlyphCache(retainOutlineBounds: true).GetEntry(7);

            Assert.False(entry.HasBounds);
            Assert.False(entry.HasGeometry);
        }

        [Fact]
        public void SetBoundsOnce_Stores_The_First_Box_And_Ignores_Later_Writes()
        {
            var entry = new GlyphCache(retainOutlineBounds: true).GetEntry(7);

            entry.SetBoundsOnce(new GlyphBounds(1, 2, 3, 4));
            Assert.True(entry.HasBounds);
            Assert.Equal(new GlyphBounds(1, 2, 3, 4), entry.Bounds);

            entry.SetBoundsOnce(new GlyphBounds(9, 9, 9, 9));
            Assert.Equal(new GlyphBounds(1, 2, 3, 4), entry.Bounds);
        }

        [Fact]
        public void GetOrBuildGeometry_Builds_Once_Then_Serves_The_Same_Payload()
        {
            var cache = new GlyphCache();
            var entry = cache.GetEntry(5);
            var builds = 0;

            var first = cache.GetOrBuildGeometry(entry, _ => { builds++; return Built(100); });
            var second = cache.GetOrBuildGeometry(entry, _ => { builds++; return Built(100); });

            Assert.Same(first, second);
            Assert.Equal(1, builds);
        }

        [Fact]
        public void A_Malformed_Glyph_Memoises_Null_Without_Rebuilding()
        {
            var cache = new GlyphCache();
            var entry = cache.GetEntry(5);
            var builds = 0;

            object? Fetch() => cache.GetOrBuildGeometry(entry, _ => { builds++; return BuiltNull(1); });

            Assert.Null(Fetch());
            Assert.Null(Fetch());
            Assert.Equal(1, builds);
        }

        [Fact]
        public void A_Glyph_Without_A_Colour_Drawing_Memoises_Null_Without_Rebuilding()
        {
            var cache = new GlyphCache();
            var entry = cache.GetColorEntry(5);
            var builds = 0;

            object? Fetch() => cache.GetOrBuildDrawing(entry,
                _ => { builds++; return BuiltNull(1, GlyphPayloadKind.ColorDrawing); });

            Assert.Null(Fetch());
            Assert.Null(Fetch());
            Assert.Equal(1, builds);
        }

        [Fact]
        public async Task Concurrent_Readers_Never_See_A_Built_Glyph_As_Malformed()
        {
            // Regression for a lock-free fast-path race: a reader could read the (not yet published)
            // geometry as null, then observe the built flag a concurrent build had just set, and
            // misreport a valid glyph as malformed (null) for that call. The re-read under the set
            // flag makes the fast path exact.
            var cache = new GlyphCache();

            for (ushort g = 0; g < 2000; g++)
            {
                var entry = cache.GetEntry(g);
                var payload = new object();
                var go = 0;

                var tasks = new Task<object?>[4];
                for (var i = 0; i < tasks.Length; i++)
                {
                    tasks[i] = Task.Run(() =>
                    {
                        SpinWait.SpinUntil(() => Volatile.Read(ref go) != 0);
                        return cache.GetOrBuildGeometry(entry, _ => Built(1, payload));
                    });
                }

                Volatile.Write(ref go, 1);
                var results = await Task.WhenAll(tasks);

                foreach (var result in results)
                {
                    Assert.Same(payload, result);
                }
            }
        }

        [Fact]
        public void Building_A_Cff_Geometry_Populates_The_Entry_Bounds()
        {
            var cache = new GlyphCache(retainOutlineBounds: true);
            var entry = cache.GetEntry(5);

            cache.GetOrBuildGeometry(entry, _ =>
                new BuiltGeometry(new object(), 100, GlyphPayloadKind.Outline, Array.Empty<ushort>(),
                    new GlyphBounds(0, 0, 20, 30), hasBounds: true));

            Assert.True(entry.HasBounds);
            Assert.Equal(new GlyphBounds(0, 0, 20, 30), entry.Bounds);
        }

        [Fact]
        public void Geometry_Beyond_Budget_Evicts_To_Stay_Within_Budget()
        {
            var cache = new GlyphCache(budgetBytes: 1024);

            for (ushort g = 0; g < 16; g++)
            {
                BuildGeometry(cache, g, cost: 256);
                Assert.True(cache.TotalCost <= 1024);
            }
        }

        [Fact]
        public void Evicting_Geometry_Keeps_A_Retained_Bounds_Entry()
        {
            // A CFF / CFF2 cache retains evicted entries for their interpreted bounds.
            var cache = new GlyphCache(retainOutlineBounds: true, budgetBytes: 1); // holds one cost-1 geometry

            var cff = cache.GetEntry(1);
            cff.SetBoundsOnce(new GlyphBounds(0, 0, 10, 10));
            cache.GetOrBuildGeometry(cff, _ => Built(1));
            Assert.True(cff.HasGeometry);

            // Building a second geometry evicts the first.
            BuildGeometry(cache, 2, cost: 1);

            // The CFF entry survived for its bounds, but its geometry was dropped.
            Assert.Same(cff, cache.GetEntry(1));
            Assert.True(cff.HasBounds);
            Assert.False(cff.HasGeometry);
        }

        [Fact]
        public void Evicting_Geometry_Drops_A_Non_Retaining_Entry_Whole()
        {
            // A glyf cache has nothing worth retaining (bounds are a cheap header read), so eviction
            // removes the entry from the map entirely.
            var cache = new GlyphCache(budgetBytes: 1);

            var glyf = cache.GetEntry(1);
            cache.GetOrBuildGeometry(glyf, _ => Built(1));

            BuildGeometry(cache, 2, cost: 1); // evicts glyph 1

            Assert.NotSame(glyf, cache.GetEntry(1));
        }

        [Fact]
        public void Referenced_Geometry_Gets_A_Second_Chance_Over_An_Unreferenced_Peer()
        {
            var cache = new GlyphCache(budgetBytes: 2); // holds two cost-1 geometries

            var a = cache.GetEntry(1);
            cache.GetOrBuildGeometry(a, _ => Built(1));
            var b = cache.GetEntry(2);
            cache.GetOrBuildGeometry(b, _ => Built(1));

            // Age both, then access only 'a'.
            a.Referenced = 0;
            b.Referenced = 0;
            cache.GetOrBuildGeometry(a, _ => Built(1)); // hit → re-references 'a'

            BuildGeometry(cache, 3, cost: 1); // forces one eviction

            Assert.True(a.HasGeometry);   // survived on its second chance
            Assert.False(b.HasGeometry);  // unreferenced peer evicted
        }

        [Fact]
        public void Accessing_A_Composite_Refreshes_Its_Cached_Component()
        {
            var cache = new GlyphCache();

            var component = cache.GetEntry(10);
            cache.GetOrBuildGeometry(component, _ => Built(1));

            var composite = cache.GetEntry(11);
            cache.GetOrBuildGeometry(composite, _ =>
                Built(1, kind: GlyphPayloadKind.CompositeOutline, dependencies: new ushort[] { 10 }));

            // Age the component, then access the composite — recency must propagate to the component.
            component.Referenced = 0;
            cache.GetOrBuildGeometry(composite, _ => Built(1)); // hit

            Assert.Equal(1, component.Referenced);
        }

        [Fact]
        public void Referencing_Payload_Pins_Its_Dependencies_Against_Eviction()
        {
            var cache = new GlyphCache(budgetBytes: 2);

            var component = cache.GetEntry(20);
            cache.GetOrBuildGeometry(component, _ => Built(1));

            // A color drawing references its component, so the component is pinned while the drawing is
            // live — even under pressure the component outlives the drawing.
            var drawing = cache.GetEntry(21);
            cache.GetOrBuildGeometry(drawing, _ =>
                Built(1, kind: GlyphPayloadKind.ColorDrawing, dependencies: new ushort[] { 20 }));

            BuildGeometry(cache, 22, cost: 1); // forces an eviction

            Assert.True(component.HasGeometry);   // pinned component survived
            Assert.False(drawing.HasGeometry);    // the referencing drawing was evicted instead
        }

        [Fact]
        public void Flattened_Composite_Does_Not_Pin_Its_Component()
        {
            var cache = new GlyphCache(budgetBytes: 2);

            var component = cache.GetEntry(30);
            cache.GetOrBuildGeometry(component, _ => Built(1));

            var composite = cache.GetEntry(31);
            cache.GetOrBuildGeometry(composite, _ =>
                Built(1, kind: GlyphPayloadKind.CompositeOutline, dependencies: new ushort[] { 30 }));

            BuildGeometry(cache, 32, cost: 1); // forces an eviction

            Assert.False(component.HasGeometry); // unpinned component reclaimed
        }

        [Fact]
        public void Evicted_Payload_Is_Not_Disposed_Because_References_May_Be_Outstanding()
        {
            // Hits are handed out lock-free and escape into retained render data, so eviction must
            // only unlink and leave reclamation to the GC — deterministic disposal here would be a
            // use-after-dispose for whoever still holds the payload.
            var cache = new GlyphCache(budgetBytes: 1);
            var payload = new DisposablePayload();

            var entry = cache.GetEntry(40);
            cache.GetOrBuildGeometry(entry, _ => Built(1, payload));

            BuildGeometry(cache, 41, cost: 1); // evicts glyph 40

            Assert.False(entry.HasGeometry);
            Assert.Equal(0, payload.DisposeCount);
        }

        [Fact]
        public void GetOrBuildDrawing_Builds_Once_And_Pins_Its_Already_Cached_Dependencies()
        {
            var cache = new GlyphCache(budgetBytes: 2);

            // The layer outline is cached first (as it would be after the colour glyph's first draw).
            BuildGeometry(cache, 60, cost: 1);

            var drawing = cache.GetColorEntry(50);
            var builds = 0;

            object? Fetch() => cache.GetOrBuildDrawing(drawing, _ =>
            {
                builds++;
                return Built(1, kind: GlyphPayloadKind.ColorDrawing, dependencies: new ushort[] { 60 });
            });

            var first = Fetch();
            var second = Fetch();

            Assert.Same(first, second);   // cached: served from the same entry
            Assert.Equal(1, builds);      // parsed once

            // Pressure: the pinned layer outline outlives the drawing that pins it.
            BuildGeometry(cache, 70, cost: 1);

            Assert.True(cache.GetEntry(60).HasGeometry); // pinned layer survived
            Assert.False(drawing.HasGeometry);           // the drawing was evicted instead
        }

        [Fact]
        public void A_Drawing_Pins_Layers_That_Are_Cached_After_It()
        {
            var cache = new GlyphCache(budgetBytes: 2);

            // The drawing is parsed before its layer outline was ever built (e.g. bounds-only use).
            var drawing = cache.GetColorEntry(50);
            cache.GetOrBuildDrawing(drawing, _ =>
                Built(1, kind: GlyphPayloadKind.ColorDrawing, dependencies: new ushort[] { 60 }));

            // Pinning created the layer's entry up front, so the outline arrives pre-pinned when built.
            var layer = cache.GetEntry(60);
            Assert.Equal(1, layer.PinCount);

            cache.GetOrBuildGeometry(layer, _ => Built(1));
            BuildGeometry(cache, 70, cost: 1); // pressure: one eviction

            Assert.True(layer.HasGeometry);    // pinned layer survived
            Assert.False(drawing.HasGeometry); // the drawing was evicted instead
        }

        [Fact]
        public void Evicting_A_Drawing_Does_Not_Release_Another_Drawings_Pin()
        {
            // Regression: Pin used to skip dependencies that were not cached yet while Unpin
            // decremented any present dependency — so evicting one drawing could release a pin a
            // second drawing was relying on. Pin now creates the entry, making Unpin exact.
            var cache = new GlyphCache(budgetBytes: 3);

            // D1 references layer 100 before the layer is built; the pin creates the entry.
            var d1 = cache.GetColorEntry(1);
            cache.GetOrBuildDrawing(d1, _ =>
                Built(1, kind: GlyphPayloadKind.ColorDrawing, dependencies: new ushort[] { 100 }));

            var layer = cache.GetEntry(100);
            cache.GetOrBuildGeometry(layer, _ => Built(1));

            // D2 shares the layer.
            var d2 = cache.GetColorEntry(2);
            cache.GetOrBuildDrawing(d2, _ =>
                Built(1, kind: GlyphPayloadKind.ColorDrawing, dependencies: new ushort[] { 100 }));

            Assert.Equal(2, layer.PinCount);

            // Age only D1 and apply pressure so the sweep selects it.
            d1.Referenced = 0;
            BuildGeometry(cache, 200, cost: 1);
            Assert.False(d1.HasGeometry);

            // D1's eviction released exactly its own pin — D2's pin still protects the layer.
            Assert.Equal(1, layer.PinCount);

            // Even aged, the still-pinned layer is skipped; the sweep reclaims an unpinned peer.
            layer.Referenced = 0;
            BuildGeometry(cache, 201, cost: 1);
            Assert.True(layer.HasGeometry);
        }

        [Fact]
        public void A_Colour_Drawing_And_An_Outline_Can_Share_A_Glyph_Id()
        {
            var cache = new GlyphCache();

            var outline = cache.GetEntry(5);
            var drawing = cache.GetColorEntry(5);
            Assert.NotSame(outline, drawing); // distinct entries in distinct maps

            var outlinePayload = new object();
            cache.GetOrBuildGeometry(outline, _ => Built(1, outlinePayload));

            var drawingPayload = new object();
            cache.GetOrBuildDrawing(drawing, _ => Built(1, drawingPayload, GlyphPayloadKind.ColorDrawing));

            // Neither clobbered the other's slot.
            Assert.Same(outlinePayload, cache.GetOrBuildGeometry(outline, _ => Built(1)));
            Assert.Same(drawingPayload,
                cache.GetOrBuildDrawing(drawing, _ => Built(1, kind: GlyphPayloadKind.ColorDrawing)));
        }

        [Fact]
        public void Colour_Drawings_With_Different_Palettes_Coexist_For_One_Glyph()
        {
            var cache = new GlyphCache();

            var palette0 = cache.GetColorEntry(5, 0);
            var palette1 = cache.GetColorEntry(5, 1);
            Assert.NotSame(palette0, palette1); // distinct entries per (glyph, palette)

            var payload0 = new object();
            var payload1 = new object();
            cache.GetOrBuildDrawing(palette0, _ => Built(1, payload0, GlyphPayloadKind.ColorDrawing));
            cache.GetOrBuildDrawing(palette1, _ => Built(1, payload1, GlyphPayloadKind.ColorDrawing));

            // Each palette variant serves its own memoised payload.
            Assert.Same(payload0, cache.GetOrBuildDrawing(cache.GetColorEntry(5, 0),
                _ => Built(1, kind: GlyphPayloadKind.ColorDrawing)));
            Assert.Same(payload1, cache.GetOrBuildDrawing(cache.GetColorEntry(5, 1),
                _ => Built(1, kind: GlyphPayloadKind.ColorDrawing)));
        }

        [Fact]
        public void Evicting_A_Palette_Variant_Removes_Only_That_Entry()
        {
            var cache = new GlyphCache(budgetBytes: 2);

            var palette0 = cache.GetColorEntry(5, 0);
            cache.GetOrBuildDrawing(palette0, _ => Built(1, kind: GlyphPayloadKind.ColorDrawing));
            var palette1 = cache.GetColorEntry(5, 1);
            cache.GetOrBuildDrawing(palette1, _ => Built(1, kind: GlyphPayloadKind.ColorDrawing));

            // Age only palette 0; pressure evicts it and must remove exactly its packed key.
            palette0.Referenced = 0;
            BuildGeometry(cache, 6, cost: 1);

            Assert.False(palette0.HasGeometry);
            Assert.NotSame(palette0, cache.GetColorEntry(5, 0)); // dropped whole from the colour map
            Assert.True(palette1.HasGeometry);                   // the other palette variant untouched
            Assert.Same(palette1, cache.GetColorEntry(5, 1));
        }

        private sealed class DisposablePayload : IDisposable
        {
            public int DisposeCount { get; private set; }

            public void Dispose() => DisposeCount++;
        }
    }
}
