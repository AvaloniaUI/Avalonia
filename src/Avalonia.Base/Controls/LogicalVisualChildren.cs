using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Controls
{
    internal class LogicalVisualChildren : ILogicalVisualChildren
    {
        private readonly StyledElement _owner;
        private AvaloniaList<ILogical>? _logical;
        private AvaloniaList<IVisual>? _visual;

        public LogicalVisualChildren(StyledElement owner) => _owner = owner;

        public IReadOnlyList<ILogical> Logical => _logical ?? (IReadOnlyList<ILogical>)Array.Empty<ILogical>();
        public IReadOnlyList<IVisual> Visual => _visual ?? (IReadOnlyList<IVisual>)Array.Empty<IVisual>();
        public IList<ILogical> LogicalMutable => GetOrCreateLogical();
        public IList<IVisual> VisualMutable => GetOrCreateVisual();

        public void AddLogicalChildrenChangedHandler(NotifyCollectionChangedEventHandler handler)
        {
            GetOrCreateLogical().CollectionChanged += handler;
        }

        public void RemoveLogicalChildrenChangedHandler(NotifyCollectionChangedEventHandler handler)
        {
            GetOrCreateLogical().CollectionChanged -= handler;
        }

        public void AddVisualChildrenChangedHandler(NotifyCollectionChangedEventHandler handler)
        {
            GetOrCreateVisual().CollectionChanged += handler;
        }

        public void RemoveVisualChildrenChangedHandler(NotifyCollectionChangedEventHandler handler)
        {
            GetOrCreateVisual().CollectionChanged -= handler;
        }

        private AvaloniaList<ILogical> GetOrCreateLogical()
        {
            if (_logical is null)
            {
                _logical = new();
                _logical.ResetBehavior = ResetBehavior.Remove;
                _logical.Validate = x => ValidateLogicalChild(x);
                _logical.CollectionChanged += OnLogicalCollectionChanged;
            }

            return _logical;
        }

        private AvaloniaList<IVisual> GetOrCreateVisual()
        {
            if (_visual is null)
            {
                _visual = new();
                _visual.ResetBehavior = ResetBehavior.Remove;
                _visual.Validate = x => ValidateVisualChild(x);
                _visual.CollectionChanged += OnVisualCollectionChanged;
            }

            return _visual;
        }

        private void OnLogicalCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            static void SetLogicalParent(IList children, StyledElement owner)
            {
                var count = children.Count;

                for (var i = 0; i < count; i++)
                {
                    var logical = (ILogical)children[i];

                    if (logical.LogicalParent is null)
                    {
                        ((ISetLogicalParent)logical).SetParent(owner);
                    }
                }
            }

            static void ClearLogicalParent(IList children, StyledElement owner)
            {
                var count = children.Count;

                for (var i = 0; i < count; i++)
                {
                    var logical = (ILogical)children[i];

                    if (logical.LogicalParent == owner)
                    {
                        ((ISetLogicalParent)logical).SetParent(null);
                    }
                }
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    SetLogicalParent(e.NewItems, _owner);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    ClearLogicalParent(e.OldItems, _owner);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    ClearLogicalParent(e.OldItems, _owner);
                    SetLogicalParent(e.NewItems, _owner);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException("Reset should not be signaled on LogicalChildren collection");
            }
        }

        private void OnVisualCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            static void SetVisualParent(IList children, Visual? parent)
            {
                var count = children.Count;

                for (var i = 0; i < count; i++)
                {
                    ((ISetVisualParent)children[i]).SetParent(parent);
                }
            }

            var owner = _owner as Visual;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    SetVisualParent(e.NewItems!, owner);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    SetVisualParent(e.OldItems!, null);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    SetVisualParent(e.OldItems!, null);
                    SetVisualParent(e.NewItems!, owner);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException("Reset should not be signaled on VisualChildren collection");
            }
        }

        private static void ValidateLogicalChild(ILogical c)
        {
            _ = c ?? throw new ArgumentException("Cannot add null to LogicalChildren.");
        }

        private static void ValidateVisualChild(IVisual c)
        {
            _ = c ?? throw new ArgumentException("Cannot add null to VisualChildren.");
            if (c.VisualParent is not null)
                throw new InvalidOperationException("The control already has a visual parent.");
        }
    }
}
