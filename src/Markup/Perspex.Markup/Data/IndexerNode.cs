// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Perspex.Markup.Data
{
    internal class IndexerNode : ExpressionNode
    {
        private readonly int[] _intArgs;

        public IndexerNode(IList<object> arguments)
        {
            Arguments = arguments;

            var intArgs = Arguments.OfType<int>().ToArray();

            if (intArgs.Length == arguments.Count)
            {
                _intArgs = intArgs;
            }
        }

        public IList<object> Arguments { get; }

        protected override void SubscribeAndUpdate(object target)
        {
            CurrentValue = GetValue(target);

            var incc = target as INotifyCollectionChanged;

            if (incc != null)
            {
                incc.CollectionChanged += CollectionChanged;
            }
        }

        protected override void Unsubscribe(object target)
        {
            var incc = target as INotifyCollectionChanged;

            if (incc != null)
            {
                incc.CollectionChanged -= CollectionChanged;
            }
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            bool update = false;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    update = _intArgs[0] >= e.NewStartingIndex;
                    break;
                case NotifyCollectionChangedAction.Remove:
                    update = _intArgs[0] >= e.OldStartingIndex;
                    break;
                case NotifyCollectionChangedAction.Replace:
                    update = _intArgs[0] >= e.NewStartingIndex &&
                             _intArgs[0] < e.NewStartingIndex + e.NewItems.Count;
                    break;
                case NotifyCollectionChangedAction.Move:
                    update = (_intArgs[0] >= e.NewStartingIndex &&
                              _intArgs[0] < e.NewStartingIndex + e.NewItems.Count) ||
                             (_intArgs[0] >= e.OldStartingIndex &&
                             _intArgs[0] < e.OldStartingIndex + e.OldItems.Count);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    update = true;
                    break;
            }

            if (update)
            {
                CurrentValue = GetValue(sender);
            }
        }

        private object GetValue(object target)
        {
            var typeInfo = target.GetType().GetTypeInfo();
            var list = target as IList;

            if (typeInfo.IsArray && _intArgs != null)
            {
                var array = (Array)target;

                if (InBounds(_intArgs, array))
                {
                    return array.GetValue(_intArgs);
                }
            }
            else if (target is IList && _intArgs?.Length == 1)
            {
                if (_intArgs[0] < list.Count)
                {
                    return list[_intArgs[0]];
                }
            }
            else
            {
                PropertyInfo indexerProperty = null;
                ParameterInfo[] indexerParameters = null;
                foreach (var property in typeInfo.DeclaredProperties)
                {
                    var indexParams = property.GetIndexParameters();
                    if (indexParams.Length > 0)
                    {
                        indexerProperty = property;
                        indexerParameters = indexParams;
                    }
                }
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
                    return indexerProperty.GetValue(target, convertedObjectArray);
                }
            }

            return PerspexProperty.UnsetValue;
        }

        private bool InBounds(int[] args, Array array)
        {
            if (args.Length == array.Rank)
            {
                for (var i = 0; i < args.Length; ++i)
                {
                    if (args[i] >= array.GetLength(i))
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
