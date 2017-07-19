using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Platform;
using Avalonia.Styling;

namespace Avalonia.Win32.Interop.Wpf
{
    [ContentProperty("Content")]
    public class WpfAvaloniaHost : FrameworkElement, IDisposable, IAddChild
    {
        private WpfTopLevelImpl _impl;
        private readonly SynchronizationContext _sync;
        private bool _hasChildren;
        public WpfAvaloniaHost()
        {
            _sync = SynchronizationContext.Current;
            _impl = new WpfTopLevelImpl();
            _impl.ControlRoot.Prepare();
            _impl.Visibility = Visibility.Visible;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            PresentationSource.AddSourceChangedHandler(this, OnSourceChanged);
        }

        private void OnSourceChanged(object sender, SourceChangedEventArgs e)
        {
            if (e.NewSource != null && !_hasChildren)
            {
                AddLogicalChild(_impl);
                AddVisualChild(_impl);
                _hasChildren = true;
            }
            else
            {
                RemoveVisualChild(_impl);
                RemoveLogicalChild(_impl);
                _hasChildren = false;
            }
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
        {
            _impl.InvalidateMeasure();
            _impl.Measure(constraint);
            return _impl.DesiredSize;
        }

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

        void IAddChild.AddChild(object value)
        {
            if (Content == null)
                Content = value;
            else
                throw new InvalidOperationException();
        }

        void IAddChild.AddText(string text)
        {
            //
        }
    }
}
