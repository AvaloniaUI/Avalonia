using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Collections.Pooled;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.HitTesting;

namespace Avalonia.Rendering.Composition
{
    /// <summary>
    /// Represents the composition output (e. g. a window, embedded control, entire screen)
    /// </summary>
    internal partial class CompositionTarget
    {
        private readonly PooledList<CompositionVisual> _hitTestChildCandidates = [];
        private bool _hitTestChildCandidatesInUse;

        partial void OnRootChanged()
        {
            if (Root != null)
                Root.Root = this;
        }

        partial void OnRootChanging()
        {
            if (Root != null)
                Root.Root = null;
        }

        /// <summary>
        /// Attempts to perform a hit-test.
        /// </summary>
        /// <returns>A list of visuals hit during the test.</returns>
        public PooledList<CompositionVisual>? TryHitTest<THitTester, T>(
            T input,
            CompositionVisual? root,
            Func<CompositionVisual, bool>? filter)
            where THitTester : struct, ICompositionHitTester<T>
        {
            Server.Compositor.Readback.NextRead();

            root ??= Root;
            if (root == null)
                return null;

            // Need to convert transform the point using visual's readback since HitTestCore will use its inverse matrix
            // NOTE: it can technically break hit-testing of the root visual itself if it has a non-identity transform,
            // need to investigate that possibility later. We might want a separate mode for root hit-testing.
            var readback = root.TryGetValidReadback();
            if (readback == null)
                return null;

            var parentInput = THitTester.Transform(input, in readback.Matrix);

            var res = new PooledList<CompositionVisual>();
            HitTestCore<THitTester, T>(root, parentInput, res, filter);
            return res;
        }

        private PooledList<CompositionVisual> RentHitTestChildCandidates(out bool releaseToField)
        {
            if (!_hitTestChildCandidatesInUse)
            {
                _hitTestChildCandidatesInUse = true;
                releaseToField = true;
                _hitTestChildCandidates.Clear();
                return _hitTestChildCandidates;
            }

            releaseToField = false;
            return [];
        }

        private void ReleaseHitTestChildCandidates(PooledList<CompositionVisual> candidates, bool releaseToField)
        {
            if (releaseToField)
            {
                candidates.Clear();
                _hitTestChildCandidatesInUse = false;
            }
            else
            {
                candidates.Dispose();
            }
        }

        private void HitTestCore<THitTester, T>(
            CompositionVisual visual,
            T parentInput,
            PooledList<CompositionVisual> result,
            Func<CompositionVisual, bool>? filter)
            where THitTester : struct, ICompositionHitTester<T>
        {
            if (!HitTestVisual<THitTester, T>(visual, parentInput, filter, out var input))
                return;

            // Inspect children
            if (visual is CompositionContainerVisual cv)
                HitTestChildren<THitTester, T>(cv, input, result, filter);

            // Hit-test the current node
            if (THitTester.HitTest(visual, input))
                result.Add(visual);
        }

        private void HitTestChildren<THitTester, T>(CompositionContainerVisual visual, T input, PooledList<CompositionVisual> result, Func<CompositionVisual, bool>? filter)
            where THitTester : struct, ICompositionHitTester<T>
        {
            if (visual.Children.Count >= CompositionContainerVisual.HitTestAabbTreeThreshold)
            {
                var candidates = RentHitTestChildCandidates(out var releaseToField);
                try
                {
                    if (visual.TryQueryHitTestChildren<THitTester, T>(input, candidates))
                    {
                        foreach (var child in candidates)
                            HitTestCore<THitTester, T>(child, input, result, filter);
                        return;
                    }
                }
                finally
                {
                    ReleaseHitTestChildCandidates(candidates, releaseToField);
                }
            }

            for (var c = visual.Children.Count - 1; c >= 0; c--)
                HitTestCore<THitTester, T>(visual.Children[c], input, result, filter);
        }

        private static bool HitTestVisual<THitTester, T>(
            CompositionVisual visual,
            T parentInput,
            Func<CompositionVisual, bool>? filter,
            [MaybeNullWhen(false)] out T input)
            where THitTester : struct, ICompositionHitTester<T>
        {
            input = default;

            if (!visual.Visible)
                return false;

            if (filter != null && !filter(visual))
                return false;

            var readback = visual.TryGetValidReadback();
            if (readback == null)
                return false;

            if (!visual.DisableSubTreeBoundsHitTestOptimization &&
                (readback.TransformedSubtreeBounds is not { } transformedSubtreeBounds ||
                 !THitTester.TransformedSubTreeBoundsMatch(transformedSubtreeBounds, parentInput)))
                return false;

            if (!readback.Matrix.TryInvert(out var invMatrix))
                return false;

            input = THitTester.Transform(parentInput, in invMatrix);

            if (visual.ClipToBounds && !THitTester.ClippedBoundsMatch(visual, input))
                return false;

            if (visual.Clip is { } clip && !THitTester.ClipMatches(clip, input))
                return false;

            return true;
        }

        public CompositionVisual? TryHitTestFirst<THitTester, T>(
            T input,
            CompositionVisual? root,
            Func<CompositionVisual, bool>? filter,
            Func<CompositionVisual, bool>? resultFilter)
            where THitTester : struct, ICompositionHitTester<T>
        {
            Server.Compositor.Readback.NextRead();

            root ??= Root;
            if (root == null)
                return null;

            // Need to convert transform the point using visual's readback since HitTestCore will use its inverse matrix
            // NOTE: it can technically break hit-testing of the root visual itself if it has a non-identity transform,
            // need to investigate that possibility later. We might want a separate mode for root hit-testing.
            var readback = root.TryGetValidReadback();
            if (readback == null)
                return null;

            var parentInput = THitTester.Transform(input, in readback.Matrix);

            return HitTestFirstCore<THitTester, T>(root, parentInput, filter, resultFilter);
        }

        internal CompositionVisual? HitTestFirstCore<THitTester, T>(
            CompositionVisual visual,
            T parentInput,
            Func<CompositionVisual, bool>? filter,
            Func<CompositionVisual, bool>? resultFilter)
            where THitTester : struct, ICompositionHitTester<T>
        {
            if (!HitTestVisual<THitTester, T>(visual, parentInput, filter, out var input))
                return null;

            if (visual is CompositionContainerVisual cv)
            {
                var queriedIndexedChildren = false;
                if (cv.Children.Count >= CompositionContainerVisual.HitTestAabbTreeThreshold)
                {
                    if (cv.TryQueryFirstHitTestChild<THitTester, T>(this, input, filter, resultFilter, out var hit))
                    {
                        queriedIndexedChildren = true;
                        if (hit != null)
                            return hit;
                    }
                }

                if (!queriedIndexedChildren)
                {
                    for (var c = cv.Children.Count - 1; c >= 0; c--)
                    {
                        var hit = HitTestFirstCore<THitTester, T>(cv.Children[c], input, filter, resultFilter);
                        if (hit != null)
                            return hit;
                    }
                }
            }

            return THitTester.HitTest(visual, input) && (resultFilter == null || resultFilter(visual)) ? visual : null;
        }

        /// <summary>
        /// Registers the composition target for explicit redraw
        /// </summary>
        public void RequestRedraw() => RegisterForSerialization();
    }
}
