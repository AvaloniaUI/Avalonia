// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Utilities;

namespace Avalonia.Markup.Parsers.Nodes
{
    internal class StringIndexerNode : IndexerNodeBase
    {
        public StringIndexerNode(IList<string> arguments)
        {
            Arguments = arguments;
        }

        public override string Description => "[" + string.Join(",", Arguments) + "]";

        protected override bool SetTargetValueCore(object value, BindingPriority priority)
        {
            Target.TryGetTarget(out object target);

            var typeInfo = target.GetType().GetTypeInfo();
            var list = target as IList;
            var dictionary = target as IDictionary;
            var indexerProperty = GetIndexer(typeInfo);
            var indexerParameters = indexerProperty?.GetIndexParameters();

            if (indexerProperty != null && indexerParameters.Length == Arguments.Count)
            {
                var convertedObjectArray = new object[indexerParameters.Length];

                for (int i = 0; i < Arguments.Count; i++)
                {
                    object temp = null;

                    if (!TypeUtilities.TryConvert(indexerParameters[i].ParameterType, Arguments[i], CultureInfo.InvariantCulture, out temp))
                    {
                        return false;
                    }

                    convertedObjectArray[i] = temp;
                }

                var intArgs = convertedObjectArray.OfType<int>().ToArray();

                // Try special cases where we can validate indices
                if (typeInfo.IsArray)
                {
                    return SetValueInArray((Array)target, intArgs, value);
                }
                else if (Arguments.Count == 1)
                {
                    if (list != null)
                    {
                        if (intArgs.Length == Arguments.Count && intArgs[0] >= 0 && intArgs[0] < list.Count)
                        {
                            list[intArgs[0]] = value;
                            return true;
                        }

                        return false;
                    }
                    else if (dictionary != null)
                    {
                        if (dictionary.Contains(convertedObjectArray[0]))
                        {
                            dictionary[convertedObjectArray[0]] = value;
                            return true;
                        }
                        else
                        {
                            dictionary.Add(convertedObjectArray[0], value);
                            return true;
                        }
                    }
                    else
                    {
                        // Fallback to unchecked access
                        indexerProperty.SetValue(target, value, convertedObjectArray);
                        return true;
                    }
                }
                else
                {
                    // Fallback to unchecked access
                    indexerProperty.SetValue(target, value, convertedObjectArray);
                    return true;
                }
            }
            // Multidimensional arrays end up here because the indexer search picks up the IList indexer instead of the
            // multidimensional indexer, which doesn't take the same number of arguments
            else if (typeInfo.IsArray)
            {
                SetValueInArray((Array)target, value);
                return true;
            }
            return false;
        }

        private bool SetValueInArray(Array array, object value)
        {
            int[] intArgs;
            if (!ConvertArgumentsToInts(out intArgs))
                return false;
            return SetValueInArray(array, intArgs);
        }


        private bool SetValueInArray(Array array, int[] indices, object value)
        {
            if (ValidBounds(indices, array))
            {
                array.SetValue(value, indices);
                return true;
            }
            return false;
        }


        public IList<string> Arguments { get; }

        public override Type PropertyType
        {
            get
            {
                Target.TryGetTarget(out object target);

                return GetIndexer(target.GetType().GetTypeInfo())?.PropertyType;
            }
        }

        protected override object GetValue(object target)
        {
            var typeInfo = target.GetType().GetTypeInfo();
            var list = target as IList;
            var dictionary = target as IDictionary;
            var indexerProperty = GetIndexer(typeInfo);
            var indexerParameters = indexerProperty?.GetIndexParameters();

            if (indexerProperty != null && indexerParameters.Length == Arguments.Count)
            {
                var convertedObjectArray = new object[indexerParameters.Length];

                for (int i = 0; i < Arguments.Count; i++)
                {
                    object temp = null;

                    if (!TypeUtilities.TryConvert(indexerParameters[i].ParameterType, Arguments[i], CultureInfo.InvariantCulture, out temp))
                    {
                        return AvaloniaProperty.UnsetValue;
                    }

                    convertedObjectArray[i] = temp;
                }

                var intArgs = convertedObjectArray.OfType<int>().ToArray();

                // Try special cases where we can validate indices
                if (typeInfo.IsArray)
                {
                    return GetValueFromArray((Array)target, intArgs);
                }
                else if (Arguments.Count == 1)
                {
                    if (list != null)
                    {
                        if (intArgs.Length == Arguments.Count && intArgs[0] >= 0 && intArgs[0] < list.Count)
                        {
                            return list[intArgs[0]];
                        }

                        return AvaloniaProperty.UnsetValue;
                    }
                    else if (dictionary != null)
                    {
                        if (dictionary.Contains(convertedObjectArray[0]))
                        {
                            return dictionary[convertedObjectArray[0]];
                        }

                        return AvaloniaProperty.UnsetValue;
                    }
                    else
                    {
                        // Fallback to unchecked access
                        return indexerProperty.GetValue(target, convertedObjectArray);
                    }
                }
                else
                {
                    // Fallback to unchecked access
                    return indexerProperty.GetValue(target, convertedObjectArray);
                }
            }
            // Multidimensional arrays end up here because the indexer search picks up the IList indexer instead of the
            // multidimensional indexer, which doesn't take the same number of arguments
            else if (typeInfo.IsArray)
            {
                return GetValueFromArray((Array)target);
            }

            return AvaloniaProperty.UnsetValue;
        }

        private object GetValueFromArray(Array array)
        {
            int[] intArgs;
            if (!ConvertArgumentsToInts(out intArgs))
                return AvaloniaProperty.UnsetValue;
            return GetValueFromArray(array, intArgs);
        }

        private object GetValueFromArray(Array array, int[] indices)
        {
            if (ValidBounds(indices, array))
            {
                return array.GetValue(indices);
            }
            return AvaloniaProperty.UnsetValue;
        }

        private bool ConvertArgumentsToInts(out int[] intArgs)
        {
            intArgs = new int[Arguments.Count];

            for (int i = 0; i < Arguments.Count; ++i)
            {
                object value;

                if (!TypeUtilities.TryConvert(typeof(int), Arguments[i], CultureInfo.InvariantCulture, out value))
                {
                    return false;
                }

                intArgs[i] = (int)value;
            }
            return true;
        }

        private static PropertyInfo GetIndexer(TypeInfo typeInfo)
        {
            PropertyInfo indexer;

            for (; typeInfo != null; typeInfo = typeInfo.BaseType?.GetTypeInfo())
            {
                // Check for the default indexer name first to make this faster.
                // This will only be false when a class in VB has a custom indexer name.
                if ((indexer = typeInfo.GetDeclaredProperty(CommonPropertyNames.IndexerName)) != null)
                {
                    return indexer;
                }

                foreach (var property in typeInfo.DeclaredProperties)
                {
                    if (property.GetIndexParameters().Any())
                    {
                        return property;
                    }
                }
            }

            return null;
        }

        private bool ValidBounds(int[] indices, Array array)
        {
            if (indices.Length == array.Rank)
            {
                for (var i = 0; i < indices.Length; ++i)
                {
                    if (indices[i] >= array.GetLength(i))
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        protected override bool ShouldUpdate(object sender, PropertyChangedEventArgs e)
        {
            var typeInfo = sender.GetType().GetTypeInfo();
            return typeInfo.GetDeclaredProperty(e.PropertyName)?.GetIndexParameters().Any() ?? false;
        }

        protected override int? TryGetFirstArgumentAsInt()
        {
            if (TypeUtilities.TryConvert(typeof(int), Arguments[0], CultureInfo.InvariantCulture, out var value))
            {
                return (int?)value;
            }
            return null;
        }
    }
}
