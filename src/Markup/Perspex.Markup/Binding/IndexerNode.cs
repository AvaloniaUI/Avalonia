// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;

namespace Perspex.Markup.Binding
{
    internal class IndexerNode : ExpressionNode
    {
        private int[] _intArgs;

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
                return ((Array)target).GetValue(_intArgs);
            }
            else if (target is IList && _intArgs?.Length == 1)
            {
                if (_intArgs[0] < list.Count)
                {
                    return list[_intArgs[0]];
                }
            }

            return PerspexProperty.UnsetValue;
        }
    }
}
