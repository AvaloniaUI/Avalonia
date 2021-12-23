using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// A <see cref="Controls"/> collection held by a <see cref="Panel"/>.
    /// </summary>
    internal class PanelChildren : Controls,
        ILogicalVisualChildren,
        IList<ILogical>,
        IList<IVisual>
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
            _owner.InvalidateOnChildrenChanged();
        }

        public override void Insert(int index, IControl item)
        {
            base.Insert(index, item);
            _owner.InvalidateOnChildrenChanged();
        }

        public override void InsertRange(int index, IEnumerable<IControl> items)
        {
            base.InsertRange(index, items);
            _owner.InvalidateOnChildrenChanged();
        }

        public override bool Remove(IControl item)
        {
            var result = base.Remove(item);
            
            if (result)
            {
                ClearParent(item);
                _owner.InvalidateOnChildrenChanged();
            }

            return result;
        }

        public override void RemoveAt(int index)
        {
            ClearParent(this[index]);
            base.RemoveAt(index);
            _owner.InvalidateOnChildrenChanged();
        }

        public override void RemoveRange(int index, int count)
        {
            if (count > 0)
            {
                base.RemoveRange(index, count);
                _owner.InvalidateOnChildrenChanged();
            }
        }

        public override void Move(int oldIndex, int newIndex)
        {
            base.Move(oldIndex, newIndex);
            _owner.InvalidateOnChildrenChanged();
        }

        public override void MoveRange(int oldIndex, int count, int newIndex)
        {
            base.MoveRange(oldIndex, count, newIndex);
            _owner.InvalidateOnChildrenChanged();
        }

        public override void Clear()
        {
            foreach (var item in this)
            {
                if (item.LogicalParent == _owner)
                    ((ISetLogicalParent)item).SetParent(null);
                ((ISetVisualParent)item).SetParent(null);
            }

            base.Clear();
            _owner.InvalidateOnChildrenChanged();
        }

        ILogical IList<ILogical>.this[int index] 
        {
            get => this[index]; 
            set => this[index] = (IControl)value;
        }

        IVisual IList<IVisual>.this[int index]
        {
            get => this[index];
            set => this[index] = (IControl)value;
        }

        IReadOnlyList<ILogical> ILogicalVisualChildren.Logical => this;
        IReadOnlyList<IVisual> ILogicalVisualChildren.Visual => this;
        IList<ILogical> ILogicalVisualChildren.LogicalMutable => this;
        IList<IVisual> ILogicalVisualChildren.VisualMutable => this;
        bool ICollection<ILogical>.IsReadOnly => true;
        bool ICollection<IVisual>.IsReadOnly => true;

        void ILogicalVisualChildren.AddLogicalChildrenChangedHandler(NotifyCollectionChangedEventHandler handler)
        {
            CollectionChanged += handler;
        }

        void ILogicalVisualChildren.RemoveLogicalChildrenChangedHandler(NotifyCollectionChangedEventHandler handler)
        {
            CollectionChanged -= handler;
        }

        void ILogicalVisualChildren.AddVisualChildrenChangedHandler(NotifyCollectionChangedEventHandler handler)
        {
            CollectionChanged += handler;
        }

        void ILogicalVisualChildren.RemoveVisualChildrenChangedHandler(NotifyCollectionChangedEventHandler handler)
        {
            CollectionChanged -= handler;
        }

        void ICollection<ILogical>.Add(ILogical item) => Add((IControl)item);
        void ICollection<IVisual>.Add(IVisual item) => Add((IControl)item);
        bool ICollection<ILogical>.Contains(ILogical item) => item is IControl c ? Contains(c) : false;
        bool ICollection<IVisual>.Contains(IVisual item) => item is IControl c ? Contains(c) : false;
        void ICollection<ILogical>.CopyTo(ILogical[] array, int arrayIndex) => ((ICollection)this).CopyTo(array, arrayIndex);
        void ICollection<IVisual>.CopyTo(IVisual[] array, int arrayIndex) => ((ICollection)this).CopyTo(array, arrayIndex);
        IEnumerator<ILogical> IEnumerable<ILogical>.GetEnumerator() => GetEnumerator();
        IEnumerator<IVisual> IEnumerable<IVisual>.GetEnumerator() => GetEnumerator();
        int IList<ILogical>.IndexOf(ILogical item) => item is IControl c ? IndexOf(c) : -1;
        int IList<IVisual>.IndexOf(IVisual item) => item is IControl c ? IndexOf(c) : -1;
        void IList<ILogical>.Insert(int index, ILogical item) => Insert(index, (IControl)item);
        void IList<IVisual>.Insert(int index, IVisual item) => Insert(index, (IControl)item);
        bool ICollection<ILogical>.Remove(ILogical item) => Remove((IControl)item);
        bool ICollection<IVisual>.Remove(IVisual item) => Remove((IControl)item);

        private void ClearParent(IControl? c)
        {
            if (c is null)
                return;
            if (c.LogicalParent == _owner)
                ((ISetLogicalParent)c).SetParent(null);
            ((ISetVisualParent)c).SetParent(null);
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
            ((ISetVisualParent)c).SetParent(_owner);
        }
    }
}
