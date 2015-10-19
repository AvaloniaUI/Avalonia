// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Glass;
using OmniXaml.ObjectAssembler;
using OmniXaml.Typing;
using Perspex.Controls;
using Perspex.Markup.Xaml.Binding;
using Perspex.Styling;

namespace Perspex.Markup.Xaml.Context
{
    public class PerspexXamlMemberValuePlugin : MemberValuePlugin
    {
        private readonly MutableXamlMember _xamlMember;

        public PerspexXamlMemberValuePlugin(MutableXamlMember xamlMember) 
            : base(xamlMember)
        {
            _xamlMember = xamlMember;
        }

        public override void SetValue(object instance, object value)
        {
            if (value is XamlBindingDefinition)
            {
                HandleXamlBindingDefinition(instance, (XamlBindingDefinition)value);
            }
            else if (IsPerspexProperty)
            {
                HandlePerspexProperty(instance, value);
            }
            else if (instance is Setter && _xamlMember.Name == "Value")
            {
                var setter = (Setter)instance;
                var targetType = setter.Property.PropertyType;
                var valuePipeline = new ValuePipeline(_xamlMember.TypeRepository, null);
                var xamlType = _xamlMember.TypeRepository.GetXamlType(targetType);
                base.SetValue(instance, valuePipeline.ConvertValueIfNecessary(value, xamlType));
            }
            else
            {
                base.SetValue(instance, value);
            }
        }

        private void HandlePerspexProperty(object instance, object value)
        {
            var pp = PerspexProperty;
            var po = (PerspexObject)instance;
            po.SetValue(pp, value);
        }

        private void HandleXamlBindingDefinition(object instance, XamlBindingDefinition def)
        {
            if (_xamlMember.XamlType.UnderlyingType == typeof(XamlBindingDefinition))
            {
                // TODO: This should search base classes.
                var property = instance.GetType().GetTypeInfo().GetDeclaredProperty(_xamlMember.Name);

                if (property == null || !property.CanWrite)
                {
                    throw new InvalidOperationException(
                        $"Cannot assign to '{_xamlMember.Name}' on '{instance.GetType()}");
                }

                property.SetValue(instance, def);
            }
            else
            {
                var perspexObject = instance as PerspexObject;
                var attached = _xamlMember as PerspexAttachableXamlMember;

                if (perspexObject == null)
                {
                    throw new InvalidOperationException(
                        $"Cannot bind to an object of type '{instance.GetType()}");
                }

                PerspexProperty property;
                string propertyName;

                if (attached == null)
                {
                    propertyName = _xamlMember.Name;
                    property = PerspexPropertyRegistry.Instance.GetRegistered(perspexObject)
                        .FirstOrDefault(x => x.Name == propertyName);
                }
                else
                {
                    // Ensure the OwnerType's static ctor has been run.
                    RuntimeHelpers.RunClassConstructor(attached.DeclaringType.UnderlyingType.TypeHandle);

                    propertyName = attached.DeclaringType.UnderlyingType.Name + '.' + _xamlMember.Name;

                    property = PerspexPropertyRegistry.Instance.GetRegistered(perspexObject)
                        .Where(x => x.IsAttached && x.OwnerType == attached.DeclaringType.UnderlyingType)
                        .FirstOrDefault(x => x.Name == _xamlMember.Name);
                }

                if (property == null)
                {
                    throw new InvalidOperationException(
                        $"Cannot find '{propertyName}' on '{instance.GetType()}");
                }

                var binding = new XamlBinding
                {
                    BindingMode = def.BindingMode,
                    SourcePropertyPath = def.SourcePropertyPath,
                };

                binding.Bind(perspexObject, property);
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public PerspexProperty PerspexProperty
        {
            get
            {
                var underlyingType = _xamlMember.DeclaringType.UnderlyingType;
                var name = _xamlMember.Name + "Property";

                var value = ReflectionExtensions.GetValueOfStaticField(underlyingType, name);
                return value as PerspexProperty;
            }
        }

        private bool ValueRequiresSpecialHandling(object value)
        {
            return value is XamlBindingDefinition || IsPerspexProperty;
        }

        private bool IsPerspexProperty => PerspexProperty != null;

        public override string ToString()
        {
            return $"{{Perspex Value Connector for member {_xamlMember}}}";
        }
    }
}