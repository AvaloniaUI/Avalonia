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

        protected override XamlMember LookupMember(string name, bool skipReadOnlyCheck)
        {
            var m = base.LookupMember(name, skipReadOnlyCheck);

            if (m == null)
            {
                //so far Portable.xaml haven't found the member/property
                //but what if we have AvaloniaProperty
                //without setter and/or without getter
                //let's try to find the AvaloniaProperty as a fallback
                var avProp = AvaloniaPropertyRegistry.Instance.FindRegistered(UnderlyingType, name);

                if (avProp != null && !(skipReadOnlyCheck && avProp.IsReadOnly))
                {
                    m = new AvaloniaPropertyXamlMember(avProp, this);
                }
            }

            return m;
        }
    }

    public class BindingXamlType : XamlType
    {
        public static BindingXamlType Create(Type type, XamlSchemaContext schemaContext)
        {
            if (type == typeof(Binding))
            {
                //in xaml we need to use the extension
                type = typeof(BindingExtension);
            }

            return new BindingXamlType(type, schemaContext);
        }

        private static List<Type> _notAssignable =
                                new List<Type>
                                {
                                    typeof (IXmlSerializable)
                                };

        private BindingXamlType(Type underlyingType, XamlSchemaContext schemaContext) :
            base(underlyingType, schemaContext)
        {
        }

        public override bool CanAssignTo(XamlType xamlType)
        {
            return !_notAssignable.Contains(xamlType.UnderlyingType);
        }
    }

    public class PropertyXamlMember : XamlMember
    {
        public PropertyXamlMember(PropertyInfo propertyInfo, XamlSchemaContext schemaContext)
            : base(propertyInfo, schemaContext)
        {
        }

        protected PropertyXamlMember(string attachablePropertyName,
            MethodInfo getter, MethodInfo setter, XamlSchemaContext schemaContext)
            : base(attachablePropertyName, getter, setter, schemaContext)
        {
        }

        protected PropertyXamlMember(string name, XamlType declaringType, bool isAttachable)
            : base(name, declaringType, isAttachable)
        {
        }

        private static readonly List<PropertyKey> _readonlyProps =
            new List<PropertyKey>()
            {
                new PropertyKey(typeof(MultiBinding),nameof(MultiBinding.Bindings)),
                new PropertyKey(typeof(Panel),nameof(Panel.Children)),
            };

        private static readonly List<PropertyKey> _updateListInsteadSet =
            new List<PropertyKey>()
            {
                new PropertyKey(typeof(Grid),nameof(Grid.RowDefinitions)),
                new PropertyKey(typeof(Grid),nameof(Grid.ColumnDefinitions)),
            };

        protected override MethodInfo LookupUnderlyingSetter()
        {
            //if we have content property a list
            //we have some issues in portable.xaml
            //but if the list is read only, this is solving the problem
            //TODO: investigate is this good enough as solution ???
            //We can add ReadOnyAttribute to cover this
            if (_readonlyProps.Contains(PropertyKey()))
            {
                return null;
            }

            return base.LookupUnderlyingSetter();
        }

        protected override XamlMemberInvoker LookupInvoker()
        {
            return new PropertyInvoker(this)
            {
                UpdateListInsteadSet = _updateListInsteadSet.Contains(PropertyKey())
            };
        }

        protected override XamlType LookupType()
        {
            var propType = GetPropertyType();

            if (propType != null)
            {
                if (propType == typeof(IEnumerable))
                {
                    //TODO: Portable.xaml is not handling well IEnumerable
                    //let's threat IEnumerable property as list
                    //revisit this when smarter solution is found
                    propType = typeof(IList);
                }

                return DeclaringType.SchemaContext.GetXamlType(propType);
            }

            return base.LookupType();
        }

        protected virtual Type GetPropertyType()
        {
            return (UnderlyingMember as PropertyInfo)?.PropertyType;
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

        private PropertyKey PropertyKey()
            => new PropertyKey(DeclaringType.UnderlyingType, Name);

        private class PropertyInvoker : XamlMemberInvoker
        {
            public bool UpdateListInsteadSet { get; set; } = false;

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

                if (UpdateListInsteadSet && value != null)
                {
                    object old = GetValue(instance);

                    if (Equals(old, value))
                    {
                        //don't set the same value
                        //usually a collections
                        return;
                    }
                    else if (old is IList && value is IList)
                    {
                        var oldList = (IList)old;
                        var curList = (IList)value;

                        oldList.Clear();

                        foreach (object item in curList)
                        {
                            oldList.Add(item);
                        }

                        return;
                    }
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

        public bool AssignBinding => (bool)(_assignBinding ?? (_assignBinding = UnderlyingMember?.GetCustomAttribute<AssignBindingAttribute>() != null));

        public AvaloniaProperty Property { get; }

        public AvaloniaPropertyXamlMember(AvaloniaProperty property,
                        PropertyInfo propertyInfo,
                        XamlSchemaContext schemaContext) :
            base(propertyInfo, schemaContext)
        {
            Property = property;
        }

        public AvaloniaPropertyXamlMember(AvaloniaProperty property, XamlType type) :
                    base(property.Name, type, false)
        {
            Property = property;
        }

        protected AvaloniaPropertyXamlMember(AvaloniaProperty property,
                string attachablePropertyName,
                MethodInfo getter, MethodInfo setter, XamlSchemaContext schemaContext)
                : base(attachablePropertyName, getter, setter, schemaContext)
        {
            Property = property;
        }

        protected override XamlMemberInvoker LookupInvoker()
        {
            return new AvaloniaPropertyInvoker(this);
        }

        protected override bool LookupIsReadOnly()
        {
            return Property.IsReadOnly;
        }

        protected override Type GetPropertyType()
        {
            return Property.PropertyType;
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

            private void ApplyBinding(IAvaloniaObject obj, IBinding binding)
            {
                var control = obj as IControl;
                var property = Property;
                var xamlBinding = binding as XamlBinding;

                if (control != null && property != Control.DataContextProperty)
                    DelayedBinding.Add(control, property, binding);
                else if (xamlBinding != null)
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
        public AvaloniaAttachedPropertyXamlMember(AvaloniaProperty property,
                                                    string attachablePropertyName,
                                                    MethodInfo getter, MethodInfo setter,
                                                    XamlSchemaContext schemaContext)
            : base(property, attachablePropertyName, getter, setter, schemaContext)
        {
        }
    }
}