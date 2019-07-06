using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia.Data.Core;
using Avalonia.Reactive;
using Avalonia.Utilities;

namespace Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings
{
    public static class NotifyingPropertyInfoHelpers
    {
        public static INotifyingPropertyInfo CreateINPCPropertyInfo(IPropertyInfo basePropertyInfo)
            => new INPCPropertyInfo(basePropertyInfo);

        public static INotifyingPropertyInfo CreateAvaloniaPropertyInfo(AvaloniaProperty property)
            => new AvaloniaPropertyInfo(property);

        public static INotifyingPropertyInfo CreateIndexerPropertyInfo(IPropertyInfo basePropertyInfo, int argument)
            => new IndexerInfo(basePropertyInfo, argument);
    }

    public interface INotifyingPropertyInfo : IPropertyInfo
    {
        void OnPropertyChanged(object target, EventHandler handler);
        void RemoveListener(object target, EventHandler handler);
    }

    internal abstract class NotifyingPropertyInfoBase : INotifyingPropertyInfo
    {
        private readonly IPropertyInfo _base;
        protected readonly ConditionalWeakTable<object, EventHandler> _changedHandlers = new ConditionalWeakTable<object, EventHandler>();

        public NotifyingPropertyInfoBase(IPropertyInfo baseProperty)
        {
            _base = baseProperty;
        }

        public string Name => _base.Name;

        public bool CanSet => _base.CanSet;

        public bool CanGet => _base.CanGet;

        public Type PropertyType => _base.PropertyType;

        public object Get(object target)
        {
            return _base.Get(target);
        }

        public void Set(object target, object value)
        {
            _base.Set(target, value);
        }

        public void OnPropertyChanged(object target, EventHandler handler)
        {
            if (ValidateTargetType(target))
            {
                return;
            }

            if (_changedHandlers.TryGetValue(target, out var value))
            {
                _changedHandlers.Remove(target);
                _changedHandlers.Add(target, (EventHandler)Delegate.Combine(value, handler));
            }
            else
            {
                _changedHandlers.Add(target, handler);
                SubscribeToChangesForNewTarget(target);
            }
        }

        protected abstract bool ValidateTargetType(object target);

        protected abstract void SubscribeToChangesForNewTarget(object target);

        protected abstract void UnsubscribeToChangesForTarget(object target);

        protected bool TryGetHandlersForTarget(object target, out EventHandler handlers)
            => _changedHandlers.TryGetValue(target, out handlers);

        public void RemoveListener(object target, EventHandler handler)
        {
            if (!ValidateTargetType(target))
            {
                return;
            }

            if (_changedHandlers.TryGetValue(target, out var value))
            {
                _changedHandlers.Remove(target);
                EventHandler modified = (EventHandler)Delegate.Remove(value, handler);
                if (modified != null)
                {
                    _changedHandlers.Add(target, modified);
                }
                else
                {
                    UnsubscribeToChangesForTarget(target);
                }
            }
        }
    }

    internal class INPCPropertyInfo : NotifyingPropertyInfoBase
    {
        public INPCPropertyInfo(IPropertyInfo baseProperty)
            :base(baseProperty)
        {
        }

        void OnNotifyPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Name == e.PropertyName && TryGetHandlersForTarget(sender, out var handlers))
            {
                handlers(sender, EventArgs.Empty);
            }
        }

        protected override bool ValidateTargetType(object target)
        {
            return target is INotifyPropertyChanged;
        }

        protected override void SubscribeToChangesForNewTarget(object target)
        {
            if (target is INotifyPropertyChanged inpc)
            {
                WeakEventHandlerManager.Subscribe<INotifyPropertyChanged, PropertyChangedEventArgs, INPCPropertyInfo>(
                    inpc,
                    nameof(INotifyPropertyChanged.PropertyChanged),
                    OnNotifyPropertyChanged);
            }
        }

        protected override void UnsubscribeToChangesForTarget(object target)
        {
            if (target is INotifyPropertyChanged)
            {
                WeakEventHandlerManager.Unsubscribe<PropertyChangedEventArgs, INPCPropertyInfo>(
                    target,
                    nameof(INotifyPropertyChanged.PropertyChanged),
                    OnNotifyPropertyChanged); 
            }
        }
    }

    internal class AvaloniaPropertyInfo : NotifyingPropertyInfoBase
    {
        private readonly AvaloniaProperty _base;

        public AvaloniaPropertyInfo(AvaloniaProperty baseProperty)
            :base(baseProperty)
        {
            _base = baseProperty;
        }

        protected override void SubscribeToChangesForNewTarget(object target)
        {
            IAvaloniaObject obj = (IAvaloniaObject)target;
            obj.PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (_base == e.Property && TryGetHandlersForTarget(sender, out var handlers))
            {
                handlers(sender, EventArgs.Empty);
            }
        }

        protected override void UnsubscribeToChangesForTarget(object target)
        {
            ((IAvaloniaObject)target).PropertyChanged -= OnPropertyChanged;
        }

        protected override bool ValidateTargetType(object target)
        {
            return target is IAvaloniaObject;
        }
    }

    internal class IndexerInfo : INPCPropertyInfo
    {
        private int _index;

        public IndexerInfo(IPropertyInfo baseProperty, int indexerArgument) : base(baseProperty)
        {
            _index = indexerArgument;
        }

        protected override void SubscribeToChangesForNewTarget(object target)
        {
            base.SubscribeToChangesForNewTarget(target);
            if (target is INotifyCollectionChanged incc)
            {
                WeakEventHandlerManager.Subscribe<INotifyCollectionChanged, NotifyCollectionChangedEventArgs, IndexerInfo>(
                  incc,
                  nameof(INotifyCollectionChanged.CollectionChanged),
                  OnNotifyCollectionChanged); 
            }
        }

        protected override void UnsubscribeToChangesForTarget(object target)
        {
            base.UnsubscribeToChangesForTarget(target);
            if (target is INotifyCollectionChanged)
            {
                WeakEventHandlerManager.Unsubscribe<NotifyCollectionChangedEventArgs, IndexerInfo>(
                  target,
                  nameof(INotifyCollectionChanged.CollectionChanged),
                  OnNotifyCollectionChanged);
            }
        }

        void OnNotifyCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (ShouldNotifyListeners(args) && TryGetHandlersForTarget(sender, out var handlers))
            {
                handlers(sender, EventArgs.Empty);
            }
        }

        bool ShouldNotifyListeners(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    return _index >= e.NewStartingIndex;
                case NotifyCollectionChangedAction.Remove:
                    return _index >= e.OldStartingIndex;
                case NotifyCollectionChangedAction.Replace:
                    return _index >= e.NewStartingIndex &&
                           _index < e.NewStartingIndex + e.NewItems.Count;
                case NotifyCollectionChangedAction.Move:
                    return (_index >= e.NewStartingIndex &&
                            _index < e.NewStartingIndex + e.NewItems.Count) ||
                           (_index >= e.OldStartingIndex &&
                            _index < e.OldStartingIndex + e.OldItems.Count);
                case NotifyCollectionChangedAction.Reset:
                    return true;
            }
            return false;
        }

        protected override bool ValidateTargetType(object target)
            => base.ValidateTargetType(target) || target is INotifyCollectionChanged;
    }
}
