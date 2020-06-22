using System;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.Views
{
    internal class MainWindow : Window, IStyleHost
    {
        private TopLevel _root;
        private IDisposable _keySubscription;

        public MainWindow()
        {
            InitializeComponent();

            _keySubscription = InputManager.Instance.Process
                .OfType<RawKeyEventArgs>()
                .Subscribe(RawKeyDown);
        }

        public TopLevel Root
        {
            get => _root;
            set
            {
                if (_root != value)
                {
                    _root = value;
                    DataContext = new MainViewModel(value);
                }
            }
        }

        IStyleHost IStyleHost.StylingParent => null;

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _keySubscription.Dispose();
            ((MainViewModel)DataContext)?.Dispose();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void RawKeyDown(RawKeyEventArgs e)
        {
            const RawInputModifiers modifiers = RawInputModifiers.Control | RawInputModifiers.Shift;

            if (e.Modifiers == modifiers)
            {
                var point = (Root as IInputRoot)?.MouseDevice?.GetPosition(Root) ?? default;
                var control = Root.GetVisualsAt(point, x => (!(x is AdornerLayer) && x.IsVisible))
                    .FirstOrDefault();

                if (control != null)
                {
                    var vm = (MainViewModel)DataContext;
                    vm.SelectControl((IControl)control);
                }
            }
        }
    }
}
