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
using Perspex.Data;
using Perspex.Markup.Xaml.Data;
using Perspex.Styling;

namespace Perspex.Markup.Xaml.Context
{
    public class PerspexXamlMemberValuePlugin : MemberValuePlugin
    {
        private readonly MutableMember _xamlMember;

        public PerspexXamlMemberValuePlugin(MutableMember xamlMember) 
            : base(xamlMember)
        {
            _xamlMember = xamlMember;
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

        public override void SetValue(object instance, object value)
        {
            if (value is IBinding)
            {
                HandleBinding(instance, (IBinding)value);
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
                var xamlType = _xamlMember.TypeRepository.GetByType(targetType);
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

        private void HandleBinding(object instance, IBinding binding)
        {
            if (!(AssignBinding(instance, binding) || ApplyBinding(instance, binding)))
            {
                throw new InvalidOperationException(
                    $"Cannot assign to '{_xamlMember.Name}' on '{instance.GetType()}");
            }
        }

        private bool AssignBinding(object instance, IBinding binding)
        {
            var property = instance.GetType()
                .GetRuntimeProperties()
                .FirstOrDefault(x => x.Name == _xamlMember.Name);

            if (property?.GetCustomAttribute<AssignBindingAttribute>() != null)
            {
                property.SetValue(instance, binding);
                return true;
            }

            return false;
        }

        private bool ApplyBinding(object instance, IBinding binding)
        {
            var targetControl = instance as IControl;
            var attached = _xamlMember as PerspexAttachableXamlMember;

            if (targetControl == null)
            {
                return false;
            }

            PerspexProperty property;
            string propertyName;

            if (attached == null)
            {
                propertyName = _xamlMember.Name;
                property = PerspexPropertyRegistry.Instance.GetRegistered((PerspexObject)targetControl)
                    .FirstOrDefault(x => x.Name == propertyName);
            }
            else
            {
                // Ensure the OwnerType's static ctor has been run.
                RuntimeHelpers.RunClassConstructor(attached.DeclaringType.UnderlyingType.TypeHandle);

                propertyName = attached.DeclaringType.UnderlyingType.Name + '.' + _xamlMember.Name;

                property = PerspexPropertyRegistry.Instance.GetRegistered((PerspexObject)targetControl)
                    .Where(x => x.IsAttached && x.OwnerType == attached.DeclaringType.UnderlyingType)
                    .FirstOrDefault(x => x.Name == _xamlMember.Name);
            }

            if (property == null)
            {
                return false;
            }

            targetControl.Bind(property, binding);
            return true;
        }

        private bool ValueRequiresSpecialHandling(object value)
        {
            return value is Data.Binding || IsPerspexProperty;
        }

        private bool IsPerspexProperty => PerspexProperty != null;

        public override string ToString()
        {
            return $"{{Perspex Value Connector for member {_xamlMember}}}";
        }
    }
}