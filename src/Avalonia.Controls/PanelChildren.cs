using System;
using System.Collections.Generic;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// A <see cref="Controls"/> collection held by a <see cref="Panel"/>.
    /// </summary>
    internal class PanelChildren : Controls
    {
        private readonly Panel _owner;

        public PanelChildren(Panel owner)
        {
            _owner = owner;
            Validate = ValidateItem;
        }

        public override IControl this[int index]
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

        public override void Add(IControl item)
        {
            base.Add(item);
            _owner.InvalidateDueToChildrenChange();
        }

        public override void Insert(int index, IControl item)
        {
            base.Insert(index, item);
            _owner.InvalidateDueToChildrenChange();
        }

        public override void InsertRange(int index, IEnumerable<IControl> items)
        {
            base.InsertRange(index, items);
            _owner.InvalidateDueToChildrenChange();
        }

        public override bool Remove(IControl item)
        {
            var result = base.Remove(item);

            if (result)
            {
                ClearParent(item);
                _owner.InvalidateDueToChildrenChange();
            }

            return result;
        }

        public override void RemoveAt(int index)
        {
            ClearParent(this[index]);
            base.RemoveAt(index);
            _owner.InvalidateDueToChildrenChange();
        }

        public override void RemoveRange(int index, int count)
        {
            if (count > 0)
            {
                for (var i = index; i < count; ++i)
                    ClearParent(this[i]);
                base.RemoveRange(index, count);
                _owner.InvalidateDueToChildrenChange();
            }
        }

        public override void Move(int oldIndex, int newIndex)
        {
            base.Move(oldIndex, newIndex);
            _owner.InvalidateDueToChildrenChange();
        }

        public override void MoveRange(int oldIndex, int count, int newIndex)
        {
            base.MoveRange(oldIndex, count, newIndex);
            _owner.InvalidateDueToChildrenChange();
        }

        public override void Clear()
        {
            foreach (var item in this)
            {
                if (item.LogicalParent == _owner)
                    ((ISetLogicalParent)item).SetParent(null);
                _owner.RemoveVisualChild(item);
            }

            base.Clear();
            _owner.InvalidateDueToChildrenChange();
        }

        private void ClearParent(IControl? c)
        {
            if (c is null)
                return;
            if (c.LogicalParent == _owner)
                ((ISetLogicalParent)c).SetParent(null);
            _owner.RemoveVisualChild(c);
        }

        private void ValidateItem(IControl c)
        {
            _ = c ?? throw new ArgumentException("Cannot add null to Panel.Children.");
            if (c.VisualParent is not null)
                throw new InvalidOperationException("The control already has a visual parent.");

            // It's a bit naughty to do this during validation, but saves iterating the
            // added items multiple times in the case of an AddRange.
            if (c.LogicalParent is null)
                ((ISetLogicalParent)c).SetParent(_owner);
            _owner.AddVisualChild(c);
        }
    }
}
