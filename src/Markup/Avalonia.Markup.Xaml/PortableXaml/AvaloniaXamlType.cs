using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Portable.Xaml;
using Portable.Xaml.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;

namespace Avalonia.Markup.Xaml.PortableXaml
{
    using Metadata;
    using PropertyKey = Tuple<Type, string>;

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

        private static readonly List<PropertyKey> _readonlyProps =
            new List<PropertyKey>()
            {
                new PropertyKey(typeof(MultiBinding),nameof(MultiBinding.Bindings)),
                new PropertyKey(typeof(Panel),nameof(Panel.Children)),
            };

        protected override MethodInfo LookupUnderlyingSetter()
        {
            var key = new PropertyKey(DeclaringType.UnderlyingType, Name);

            //if we have content property a list
            //we have some issues in portable.xaml
            //but if the list is read only, this is solving the problem
            //TODO: investigate is this good enough as solution ???
            //We can add ReadOnyAttribute to cover this
            if (_readonlyProps.Contains(key))
            {
                return null;
            }

            return base.LookupUnderlyingSetter();
        }

        protected override XamlMemberInvoker LookupInvoker()
        {
            return new PropertyInvoker(this);
        }

        protected override XamlType LookupType()
        {
            var pi = UnderlyingMember as PropertyInfo;
            if (pi != null)
            {
                if (pi.PropertyType == typeof(IEnumerable))
                {
                    //let's threat IEnumerable property as list
                    return DeclaringType.SchemaContext.GetXamlType(typeof(IList));
                }
            }

            return base.LookupType();
        }

        private IList<XamlMember> _dependsOn;

        protected override IList<XamlMember> LookupDependsOn()
        {
            if (_dependsOn == null)
            {
                var attrib = UnderlyingMember.GetCustomAttribute<DependsOnAttribute>(true);

                if (attrib != null)
                {
                    var member = DeclaringType.GetMember(attrib.Name);

                    _dependsOn = new XamlMember[] { member };
                }
                else
                {
                    _dependsOn = base.LookupDependsOn();
                }
            }

            return _dependsOn;
        }

        private class PropertyInvoker : XamlMemberInvoker
        {
            public PropertyInvoker(XamlMember member) : base(member)
            {
            }

            public override void SetValue(object instance, object value)
            {
                if (Member.DependsOn.Count == 1 &&
                    value is string)
                {
                    value = TransformDependsOnValue(instance, value);
                }

                if (value is XamlBinding)
                {
                    value = (value as XamlBinding).Value;
                }

                base.SetValue(instance, value);
            }

            private object TransformDependsOnValue(object instance, object value)
            {
                if (value is string &&
                        (Member.UnderlyingMember as PropertyInfo)
                                        .PropertyType != typeof(string))
                {
                    var dpm = Member.DependsOn[0];

                    object depPropValue = dpm.Invoker.GetValue(instance);

                    Type targetType = (depPropValue as AvaloniaProperty)?.PropertyType ??
                                                    (depPropValue as Type);

                    if (targetType == null)
                    {
                        return value;
                    }

                    var xamTargetType = Member.DeclaringType.SchemaContext.GetXamlType(targetType);
                    var ttConv = xamTargetType?.TypeConverter?.ConverterInstance;
                    if (ttConv != null)
                    {
                        value = ttConv.ConvertFromString(value as string);
                    }
                }

                return value;
            }
        }
    }

    public class AvaloniaPropertyXamlMember : PropertyXamlMember
    {
        private bool? _assignBinding;

        public bool AssignBinding => (bool)(_assignBinding ?? (_assignBinding = UnderlyingMember.GetCustomAttribute<AssignBindingAttribute>() != null));

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
            return base.LookupUnderlyingSetter();
            //TODO: investigate don't call base stack overflow
            return _setter;
        }
    }
}