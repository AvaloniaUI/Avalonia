using System.Collections.Generic;
using Avalonia.LogicalTree;
using Avalonia.Media;

namespace Avalonia.Controls.Primitives
{
    public class VisualLayerManager : Decorator
    {
        private const int AdornerZIndex = int.MaxValue - 100;
        private const int ChromeZIndex = int.MaxValue - 99;
        private const int LightDismissOverlayZIndex = int.MaxValue - 98;
        private const int OverlayZIndex = int.MaxValue - 97;

        private ILogicalRoot _logicalRoot;
        private readonly List<Control> _layers = new List<Control>();

        public static readonly StyledProperty<ChromeOverlayLayer> ChromeOverlayLayerProperty =
            AvaloniaProperty.Register<VisualLayerManager, ChromeOverlayLayer>(nameof(ChromeOverlayLayer));

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

        public OverlayLayer OverlayLayer
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

        public LightDismissOverlayLayer LightDismissOverlayLayer
        {
            get
            {
                if (IsPopup)
                    return null;
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

        T FindLayer<T>() where T : class
        {
            foreach (var layer in _layers)
                if (layer is T match)
                    return match;
            return null;
        }

        void AddLayer(Control layer, int zindex)
        {
            _layers.Add(layer);
            ((ISetLogicalParent)layer).SetParent(this);
            layer.ZIndex = zindex;
            VisualChildren.Add(layer);
            if (((ILogical)this).IsAttachedToLogicalTree)
                ((ILogical)layer).NotifyAttachedToLogicalTree(
                    new LogicalTreeAttachmentEventArgs(_logicalRoot, layer, this));
            InvalidateArrange();
        }

        protected override void NotifyChildResourcesChanged(ResourcesChangedEventArgs e)
        {
            foreach (var l in _layers)
                ((ILogical)l).NotifyResourcesChanged(e);

            base.NotifyChildResourcesChanged(e);
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            _logicalRoot = e.Root;

            foreach (var l in _layers)
                ((ILogical)l).NotifyAttachedToLogicalTree(e);
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            _logicalRoot = null;
            base.OnDetachedFromLogicalTree(e);
            foreach (var l in _layers)
                ((ILogical)l).NotifyDetachedFromLogicalTree(e);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (var l in _layers)
                l.Measure(availableSize);
            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (var l in _layers)
                l.Arrange(new Rect(finalSize));
            return base.ArrangeOverride(finalSize);
        }
    }
}
