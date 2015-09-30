// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;

namespace Perspex.Markup.Binding
{
    public class ExpressionObserver : ObservableBase<ExpressionValue>
    {
        private int _count;

        public ExpressionObserver(object root, string expression)
        {
            Root = root;
            Nodes = ExpressionNodeBuilder.Build(expression);
        }

        public object Root { get; }

        public IList<ExpressionNode> Nodes { get; }

        protected override IDisposable SubscribeCore(IObserver<ExpressionValue> observer)
        {
            IncrementCount();

            var subscription = Nodes[0].Subscribe(observer);

            return Disposable.Create(() =>
            {
                DecrementCount();
                subscription.Dispose();
            });
        }

        private void IncrementCount()
        {
            if (_count++ == 0)
            {
                Nodes[0].Target = Root;
            }
        }

        private void DecrementCount()
        {
            if (--_count == 0)
            {
                Nodes[0].Target = null;
            }
        }
    }
}
