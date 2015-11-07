// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Perspex.Markup.Data.Plugins;

namespace Perspex.Markup.Data
{
    /// <summary>
    /// Observes and sets the value of an expression on an object.
    /// </summary>
    public class ExpressionObserver : ObservableBase<object>, IDescription
    {
        /// <summary>
        /// An ordered collection of property accessor plugins that can be used to customize
        /// the reading and subscription of property values on a type.
        /// </summary>
        public static readonly IList<IPropertyAccessorPlugin> PropertyAccessors =
            new List<IPropertyAccessorPlugin>
            {
                new PerspexPropertyAccessorPlugin(),
                new InpcPropertyAccessorPlugin(),
            };

        private readonly object _root;
        private readonly Func<object> _rootGetter;
        private readonly IObservable<object> _rootObservable;
        private IDisposable _rootObserverSubscription;
        private int _count;
        private readonly ExpressionNode _node;
        private ISubject<object> _empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="root">The root object.</param>
        /// <param name="expression">The expression.</param>
        public ExpressionObserver(object root, string expression)
        {
            Contract.Requires<ArgumentNullException>(expression != null);

            _root = root;

            if (!string.IsNullOrWhiteSpace(expression))
            {
                _node = ExpressionNodeBuilder.Build(expression);
            }

            Expression = expression;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="rootObservable">An observable which provides the root object.</param>
        /// <param name="expression">The expression.</param>
        public ExpressionObserver(IObservable<object> rootObservable, string expression)
        {
            Contract.Requires<ArgumentNullException>(rootObservable != null);
            Contract.Requires<ArgumentNullException>(expression != null);

            _rootObservable = rootObservable;

            if (!string.IsNullOrWhiteSpace(expression))
            {
                _node = ExpressionNodeBuilder.Build(expression);
            }

            Expression = expression;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="rootGetter">A function which gets the root object.</param>
        /// <param name="expression">The expression.</param>
        public ExpressionObserver(Func<object> rootGetter, string expression)
        {
            Contract.Requires<ArgumentNullException>(rootGetter != null);
            Contract.Requires<ArgumentNullException>(expression != null);

            _rootGetter = rootGetter;

            if (!string.IsNullOrWhiteSpace(expression))
            {
                _node = ExpressionNodeBuilder.Build(expression);
            }

            Expression = expression;
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
            IncrementCount();
            UpdateRoot();

            try
            {
                return _node?.SetValue(value) ?? false;
            }
            finally
            {
                DecrementCount();
            }
        }

        /// <summary>
        /// Gets the expression being observed.
        /// </summary>
        public string Expression { get; }

        /// <summary>
        /// Gets the type of the expression result or null if the expression could not be 
        /// evaluated.
        /// </summary>
        public Type ResultType
        {
            get
            {
                IncrementCount();

                try
                {
                    if (_node != null)
                    {
                        return (Leaf as PropertyAccessorNode)?.PropertyType;
                    }
                    else if(_rootGetter != null)
                    {
                        return _rootGetter()?.GetType();
                    }
                    else
                    {
                        return _root?.GetType();
                    }
                }
                finally
                {
                    DecrementCount();
                }
            }
        }

        /// <inheritdoc/>
        string IDescription.Description => Expression;

        /// <summary>
        /// Gets the leaf node.
        /// </summary>
        private ExpressionNode Leaf
        {
            get
            {
                var node = _node;
                while (node.Next != null) node = node.Next;
                return node;
            }
        }

        /// <summary>
        /// Causes the root object to be re-read from the root getter.
        /// </summary>
        public void UpdateRoot()
        {
            if (_count > 0 && _rootGetter != null)
            {
                if (_node != null)
                {
                    _node.Target = _rootGetter();
                }
                else if (_empty != null)
                {
                    _empty.OnNext(_rootGetter());
                }
            }
        }

        /// <inheritdoc/>
        protected override IDisposable SubscribeCore(IObserver<object> observer)
        {
            IncrementCount();

            if (_node != null)
            {
                var subscription = _node.Subscribe(observer);

                return Disposable.Create(() =>
                {
                    DecrementCount();
                    subscription.Dispose();
                });
            }
            else
            {
                if (_empty == null)
                {
                    _empty = new BehaviorSubject<object>(_rootGetter());
                }

                return _empty.Subscribe(observer);
            }
        }

        private void IncrementCount()
        {
            if (_count++ == 0 && _node != null)
            {
                if (_rootGetter != null)
                {
                    _node.Target = _rootGetter();
                }
                else if (_rootObservable != null)
                {
                    _rootObserverSubscription = _rootObservable.Subscribe(x => _node.Target = x);
                }
                else
                {
                    _node.Target = _root;
                }
            }
        }

        private void DecrementCount()
        {
            if (--_count == 0 && _node != null)
            {
                if (_rootObserverSubscription != null)
                {
                    _rootObserverSubscription.Dispose();
                    _rootObserverSubscription = null;
                }

                _node.Target = null;
            }
        }
    }
}
