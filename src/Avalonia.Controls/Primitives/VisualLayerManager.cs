using System.Collections.Generic;
using Avalonia.LogicalTree;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// A control that manages multiple layers such as adorners, overlays, text selectors, and popups.
    /// </summary>
    public sealed class VisualLayerManager : Decorator
    {
        private const int AdornerZIndex = int.MaxValue - 100;
        private const int OverlayZIndex = int.MaxValue - 98;
        private const int LightDismissOverlayZIndex = int.MaxValue - 97;
        private const int PopupOverlayZIndex = int.MaxValue - 96;
        private const int TextSelectorLayerZIndex = int.MaxValue - 95;

        private ILogicalRoot? _logicalRoot;
        private readonly List<Control> _layers = new();
        private OverlayLayer? _overlayLayer;

        /// <summary>
        /// Gets or sets a value indicating whether an <see cref="Avalonia.Controls.Primitives.AdornerLayer"/> is
        /// created for this <see cref="VisualLayerManager"/>. When enabled, the adorner layer is added to the
        /// visual tree, providing a dedicated layer for rendering adorners.
        /// </summary>
        public bool EnableAdornerLayer { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether an <see cref="Avalonia.Controls.Primitives.OverlayLayer"/> is
        /// created for this <see cref="VisualLayerManager"/>. When enabled, the overlay layer is added to the
        /// visual tree, providing a dedicated layer for rendering overlay visuals.
        /// </summary>
        public bool EnableOverlayLayer { get; set; }

        internal bool EnablePopupOverlayLayer { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a <see cref="Avalonia.Controls.Primitives.TextSelectorLayer"/> is
        /// created for this <see cref="VisualLayerManager"/>. When enabled, the overlay layer is added to the
        /// visual tree, providing a dedicated layer for rendering text selection handles.
        /// </summary>
        public bool EnableTextSelectorLayer { get; set; }

        internal AdornerLayer? AdornerLayer
        {
            get
            {
                if (!EnableAdornerLayer)
                    return null;
                var rv = FindLayer<AdornerLayer>();
                if (rv == null)
                    AddLayer(rv = new AdornerLayer(), AdornerZIndex);
                return rv;
            }
        }

        internal PopupOverlayLayer? PopupOverlayLayer
        {
            get
            {
                if (!EnablePopupOverlayLayer)
                    return null;
                var rv = FindLayer<PopupOverlayLayer>();
                if (rv == null)
                    AddLayer(rv = new PopupOverlayLayer(), PopupOverlayZIndex);
                return rv;
            }
        }
        
        internal OverlayLayer? OverlayLayer
        {
            get
            {
                if (!EnableOverlayLayer)
                    return null;
                if (_overlayLayer == null)
                {
                    _overlayLayer = new OverlayLayer();
                    var adorner = new AdornerLayer();
                    _overlayLayer.AdornerLayer = adorner;
                    
                    var panel = new Panel();
                    panel.Children.Add(_overlayLayer);
                    panel.Children.Add(adorner);
                    
                    AddLayer(panel, OverlayZIndex);
                }
                return _overlayLayer;
            }
        }

        internal TextSelectorLayer? TextSelectorLayer
        {
            get
            {
                if (!EnableTextSelectorLayer)
                    return null;
                var rv = FindLayer<TextSelectorLayer>();
                if (rv == null)
                    AddLayer(rv = new TextSelectorLayer(), TextSelectorLayerZIndex);
                return rv;
            }
        }

        internal LightDismissOverlayLayer LightDismissOverlayLayer
        {
            get
            {
                var rv = FindLayer<LightDismissOverlayLayer>();
                if (rv == null)
                {
                    rv = new LightDismissOverlayLayer
                    {
                        IsVisible = false
                    };

                    AddLayer(rv, LightDismissOverlayZIndex);
                }
                return rv;
            }
        }

        private T? FindLayer<T>() where T : class
        {
            foreach (var layer in _layers)
                if (layer is T match)
                    return match;
            return null;
        }

        private void AddLayer(Control layer, int zindex)
        {
            _layers.Add(layer);
            ((ISetLogicalParent)layer).SetParent(this);
            layer.ZIndex = zindex;
            VisualChildren.Add(layer);
            if (((ILogical)this).IsAttachedToLogicalTree)
                ((ILogical)layer).NotifyAttachedToLogicalTree(
                    new LogicalTreeAttachmentEventArgs(_logicalRoot!, layer, this));
            InvalidateArrange();
        }

        /// <inheritdoc />
        internal override void NotifyChildResourcesChanged(ResourcesChangedEventArgs e)
        {
            foreach (var l in _layers)
                l.NotifyResourcesChanged(e);

            base.NotifyChildResourcesChanged(e);
        }

        /// <inheritdoc />
        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            _logicalRoot = e.Root;

            foreach (var l in _layers)
                ((ILogical)l).NotifyAttachedToLogicalTree(e);
        }

        /// <inheritdoc />
        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            _logicalRoot = null;
            base.OnDetachedFromLogicalTree(e);
            foreach (var l in _layers)
                ((ILogical)l).NotifyDetachedFromLogicalTree(e);
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            for (var index = 0; index < _layers.Count; index++)
            {
                var l = _layers[index];
                l.Measure(availableSize);
            }

            return base.MeasureOverride(availableSize);
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            for (var index = 0; index < _layers.Count; index++)
            {
                var l = _layers[index];
                l.Arrange(new Rect(finalSize));
            }

            return base.ArrangeOverride(finalSize);
        }
    }
}
