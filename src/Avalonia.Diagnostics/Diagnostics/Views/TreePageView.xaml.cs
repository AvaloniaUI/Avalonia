using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Diagnostics.Controls.VirtualizedTreeView;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Avalonia.Diagnostics.Views
{
    internal class TreePageView : UserControl
    {
        private readonly Panel _adorner;
        private AdornerLayer? _currentLayer;
        private VirtualizedTreeViewItem? _hovered;
        private VirtualizedTreeView _tree;

        public TreePageView()
        {
            InitializeComponent();
            _tree = this.GetControl<VirtualizedTreeView>("tree");

            _adorner = new Panel
            {
                ClipToBounds = false,
                Children =
                {
                    //Padding frame
                    new Border { BorderBrush = new SolidColorBrush(Colors.Green, 0.5) },
                    //Content frame
                    new Border { Background = new SolidColorBrush(Color.FromRgb(160, 197, 232), 0.5) },
                    //Margin frame
                    new Border { BorderBrush = new SolidColorBrush(Colors.Yellow, 0.5) }
                },
            };
            AdornerLayer.SetIsClipEnabled(_adorner, false);
        }

        private static Thickness InvertThickness(Thickness input)
        {
            return new Thickness(-input.Left, -input.Top, -input.Right, -input.Bottom);
        }

        protected void AddAdorner(object? sender, PointerEventArgs e)
        {
            var node = (TreeNode?)((FlatTreeNode?)((Control)sender!).DataContext)?.Node;
            var vm = (TreePageViewModel?)DataContext;
            if (node is null || vm is null)
            {
                return;
            }

            var visual = node.Visual as Visual;

            if (visual is null)
            {
                return;
            }

            _currentLayer = AdornerLayer.GetAdornerLayer(visual);

            if (_currentLayer == null ||
                _currentLayer.Children.Contains(_adorner))
            {
                return;
            }

            _currentLayer.Children.Add(_adorner);
            AdornerLayer.SetAdornedElement(_adorner, visual);

            if (vm.MainView.ShouldVisualizeMarginPadding)
            {
                var paddingBorder = (Border)_adorner.Children[0];
                paddingBorder.BorderThickness = visual.GetValue(PaddingProperty);

                var contentBorder = (Border)_adorner.Children[1];
                contentBorder.Margin = visual.GetValue(PaddingProperty);

                var marginBorder = (Border)_adorner.Children[2];
                marginBorder.BorderThickness = visual.GetValue(MarginProperty);
                marginBorder.Margin = InvertThickness(visual.GetValue(MarginProperty));
            }
        }

        protected void RemoveAdorner(object? sender, PointerEventArgs e)
        {
            foreach (var border in _adorner.Children.OfType<Border>())
            {
                border.Margin = default;
                border.Padding = default;
                border.BorderThickness = default;
            }

            _currentLayer?.Children.Remove(_adorner);
            _currentLayer = null;
        }

        protected void UpdateAdorner(object? sender, PointerEventArgs e)
        {
            if (e.Source is not StyledElement source)
            {
                return;
            }

            var item = source.FindLogicalAncestorOfType<VirtualizedTreeViewItem>();
            if (item == _hovered)
            {
                return;
            }

            RemoveAdorner(sender, e);

            if (item is null)
            {
                _hovered = null;
                return;
            }

            _hovered = item;
            AddAdorner(item, e);
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
