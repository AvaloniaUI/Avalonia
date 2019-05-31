using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Repeaters
{
    internal class ViewportManager
    {
        private const double CacheBufferPerSideInflationPixelDelta = 40.0;
        private readonly ItemsRepeater _owner;
        private IControl _makeAnchorElement;
        private bool _isAnchorOutsideRealizedRange;
        private Task _cacheBuildAction;
        private Rect _visibleWindow;
        private Rect _layoutExtent;
        // This is the expected shift by the layout.
        private Point _expectedViewportShift;
        // This is what is pending and not been accounted for. 
        // Sometimes the scrolling surface cannot service a shift (for example
        // it is already at the top and cannot shift anymore.)
        private Point _pendingViewportShift;
        // Unshiftable shift amount that this view manager can
        // handle on its own to fake it to the layout as if the shift
        // actually happened. This can happen in cases where no scrollviewer
        // in the parent chain can scroll in the shift direction.
        private Point _unshiftableShift;
        private double _maximumHorizontalCacheLength = 2.0;
        private double _maximumVerticalCacheLength = 2.0;
        private double _horizontalCacheBufferPerSide;
        private double _verticalCacheBufferPerSide;
        private bool _isBringIntoViewInProgress;
        // For non-virtualizing layouts, we do not need to keep
        // updating viewports and invalidating measure often. So when
        // a non virtualizing layout is used, we stop doing all that work.
        bool _managingViewportDisabled;
        private IDisposable _effectiveViewportChangedRevoker;
        private bool _layoutUpdatedSubscribed;

        public ViewportManager(ItemsRepeater owner)
        {
            _owner = owner;
        }

        // TODO: Implement
        public IControl SuggestedAnchor => null;

        // TODO: Implement
        public bool HasScroller => false;

        public IControl MadeAnchor => _makeAnchorElement;

        public double HorizontalCacheLength
        {
            get => _maximumHorizontalCacheLength;
            set
            {
                if (_maximumHorizontalCacheLength != value)
                {
                    ValidateCacheLength(value);
                    _maximumHorizontalCacheLength = value;
                    ResetCacheBuffer();
                }
            }
        }

        public double VerticalCacheLength
        {
            get => _maximumVerticalCacheLength;
            set
            {
                if (_maximumVerticalCacheLength != value)
                {
                    ValidateCacheLength(value);
                    _maximumVerticalCacheLength = value;
                    ResetCacheBuffer();
                }
            }
        }
        
        private Rect GetLayoutVisibleWindowDiscardAnchor()
        {
            var visibleWindow = _visibleWindow;

            if (HasScroller)
            {
                visibleWindow = new Rect(
                    visibleWindow.X + _layoutExtent.X + _expectedViewportShift.X + _unshiftableShift.X,
                    visibleWindow.Y + _layoutExtent.Y + _expectedViewportShift.Y + _unshiftableShift.Y,
                    visibleWindow.Width,
                    visibleWindow.Height);
            }

            return visibleWindow;
        }

        public Rect GetLayoutVisibleWindow()
        {
            var visibleWindow = _visibleWindow;

            if (_makeAnchorElement != null)
            {
                // The anchor is not necessarily laid out yet. Its position should default
                // to zero and the layout origin is expected to change once layout is done.
                // Until then, we need a window that's going to protect the anchor from
                // getting recycled.
                visibleWindow = visibleWindow.WithX(0).WithY(0);
            }
            else if (HasScroller)
            {
                visibleWindow = new Rect(
                    visibleWindow.X + _layoutExtent.X + _expectedViewportShift.X + _unshiftableShift.X,
                    visibleWindow.Y + _layoutExtent.Y + _expectedViewportShift.Y + _unshiftableShift.Y,
                    visibleWindow.Width,
                    visibleWindow.Height);
            }

            return visibleWindow;
        }

        public Rect GetLayoutRealizationWindow()
        {
            var realizationWindow = GetLayoutVisibleWindow();
            if (HasScroller)
            {
                realizationWindow = new Rect(
                    realizationWindow.X - _horizontalCacheBufferPerSide,
                    realizationWindow.Y - _verticalCacheBufferPerSide,
                    realizationWindow.Width + _horizontalCacheBufferPerSide * 2.0,
                    realizationWindow.Height + _verticalCacheBufferPerSide * 2.0);
            }

            return realizationWindow;
        }

        public void SetLayoutExtent(Rect extent)
        {
            _expectedViewportShift = new Point(
                _expectedViewportShift.X + _layoutExtent.X - extent.X,
                _expectedViewportShift.Y + _layoutExtent.Y - extent.Y);

            // We tolerate viewport imprecisions up to 1 pixel to avoid invaliding layout too much.
            if (Math.Abs(_expectedViewportShift.X) > 1 || Math.Abs(_expectedViewportShift.Y) > 1)
            {
                // There are cases where we might be expecting a shift but not get it. We will
                // be waiting for the effective viewport event but if the scroll viewer is not able
                // to perform the shift (perhaps because it cannot scroll in negative offset),
                // then we will end up not realizing elements in the visible 
                // window. To avoid this, we register to layout updated for this layout pass. If we 
                // get an effective viewport, we know we have a new viewport and we unregister from
                // layout updated. If we get the layout updated handler, then we know that the 
                // scroller was unable to perform the shift and we invalidate measure and unregister
                // from the layout updated event.
                if (!_layoutUpdatedSubscribed)
                {
                    _owner.LayoutUpdated += OnLayoutUpdated;
                    _layoutUpdatedSubscribed = true;
                }
            }

            _layoutExtent = extent;
            _pendingViewportShift = _expectedViewportShift;

            // We just finished a measure pass and have a new extent.
            // Let's make sure the scrollers will run its arrange so that they track the anchor.
            ////if (_scroller != null)
            ////{
            ////    ((IControl)_scroller).InvalidateArrange();
            ////}
        }

        public Point GetOrigin() => throw new NotImplementedException();

        public void OnLayoutChanged(bool isVirtualizing)
        {
            _managingViewportDisabled = !isVirtualizing;

            _layoutExtent = default;
            _expectedViewportShift = default;
            _pendingViewportShift = default;
            _unshiftableShift = default;
            ResetCacheBuffer();

            _effectiveViewportChangedRevoker?.Dispose();

            if (!_managingViewportDisabled)
            {
                // HACK: This is a bit of a hack. We need the effective viewport of the ItemsRepeater -
                // we can get this from TransformedBounds, but this property is updated after layout has
                // run, resulting in the UI being updated too late when scrolling quickly. We can
                // partially remedey this by triggering also on Bounds changes, but this won't work so 
                // well for nested ItemsRepeaters.
                //
                // UWP uses the EffectiveBoundsChanged event (which I think was implemented specially
                // for this case): we need to implement that in Avalonia.
                _effectiveViewportChangedRevoker = _owner.GetObservable(Visual.TransformedBoundsProperty)
                    .Merge(_owner.GetObservable(Visual.BoundsProperty).Select(_ => _owner.TransformedBounds))
                    .Skip(1)
                    .Subscribe(OnEffectiveViewportChanged);
            }
        }

        public void OnElementPrepared(IControl element)
        {
            // If we have an anchor element, we do not want the
            // scroll anchor provider to start anchoring some other element.
            ////element.CanBeScrollAnchor(true);
        }

        public void OnElementCleared(IControl element)
        {
            ////element.CanBeScrollAnchor(false);
        }

        public void OnOwnerMeasuring()
        {
            // This is because of a bug that causes effective viewport to not
            // fire if you register during arrange.
            // Bug 17411076: EffectiveViewport: registering for effective viewport in arrange should invalidate viewport
            //EnsureScroller();
        }

        public void OnOwnerArranged()
        {
            _expectedViewportShift = default;

            if (!_managingViewportDisabled)
            {
                // This is because of a bug that causes effective viewport to not 
                // fire if you register during arrange.
                // Bug 17411076: EffectiveViewport: registering for effective viewport in arrange should invalidate viewport
                // EnsureScroller();

                if (HasScroller)
                {
                    double maximumHorizontalCacheBufferPerSide = _maximumHorizontalCacheLength * _visibleWindow.Width / 2.0;
                    double maximumVerticalCacheBufferPerSide = _maximumVerticalCacheLength * _visibleWindow.Height / 2.0;

                    bool continueBuildingCache =
                        _horizontalCacheBufferPerSide < maximumHorizontalCacheBufferPerSide ||
                        _verticalCacheBufferPerSide < maximumVerticalCacheBufferPerSide;

                    if (continueBuildingCache)
                    {
                        _horizontalCacheBufferPerSide += CacheBufferPerSideInflationPixelDelta;
                        _verticalCacheBufferPerSide += CacheBufferPerSideInflationPixelDelta;

                        _horizontalCacheBufferPerSide = Math.Min(_horizontalCacheBufferPerSide, maximumHorizontalCacheBufferPerSide);
                        _verticalCacheBufferPerSide = Math.Min(_verticalCacheBufferPerSide, maximumVerticalCacheBufferPerSide);

                        // Since we grow the cache buffer at the end of the arrange pass,
                        // we need to register work even if we just reached cache potential.
                        RegisterCacheBuildWork();
                    }
                }
            }
        }

        private void OnLayoutUpdated(object sender, EventArgs args)
        {
            _owner.LayoutUpdated -= OnLayoutUpdated;
            if (_managingViewportDisabled)
            {
                return;
            }

            // We were expecting a viewport shift but we never got one and we are not going to in this
            // layout pass. We likely will never get this shift, so lets assume that we are never going to get it and
            // adjust our expected shift to track that. One case where this can happen is when there is no scrollviewer
            // that can scroll in the direction where the shift is expected.
            if (_pendingViewportShift.X != 0 || _pendingViewportShift.Y != 0)
            {
                // Assume this is never going to come.
                _unshiftableShift = new Point(
                    _unshiftableShift.X + _pendingViewportShift.X,
                    _unshiftableShift.Y + _pendingViewportShift.Y);
                _pendingViewportShift = default;
                _expectedViewportShift = default;

                TryInvalidateMeasure();
            }
        }

        public void OnMakeAnchor(IControl anchor, bool isAnchorOutsideRealizedRange)
        {
            _makeAnchorElement = anchor;
            _isAnchorOutsideRealizedRange = isAnchorOutsideRealizedRange;
        }

        public void OnBringIntoViewRequested(RequestBringIntoViewEventArgs args)
        {
            if (!_managingViewportDisabled)
            {
                // We do not animate bring-into-view operations where the anchor is disconnected because
                // it doesn't look good (the blank space is obvious because the layout can't keep track
                // of two realized ranges while the animation is going on).
                if (_isAnchorOutsideRealizedRange)
                {
                    ////args.AnimationDesired(false);
                }

                // During the time between a bring into view request and the element coming into view we do not
                // want the anchor provider to pick some anchor and jump to it. Instead we want to anchor on the
                // element that is being brought into view. We can do this by making just that element as a potential
                // anchor candidate and ensure no other element of this repeater is an anchor candidate.
                // Once the layout pass is done and we render the frame, the element will be in frame and we can
                // switch back to letting the anchor provider pick a suitable anchor.

                // get the targetChild - i.e the immediate child of this repeater that is being brought into view.
                // Note that the element being brought into view could be a descendant.
                var targetChild = GetImmediateChildOfRepeater((IControl)args.TargetObject);

                // Make sure that only the target child can be the anchor during the bring into view operation.
                foreach (var child in _owner.Children)
                {
                    ////if (child.CanBeScrollAnchor && child != targetChild)
                    ////{
                    ////    child.CanBeScrollAnchor = false;
                    ////}
                }

                // Register to rendering event to go back to how things were before where any child can be the anchor.
                _isBringIntoViewInProgress = true;
                ////if (!m_renderingToken)
                ////{
                ////    winrt::Windows::UI::Xaml::Media::CompositionTarget compositionTarget{ nullptr };
                ////    m_renderingToken = compositionTarget.Rendering(winrt::auto_revoke, { this, &ViewportManagerWithPlatformFeatures::OnCompositionTargetRendering });
                ////}
            }
        }

        private IControl GetImmediateChildOfRepeater(IControl descendant)
        {
            var targetChild = descendant;
            var parent = descendant.Parent;
            while (parent != null && parent != _owner)
            {
                targetChild = parent;
                parent = (IControl)parent.VisualParent;
            }

            if (parent == null)
            {
                throw new InvalidOperationException("OnBringIntoViewRequested called with args.target element not under the ItemsRepeater that recieved the call");
            }

            return targetChild;
        }

        public void ResetScrollers()
        {
            ////_scroller = null;
            ////_effectiveViewportChangedRevoker.Dispose();
            ////m_ensuredScroller = false;
        }

        private void OnEffectiveViewportChanged(TransformedBounds? bounds)
        {
            if (!bounds.HasValue)
            {
                return;
            }

            var globalClip = bounds.Value.Clip;
            var transform = _owner.GetVisualRoot().TransformToVisual(_owner).Value;
            var clip = globalClip.TransformToAABB(transform);
            var effectiveViewport = clip.Intersect(bounds.Value.Bounds);

            UpdateViewport(effectiveViewport);

            _pendingViewportShift = default;
            _unshiftableShift = default;
            if (_visibleWindow.IsEmpty)
            {
                // We got cleared.
                _layoutExtent = default;
            }

            // We got a new viewport, we dont need to wait for layout updated anymore to 
            // see if our request for a pending shift was handled.
            if (_layoutUpdatedSubscribed)
            {
                _owner.LayoutUpdated -= OnLayoutUpdated;
            }
        }

        private void UpdateViewport(Rect viewport)
        {
            //assert(!m_managingViewportDisabled);
            var previousVisibleWindow = _visibleWindow;
            var currentVisibleWindow = viewport;

            if (-currentVisibleWindow.X <= ItemsRepeater.ClearedElementsArrangePosition.X &&
                -currentVisibleWindow.Y <= ItemsRepeater.ClearedElementsArrangePosition.Y)
            {
                // We got cleared.
                _visibleWindow = default;
            }
            else
            {
                _visibleWindow = currentVisibleWindow;
            }

            TryInvalidateMeasure();
        }

        private void ResetCacheBuffer()
        {
            _horizontalCacheBufferPerSide = 0.0;
            _verticalCacheBufferPerSide = 0.0;

            if (!_managingViewportDisabled)
            {
                // We need to start building the realization buffer again.
                RegisterCacheBuildWork();
            }
        }

        private static void ValidateCacheLength(double cacheLength)
        {
            if (cacheLength < 0.0 || double.IsInfinity(cacheLength) || double.IsNaN(cacheLength))
            {
                throw new ArgumentException("The maximum cache length must be equal or superior to zero.");
            }
        }

        private void RegisterCacheBuildWork()
        {
            ////assert(!m_managingViewportDisabled);
            if (_owner.Layout != null &&
                _cacheBuildAction == null)
            {
                // We capture 'owner' (a strong refernce on ItemsRepeater) to make sure ItemsRepeater is still around
                // when the async action completes. By protecting ItemsRepeater, we also ensure that this instance
                // of ViewportManager (referenced by 'this' pointer) is valid because the lifetime of ItemsRepeater
                // and ViewportManager is the same (see ItemsRepeater::m_viewportManager).
                // We can't simply hold a strong reference on ViewportManager because it's not a COM object.
                ////auto strongOwner = m_owner->get_strong();
                ////m_cacheBuildAction.set(
                ////    m_owner->Dispatcher().RunIdleAsync([this, strongOwner](const winrt::IdleDispatchedHandlerArgs&)
                ////{
                ////    OnCacheBuildActionCompleted();
                ////}));
            }
        }

        private void TryInvalidateMeasure()
        {
            // Don't invalidate measure if we have an invalid window.
            if (!_visibleWindow.IsEmpty)
            {
                // We invalidate measure instead of just invalidating arrange because
                // we don't invalidate measure in UpdateViewport if the view is changing to
                // avoid layout cycles.
                _owner.InvalidateMeasure();
            }
        }

        private class ScrollerInfo
        {
            public ScrollerInfo(ScrollViewer scroller)
            {
                Scroller = scroller;
            }

            public ScrollViewer Scroller { get; }
        }
    };
}
