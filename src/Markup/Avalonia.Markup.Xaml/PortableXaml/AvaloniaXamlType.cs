using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Data;
using Avalonia.Metadata;
using Avalonia.Styling;
using Portable.Xaml;
using Portable.Xaml.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Xml.Serialization;

namespace Avalonia.Markup.Xaml.PortableXaml
{
    using Converters;
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
        private static List<Type> _notAssignable =
                                new List<Type>
                                {
                                    typeof (IXmlSerializable)
                                };

        public BindingXamlType(Type underlyingType, XamlSchemaContext schemaContext) :
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

        private bool IsReadOnlyCollectionProperty
        {
            get
            {
                //Collection properties like:
                //MultiBinding.Bindings, Panel.Children, Control.Styles,
                //need to be readonly for Portable.Xaml
                //Collection properties like: 
                //Grid.RowDefinitions, Grid.ColumnDefinitions
                //need to be set only once, and subsequent changes to be
                //added to collection  
                //TODO: investigate is this good enough as solution ???
                //We can add some ReadOnyXamlPropertyCollectionAttribute to cover this            
                return Type.IsCollection;
            }
        }

        private bool HasCollectionTypeConverter
        {
            get
            {
                return Type.IsCollection && Type.TypeConverter != null;
            }
        }

        protected override MethodInfo LookupUnderlyingSetter()
        {
            //if we have content property a list
            //we have some issues in portable.xaml
            //but if the list is read only, this is solving the problem
           
            if (IsReadOnlyCollectionProperty &&
                !HasCollectionTypeConverter)
            {
                return null;
            }

            return base.LookupUnderlyingSetter();
        }

        protected override XamlMemberInvoker LookupInvoker()
        {
            //if we have a IList property and it has TypeConverter
            //Portable.xaml need to be able to set the value
            //but instead directly set new value we'll sync the lists
            bool updateListInsteadSet = HasCollectionTypeConverter;
            return new PropertyInvoker(this)
            {
                UpdateListInsteadSet = updateListInsteadSet
            };
        }

        protected override bool LookupIsUnknown() => false;

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
                //can't make it work to assign TypeConverter to Setter.Value
                //so we need it hard coded
                //TODO: try to assosiate TypeConverter with Setter.Value
                //and remove this lines
                if (instance is Setter &&
                    Member.Name == nameof(Setter.Value) &&
                    value is string)
                {
                    value = SetterValueTypeConverter.ConvertSetterValue(null,
                                            Member.DeclaringType.SchemaContext, CultureInfo.InvariantCulture,
                                            instance as Setter,
                                            value);
                }

                if (UpdateListInsteadSet &&
                    value != null &&
                    UpdateListInsteadSetValue(instance, value))
                {
                    return;
                }

                base.SetValue(instance, value);
            }

            private bool UpdateListInsteadSetValue(object instance, object value)
            {
                object old = GetValue(instance);

                if (Equals(old, value))
                {
                    //don't set the same collection value
                    return true;
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

                    return true;
                }

                return false;
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
                            obj.SetValue(Property, value);
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
                if (Property != null && !Property.IsAttached)
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

                if (control != null && property != Control.DataContextProperty)
                    DelayedBinding.Add(control, property, binding);
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