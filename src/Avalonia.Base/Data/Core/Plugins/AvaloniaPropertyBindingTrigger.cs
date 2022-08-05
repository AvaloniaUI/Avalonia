using System;
using System.Linq.Expressions;
using System.Reflection;
using Avalonia.Utilities;

namespace Avalonia.Data.Core.Plugins
{
    internal class AvaloniaPropertyBindingTrigger<TIn> : TypedBindingTrigger<TIn>,
        IWeakEventSubscriber<AvaloniaPropertyChangedEventArgs>
    {
        private readonly Func<TIn, object?> _read;
        private readonly AvaloniaProperty _property;
        private WeakReference<AvaloniaObject?>? _source;

        public AvaloniaPropertyBindingTrigger(
            int index,
            Func<TIn, object?> read,
            AvaloniaProperty property)
            : base(index)
        {
            _read = read;
            _property = property;
        }

        internal static TypedBindingTrigger<TIn>? TryCreate(
            int index, 
            MemberExpression node,
            LambdaExpression rootExpression)
        {
            var type = node.Expression?.Type;
            var member = node.Member;

            if (member.DeclaringType is null ||
                member.MemberType != MemberTypes.Property ||
                !typeof(AvaloniaObject).IsAssignableFrom(type))
                return null;

            var property = GetProperty(member);

            if (property is null)
                return null;

            var lambda = Expression.Lambda<Func<TIn, object>>(node.Expression!, rootExpression.Parameters);
            var read = lambda.Compile();

            return new AvaloniaPropertyBindingTrigger<TIn>(index, read, property);
        }

        internal static TypedBindingTrigger<TIn>? TryCreate(
            int index,
            MethodCallExpression node,
            LambdaExpression rootExpression)
        {
            var type = node.Object?.Type;
            var method = node.Method;

            if (method.Name != "get_Item" ||
                method.DeclaringType is null ||
                node.Arguments.Count != 1 ||
                GetProperty(node.Arguments[0]) is not AvaloniaProperty property ||
                !typeof(AvaloniaObject).IsAssignableFrom(type))
                return null;

            var lambda = Expression.Lambda<Func<TIn, object>>(node.Object!, rootExpression.Parameters);
            var read = lambda.Compile();

            return new AvaloniaPropertyBindingTrigger<TIn>(index, read, property);
        }

        void IWeakEventSubscriber<AvaloniaPropertyChangedEventArgs>.OnEvent(
            object? sender, 
            WeakEvent ev, 
            AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == _property)
                OnChanged();
        }

        protected override bool SubscribeCore(TIn root)
        {
            var o = _read(root) as AvaloniaObject;
            _source = new(o);

            if (o is null)
                return false;

            WeakEvents.AvaloniaPropertyChanged.Subscribe(o, this);
            return true;
        }

        protected override void UnsubscribeCore()
        {
            if (_source?.TryGetTarget(out var o) == true)
                WeakEvents.AvaloniaPropertyChanged.Unsubscribe(o, this);
        }

        private static AvaloniaProperty? GetProperty(Expression expression)
        {
            if (expression is not MemberExpression member ||
                member.Member is not FieldInfo field ||
                !field.IsStatic)
                return null;

            return field.GetValue(null) as AvaloniaProperty;
        }

        private static AvaloniaProperty? GetProperty(MemberInfo member)
        {
            var propertyName = member.Name;
            var propertyField = member.DeclaringType?.GetField(
                propertyName + "Property",
                BindingFlags.Static);
            return propertyField?.GetValue(null) as AvaloniaProperty;
        }
    }
}
