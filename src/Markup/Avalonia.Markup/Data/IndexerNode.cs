// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reactive.Linq;
using Avalonia.Data;

namespace Avalonia.Markup.Data
{
    internal class IndexerNode : ExpressionNode, ISettableNode
    {
        public IndexerNode(IList<string> arguments)
        {
            Arguments = arguments;
        }

        public override string Description => "[" + string.Join(",", Arguments) + "]";

        protected override IObservable<object> StartListeningCore(WeakReference reference)
        {
            var target = reference.Target;
            var incc = target as INotifyCollectionChanged;
            var inpc = target as INotifyPropertyChanged;
            var inputs = new List<IObservable<object>>();

            if (incc != null)
            {
                inputs.Add(WeakObservable.FromEventPattern<INotifyCollectionChanged, NotifyCollectionChangedEventArgs>(
                    incc,
                    nameof(incc.CollectionChanged))
                    .Where(x => ShouldUpdate(x.Sender, x.EventArgs))
                    .Select(_ => GetValue(target)));
            }

            if (inpc != null)
            {
                inputs.Add(WeakObservable.FromEventPattern<INotifyPropertyChanged, PropertyChangedEventArgs>(
                    inpc,
                    nameof(inpc.PropertyChanged))
                    .Where(x => ShouldUpdate(x.Sender, x.EventArgs))
                    .Select(_ => GetValue(target)));
            }

            return Observable.Merge(inputs).StartWith(GetValue(target));
        }

        public bool SetTargetValue(object value, BindingPriority priority)
        {
            var typeInfo = Target.Target.GetType().GetTypeInfo();
            var list = Target.Target as IList;
            var dictionary = Target.Target as IDictionary;
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

                // Try special cases where we can validate indicies
                if (typeInfo.IsArray)
                {
                    return SetValueInArray((Array)Target.Target, intArgs, value);
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
                        indexerProperty.SetValue(Target.Target, value, convertedObjectArray);
                        return true;
                    }
                }
                else
                {
                    // Fallback to unchecked access
                    indexerProperty.SetValue(Target.Target, value, convertedObjectArray);
                    return true;
                }
            }
            // Multidimensional arrays end up here because the indexer search picks up the IList indexer instead of the
            // multidimensional indexer, which doesn't take the same number of arguments
            else if (typeInfo.IsArray)
            {
                SetValueInArray((Array)Target.Target, value);
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


        private bool SetValueInArray(Array array, int[] indicies, object value)
        {
            if (ValidBounds(indicies, array))
            {
                array.SetValue(value, indicies);
                return true;
            }
            return false;
        }


        public IList<string> Arguments { get; }

        public Type PropertyType => GetIndexer(Target.Target.GetType().GetTypeInfo())?.PropertyType;

        private object GetValue(object target)
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

                // Try special cases where we can validate indicies
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

        private object GetValueFromArray(Array array, int[] indicies)
        {
            if (ValidBounds(indicies, array))
            {
                return array.GetValue(indicies);
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

        private bool ValidBounds(int[] indicies, Array array)
        {
            if (indicies.Length == array.Rank)
            {
                for (var i = 0; i < indicies.Length; ++i)
                {
                    if (indicies[i] >= array.GetLength(i))
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

        private bool ShouldUpdate(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (sender is IList)
            {
                object indexObject;

                if (!TypeUtilities.TryConvert(typeof(int), Arguments[0], CultureInfo.InvariantCulture, out indexObject))
                {
                    return false;
                }

                var index = (int)indexObject;

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        return index >= e.NewStartingIndex;
                    case NotifyCollectionChangedAction.Remove:
                        return index >= e.OldStartingIndex;
                    case NotifyCollectionChangedAction.Replace:
                        return index >= e.NewStartingIndex &&
                               index < e.NewStartingIndex + e.NewItems.Count;
                    case NotifyCollectionChangedAction.Move:
                        return (index >= e.NewStartingIndex &&
                                index < e.NewStartingIndex + e.NewItems.Count) ||
                               (index >= e.OldStartingIndex &&
                                index < e.OldStartingIndex + e.OldItems.Count);
                    case NotifyCollectionChangedAction.Reset:
                        return true;
                }
            }

            return true; // Implementation defined meaning for the index, so just try to update anyway
        }

        private bool ShouldUpdate(object sender, PropertyChangedEventArgs e)
        {
            var typeInfo = sender.GetType().GetTypeInfo();
            return typeInfo.GetDeclaredProperty(e.PropertyName)?.GetIndexParameters().Any() ?? false;
        }
    }
}
