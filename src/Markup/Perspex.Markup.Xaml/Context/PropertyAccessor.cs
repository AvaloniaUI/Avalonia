﻿// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using OmniXaml.ObjectAssembler;
using OmniXaml.TypeConversion;
using OmniXaml.Typing;
using Perspex.Controls;
using Perspex.Data;
using Perspex.Markup.Xaml.Data;
using Perspex.Styling;

namespace Perspex.Markup.Xaml.Context
{
    internal static class PropertyAccessor
    {
        public static void SetValue(
            object instance, 
            MutableMember member, 
            object value,
            IValueContext context)
        {
            var perspexProperty = FindPerspexProperty(instance, member);

            if (value is IBinding)
            {
                SetBinding(instance, member, perspexProperty, context, (IBinding)value);
            }
            else if (perspexProperty != null)
            {
                ((PerspexObject)instance).SetValue(perspexProperty, value);
            }
            else if (instance is Setter && member.Name == "Value")
            {
                // TODO: Make this more generic somehow.
                var setter = (Setter)instance;
                var targetType = setter.Property.PropertyType;
                var xamlType = member.TypeRepository.GetByType(targetType);
                var convertedValue = default(object);

                if (CommonValueConversion.TryConvert(value, xamlType, context, out convertedValue))
                {
                    SetClrProperty(instance, member, convertedValue);
                }
            }
            else
            {
                SetClrProperty(instance, member, value);
            }
        }

        private static PerspexProperty FindPerspexProperty(object instance, MutableMember member)
        {
            var target = instance as PerspexObject;

            if (target == null)
            {
                return null;
            }

            var attached = member as PerspexAttachableXamlMember;
            var propertyName = attached == null ? 
                member.Name : 
                member.DeclaringType.Name + "." + member.Name;

            var registered = PerspexPropertyRegistry.Instance.FindRegistered(target, propertyName);
            if (registered == null)
            {
                // Find attached property when using bindings.
                return PerspexPropertyRegistry.Instance.FindRegistered(member.DeclaringType.UnderlyingType, propertyName);
            }

            return registered;
        }

        private static void SetBinding(
            object instance,
            MutableMember member, 
            PerspexProperty property, 
            IValueContext context,
            IBinding binding)
        {
            if (!(AssignBinding(instance, member, binding) || 
                  ApplyBinding(instance, property, context, binding)))
            {
                throw new InvalidOperationException(
                    $"Cannot assign to '{member.Name}' on '{instance.GetType()}");
            }
        }

        private static void SetClrProperty(object instance, MutableMember member, object value)
        {
            if (member.IsAttachable)
            {
                member.Setter.Invoke(null, new[] { instance, value });
            }
            else
            {
                member.Setter.Invoke(instance, new[] { value });
            }
        }

        private static bool AssignBinding(object instance, MutableMember member, IBinding binding)
        {
            var property = instance.GetType()
                .GetRuntimeProperties()
                .FirstOrDefault(x => x.Name == member.Name);

            if (property?.GetCustomAttribute<AssignBindingAttribute>() != null)
            {
                property.SetValue(instance, binding);
                return true;
            }

            return false;
        }

        private static bool ApplyBinding(
            object instance, 
            PerspexProperty property,
            IValueContext context,
            IBinding binding)
        {
            if (property == null)
            {
                return false;
            }

            var control = instance as IControl;

            if (control != null)
            {
                DelayedBinding.Add(control, property, binding);
            }
            else
            {
                // The target is not a control, so we need to find an anchor that will let us look
                // up named controls and style resources. First look for the closest IControl in
                // the TopDownValueContext.
                object anchor = context.TopDownValueContext.StoredInstances
                    .Select(x => x.Instance)
                    .OfType<IControl>()
                    .LastOrDefault();

                // If a control was not found, then try to find the highest-level style as the XAML
                // file could be a XAML file containing only styles.
                if (anchor == null)
                {
                    anchor = context.TopDownValueContext.StoredInstances
                        .Select(x => x.Instance)
                        .OfType<IStyle>()
                        .FirstOrDefault();
                }

                ((IPerspexObject)instance).Bind(property, binding, anchor);
            }

            return true;
        }
    }
}
