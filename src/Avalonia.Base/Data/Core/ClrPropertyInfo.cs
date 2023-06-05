using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Avalonia.Data.Core
{
    public class ClrPropertyInfo : IPropertyInfo
    {
        private readonly Func<object, object?>? _getter;
        private readonly Action<object, object?>? _setter;

        public ClrPropertyInfo(string name, Func<object, object?>? getter, Action<object, object?>? setter, Type propertyType)
        {
            _getter = getter;
            _setter = setter;
            PropertyType = propertyType;
            Name = name;
        }

        public string Name { get; }
        public Type PropertyType { get; }

        public object? Get(object target)
        {
            if (_getter == null)
                throw new NotSupportedException("Property " + Name + " doesn't have a getter");
            return _getter(target);
        }

        public void Set(object target, object? value)
        {
            if (_setter == null)
                throw new NotSupportedException("Property " + Name + " doesn't have a setter");
            _setter(target, value);
        }

        public bool CanSet => _setter != null;
        public bool CanGet => _getter != null;
    }

    public class ReflectionClrPropertyInfo : ClrPropertyInfo
    {
        static Action<object, object?>? CreateSetter(PropertyInfo info)
        {
            if (info.SetMethod == null)
                return null;
            var target = Expression.Parameter(typeof(object), "target");
            var value = Expression.Parameter(typeof(object), "value");
            return Expression.Lambda<Action<object, object?>>(
                    Expression.Call(Expression.Convert(target, info.DeclaringType!), info.SetMethod,
                        Expression.Convert(value, info.SetMethod.GetParameters()[0].ParameterType)),
                    target, value)
                .Compile();
        }
        
        static Func<object, object>? CreateGetter(PropertyInfo info)
        {
            if (info.GetMethod == null)
                return null;
            var target = Expression.Parameter(typeof(object), "target");
            return Expression.Lambda<Func<object, object>>(
                    Expression.Convert(Expression.Call(Expression.Convert(target, info.DeclaringType!), info.GetMethod),
                        typeof(object)),
                    target)
                .Compile();
        }

        public ReflectionClrPropertyInfo(PropertyInfo info) : base(info.Name,
            CreateGetter(info), CreateSetter(info), info.PropertyType)
        {
            
        }
    }
}
