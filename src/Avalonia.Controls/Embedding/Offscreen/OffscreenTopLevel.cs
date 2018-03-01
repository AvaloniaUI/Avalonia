using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Layout;
using Avalonia.Styling;

namespace Avalonia.Controls.Embedding.Offscreen
{
    class OffscreenTopLevel : TopLevel, IStyleable
    {
        public OffscreenTopLevelImplBase Impl { get; }

        public OffscreenTopLevel(OffscreenTopLevelImplBase impl) : base(impl)
        {
            Impl = impl;
            Prepare();
        }

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
        public void Dispose()
        {
            PlatformImpl.Dispose();
        }
    }
}
