using System.Collections.Generic;
using Avalonia.LogicalTree;

namespace Avalonia.Controls.Primitives
{
    public class VisualLayerManager : Decorator
    {
        private const int AdornerZIndex = int.MaxValue - 100;
        private const int ChromeZIndex = int.MaxValue - 99;
        private const int LightDismissOverlayZIndex = int.MaxValue - 98;
        private const int OverlayZIndex = int.MaxValue - 97;
        private const int TextSelectorLayerZIndex = int.MaxValue - 96;

        private ILogicalRoot? _logicalRoot;
        private readonly List<Control> _layers = new();

        public static readonly StyledProperty<ChromeOverlayLayer?> ChromeOverlayLayerProperty =
            AvaloniaProperty.Register<VisualLayerManager, ChromeOverlayLayer?>(nameof(ChromeOverlayLayer));

        public bool IsPopup { get; set; }

        public AdornerLayer AdornerLayer
        {
            get
            {
                var rv = FindLayer<AdornerLayer>();
                if (rv == null)
                    AddLayer(rv = new AdornerLayer(), AdornerZIndex);
                return rv;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1030")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1031",
            Justification = "A hack to make ChromeOverlayLayer lazily creatable. It is expected that GetValue(ChromeOverlayLayerProperty) alone won't work.")]
        public ChromeOverlayLayer ChromeOverlayLayer
        {
            get
            {
                var current = GetValue(ChromeOverlayLayerProperty);

                if (current is null)
                {
                    var chromeOverlayLayer = new ChromeOverlayLayer();
                    AddLayer(chromeOverlayLayer, ChromeZIndex);

                    SetValue(ChromeOverlayLayerProperty, chromeOverlayLayer);

                    current = chromeOverlayLayer;
                }

                return current;
            }
        }

        public OverlayLayer? OverlayLayer
        {
            get
            {
                if (IsPopup)
                    return null;
                var rv = FindLayer<OverlayLayer>();
                if (rv == null)
                    AddLayer(rv = new OverlayLayer(), OverlayZIndex);
                return rv;
            }
        }

        public TextSelectorLayer? TextSelectorLayer
        {
            get
            {
                if (IsPopup)
                    return null;
                var rv = FindLayer<TextSelectorLayer>();
                if (rv == null)
                    AddLayer(rv = new TextSelectorLayer(), TextSelectorLayerZIndex);
                return rv;
            }
        }

        public LightDismissOverlayLayer LightDismissOverlayLayer
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
                ((ILogical)l).NotifyResourcesChanged(e);

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
            foreach (var l in _layers)
                l.Arrange(new Rect(finalSize));
            return base.ArrangeOverride(finalSize);
        }
    }
}
