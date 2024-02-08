using System;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Utilities;
using Avalonia.Reactive;

namespace Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings
{
    public static class PropertyInfoAccessorFactory
    {
        public static IPropertyAccessor CreateInpcPropertyAccessor(WeakReference<object?> target, IPropertyInfo property)
            => new InpcPropertyAccessor(target, property);

        public static IPropertyAccessor CreateAvaloniaPropertyAccessor(WeakReference<object?> target, IPropertyInfo property)
            => new AvaloniaPropertyAccessor(new WeakReference<AvaloniaObject?>((AvaloniaObject?)(target.TryGetTarget(out var o) ? o : null)), (AvaloniaProperty)property);

        public static IPropertyAccessor CreateIndexerPropertyAccessor(WeakReference<object?> target, IPropertyInfo property, int argument)
            => new IndexerAccessor(target, property, argument);
    }

    internal class AvaloniaPropertyAccessor : PropertyAccessorBase
    {
        private readonly WeakReference<AvaloniaObject?> _reference;
        private readonly AvaloniaProperty _property;
        private IDisposable? _subscription;

        public AvaloniaPropertyAccessor(WeakReference<AvaloniaObject?> reference, AvaloniaProperty property)
        {
            _reference = reference ?? throw new ArgumentNullException(nameof(reference));
            _property = property ?? throw new ArgumentNullException(nameof(property));
        }

        public AvaloniaObject? Instance
        {
            get
            {
                _reference.TryGetTarget(out var result);
                return result;
            }
        }

        public override Type PropertyType => _property.PropertyType;
        public override object? Value => Instance?.GetValue(_property);

        public override bool SetValue(object? value, BindingPriority priority)
        {
            if (!_property.IsReadOnly && Instance is { } instance)
            {
                instance.SetValue(_property, value, priority);
                return true;
            }

            return false;
        }

        protected override void SubscribeCore()
        {
            _subscription = Instance?.GetObservable(_property).Subscribe(PublishValue);
        }

        protected override void UnsubscribeCore()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }

    internal class InpcPropertyAccessor : PropertyAccessorBase, IWeakEventSubscriber<PropertyChangedEventArgs>
    {
        protected readonly WeakReference<object?> _reference;
        private readonly IPropertyInfo _property;

        public InpcPropertyAccessor(WeakReference<object?> reference, IPropertyInfo property)
        {
            _reference = reference ?? throw new ArgumentNullException(nameof(reference));
            _property = property ?? throw new ArgumentNullException(nameof(property));
        }

        public override Type PropertyType => _property.PropertyType;

        public override object? Value
        {
            get
            {
                return _reference.TryGetTarget(out var o) ? _property.Get(o) : null;
            }
        }

        public override bool SetValue(object? value, BindingPriority priority)
        {
            if (_property.CanSet && _reference.TryGetTarget(out var o))
            {
                _property.Set(o, value);

                SendCurrentValue();

                return true;
            }

            return false;
        }

        public void OnEvent(object? sender, WeakEvent ev, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == _property.Name || string.IsNullOrEmpty(e.PropertyName))
            {
                SendCurrentValue();
            }
        }

        protected override void SubscribeCore()
        {
            SendCurrentValue();
            SubscribeToChanges();
        }

        protected override void UnsubscribeCore()
        {
            if (_reference.TryGetTarget(out var o) && o is INotifyPropertyChanged inpc)
            {
                WeakEvents.ThreadSafePropertyChanged.Unsubscribe(inpc, this);
            }
        }

        protected void SendCurrentValue()
        {
            try
            {
                var value = Value;
                PublishValue(value);
            }
            catch (Exception e)
            {
                PublishValue(new BindingNotification(e, BindingErrorType.Error));
            }
        }

        private void SubscribeToChanges()
        {
            if (_reference.TryGetTarget(out var o) && o is INotifyPropertyChanged inpc)
                WeakEvents.ThreadSafePropertyChanged.Subscribe(inpc, this);
        }
    }

    internal class IndexerAccessor : InpcPropertyAccessor, IWeakEventSubscriber<NotifyCollectionChangedEventArgs>
    {
        private readonly int _index;

        public IndexerAccessor(WeakReference<object?> target, IPropertyInfo basePropertyInfo, int argument)
            :base(target, basePropertyInfo)
        {
            _index = argument;
        }


        protected override void SubscribeCore()
        {
            base.SubscribeCore();
            if (_reference.TryGetTarget(out var o) && o is INotifyCollectionChanged incc)
                WeakEvents.CollectionChanged.Subscribe(incc, this);
        }

        protected override void UnsubscribeCore()
        {
            base.UnsubscribeCore();
            if (_reference.TryGetTarget(out var o) && o is INotifyCollectionChanged incc)
                WeakEvents.CollectionChanged.Unsubscribe(incc, this);
        }
        
        public void OnEvent(object? sender, WeakEvent ev, NotifyCollectionChangedEventArgs args)
        {
            if (ShouldNotifyListeners(args))
            {
                SendCurrentValue();
            }
        }

        private bool ShouldNotifyListeners(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    return _index >= e.NewStartingIndex;
                case NotifyCollectionChangedAction.Remove:
                    return _index >= e.OldStartingIndex;
                case NotifyCollectionChangedAction.Replace:
                    return _index >= e.NewStartingIndex &&
                           _index < e.NewStartingIndex + e.NewItems!.Count;
                case NotifyCollectionChangedAction.Move:
                    return (_index >= e.NewStartingIndex &&
                            _index < e.NewStartingIndex + e.NewItems!.Count) ||
                           (_index >= e.OldStartingIndex &&
                            _index < e.OldStartingIndex + e.OldItems!.Count);
                case NotifyCollectionChangedAction.Reset:
                    return true;
            }
            return false;
        }
    }
}
