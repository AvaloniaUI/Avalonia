// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Data;
using Avalonia.Data.Core.Plugins;
using Avalonia.Reactive;

namespace Avalonia.Data.Core
{
    /// <summary>
    /// Observes and sets the value of an expression on an object.
    /// </summary>
    public class ExpressionObserver : LightweightObservableBase<object>, IDescription
    {
        /// <summary>
        /// An ordered collection of property accessor plugins that can be used to customize
        /// the reading and subscription of property values on a type.
        /// </summary>
        public static readonly IList<IPropertyAccessorPlugin> PropertyAccessors =
            new List<IPropertyAccessorPlugin>
            {
                new AvaloniaPropertyAccessorPlugin(),
                new MethodAccessorPlugin(),
                new InpcPropertyAccessorPlugin(),
            };

        /// <summary>
        /// An ordered collection of validation checker plugins that can be used to customize
        /// the validation of view model and model data.
        /// </summary>
        public static readonly IList<IDataValidationPlugin> DataValidators =
            new List<IDataValidationPlugin>
            {
                new DataAnnotationsValidationPlugin(),
                new IndeiValidationPlugin(),
                new ExceptionValidationPlugin(),
            };

        /// <summary>
        /// An ordered collection of stream plugins that can be used to customize the behavior
        /// of the '^' stream binding operator.
        /// </summary>
        public static readonly IList<IStreamPlugin> StreamHandlers =
            new List<IStreamPlugin>
            {
                new TaskStreamPlugin(),
                new ObservableStreamPlugin(),
            };

        private static readonly object UninitializedValue = new object();
        private readonly ExpressionNode _node;
        private object _root;
        private IDisposable _rootSubscription;
        private WeakReference<object> _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="root">The root object.</param>
        /// <param name="expression">The expression.</param>
        /// <param name="enableDataValidation">Whether data validation should be enabled.</param>
        /// <param name="description">
        /// A description of the expression. If null, <paramref name="expression"/> will be used.
        /// </param>
        public ExpressionObserver(
            object root,
            string expression,
            bool enableDataValidation = false,
            string description = null)
        {
            Contract.Requires<ArgumentNullException>(expression != null);

            if (root == AvaloniaProperty.UnsetValue)
            {
                root = null;
            }

            Expression = expression;
            Description = description ?? expression;
            _node = Parse(expression, enableDataValidation);
            _root = new WeakReference(root);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="rootObservable">An observable which provides the root object.</param>
        /// <param name="expression">The expression.</param>
        /// <param name="enableDataValidation">Whether data validation should be enabled.</param>
        /// <param name="description">
        /// A description of the expression. If null, <paramref name="expression"/> will be used.
        /// </param>
        public ExpressionObserver(
            IObservable<object> rootObservable,
            string expression,
            bool enableDataValidation = false,
            string description = null)
        {
            Contract.Requires<ArgumentNullException>(rootObservable != null);
            Contract.Requires<ArgumentNullException>(expression != null);

            Expression = expression;
            Description = description ?? expression;
            _node = Parse(expression, enableDataValidation);
            _root = rootObservable;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="rootGetter">A function which gets the root object.</param>
        /// <param name="expression">The expression.</param>
        /// <param name="update">An observable which triggers a re-read of the getter.</param>
        /// <param name="enableDataValidation">Whether data validation should be enabled.</param>
        /// <param name="description">
        /// A description of the expression. If null, <paramref name="expression"/> will be used.
        /// </param>
        public ExpressionObserver(
            Func<object> rootGetter,
            string expression,
            IObservable<Unit> update,
            bool enableDataValidation = false,
            string description = null)
        {
            Contract.Requires<ArgumentNullException>(rootGetter != null);
            Contract.Requires<ArgumentNullException>(expression != null);
            Contract.Requires<ArgumentNullException>(update != null);

            Expression = expression;
            Description = description ?? expression;
            _node = Parse(expression, enableDataValidation);
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
            if (Leaf is SettableNode settable)
            {
                var node = _node;
                while (node != null)
                {
                    if (node is ITransformNode transform)
                    {
                        value = transform.Transform(value);
                        if (value is BindingNotification)
                        {
                            return false;
                        }
                    }
                    node = node.Next;
                }
                return settable.SetTargetValue(value, priority);
            }
            return false;
        }

        /// <summary>
        /// Gets a description of the expression being observed.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the expression being observed.
        /// </summary>
        public string Expression { get; }

        /// <summary>
        /// Gets the type of the expression result or null if the expression could not be 
        /// evaluated.
        /// </summary>
        public Type ResultType => (Leaf as SettableNode)?.PropertyType;

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

        protected override void Initialize()
        {
            _value = null;
            _node.Subscribe(ValueChanged);
            StartRoot();
        }

        protected override void Deinitialize()
        {
            _rootSubscription?.Dispose();
            _rootSubscription = null;
            _node.Unsubscribe();
        }

        protected override void Subscribed(IObserver<object> observer, bool first)
        {
            if (!first && _value != null && _value.TryGetTarget(out var value))
            {
                observer.OnNext(value);
            }
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

        private void StartRoot()
        {
            if (_root is IObservable<object> observable)
            {
                _rootSubscription = observable.Subscribe(
                    x => _node.Target = new WeakReference(x != AvaloniaProperty.UnsetValue ? x : null),
                    x => PublishCompleted(),
                    () => PublishCompleted());
            }
            else
            {
                _node.Target = (WeakReference)_root;
            }
        }

        private void ValueChanged(object value)
        {
            var broken = BindingNotification.ExtractError(value) as MarkupBindingChainException;
            broken?.Commit(Description);
            _value = new WeakReference<object>(value);
            PublishNext(value);
        }
    }
}
