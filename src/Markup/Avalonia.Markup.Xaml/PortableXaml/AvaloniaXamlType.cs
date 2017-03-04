using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Portable.Xaml;
using Portable.Xaml.ComponentModel;
using Portable.Xaml.Schema;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;

namespace Avalonia.Markup.Xaml.PortableXaml
{
    public class AvaloniaXamlType : XamlType
    {
        public AvaloniaXamlType(Type underlyingType, XamlSchemaContext schemaContext) :
            base(underlyingType, schemaContext)
        {
        }
    }

    public class BindingXamlType : AvaloniaXamlType
    {
        public static BindingXamlType Create(Type type, XamlSchemaContext schemaContext)
        {
            if (type == typeof(Binding))
            {
                //in xmal we need to use the extension
                type = typeof(BindingExtension);
            }

            return new BindingXamlType(type, schemaContext);
        }

        private static HashSet<Type> _notAssignable =
        new HashSet<Type>()
        {
            typeof (IXmlSerializable)
        };

        private BindingXamlType(Type underlyingType, XamlSchemaContext schemaContext) :
            base(underlyingType, schemaContext)
        {
        }

        public override bool CanAssignTo(XamlType xamlType)
        {
            if (_notAssignable.Contains(xamlType.UnderlyingType))
            {
                return false;
            }

            return true;
        }

        protected override XamlMember LookupAliasedProperty(XamlDirective directive)
        {
            return base.LookupAliasedProperty(directive);
        }

        protected override bool LookupIsMarkupExtension()
        {
            return base.LookupIsMarkupExtension();
        }
    }

    public class PropertyXamlMember : XamlMember
    {
        protected PropertyXamlMember(string attachablePropertyName,
            MethodInfo getter, MethodInfo setter, XamlSchemaContext schemaContext)
            : base(attachablePropertyName, getter, setter, schemaContext)
        {
        }

        public PropertyXamlMember(
                PropertyInfo propertyInfo,
                XamlSchemaContext schemaContext) :
            base(propertyInfo, schemaContext)
        {
        }

        protected override MethodInfo LookupUnderlyingSetter()
        {
            //if we have content property a list
            //we have some issues in portable.xaml
            //but if the list is read only, this is solving the problem
            //TODO: investigate is this good enough as solution ???
            //We can add ReadOnyAttribute to cover this
            if ((Type.IsCollection || Type.IsDictionary) &&
                 Name == DeclaringType.ContentProperty?.Name)
            {
                return null;
            }

            return base.LookupUnderlyingSetter();
        }
    }

    public class AvaloniaPropertyXamlMember : PropertyXamlMember
    {
        public bool AssignBinding { get; set; } = false;

        public AvaloniaProperty Property { get; }

        protected AvaloniaPropertyXamlMember(AvaloniaProperty property,
            string attachablePropertyName,
            MethodInfo getter, MethodInfo setter, XamlSchemaContext schemaContext)
            : base(attachablePropertyName, getter, setter, schemaContext)
        {
            Property = property;
        }

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

        protected override bool LookupIsReadOnly()
        {
            if (Property.IsReadOnly)
            {
                return true;
            }

            return base.LookupIsReadOnly();
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
                    if (value is IBinding)
                    {
                        if (!Member.AssignBinding)
                            ApplyBinding(obj, (IBinding)value);
                        else
                            obj.SetValue(Property, value is XamlBinding ?
                                                        (value as XamlBinding).Value :
                                                        value);
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

            public void ApplyBinding(IAvaloniaObject obj, IBinding binding)
            {
                var control = obj as IControl;
                var property = Property;
                var xamlBinding = binding as XamlBinding;
                //if (control != null && property != Control.DataContextProperty)
                //    DelayedBinding.Add(control, property, binding);
                //else
                if (xamlBinding != null)
                    obj.Bind(property, xamlBinding.Value, xamlBinding.Anchor?.Target);
                else
                    obj.Bind(property, binding);
            }

            public void SetValue(ITypeDescriptorContext context, object instance, object value)
            {
                throw new NotImplementedException();
            }

            private AvaloniaProperty Property => Member.Property;

            private new AvaloniaPropertyXamlMember Member =>
                            (AvaloniaPropertyXamlMember)base.Member;
        }
    }

    public class AvaloniaAttachedPropertyXamlMember : AvaloniaPropertyXamlMember
    {
        private MethodInfo _setter;

        public AvaloniaAttachedPropertyXamlMember(AvaloniaProperty property,
                                                    string attachablePropertyName,
                                                    MethodInfo getter, MethodInfo setter, 
                                                    XamlSchemaContext schemaContext)
            : base(property, attachablePropertyName, getter, setter, schemaContext)
        {
            _setter = setter;
        }

        protected override MethodInfo LookupUnderlyingSetter()
        {
            //TODO: investigate don't call base stack overflow
            return _setter;
        }
    }

    public class DependOnXamlMember : PropertyXamlMember
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
                if (value is XamlBinding)
                {
                    value = (value as XamlBinding).Value;
                }

                base.SetValue(instance, value);
            }
        }
    }
}