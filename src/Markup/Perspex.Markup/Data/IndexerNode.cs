// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Perspex.Markup.Data
{
    internal class IndexerNode : ExpressionNode, 
        IWeakSubscriber<NotifyCollectionChangedEventArgs>,
        IWeakSubscriber<PropertyChangedEventArgs>
    {
        public IndexerNode(IList<string> arguments)
        {
            Arguments = arguments;
        }

        public IList<string> Arguments { get; }

        void IWeakSubscriber<NotifyCollectionChangedEventArgs>.OnEvent(object sender, NotifyCollectionChangedEventArgs e)
        {
            var update = false;
            if (sender is IList)
            {
                object indexObject;
                if (!TypeUtilities.TryConvert(typeof(int), Arguments[0], CultureInfo.InvariantCulture, out indexObject))
                {
                    return;
                }
                var index = (int)indexObject;
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        update = index >= e.NewStartingIndex;
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        update = index >= e.OldStartingIndex;
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        update = index >= e.NewStartingIndex &&
                                 index < e.NewStartingIndex + e.NewItems.Count;
                        break;
                    case NotifyCollectionChangedAction.Move:
                        update = (index >= e.NewStartingIndex &&
                                  index < e.NewStartingIndex + e.NewItems.Count) ||
                                 (index >= e.OldStartingIndex &&
                                 index < e.OldStartingIndex + e.OldItems.Count);
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        update = true;
                        break;
                }
            }
            else
            {
                update = true;
            }

            if (update)
            {
                CurrentValue = new WeakReference(GetValue(sender));
            }
        }

        void IWeakSubscriber<PropertyChangedEventArgs>.OnEvent(object sender, PropertyChangedEventArgs e)
        {
            var typeInfo = sender.GetType().GetTypeInfo();

            if (typeInfo.GetDeclaredProperty(e.PropertyName) == null)
            {
                return;
            }

            if (typeInfo.GetDeclaredProperty(e.PropertyName).GetIndexParameters().Any())
            {
                CurrentValue = new WeakReference(GetValue(sender));
            }
        }

        protected override void SubscribeAndUpdate(WeakReference reference)
        {
            object target = reference.Target;

            CurrentValue = new WeakReference(GetValue(target));

            var incc = target as INotifyCollectionChanged;

            if (incc != null)
            {
                WeakSubscriptionManager.Subscribe<NotifyCollectionChangedEventArgs>(
                    incc,
                    nameof(incc.CollectionChanged),
                    this);
            }

            var inpc = target as INotifyPropertyChanged;

            if (inpc != null)
            {
                WeakSubscriptionManager.Subscribe<PropertyChangedEventArgs>(
                    inpc,
                    nameof(inpc.PropertyChanged),
                    this);
            }
        }

        protected override void Unsubscribe(object target)
        {
            var incc = target as INotifyCollectionChanged;

            if (incc != null)
            {
                WeakSubscriptionManager.Unsubscribe<NotifyCollectionChangedEventArgs>(
                    incc,
                    nameof(incc.CollectionChanged),
                    this);
            }

            var inpc = target as INotifyPropertyChanged;

            if (inpc != null)
            {
                WeakSubscriptionManager.Unsubscribe<PropertyChangedEventArgs>(
                    inpc,
                    nameof(inpc.PropertyChanged),
                    this);
            }
        }

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
                        return PerspexProperty.UnsetValue;
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
                        return PerspexProperty.UnsetValue;
                    }
                    else if (dictionary != null)
                    {
                        if (dictionary.Contains(convertedObjectArray[0]))
                        {
                            return dictionary[convertedObjectArray[0]]; 
                        }
                        return PerspexProperty.UnsetValue;
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
            //  multidimensional indexer, which doesn't take the same number of arguments
            else if (typeInfo.IsArray)
            {
                return GetValueFromArray((Array)target);
            }

            return PerspexProperty.UnsetValue;
        }

        private object GetValueFromArray(Array array)
        {
            int[] intArgs;
            if (!ConvertArgumentsToInts(out intArgs))
                return PerspexProperty.UnsetValue;
            return GetValueFromArray(array, intArgs);
        }

        private object GetValueFromArray(Array array, int[] indicies)
        {
            if (ValidBounds(indicies, array))
            {
                return array.GetValue(indicies);
            }
            return PerspexProperty.UnsetValue;
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
            for (;typeInfo != null; typeInfo = typeInfo.BaseType?.GetTypeInfo())
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
    }
}
