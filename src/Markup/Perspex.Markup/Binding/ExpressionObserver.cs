// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;

namespace Perspex.Markup.Binding
{
    /// <summary>
    /// Observes the value of an expression on a root object.
    /// </summary>
    public class ExpressionObserver : ObservableBase<ExpressionValue>
    {
        private int _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="root">The root object.</param>
        /// <param name="expression">The expression.</param>
        public ExpressionObserver(object root, string expression)
        {
            Root = root;
            Nodes = ExpressionNodeBuilder.Build(expression);
        }

        /// <summary>
        /// Attempts to set the value of a property expression.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <returns>
        /// True if the value could be set; false if the expression does not evaluate to a 
        /// property.
        /// </returns>
        public bool SetValue(object value)
        {
            var last = Nodes.Last() as PropertyAccessorNode;

            if (last != null)
            {
                try
                {
                    IncrementCount();
                    return last.SetValue(value);
                }
                finally
                {
                    DecrementCount();
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the root object that the expression is being observed on.
        /// </summary>
        public object Root { get; }

        /// <summary>
        /// Gets a list of nodes representing the parts of the expression.
        /// </summary>
        public IList<ExpressionNode> Nodes { get; }

        /// <inheritdoc/>
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
