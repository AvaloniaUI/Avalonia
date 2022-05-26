using System.Collections.ObjectModel;
using Avalonia.Controls;

namespace Avalonia.Styling
{
    internal class StyleChildren : Collection<IStyle>
    {
        private readonly Style _owner;

        public StyleChildren(Style owner) => _owner = owner;

        protected override void InsertItem(int index, IStyle item)
        {
            (item as Style)?.SetParent(_owner);
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            var item = Items[index];
            (item as Style)?.SetParent(null);
            if (_owner.Owner is IResourceHost host)
                (item as IResourceProvider)?.RemoveOwner(host);
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, IStyle item)
        {
            (item as Style)?.SetParent(_owner);
            base.SetItem(index, item);
            if (_owner.Owner is IResourceHost host)
                (item as IResourceProvider)?.AddOwner(host);
        }
    }
}
