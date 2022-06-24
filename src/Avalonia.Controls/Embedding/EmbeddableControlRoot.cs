using System;
using System.ComponentModel;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.VisualTree;
using JetBrains.Annotations;

namespace Avalonia.Controls.Embedding
{
    public class EmbeddableControlRoot : TopLevel, IStyleable, IFocusScope, IDisposable
    {
        private FocusManager _focusManager;
        private OverlayLayer? _overlayLayer;

        public EmbeddableControlRoot(ITopLevelImpl impl) : base(impl)
        {
            _focusManager = new FocusManager(this);
        }

        public EmbeddableControlRoot() : base(PlatformManager.CreateEmbeddableWindow())
        {
            _focusManager = new FocusManager(this);
        }

        protected bool EnforceClientSize { get; set; } = true;

        FocusManager IFocusScope.FocusManager => _focusManager;

        IOverlayHost? IFocusScope.OverlayHost
        {
            get
            {
                if (_overlayLayer == null)
                {
                    _overlayLayer = OverlayLayer.GetOverlayLayer(this);
                }

                return _overlayLayer;
            }
        }

        public void Prepare()
        {
            EnsureInitialized();
            ApplyTemplate();
            LayoutManager.ExecuteInitialLayoutPass();
        }

        private void EnsureInitialized()
        {
            if (!this.IsInitialized)
            {
                var init = (ISupportInitialize)this;
                init.BeginInit();
                init.EndInit();
            }
        }
        
        protected override Size MeasureOverride(Size availableSize)
        {
            if (EnforceClientSize)
                availableSize = PlatformImpl?.ClientSize ?? default(Size);
            var rv = base.MeasureOverride(availableSize);
            if (EnforceClientSize)
                return availableSize;
            return rv;
        }

        Type IStyleable.StyleKey => typeof(EmbeddableControlRoot);
        public void Dispose() => PlatformImpl?.Dispose();
    }
}
