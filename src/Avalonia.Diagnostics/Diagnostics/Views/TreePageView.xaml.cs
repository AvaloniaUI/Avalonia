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

        private void RemoveAdorner(object? sender, PointerEventArgs e)
        {
            _adorner?.Dispose();
            _adorner = null;
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

        private void OnClipboardCopyRequested(object? sender, string selector)
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            {
                var @do = new DataObject();
                var text = ToText(selector);
                @do.Set(DataFormats.Text, text);
                @do.Set(Constants.DataFormats.Avalonia_DevTools_Selector, selector);
                clipboard.SetDataObjectAsync(@do);
            }
        }

        private static string ToText(string text)
        {
            var sb = new System.Text.StringBuilder();
            var bufferStartIndex = -1;
            for (var ic = 0; ic < text.Length; ic++)
            {
                var c = text[ic];
                switch (c)
                {
                    case '{':
                        bufferStartIndex = sb.Length;
                        break;
                    case '}' when bufferStartIndex > -1:
                        sb.Remove(bufferStartIndex, sb.Length - bufferStartIndex);
                        bufferStartIndex = sb.Length;
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
