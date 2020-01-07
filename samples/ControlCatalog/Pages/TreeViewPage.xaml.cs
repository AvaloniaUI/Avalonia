using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public class TreeViewPage : UserControl
    {
        private TreeView _treeView;
        private DispatcherTimer _timer;

        public TreeViewPage()
        {
            InitializeComponent();
            _treeView = this.FindControl<TreeView>("treeView");
            _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, TimerTick);
            DataContext = new TreeViewPageViewModel();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _timer.Start();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _timer.Stop();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void TimerTick(object sender, EventArgs e)
        {
            var vm = (TreeViewPageViewModel)DataContext;
            vm.ContainerCount = _treeView.GetVisualDescendants().OfType<TreeViewItem>().Count();
        }
    }
}
