using System;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using Avalonia.Win32.Interoperability.Wpf;
using AvControl = Avalonia.Controls.Control;

namespace Avalonia.Win32.Interoperability
{
    /// <summary>
    /// An element that allows you to host a Avalonia control on a WPF page.
    /// </summary>
    [ContentProperty("Content")]
    public class WpfAvaloniaHost : FrameworkElement, IDisposable, IAddChild
    {
        private WpfTopLevelImpl _impl;
        private readonly SynchronizationContext _sync;
        private bool _hasChildren;

        /// <summary>
        /// Initializes a new instance of the <see cref="WpfAvaloniaHost"/> class.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the Avalonia control hosted by the <see cref="WpfAvaloniaHost"/> element.
        /// </summary>
        public AvControl Content
        {
            get => (AvControl)_impl.ControlRoot.Content;
            set => _impl.ControlRoot.Content = value;
        }

        //Separate class is needed to prevent accidental resurrection
        private class Disposer
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

        /// <inheritdoc />
        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint)
        {
            _impl.InvalidateMeasure();
            _impl.Measure(constraint);
            return _impl.DesiredSize;
        }

        /// <inheritdoc />
        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize)
        {
            _impl.Arrange(new System.Windows.Rect(arrangeSize));
            return arrangeSize;
        }
        
        /// <inheritdoc />
        protected override int VisualChildrenCount => 1;

        /// <inheritdoc />
        protected override System.Windows.Media.Visual GetVisualChild(int index) => _impl;

        ~WpfAvaloniaHost()
        {
            if (_impl != null)
                _sync.Post(new Disposer(_impl).Callback, null);
        }

        /// <inheritdoc />
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
                if (value is AvControl avControl)
                    Content = avControl;
                else
                    throw new InvalidOperationException("WpfAvaloniaHost.Content only accepts value of Avalonia.Controls.Control type.");
            else
                throw new InvalidOperationException("WpfAvaloniaHost.Content was already set.");
        }

        void IAddChild.AddText(string text)
        {
            //
        }
    }
}
