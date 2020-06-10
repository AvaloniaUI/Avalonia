using System.Collections.Generic;
using Avalonia.LogicalTree;

namespace Avalonia.Controls.Primitives
{
    public class VisualLayerManager : Decorator
    {
        private const int AdornerZIndex = int.MaxValue - 100;
        private const int ChromeZIndex = int.MaxValue - 99;
        private const int OverlayZIndex = int.MaxValue - 98;

        private ILogicalRoot _logicalRoot;
        private readonly List<Control> _layers = new List<Control>();
        

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
                var rv = FindLayer<ChromeOverlayLayer>();
                if (rv == null)
                    AddLayer(rv = new ChromeOverlayLayer(), ChromeZIndex);
                return rv;
            }
        }

        public OverlayLayer OverlayLayer
        {
            get
            {
                if (IsPopup)
                    return null;
                var rv = FindLayer<OverlayLayer>();
                if(rv == null)
                    AddLayer(rv = new OverlayLayer(), OverlayZIndex);
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
                ((ILogical)layer).NotifyAttachedToLogicalTree(new LogicalTreeAttachmentEventArgs(_logicalRoot, layer, this));
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
