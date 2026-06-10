using System;
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

        // Builds and caches a geometry for `glyph`, returning the payload that was stored.
        private static object BuildGeometry(GlyphCache cache, ushort glyph, int cost,
            GlyphPayloadKind kind = GlyphPayloadKind.Outline, ushort[]? dependencies = null,
            bool retainBounds = false)
        {
            var payload = new object();
            var entry = cache.GetEntry(glyph, retainBounds);
            cache.GetOrBuildGeometry(entry, _ => Built(cost, payload, kind, dependencies));
            return payload;
        }

        [Fact]
        public void New_Entry_Has_Neither_Bounds_Nor_Geometry()
        {
            var entry = new GlyphCache().GetEntry(7, retainBounds: true);

            Assert.False(entry.HasBounds);
            Assert.False(entry.HasGeometry);
        }

        [Fact]
        public void SetBoundsOnce_Stores_The_First_Box_And_Ignores_Later_Writes()
        {
            var entry = new GlyphCache().GetEntry(7, retainBounds: true);

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
            var entry = cache.GetEntry(5, retainBounds: false);
            var builds = 0;

            var first = cache.GetOrBuildGeometry(entry, _ => { builds++; return Built(100); });
            var second = cache.GetOrBuildGeometry(entry, _ => { builds++; return Built(100); });

            Assert.Same(first, second);
            Assert.Equal(1, builds);
        }

        [Fact]
        public void Building_A_Cff_Geometry_Populates_The_Entry_Bounds()
        {
            var cache = new GlyphCache();
            var entry = cache.GetEntry(5, retainBounds: true);

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
            var cache = new GlyphCache(budgetBytes: 1); // holds one cost-1 geometry

            // A CFF entry: bounds plus geometry, retainBounds = true.
            var cff = cache.GetEntry(1, retainBounds: true);
            cff.SetBoundsOnce(new GlyphBounds(0, 0, 10, 10));
            cache.GetOrBuildGeometry(cff, _ => Built(1));
            Assert.True(cff.HasGeometry);

            // Building a second geometry evicts the first.
            BuildGeometry(cache, 2, cost: 1, retainBounds: false);

            // The CFF entry survived for its bounds, but its geometry was dropped.
            Assert.Same(cff, cache.GetEntry(1, retainBounds: true));
            Assert.True(cff.HasBounds);
            Assert.False(cff.HasGeometry);
        }

        [Fact]
        public void Referenced_Geometry_Gets_A_Second_Chance_Over_An_Unreferenced_Peer()
        {
            var cache = new GlyphCache(budgetBytes: 2); // holds two cost-1 geometries

            var a = cache.GetEntry(1, retainBounds: false);
            cache.GetOrBuildGeometry(a, _ => Built(1));
            var b = cache.GetEntry(2, retainBounds: false);
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

            var component = cache.GetEntry(10, retainBounds: false);
            cache.GetOrBuildGeometry(component, _ => Built(1));

            var composite = cache.GetEntry(11, retainBounds: false);
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

            var component = cache.GetEntry(20, retainBounds: false);
            cache.GetOrBuildGeometry(component, _ => Built(1));

            // A color drawing references its component, so the component is pinned while the drawing is
            // live — even under pressure the component outlives the drawing.
            var drawing = cache.GetEntry(21, retainBounds: false);
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

            var component = cache.GetEntry(30, retainBounds: false);
            cache.GetOrBuildGeometry(component, _ => Built(1));

            var composite = cache.GetEntry(31, retainBounds: false);
            cache.GetOrBuildGeometry(composite, _ =>
                Built(1, kind: GlyphPayloadKind.CompositeOutline, dependencies: new ushort[] { 30 }));

            BuildGeometry(cache, 32, cost: 1); // forces an eviction

            Assert.False(component.HasGeometry); // unpinned component reclaimed
        }

        [Fact]
        public void Evicted_Disposable_Payload_Is_Disposed_Once()
        {
            var cache = new GlyphCache(budgetBytes: 1);
            var payload = new DisposablePayload();

            var entry = cache.GetEntry(40, retainBounds: false);
            cache.GetOrBuildGeometry(entry, _ => Built(1, payload));

            BuildGeometry(cache, 41, cost: 1); // evicts glyph 40

            Assert.False(entry.HasGeometry);
            Assert.Equal(1, payload.DisposeCount);
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

            Assert.True(cache.GetEntry(60, retainBounds: false).HasGeometry); // pinned layer survived
            Assert.False(drawing.HasGeometry);                               // the drawing was evicted instead
        }

        [Fact]
        public void A_Colour_Drawing_And_An_Outline_Can_Share_A_Glyph_Id()
        {
            var cache = new GlyphCache();

            var outline = cache.GetEntry(5, retainBounds: false);
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

        private sealed class DisposablePayload : IDisposable
        {
            public int DisposeCount { get; private set; }

            public void Dispose() => DisposeCount++;
        }
    }
}
