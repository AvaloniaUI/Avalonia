using System;
using Avalonia.Controls;

#if SYSTEM_XAML
using System.Xaml;
#else
using Portable.Xaml;
#endif

namespace Avalonia.Markup.Xaml.Context
{
    internal class AvaloniaObjectXamlType : AvaloniaXamlType
    {
        public AvaloniaObjectXamlType(Type underlyingType, XamlSchemaContext schemaContext)
            : base(underlyingType, schemaContext)
        {
        }

        protected override XamlMember LookupAliasedProperty(XamlDirective directive)
        {
            if (directive == XamlLanguage.Name)
            {
                if (typeof(INamed).IsAssignableFrom(UnderlyingType))
                {
                    return GetMember(nameof(INamed.Name));
                }
            }

            return base.LookupAliasedProperty(directive);
        }

        protected override bool LookupUsableDuringInitialization()
        {
            return typeof(IControl).IsAssignableFrom(UnderlyingType) ||
                base.LookupUsableDuringInitialization();
        }

        protected override XamlMember LookupMember(string name, bool skipReadOnlyCheck)
        {
            var registered = AvaloniaPropertyRegistry.Instance.FindRegistered(UnderlyingType, name);

            if (registered != null && (skipReadOnlyCheck || !registered.IsReadOnly))
            {
                var propertyInfo = UnderlyingType.GetProperty(registered.Name);
                return propertyInfo != null ?
                    new AvaloniaPropertyXamlMember(propertyInfo, registered, this) :
                    new AvaloniaPropertyXamlMember(registered, this);
            }

            return base.LookupMember(name, skipReadOnlyCheck);
        }
    }
}
