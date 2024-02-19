using Avalonia.Controls;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;

namespace Avalonia.Diagnostics.Views
{
    internal class TreePageView : UserControl
    {
        private TreeViewItem? _hovered;
        private TreeView _tree;
        private System.IDisposable? _adorner;

        public TreePageView()
        {
            InitializeComponent();
            _tree = this.GetControl<TreeView>("tree");
        }

        protected void UpdateAdorner(object? sender, PointerEventArgs e)
        {
            if (e.Source is not StyledElement source)
            {
                return;
            }

            var item = source.FindLogicalAncestorOfType<TreeViewItem>();
            if (item == _hovered)
            {
                return;
            }

            _adorner?.Dispose();

            if (item is null || item.TreeViewOwner != _tree)
            {
                _hovered = null;
                return;
            }

            _hovered = item;

            var visual = (item.DataContext as TreeNode)?.Visual as Visual;
            var shouldVisualizeMarginPadding = (DataContext as TreePageViewModel)?.MainView.ShouldVisualizeMarginPadding;
            if (visual is null || shouldVisualizeMarginPadding is null)
            {
                return;
            }

            _adorner = Controls.ControlHighlightAdorner.Add(visual, visualizeMarginPadding: shouldVisualizeMarginPadding == true);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == DataContextProperty)
            {
                if (change.GetOldValue<object?>() is TreePageViewModel oldViewModel)
                    oldViewModel.ClipboardCopyRequested -= OnClipboardCopyRequested;
                if (change.GetNewValue<object?>() is TreePageViewModel newViewModel)
                    newViewModel.ClipboardCopyRequested += OnClipboardCopyRequested;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnClipboardCopyRequested(object? sender, string e)
        {
            TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(e);
        }
    }
}
