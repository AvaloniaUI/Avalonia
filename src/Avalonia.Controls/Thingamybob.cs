using Avalonia.Media;
using System;

namespace Avalonia.Controls
{
    public class Thingamybob : Decorator
    {
        private int _lastIndex;

        public override void ApplyTemplate()
        {
            if (Child == null)
            {
                Child = new VirtualizingStackPanel();
                ((IVirtualizingPanel)Child).ArrangeCompleted = CheckPanel;
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var result = base.ArrangeOverride(finalSize);
            CreateItems();
            return result;
        }

        private void CreateItems()
        {
            var panel = Child as IVirtualizingPanel;
            var randomColor = Color.FromUInt32(
                (uint)(0xff000000 + new Random().Next(0xffffff)));

            while (!panel.IsFull)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "Item " + ++_lastIndex,
                    Background = new SolidColorBrush(randomColor),
                });
            }
        }

        private void RemoveItems()
        {
            var panel = Child as IVirtualizingPanel;
            var remove = panel.OverflowCount;

            panel.Children.RemoveRange(
                panel.Children.Count - remove,
                panel.OverflowCount);
            _lastIndex -= remove;
        }

        private void CheckPanel()
        {
            var panel = Child as IVirtualizingPanel;

            if (!panel.IsFull)
            {
                CreateItems();
            }
            else if (panel.OverflowCount > 0)
            {
                RemoveItems();
            }
        }
    }
}
