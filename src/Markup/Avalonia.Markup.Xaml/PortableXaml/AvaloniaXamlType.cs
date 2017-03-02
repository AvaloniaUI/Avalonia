using System;
using System.Collections.Generic;
using System.Reflection;
using Portable.Xaml;
using Portable.Xaml.Schema;
using am = Avalonia.Metadata;

namespace Avalonia.Markup.Xaml.PortableXaml
{
    public class AvaloniaXamlType : XamlType
    {
        public AvaloniaXamlType(Type underlyingType, XamlSchemaContext schemaContext) :
            base(underlyingType, schemaContext)
        {
        }

        protected override XamlMember LookupMember(string name, bool skipReadOnlyCheck)
        {
            var pi = UnderlyingType.GetRuntimeProperty(name);

            var dependAttr = pi.GetCustomAttribute<am.DependsOnAttribute>();

            if (dependAttr != null)
            {
                return new DependOnXamlMember(dependAttr.Name, pi, SchemaContext);
            }

            return base.LookupMember(name, skipReadOnlyCheck);
        }
    }

    public class DependOnXamlMember : XamlMember
    {
        private string _dependOn;

        public DependOnXamlMember(string dependOn,
            PropertyInfo propertyInfo,
            XamlSchemaContext schemaContext) :
            base(propertyInfo, schemaContext)
        {
            _dependOn = dependOn;
        }

        private XamlMember _dependOnMember;

        public XamlMember DependOnMember
        {
            get
            {
                return _dependOnMember ??
                        (_dependOnMember = DeclaringType.GetMember(_dependOn));
            }
        }

        protected override IList<XamlMember> LookupDependsOn()
        {
            return new List<XamlMember>() { DeclaringType.GetMember(_dependOn) };
        }

        protected override XamlMemberInvoker LookupInvoker()
        {
            return new DependOnInvoker(this);
        }

        private class DependOnInvoker : XamlMemberInvoker
        {
            public DependOnInvoker(XamlMember member) : base(member)
            {
            }

            public override void SetValue(object instance, object value)
            {
                if (value is string &&
                    (Member.UnderlyingMember as PropertyInfo).PropertyType != typeof(string))
                {
                    var dpm = (Member as DependOnXamlMember).DependOnMember.UnderlyingMember;
                    var pi = (dpm as PropertyInfo);
                    var avp = pi.GetValue(instance) as AvaloniaProperty;

                    Type targetType = avp != null ? avp.PropertyType : pi.PropertyType;

                    var xamTargetType = Member.DeclaringType.SchemaContext.GetXamlType(targetType);
                    var ttConv = xamTargetType?.TypeConverter?.ConverterInstance;
                    if (ttConv != null)
                    {
                        value = ttConv.ConvertFromString(value as string);
                    }
                }

                base.SetValue(instance, value);
            }
        }
    }
}