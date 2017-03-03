using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Data;
using Portable.Xaml;
using Portable.Xaml.Schema;


namespace Avalonia.Markup.Xaml.PortableXaml
{
    //public class AvaloniaXamlType : XamlType
    //{
    //    public AvaloniaXamlType(Type underlyingType, XamlSchemaContext schemaContext) :
    //        base(underlyingType, schemaContext)
    //    {
    //    }

    //    protected override XamlMember LookupMember(string name, bool skipReadOnlyCheck)
    //    {
    //        return base.LookupMember(name, skipReadOnlyCheck);
    //    }
    //}

    public class AvaloniaPropertyXamlMember : XamlMember
    {
        public bool AssignBinding { get; set; } = false;

        public AvaloniaProperty Property { get; }

        public AvaloniaPropertyXamlMember(AvaloniaProperty property,
                        PropertyInfo propertyInfo,
                        XamlSchemaContext schemaContext) :
            base(propertyInfo, schemaContext)
        {
            Property = property;
        }

        protected override XamlMemberInvoker LookupInvoker()
        {
            return new AvaloniaPropertyInvoker(this);
        }

        private class AvaloniaPropertyInvoker : XamlMemberInvoker
        {
            public AvaloniaPropertyInvoker(XamlMember member) : base(member)
            {
            }

            public override void SetValue(object instance, object value)
            {
                if (Property != null)
                {
                    var obj = ((IAvaloniaObject)instance);
                    if (value is IBinding && !Member.AssignBinding)
                    {
                        ApplyBinding(obj, (IBinding)value);
                    }
                    else
                    {
                        obj.SetValue(Property, value);
                    }
                }
                else
                {
                    base.SetValue(instance, value);
                }
            }

            public override object GetValue(object instance)
            {
                if (Property != null)
                {
                    return ((IAvaloniaObject)instance).GetValue(Property);
                }
                else
                {
                    return base.GetValue(instance);
                }
            }

            private void ApplyBinding(IAvaloniaObject obj, IBinding binding)
            {
                //TODO: in Context.PropertyAccessor there is
                //some quirk stuff check it later
                obj.Bind(Property, binding);
            }

            private AvaloniaProperty Property => Member.Property;

            private new AvaloniaPropertyXamlMember Member =>
                            (AvaloniaPropertyXamlMember)base.Member;
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