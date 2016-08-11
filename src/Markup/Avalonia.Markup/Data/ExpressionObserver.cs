// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Data;
using Avalonia.Markup.Data.Plugins;

namespace Avalonia.Markup.Data
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
                new AvaloniaPropertyAccessorPlugin(),
                new InpcPropertyAccessorPlugin(),
            };

        /// <summary>
        /// An ordered collection of validation checker plugins that can be used to customize
        /// the validation of view model and model data.
        /// </summary>
        public static readonly IList<IDataValidationPlugin> DataValidators =
            new List<IDataValidationPlugin>
            {
                new IndeiValidationPlugin(),
                new ExceptionValidationPlugin(),
            };

        /// <summary>
        /// An ordered collection of value handlers that can be used to customize the handling
        /// of certain values.
        /// </summary>
        public static readonly IList<IValuePlugin> ValueHandlers =
            new List<IValuePlugin>
            {
                new TaskValuePlugin(),
                new ObservableValuePlugin(),
            };

        private static readonly object UninitializedValue = new object();
        private readonly ExpressionNode _node;
        private readonly Subject<Unit> _finished;
        private readonly object _root;
        private IObservable<object> _result;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="root">The root object.</param>
        /// <param name="expression">The expression.</param>
        /// <param name="enableDataValidation">Whether data validation should be enabled.</param>
        public ExpressionObserver(object root, string expression, bool enableDataValidation = false)
        {
            Contract.Requires<ArgumentNullException>(expression != null);

            if (root == AvaloniaProperty.UnsetValue)
            {
                root = null;
            }

            Expression = expression;
            _node = Parse(expression, enableDataValidation);
            _root = new WeakReference(root);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="rootObservable">An observable which provides the root object.</param>
        /// <param name="expression">The expression.</param>
        /// <param name="enableDataValidation">Whether data validation should be enabled.</param>
        public ExpressionObserver(
            IObservable<object> rootObservable,
            string expression,
            bool enableDataValidation = false)
        {
            Contract.Requires<ArgumentNullException>(rootObservable != null);
            Contract.Requires<ArgumentNullException>(expression != null);

            Expression = expression;
            _node = Parse(expression, enableDataValidation);
            _finished = new Subject<Unit>();
            _root = rootObservable;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="rootGetter">A function which gets the root object.</param>
        /// <param name="expression">The expression.</param>
        /// <param name="update">An observable which triggers a re-read of the getter.</param>
        /// <param name="enableDataValidation">Whether data validation should be enabled.</param>
        public ExpressionObserver(
            Func<object> rootGetter,
            string expression,
            IObservable<Unit> update,
            bool enableDataValidation = false)
        {
            Contract.Requires<ArgumentNullException>(rootGetter != null);
            Contract.Requires<ArgumentNullException>(expression != null);
            Contract.Requires<ArgumentNullException>(update != null);

            Expression = expression;
            _node = Parse(expression, enableDataValidation);
            _finished = new Subject<Unit>();

            _node.Target = new WeakReference(rootGetter());
            _root = update.Select(x => rootGetter());
        }

        /// <summary>
        /// Attempts to set the value of a property expression.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <param name="priority">The binding priority to use.</param>
        /// <returns>
        /// True if the value could be set; false if the expression does not evaluate to a 
        /// property. Note that the <see cref="ExpressionObserver"/> must be subscribed to
        /// before setting the target value can work, as setting the value requires the
        /// expression to be evaluated.
        /// </returns>
        public bool SetValue(object value, BindingPriority priority = BindingPriority.LocalValue)
        {
            return (Leaf as PropertyAccessorNode)?.SetTargetValue(value, priority) ?? false;
        }

        /// <summary>
        /// Gets the expression being observed.
        /// </summary>
        public string Expression { get; }

        /// <summary>
        /// Gets the type of the expression result or null if the expression could not be 
        /// evaluated.
        /// </summary>
        public Type ResultType => (Leaf as PropertyAccessorNode)?.PropertyType;

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

        /// <inheritdoc/>
        protected override IDisposable SubscribeCore(IObserver<object> observer)
        {
            if (_result == null)
            {
                var source = (IObservable<object>)_node;

                if (_finished != null)
                {
                    source = source.TakeUntil(_finished);
                }

                _result = Observable.Using(StartRoot, _ => source)
                    .Publish(UninitializedValue)
                    .RefCount()
                    .Where(x => x != UninitializedValue);
            }

            return _result.Subscribe(observer);
        }

        private static ExpressionNode Parse(string expression, bool enableDataValidation)
        {
            if (!string.IsNullOrWhiteSpace(expression))
            {
                return ExpressionNodeBuilder.Build(expression, enableDataValidation);
            }
            else
            {
                return new EmptyExpressionNode();
            }
        }

        private IDisposable StartRoot()
        {
            var observable = _root as IObservable<object>;

            if (observable != null)
            {
                return observable.Subscribe(
                    x => _node.Target = new WeakReference(x != AvaloniaProperty.UnsetValue ? x : null),
                    _ => _finished.OnNext(Unit.Default),
                    () => _finished.OnNext(Unit.Default));
            }
            else
            {
                _node.Target = (WeakReference)_root;
                return Disposable.Empty;
            }
        }
    }
}
