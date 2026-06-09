using System;
using Avalonia.Media.Fonts;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts
{
    /// <summary>
    /// Unit tests for the bounded, cost-aware glyph payload cache (CLOCK eviction). Exercised in
    /// isolation — entries are hand-built with explicit cost / kind / dependencies, so no font,
    /// geometry, or render interface is needed.
    /// </summary>
    public class GlyphCacheTests
    {
        private static GlyphCacheEntry Entry(ushort glyph, int cost,
            GlyphPayloadKind kind = GlyphPayloadKind.Outline, ushort[]? dependencies = null,
            object? payload = null)
            => new(glyph, kind, payload ?? new object(), cost, dependencies ?? Array.Empty<ushort>(), default);

        [Fact]
        public void TryGet_Returns_False_For_Missing_Glyph()
        {
            var cache = new GlyphCache();

            Assert.False(cache.TryGet(7, out _));
        }

        [Fact]
        public void GetOrAdd_Builds_Once_Then_Serves_The_Same_Entry()
        {
            var cache = new GlyphCache();
            var builds = 0;

            GlyphCacheEntry Factory(ushort g)
            {
                builds++;
                return Entry(g, cost: 100);
            }

            var first = cache.GetOrAdd(5, Factory);
            var second = cache.GetOrAdd(5, Factory);

            Assert.Same(first, second);
            Assert.Equal(1, builds);

            Assert.True(cache.TryGet(5, out var hit));
            Assert.Same(first, hit);
            Assert.Equal(1, builds);
        }

        [Fact]
        public void Insert_Beyond_Budget_Evicts_To_Stay_Within_Budget()
        {
            // Budget holds ~4 cost-256 entries; insert 16 and the total must never exceed the budget.
            var cache = new GlyphCache(budgetBytes: 1024);

            for (ushort g = 0; g < 16; g++)
            {
                cache.GetOrAdd(g, gid => Entry(gid, cost: 256));
                Assert.True(cache.TotalCost <= 1024);
            }

            Assert.True(cache.Count <= 4);
        }

        [Fact]
        public void Referenced_Entry_Gets_A_Second_Chance_Over_An_Unreferenced_Peer()
        {
            var cache = new GlyphCache(budgetBytes: 2); // holds two cost-1 entries

            var a = cache.GetOrAdd(1, g => Entry(g, cost: 1));
            var b = cache.GetOrAdd(2, g => Entry(g, cost: 1));

            // Age both, then touch only 'a'.
            a.Referenced = 0;
            b.Referenced = 0;
            Assert.True(cache.TryGet(1, out _)); // re-references 'a'

            // Inserting a third entry forces one eviction. 'a' survives on its second chance; the
            // unreferenced 'b' is reclaimed.
            cache.GetOrAdd(3, g => Entry(g, cost: 1));

            Assert.True(cache.TryGet(1, out _));
            Assert.False(cache.TryGet(2, out _));
            Assert.True(cache.TryGet(3, out _));
        }

        [Fact]
        public void Accessing_A_Composite_Refreshes_Its_Cached_Dependencies()
        {
            var cache = new GlyphCache();

            var component = cache.GetOrAdd(10, g => Entry(g, cost: 1));
            cache.GetOrAdd(11, g => Entry(g, cost: 1,
                kind: GlyphPayloadKind.CompositeOutline, dependencies: new ushort[] { 10 }));

            // Age the component, then access the composite — recency must propagate to the component.
            component.Referenced = 0;
            Assert.True(cache.TryGet(11, out _));

            Assert.Equal(1, component.Referenced);
        }

        [Fact]
        public void Referencing_Payload_Pins_Its_Dependencies_Against_Eviction()
        {
            var cache = new GlyphCache(budgetBytes: 2); // holds two cost-1 entries

            // A color drawing references its component outline, so the component is pinned while the
            // drawing is live — even under budget pressure the component outlives the drawing.
            cache.GetOrAdd(20, g => Entry(g, cost: 1));
            cache.GetOrAdd(21, g => Entry(g, cost: 1,
                kind: GlyphPayloadKind.ColorDrawing, dependencies: new ushort[] { 20 }));

            cache.GetOrAdd(22, g => Entry(g, cost: 1)); // forces an eviction

            Assert.True(cache.TryGet(20, out _));   // pinned component survived
            Assert.False(cache.TryGet(21, out _));  // the referencing drawing was evicted instead
            Assert.True(cache.TryGet(22, out _));
        }

        [Fact]
        public void Flattened_Composite_Does_Not_Pin_Its_Dependencies()
        {
            var cache = new GlyphCache(budgetBytes: 2);

            // An outline composite is self-contained, so its component is NOT pinned (pinning would
            // double-retain). Without a recency touch the component is a normal eviction candidate.
            cache.GetOrAdd(30, g => Entry(g, cost: 1));
            cache.GetOrAdd(31, g => Entry(g, cost: 1,
                kind: GlyphPayloadKind.CompositeOutline, dependencies: new ushort[] { 30 }));

            cache.GetOrAdd(32, g => Entry(g, cost: 1)); // forces an eviction

            Assert.False(cache.TryGet(30, out _)); // unpinned component reclaimed
        }

        [Fact]
        public void Evicted_Disposable_Payload_Is_Disposed_Once()
        {
            var cache = new GlyphCache(budgetBytes: 1);
            var payload = new DisposablePayload();

            cache.GetOrAdd(40, g => Entry(g, cost: 1, payload: payload));
            cache.GetOrAdd(41, g => Entry(g, cost: 1)); // evicts glyph 40

            Assert.False(cache.TryGet(40, out _));
            Assert.Equal(1, payload.DisposeCount);
        }

        private sealed class DisposablePayload : IDisposable
        {
            public int DisposeCount { get; private set; }

            public void Dispose() => DisposeCount++;
        }
    }
}
