using System;
using System.Collections.Specialized;
using System.Linq.Expressions;
using Avalonia.Utilities;

namespace Avalonia.Data.Core.Plugins
{
    internal class InccBindingTrigger<TIn> : TypedBindingTrigger<TIn>,
        IWeakEventSubscriber<NotifyCollectionChangedEventArgs>
    {
        private readonly Func<TIn, object?> _read;
        private WeakReference<INotifyCollectionChanged?>? _source;

        public InccBindingTrigger(
            int index,
            Func<TIn, object?> read)
            : base(index)
        {
            _read = read;
        }

        internal static TypedBindingTrigger<TIn>? TryCreate(
            int index,
            MethodCallExpression node,
            LambdaExpression rootExpression)
        {
            var type = node.Object?.Type;
            var method = node.Method;

            if (method.Name != "get_Item" ||
                !typeof(INotifyCollectionChanged).IsAssignableFrom(type))
                return null;

            var lambda = Expression.Lambda<Func<TIn, object>>(node.Object!, rootExpression.Parameters);
            var read = lambda.Compile();

            return new InccBindingTrigger<TIn>(index, read);
        }

        void IWeakEventSubscriber<NotifyCollectionChangedEventArgs>.OnEvent(
            object? sender, 
            WeakEvent ev,
            NotifyCollectionChangedEventArgs e)
        {
            OnChanged();
        }

        protected override bool SubscribeCore(TIn root)
        {
            var o = _read(root) as INotifyCollectionChanged;
            _source = new(o);

            if (o is null)
                return false;

            WeakEvents.CollectionChanged.Subscribe(o, this);
            return true;
        }

        protected override void UnsubscribeCore()
        {
            if (_source?.TryGetTarget(out var o) == true)
                WeakEvents.CollectionChanged.Unsubscribe(o, this);
        }
    }
}
