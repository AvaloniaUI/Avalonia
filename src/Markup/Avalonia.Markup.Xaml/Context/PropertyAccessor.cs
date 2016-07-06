// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using OmniXaml.ObjectAssembler;
using OmniXaml.TypeConversion;
using OmniXaml.Typing;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Data;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.Context
{
    internal static class PropertyAccessor
    {
        public static void SetValue(
            object instance, 
            MutableMember member, 
            object value,
            IValueContext context)
        {
            var avaloniaProperty = FindAvaloniaProperty(instance, member);

            if (value is IBinding)
            {
                SetBinding(instance, member, avaloniaProperty, context, (IBinding)value);
            }
            else if (avaloniaProperty != null)
            {
                ((AvaloniaObject)instance).SetValue(avaloniaProperty, value);
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

        private static AvaloniaProperty FindAvaloniaProperty(object instance, MutableMember member)
        {
            var registry = AvaloniaPropertyRegistry.Instance;
            var attached = member as AvaloniaAttachableXamlMember;
            var target = instance as AvaloniaObject;

            if (target == null)
            {
                return null;
            }

            if (attached == null)
            {
                return registry.FindRegistered(target, member.Name);
            }
            else
            {
                var ownerType = attached.DeclaringType.UnderlyingType;

                RuntimeHelpers.RunClassConstructor(ownerType.TypeHandle);

                return registry.GetRegistered(target)
                    .Where(x => x.OwnerType == ownerType && x.Name == attached.Name)
                    .FirstOrDefault();
            }
        }

        private static void SetBinding(
            object instance,
            MutableMember member, 
            AvaloniaProperty property, 
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
            AvaloniaProperty property,
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
                if (property != Control.DataContextProperty)
                {
                    DelayedBinding.Add(control, property, binding);
                }
                else
                {
                    control.Bind(property, binding);
                }
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

                ((IAvaloniaObject)instance).Bind(property, binding, anchor);
            }

            return true;
        }
    }
}
