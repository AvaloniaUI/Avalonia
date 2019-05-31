using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Controls.Repeaters
{
    internal class UniqueIdElementPool : IEnumerable<KeyValuePair<string, IControl>>
    {
        private readonly Dictionary<string, IControl> _elementMap = new Dictionary<string, IControl>();
        private readonly ItemsRepeater _owner;

        public UniqueIdElementPool(ItemsRepeater owner) => _owner = owner;

        public void Add(IControl element)
        {
            //MUX_ASSERT(_owner.ItemsSourceView.HasKeyIndexMapping);

            var virtInfo = ItemsRepeater.GetVirtualizationInfo(element);
            var key = virtInfo.UniqueId;

            if (_elementMap.ContainsKey(key))
            {
                throw new InvalidOperationException($"The unique id provided ({key}) is not unique.");
            }

            _elementMap.Add(key, element);
        }

        public IControl Remove(int index)
        {
            //MUX_ASSERT(_owner.ItemsSourceView.HasKeyIndexMapping);

            // Check if there is already a element in the mapping and if so, use it.
            string key = _owner.ItemsSourceView.KeyFromIndex(index);

            if (_elementMap.TryGetValue(key, out var element))
            {
                _elementMap.Remove(key);
            }

            return element;
        }

        public void Clear()
        {
            //MUX_ASSERT(_owner.ItemsSourceView.HasKeyIndexMapping);
            _elementMap.Clear();
        }

        public IEnumerator<KeyValuePair<string, IControl>> GetEnumerator() => _elementMap.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
