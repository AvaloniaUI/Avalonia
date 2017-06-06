using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Avalonia.Win32.Interop.Wpf
{
    public class WpfAvaloniaHost : FrameworkElement, IDisposable
    {
        private WpfTopLevelImpl _impl = new WpfTopLevelImpl();
        private readonly SynchronizationContext _sync;
        public WpfAvaloniaHost()
        {
            _sync = SynchronizationContext.Current;
            _impl.ControlRoot.Prepare();
            _impl.Visibility = Visibility.Visible;
            AddLogicalChild(_impl);
            AddVisualChild(_impl);
        }
        

        public object Content
        {
            get => _impl.ControlRoot.Content;
            set => _impl.ControlRoot.Content = value;
        }

        //Separate class is needed to prevent accidential resurrection
        class Disposer
        {
            private readonly WpfTopLevelImpl _impl;

            public Disposer(WpfTopLevelImpl impl)
            {
                _impl = impl;
            }

            public void Callback(object state)
            {
                _impl.Dispose();
            }
        }

        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint) 
            => _impl.ControlRoot.MeasureBase(constraint.ToAvaloniaSize()).ToWpfSize();
        
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize)
        {
            _impl.Arrange(new System.Windows.Rect(arrangeSize));
            return arrangeSize;
        }
        
        protected override int VisualChildrenCount => 1;
        protected override System.Windows.Media.Visual GetVisualChild(int index) => _impl;

        ~WpfAvaloniaHost()
        {
            if (_impl != null)
                _sync.Post(new Disposer(_impl).Callback, null);
        }

        public void Dispose()
        {
            if (_impl != null)
            {
                RemoveVisualChild(_impl);
                RemoveLogicalChild(_impl);
                _impl.Dispose();
                _impl = null;
                GC.SuppressFinalize(this);
            }
        }
    }
}
