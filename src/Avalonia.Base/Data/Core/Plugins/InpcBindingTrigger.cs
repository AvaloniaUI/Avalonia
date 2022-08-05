using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using Avalonia.Utilities;

namespace Avalonia.Data.Core.Plugins
{
    internal class InpcBindingTrigger<TIn> : TypedBindingTrigger<TIn>,
        IWeakEventSubscriber<PropertyChangedEventArgs>
    {
        private readonly Func<TIn, object?> _read;
        private readonly string _propertyName;
        private WeakReference<INotifyPropertyChanged?>? _source;

        public InpcBindingTrigger(
            int index,
            Func<TIn, object?> read,
            string propertyName)
            : base(index)
        {
            _read = read;
            _propertyName = propertyName;
        }

        internal static TypedBindingTrigger<TIn>? TryCreate(
            int index,
            MemberExpression node,
            LambdaExpression rootExpression)
        {
            var type = node.Expression?.Type;
            var member = node.Member;

            if (member.MemberType != MemberTypes.Property ||
                !typeof(INotifyPropertyChanged).IsAssignableFrom(type))
                return null;

            var lambda = Expression.Lambda<Func<TIn, object>>(node.Expression!, rootExpression.Parameters);
            var read = lambda.Compile();

            return new InpcBindingTrigger<TIn>(index, read, member.Name);
        }

        internal static TypedBindingTrigger<TIn>? TryCreate(
            int index,
            MethodCallExpression node,
            LambdaExpression rootExpression)
        {
            var type = node.Object?.Type;

            if (node.Method.Name != "get_Item" ||
                !typeof(INotifyPropertyChanged).IsAssignableFrom(type) ||
                node.Arguments.Count != 1)
                return null;

            var lambda = Expression.Lambda<Func<TIn, object>>(node.Object!, rootExpression.Parameters);
            var read = lambda.Compile();

            return new InpcBindingTrigger<TIn>(index, read, CommonPropertyNames.IndexerName);
        }

        void IWeakEventSubscriber<PropertyChangedEventArgs>.OnEvent(
            object? sender, 
            WeakEvent ev, 
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName == _propertyName || string.IsNullOrEmpty(e.PropertyName))
                OnChanged();
        }

        protected override bool SubscribeCore(TIn root)
        {
            var o = _read(root) as INotifyPropertyChanged;
            _source = new(o);

            if (o is null)
                return false;

            WeakEvents.PropertyChanged.Subscribe(o, this);
            return true;
        }

        protected override void UnsubscribeCore()
        {
            if (_source?.TryGetTarget(out var o) == true)
                WeakEvents.PropertyChanged.Unsubscribe(o, this);
        }
    }
}
