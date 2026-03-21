using System;
using System.Diagnostics;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

partial class MultiDirtyRectTracker
{
    /// <summary>
    /// This is a port of CDirtyRegion2 from WPF
    /// </summary>
    class CDirtyRegion2(int MaxDirtyRegionCount)
    {

        private readonly LtrbRect[] _dirtyRegions = new LtrbRect[MaxDirtyRegionCount];
        private readonly LtrbRect[] _resolvedRegions = new LtrbRect[MaxDirtyRegionCount];
        private readonly double[,] _overhead = new double[MaxDirtyRegionCount + 1, MaxDirtyRegionCount];
        private LtrbRect _surfaceBounds;
        private double _allowedDirtyRegionOverhead;
        private int _regionCount;
        private bool _optimized;
        private bool _maxSurfaceFallback;

        private readonly struct UnionResult
        {
            public readonly double Overhead;
            // Left here for debugging purposes
            public readonly double Area;
            public readonly LtrbRect Union;

            public UnionResult(double overhead, double area, LtrbRect union)
            {
                Overhead = overhead;
                Area = area;
                Union = union;
            }
        }

        private static double RectArea(LtrbRect r)
        {
            return (r.Right - r.Left) * (r.Bottom - r.Top);
        }

        private static LtrbRect RectUnion(LtrbRect left, LtrbRect right)
        {
            if (left.IsZeroSize)
                return right;
            if (right.IsZeroSize)
                return left;
            return left.Union(right);
        }

        private static UnionResult ComputeUnion(LtrbRect r0, LtrbRect r1)
        {
            var unioned = RectUnion(r0, r1);
            var intersected = r0.IntersectOrEmpty(r1);

            double areaOfUnion = RectArea(unioned);
            double overhead = areaOfUnion - (RectArea(r0) + RectArea(r1) - RectArea(intersected));


            // Use 0 as overhead if computed overhead is negative or overhead
            // computation returns a nan.  (If more than one of the previous
            // area computations overflowed then overhead could be not a
            // number.)
            if (!(overhead > 0))
            {
                overhead = 0;
            }

            return new UnionResult(overhead, areaOfUnion, unioned);
        }

        private void SetOverhead(int i, int j, double value)
        {
            if (i > j)
            {
                _overhead[i, j] = value;
            }
            else if (i < j)
            {
                _overhead[j, i] = value;
            }
        }

        private double GetOverhead(int i, int j)
        {
            if (i > j)
            {
                return _overhead[i, j];
            }

            if (i < j)
            {
                return _overhead[j, i];
            }

            return double.MaxValue;
        }

        private void UpdateOverhead(int regionIndex)
        {
            ref readonly var regionAtIndex = ref _dirtyRegions[regionIndex];
            for (int i = 0; i < MaxDirtyRegionCount; i++)
            {
                if (regionIndex != i)
                {
                    var ur = ComputeUnion(_dirtyRegions[i], regionAtIndex);
                    SetOverhead(i, regionIndex, ur.Overhead);
                }
            }
        }

        /// <summary>
        /// Initialize must be called before adding dirty rects. Initialize can also be called to
        /// reset the dirty region.
        /// </summary>
        public void Initialize(LtrbRect surfaceBounds, double allowedDirtyRegionOverhead)
        {
            _allowedDirtyRegionOverhead = allowedDirtyRegionOverhead;
            Array.Clear(_dirtyRegions);
            Array.Clear(_overhead);
            _optimized = false;
            _maxSurfaceFallback = false;
            _regionCount = 0;

            _surfaceBounds = surfaceBounds;
        }

        /// <summary>
        /// Adds a new dirty rectangle to the dirty region.
        /// </summary>
        public void Add(LtrbRect newRegion)
        {
            
            // // We've already fallen back to setting the whole surface as a dirty region
            // // because of invalid dirty rects, so no need to add any new ones
            if (_maxSurfaceFallback)
            {
                return;
            }
            
            // // Check if rectangle is well formed before we try to intersect it, 
            // // because Intersect will fail for badly formed rects
            if (!newRegion.IsWellOrdered)
            {
                // If we're here it means that we've been passed an invalid rectangle as a dirty
                // region, containing NAN or a non well ordered rectangle.
                // In this case, make the dirty region the full surface size and warn in the debugger
                // since this could cause a serious perf regression.
                //
                Debug.Assert(false);

                // 
                // Remove all dirty regions from this object, since
                // they're no longer relevant.
                //
                Initialize(_surfaceBounds, _allowedDirtyRegionOverhead);
                _maxSurfaceFallback = true;
                _regionCount = 1;
                return;
            }

            var clippedNewRegion = newRegion.IntersectOrEmpty(_surfaceBounds);

            if (clippedNewRegion.IsEmpty)
            {
                return;
            }

            // Always keep bounding boxes device space integer.
            clippedNewRegion = new LtrbRect(
                Math.Floor(clippedNewRegion.Left),
                Math.Floor(clippedNewRegion.Top),
                Math.Ceiling(clippedNewRegion.Right),
                Math.Ceiling(clippedNewRegion.Bottom));

            // Compute the overhead for the new region combined with all existing regions
            for (int n = 0; n < MaxDirtyRegionCount; n++)
            {
                var ur = ComputeUnion(_dirtyRegions[n], clippedNewRegion);
                SetOverhead(MaxDirtyRegionCount, n, ur.Overhead);
            }
            
            // Find the pair of dirty regions that if merged create the minimal overhead. A overhead
            // of 0 is perfect in the sense that it can not get better. In that case we break early
            // out of the loop.
            double minimalOverhead = double.MaxValue;
            int bestMatchN = 0;
            int bestMatchK = 0;
            bool matchFound = false;

            for (int n = MaxDirtyRegionCount; n > 0; n--)
            {
                for (int k = 0; k < n; k++)
                {
                    double overheadNK = GetOverhead(n, k);
                    if (minimalOverhead >= overheadNK)
                    {
                        minimalOverhead = overheadNK;
                        bestMatchN = n;
                        bestMatchK = k;
                        matchFound = true;

                        if (overheadNK < _allowedDirtyRegionOverhead)
                        {
                            // If the overhead is very small, we bail out early since this
                            // saves us some valuable cycles. Note that "small" means really
                            // nothing here. In fact we don't always know if that number is
                            // actually small. However, it the algorithm stays still correct
                            // in the sense that we render everything that is necessary. It
                            // might just be not optimal.
                            goto LoopExit;
                        }
                    }
                }
            }

            if (!matchFound)
            {
                return;
            }

            LoopExit:

            // Case A: The new dirty region can be combined with an existing one
            if (bestMatchN == MaxDirtyRegionCount)
            {
                var ur = ComputeUnion(clippedNewRegion, _dirtyRegions[bestMatchK]);
                var unioned = ur.Union;

                if (_dirtyRegions[bestMatchK].Contains(unioned))
                {
                    // newDirtyRegion is enclosed by dirty region bestMatchK
                    return;
                }

                _dirtyRegions[bestMatchK] = unioned;
                UpdateOverhead(bestMatchK);
            }
            else
            {
                // Case B: Merge region N with region K, store new region slot K
                var ur = ComputeUnion(_dirtyRegions[bestMatchN], _dirtyRegions[bestMatchK]);
                _dirtyRegions[bestMatchN] = ur.Union;
                _dirtyRegions[bestMatchK] = clippedNewRegion;
                UpdateOverhead(bestMatchN);
                UpdateOverhead(bestMatchK);
            }
        }

        /// <summary>
        /// Returns an array of dirty rectangles describing the dirty region.
        /// </summary>
        public ReadOnlySpan<LtrbRect> GetUninflatedDirtyRegions()
        {
            if (_maxSurfaceFallback)
            {
                return new ReadOnlySpan<LtrbRect>(in _surfaceBounds);
            }

            if (!_optimized)
            {
                Array.Clear(_resolvedRegions);

                // Consolidate the dirtyRegions array
                int addedDirtyRegionCount = 0;
                for (int i = 0; i < MaxDirtyRegionCount; i++)
                {
                    if (!_dirtyRegions[i].IsEmpty)
                    {
                        if (i != addedDirtyRegionCount)
                        {
                            _dirtyRegions[addedDirtyRegionCount] = _dirtyRegions[i];
                            UpdateOverhead(addedDirtyRegionCount);
                        }

                        addedDirtyRegionCount++;
                    }
                }

                // Merge all dirty rects that we can
                bool couldMerge = true;
                while (couldMerge)
                {
                    couldMerge = false;
                    for (int n = 0; n < addedDirtyRegionCount; n++)
                    {
                        for (int k = n + 1; k < addedDirtyRegionCount; k++)
                        {
                            if (!_dirtyRegions[n].IsEmpty
                                && !_dirtyRegions[k].IsEmpty
                                && GetOverhead(n, k) < _allowedDirtyRegionOverhead)
                            {
                                var ur = ComputeUnion(_dirtyRegions[n], _dirtyRegions[k]);
                                _dirtyRegions[n] = ur.Union;
                                _dirtyRegions[k] = default;
                                UpdateOverhead(n);
                                couldMerge = true;
                            }
                        }
                    }
                }

                // Consolidate and copy into resolvedRegions
                int finalRegionCount = 0;
                for (int i = 0; i < addedDirtyRegionCount; i++)
                {
                    if (!_dirtyRegions[i].IsEmpty)
                    {
                        _resolvedRegions[finalRegionCount] = _dirtyRegions[i];
                        finalRegionCount++;
                    }
                }

                _regionCount = finalRegionCount;
                _optimized = true;
            }

            return _resolvedRegions.AsSpan(0, _regionCount);
        }

        /// <summary>
        /// Checks if the dirty region is empty.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                for (int i = 0; i < MaxDirtyRegionCount; i++)
                {
                    if (!_dirtyRegions[i].IsEmpty)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Returns the dirty region count.
        /// NOTE: The region count is NOT VALID until GetUninflatedDirtyRegions is called.
        /// </summary>
        public int RegionCount => _regionCount;
    }
}
