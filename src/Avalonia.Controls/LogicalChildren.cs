using System;
using Avalonia.Collections;
using Avalonia.LogicalTree;

#nullable enable

namespace Avalonia.Controls
{
    internal class LogicalChildren : AvaloniaList<ILogical>
    {
        private readonly IControl _owner;

        public LogicalChildren(IControl owner)
        {
            _owner = owner;
            Validate = ValidateItem;
        }

        public override ILogical this[int index]
        {
            get => base[index];
            set
            {
                var oldValue = base[index];

                if (oldValue != value)
                {
                    ClearParent(oldValue);
                    base[index] = value;
                }
            }
        }

        public override bool Remove(ILogical item)
        {
            var result = base.Remove(item);
            if (result)
                ClearParent(item);
            return result;
        }

        public override void RemoveAt(int index)
        {
            ClearParent(this[index]);
            base.RemoveAt(index);
        }

        public override void RemoveRange(int index, int count)
        {
            if (count > 0)
            {
                for (var i = index; i < count; ++i)
                    ClearParent(this[i]);
                base.RemoveRange(index, count);
            }
        }

        public override void Clear()
        {
            foreach (var item in this)
            {
                if (item.LogicalParent == _owner)
                    ((ISetLogicalParent)item).SetParent(null);
            }

            base.Clear();
        }

        private void ClearParent(ILogical? c)
        {
            if (c is null)
                return;
            if (c.LogicalParent == _owner)
                ((ISetLogicalParent)c).SetParent(null);
        }

        private void ValidateItem(ILogical c)
        {
            _ = c ?? throw new ArgumentException("Cannot add null to LogicalChildren.");

            // It's a bit naughty to do this during validation, but saves iterating the
            // added items multiple times in the case of an AddRange.
            if (c.LogicalParent is null)
                ((ISetLogicalParent)c).SetParent(_owner);
        }
    }
}
