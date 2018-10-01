using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Xaml;
using System.Xaml.Schema;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.Context
{
    internal class AvaloniaXamlType : XamlType
    {
        public AvaloniaXamlType(Type underlyingType, XamlSchemaContext schemaContext)
            : base(underlyingType, schemaContext)
        {
        }

        protected override XamlMember LookupAttachableMember(string name)
        {
            var registered = AvaloniaPropertyRegistry.Instance.FindRegistered(UnderlyingType, name);

            return registered?.IsAttached == true ?
                new AvaloniaPropertyXamlMember(registered, this) :
                base.LookupAttachableMember(name);
        }

        protected override XamlMember LookupContentProperty()
        {
            var attrs = UnderlyingType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.GetCustomAttribute<ContentAttribute>() != null)
                .ToList();

            if (attrs.Count == 1)
            {
                var name = attrs[0].Name;
                return GetMember(name) ?? LookupMember(name, true);
            }
            else if (attrs.Count == 0)
            {
                return null;
            }
            else
            {
                throw new XamlObjectReaderException($"Content property defined more than once on {UnderlyingType}.");
            }
        }

        protected override bool LookupIsAmbient()
        {
            return typeof(ControlTemplate).IsAssignableFrom(UnderlyingType) || 
                typeof(IStyledElement).IsAssignableFrom(UnderlyingType) ||
                typeof(IStyle).IsAssignableFrom(UnderlyingType) ||
                base.LookupIsAmbient();
        }

        protected override XamlMember LookupMember(string name, bool skipReadOnlyCheck)
        {
            var propertyInfo = UnderlyingType.GetProperty(name);

            if (propertyInfo != null)
            {
                var propertyType = SchemaContext.GetXamlType(propertyInfo.PropertyType);

                if (skipReadOnlyCheck || propertyInfo.CanWrite || propertyType.IsCollection)
                {
                    return new AvaloniaXamlMember(propertyInfo, SchemaContext);
                }
            }

            var eventInfo = UnderlyingType.GetEvent(name);

            if (eventInfo != null)
            {
                return new AvaloniaXamlMember(eventInfo, SchemaContext);
            }

            return null;
        }

        protected override XamlValueConverter<TypeConverter> LookupTypeConverter()
        {
            var converter = AvaloniaTypeConverters.GetTypeConverter(UnderlyingType);

            if (converter != null)
            {
                return new XamlValueConverter<TypeConverter>(converter, this);
            }

            return base.LookupTypeConverter();
        }
    }
}
