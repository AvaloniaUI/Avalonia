using System;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Avalonia.Controls.Presenters
{
    public class ThingamybobPresenter : Decorator, IItemsPresenter, IScrollable
    {
        private IVirtualizingPanel _panel;
        private int _firstIndex;
        private int _lastIndex;

        public override void ApplyTemplate()
        {
            if (_panel == null)
            {
                _panel = new VirtualizingStackPanel();
                _panel.ArrangeCompleted = CheckPanel;
                Child = _panel;
                CheckPanel();
            }
        }

        public IPanel Panel => _panel;

        Action IScrollable.InvalidateScroll { get; set; }

        Size IScrollable.Extent => new Size(1, 100 * AverageItemSize );

        Vector IScrollable.Offset
        {
            get
            {
                return new Vector(0, (_firstIndex * AverageItemSize) + (_panel?.PixelOffset ?? 0));
            }

            set
            {
                var count = _lastIndex - _firstIndex;
                var firstIndex = (int)(value.Y / AverageItemSize);
                var firstIndexChanged = _firstIndex != firstIndex;
                _firstIndex = firstIndex;
                _lastIndex = _firstIndex + count;
                _panel.PixelOffset = value.Y % AverageItemSize;

                if (firstIndexChanged)
                {
                    Renumber();
                }
            }
        }

        Size IScrollable.Viewport => new Size(1, _panel?.Bounds.Height ?? 0);
        Size IScrollable.ScrollSize => new Size(0, 1);
        Size IScrollable.PageScrollSize => new Size(0, 1);

        private double AverageItemSize => _panel?.AverageItemSize ?? 1;

        protected override Size ArrangeOverride(Size finalSize)
        {
            var result = base.ArrangeOverride(finalSize);
            CreateItems();
            ((IScrollable)this).InvalidateScroll();
            return result;
        }

        private void CreateItems()
        {
            var randomColor = Color.FromUInt32(
                (uint)(0xff000000 + new Random().Next(0xffffff)));

            while (!_panel.IsFull)
            {
                _panel.Children.Add(new TextBlock
                {
                    Text = "Item " + ++_lastIndex,
                    Background = new SolidColorBrush(randomColor),
                });
            }
        }

        private void RemoveItems()
        {
            var remove = _panel.OverflowCount;

            _panel.Children.RemoveRange(
                _panel.Children.Count - remove,
                _panel.OverflowCount);
            _lastIndex -= remove;
        }

        private void Renumber()
        {
            var index = _firstIndex;

            foreach (TextBlock child in _panel.Children)
            {
                child.Text = "Item " + ++index;
            }
        }

        private void CheckPanel()
        {
            if (!_panel.IsFull)
            {
                CreateItems();
            }
            else if (_panel.OverflowCount > 1)
            {
                RemoveItems();
            }
        }
    }
}
