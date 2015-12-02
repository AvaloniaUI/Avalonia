// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Perspex.Xaml.Interactions.Core
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Interactivity;
    using Markup.Xaml.Data;

    internal static class DataBindingHelper
    {
        private static readonly Dictionary<Type, List<PerspexProperty>> DependenciesPropertyCache = new Dictionary<Type, List<PerspexProperty>>();

        /// <summary>
        /// Ensures that all binding expression on actions are up to date.
        /// </summary>
        /// <remarks>
        /// DataTriggerBehavior fires during data binding phase. Since the ActionCollection is a child of the behavior,
        /// bindings on the action  may not be up-to-date. This routine is called before the action
        /// is executed in order to guarantee that all bindings are refreshed with the most current data.
        /// </remarks>
        public static void RefreshDataBindingsOnActions(ActionCollection actions)
        {
            foreach (PerspexObject action in actions)
            {
                foreach (PerspexProperty property in DataBindingHelper.GetDependencyProperties(action.GetType()))
                {
                    DataBindingHelper.RefreshBinding(action, property);
                }
            }
        }

        private static IEnumerable<PerspexProperty> GetDependencyProperties(Type type)
        {
            List<PerspexProperty> propertyList = null;

            if (!DataBindingHelper.DependenciesPropertyCache.TryGetValue(type, out propertyList))
            {
                propertyList = new List<PerspexProperty>();

                while (type != null && type != typeof(PerspexObject))
                {
                    foreach (FieldInfo fieldInfo in type.GetRuntimeFields())
                    {
                        if (fieldInfo.IsPublic && fieldInfo.FieldType == typeof(PerspexProperty))
                        {
                            PerspexProperty property = fieldInfo.GetValue(null) as PerspexProperty;
                            if (property != null)
                            {
                                propertyList.Add(property);
                            }
                        }
                    }

                    type = type.GetTypeInfo().BaseType;
                }

                DataBindingHelper.DependenciesPropertyCache[type] = propertyList;
            }

            return propertyList;
        }

        private static void RefreshBinding(PerspexObject target, PerspexProperty property)
        {
            IXamlBinding binding = target.GetValue(property) as IXamlBinding;
            if (binding != null)
            {
                binding.Bind(target, property);
            }
        }
    }
}
