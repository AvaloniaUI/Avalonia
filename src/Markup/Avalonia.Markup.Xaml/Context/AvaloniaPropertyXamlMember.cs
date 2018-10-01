using System;
using System.Collections;
using System.Reflection;
using Avalonia.Data;
using Avalonia.Metadata;

#if SYSTEM_XAML
using System.Xaml;
using System.Xaml.Schema;
#else
using Portable.Xaml;
using Portable.Xaml.Schema;
#endif

namespace Avalonia.Markup.Xaml.Context
{
    internal class AvaloniaPropertyXamlMember : AvaloniaXamlMember
    {
        private readonly AvaloniaProperty _property;

        public AvaloniaPropertyXamlMember(
            PropertyInfo propertyInfo,
            AvaloniaProperty property,
            XamlType declaringType)
            : base(propertyInfo, declaringType.SchemaContext)
        {
            Contract.Requires<ArgumentNullException>(property != null);

            _property = property;
        }

        public AvaloniaPropertyXamlMember(AvaloniaProperty property, XamlType declaringType)
            : base(property.Name, declaringType, property.IsAttached)
        {
            Contract.Requires<ArgumentNullException>(property != null);

            _property = property;
        }

        protected override bool LookupIsReadOnly() => _property.IsReadOnly;
        protected override bool LookupIsUnknown() => false;

        protected override XamlType LookupType()
        {
            if (UnderlyingMember?.GetCustomAttribute<RuntimeListAttribute>() != null)
            {
                return DeclaringType.SchemaContext.GetXamlType(typeof(IList));
            }

            return DeclaringType.SchemaContext.GetXamlType(_property.PropertyType);
        }

        protected override XamlMemberInvoker LookupInvoker() => new Invoker(this);

        private new class Invoker : XamlMemberInvoker
        {
            private readonly AvaloniaPropertyXamlMember _member;

            public Invoker(AvaloniaPropertyXamlMember member)
                : base(member)
            {
                _member = member;
            }

            public override object GetValue(object instance)
            {
                return ((IAvaloniaObject)instance).GetValue(_member._property);
            }

            public override void SetValue(object instance, object value)
            {
                var target = (IAvaloniaObject)instance;
                var property = _member._property;

                if (value is IBinding binding)
                {
                    target.Bind(property, binding);
                }
                else
                {
                    target.SetValue(property, value);
                }
            }
        }
    }
}
