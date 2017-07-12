using System;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Styling;
using JetBrains.Annotations;

namespace Avalonia.Controls.Embedding
{
    public class EmbeddableControlRoot : TopLevel, IStyleable, IFocusScope, INameScope, IDisposable
    {
        public EmbeddableControlRoot(IEmbeddableWindowImpl impl) : base(impl)
        {
            
        }

        public EmbeddableControlRoot() : base(PlatformManager.CreateEmbeddableWindow())
        {
        }

        [CanBeNull]
        public new IEmbeddableWindowImpl PlatformImpl => (IEmbeddableWindowImpl) base.PlatformImpl;

        public void Prepare()
        {
            EnsureInitialized();
            ApplyTemplate();
            LayoutManager.Instance.ExecuteInitialLayoutPass(this);
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
            var cs = PlatformImpl?.ClientSize ?? default(Size);
            base.MeasureOverride(cs);
            return cs;
        }

        private readonly NameScope _nameScope = new NameScope();
        public event EventHandler<NameScopeEventArgs> Registered
        {
            add { _nameScope.Registered += value; }
            remove { _nameScope.Registered -= value; }
        }

        public event EventHandler<NameScopeEventArgs> Unregistered
        {
            add { _nameScope.Unregistered += value; }
            remove { _nameScope.Unregistered -= value; }
        }

        public void Register(string name, object element) => _nameScope.Register(name, element);

        public object Find(string name) => _nameScope.Find(name);

        public void Unregister(string name) => _nameScope.Unregister(name);

        Type IStyleable.StyleKey => typeof(EmbeddableControlRoot);
        public void Dispose() => PlatformImpl?.Dispose();
    }
}
