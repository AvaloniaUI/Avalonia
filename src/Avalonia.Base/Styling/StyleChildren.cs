using System.Collections.ObjectModel;

namespace Avalonia.Styling
{
    internal class StyleChildren : Collection<IStyle>
    {
        private readonly Style _owner;

        public StyleChildren(Style owner) => _owner = owner;

        protected override void InsertItem(int index, IStyle item)
        {
            base.InsertItem(index, item);
            (item as Style)?.SetParent(_owner);
        }

        protected override void RemoveItem(int index)
        {
            (Items[index] as Style)?.SetParent(null);
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, IStyle item)
        {
            base.SetItem(index, item);
            (item as Style)?.SetParent(_owner);
        }
    }
}
