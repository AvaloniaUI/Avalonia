using System;
using System.ComponentModel;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Styling;
using JetBrains.Annotations;

namespace Avalonia.Controls.Embedding
{
    public class EmbeddableControlRoot : TopLevel, IStyleable, IFocusScope, IDisposable
    {
        public EmbeddableControlRoot(IEmbeddableWindowImpl impl) : base(impl)
        {
            
        }

        public EmbeddableControlRoot() : base(PlatformManager.CreateEmbeddableWindow())
        {
        }

        [CanBeNull]
        public new IEmbeddableWindowImpl PlatformImpl => (IEmbeddableWindowImpl) base.PlatformImpl;

        protected bool EnforceClientSize { get; set; } = true;

        public void Prepare()
        {
            EnsureInitialized();
            ApplyTemplate();
            LayoutManager.ExecuteInitialLayoutPass(this);
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
