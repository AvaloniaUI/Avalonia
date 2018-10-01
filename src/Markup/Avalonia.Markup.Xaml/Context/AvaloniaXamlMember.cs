using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Markup;
using System.Xaml;
using System.Xaml.Schema;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.Context
{
    internal class AvaloniaXamlMember : XamlMember
    {
        public AvaloniaXamlMember(PropertyInfo propertyInfo, XamlSchemaContext schemaContext)
            : base(propertyInfo, schemaContext)
        {
        }

        public AvaloniaXamlMember(EventInfo eventInfo, XamlSchemaContext schemaContext)
            : base(eventInfo, schemaContext)
        {
        }

        public AvaloniaXamlMember(string name, XamlType declaringType, bool isAttachable)
            : base(name, declaringType, isAttachable)
        {
        }

        protected override IList<XamlMember> LookupDependsOn()
        {
            var dependsOn = UnderlyingMember?.GetCustomAttributes<DependsOnAttribute>();

            if (dependsOn != null)
            {
                return dependsOn.Select(x => DeclaringType.GetMember(x.Name)).ToList();
            }

            return base.LookupDependsOn();
        }

        protected override XamlValueConverter<TypeConverter> LookupTypeConverter()
        {
            if (UnderlyingMember != null &&
                UnderlyingMember.DeclaringType == typeof(Setter) &&
                UnderlyingMember.Name == nameof(Setter.Value))
            {
                return new XamlValueConverter<TypeConverter>(
                    typeof(SetterValueTypeConverter),
                    Type);
            }

            return base.LookupTypeConverter();
        }
    }
}
