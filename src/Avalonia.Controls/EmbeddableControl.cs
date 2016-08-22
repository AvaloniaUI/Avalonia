using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    public class EmbeddableControl : TopLevel, IStyleable, IFocusScope, INameScope
    {
        public EmbeddableControl() : base(PlatformManager.CreateEmbeddableWindow())
        {
            PlatformImpl.Show();
        }

        public new IEmbeddableWindowImpl PlatformImpl => (IEmbeddableWindowImpl) base.PlatformImpl;

        public void Prepare()
        {
            EnsureInitialized();
            ApplyTemplate();
            LayoutManager.Instance.ExecuteInitialLayoutPass(this);
            PlatformImpl.Show();
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
            return PlatformImpl.ClientSize;
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

        Type IStyleable.StyleKey => typeof(EmbeddableControl);
    }
}
