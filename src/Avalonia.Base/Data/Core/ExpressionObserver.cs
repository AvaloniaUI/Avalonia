// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Data;
using Avalonia.Data.Core.Parsers;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data.Core
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
        private readonly Subject<Unit> _finished;
        private readonly object _root;
        private IObservable<object> _result;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="root">The root object.</param>
        /// <param name="node">The expression.</param>
        /// <param name="description">
        /// A description of the expression. If null, <paramref name="node"/> will be used.
        /// </param>
        public ExpressionObserver(
            object root,
            ExpressionNode node,
            string description = null)
        {
            if (root == AvaloniaProperty.UnsetValue)
            {
                root = null;
            }

            _node = node;
            Description = description;
            _root = new WeakReference(root);
        }

        public static ExpressionObserver Create<T, U>(
            T root,
            Expression<Func<T, U>> expression,
            bool enableDataValidation = false,
            string description = null)
        {
            return new ExpressionObserver(root, Parse(expression, enableDataValidation), description ?? expression.ToString());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="rootObservable">An observable which provides the root object.</param>
        /// <param name="node">The expression.</param>
        /// <param name="description">
        /// A description of the expression. If null, <paramref name="node"/> will be used.
        /// </param>
        public ExpressionObserver(
            IObservable<object> rootObservable,
            ExpressionNode node,
            string description)
        {
            Contract.Requires<ArgumentNullException>(rootObservable != null);
            
            _node = node;
            Description = description;
            _root = rootObservable;
            _finished = new Subject<Unit>();
        }

        public static ExpressionObserver Create<T, U>(
            IObservable<T> rootObservable,
            Expression<Func<T, U>> expression,
            bool enableDataValidation = false,
            string description = null)
        {
            Contract.Requires<ArgumentNullException>(rootObservable != null);
            return new ExpressionObserver(
                rootObservable.Select(o => (object)o),
                Parse(expression, enableDataValidation),
                description ?? expression.ToString());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="rootGetter">A function which gets the root object.</param>
        /// <param name="node">The expression.</param>
        /// <param name="update">An observable which triggers a re-read of the getter.</param>
        /// <param name="description">
        /// A description of the expression. If null, <paramref name="node"/> will be used.
        /// </param>
        public ExpressionObserver(
            Func<object> rootGetter,
            ExpressionNode node,
            IObservable<Unit> update,
            string description)
        {
            Contract.Requires<ArgumentNullException>(rootGetter != null);
            Contract.Requires<ArgumentNullException>(update != null);

            Description = description;
            _node = node;
            _finished = new Subject<Unit>();
            _node.Target = new WeakReference(rootGetter());
            _root = update.Select(x => rootGetter());
        }

        public static ExpressionObserver Create<T, U>(
            Func<T> rootGetter,
            Expression<Func<T, U>> expression,
            IObservable<Unit> update,
            bool enableDataValidation = false,
            string description = null)
        {
            Contract.Requires<ArgumentNullException>(rootGetter != null);

            return new ExpressionObserver(
                () => rootGetter(),
                Parse(expression, enableDataValidation),
                update,
                description ?? expression.ToString());
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
            if (Leaf is ISettableNode settable)
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
        public Type ResultType => (Leaf as ISettableNode)?.PropertyType;

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
                    .Select(ToWeakReference)
                    .Publish(UninitializedValue)
                    .RefCount()
                    .Where(x => x != UninitializedValue)
                    .Select(Translate);
            }

            return _result.Subscribe(observer);
        }

        private static ExpressionNode Parse(LambdaExpression expression, bool enableDataValidation)
        {
            var parser = new ExpressionTreeParser(enableDataValidation);
            return parser.Parse(expression);
        }

        private static object ToWeakReference(object o)
        {
            return o is BindingNotification ? o : new WeakReference(o);
        }

        private object Translate(object o)
        {
            if (o is WeakReference weak)
            {
                return weak.Target;
            }
            else if (BindingNotification.ExtractError(o) is MarkupBindingChainException broken)
            {
                broken.Commit(Description);
            }

            return o;
        }

        private IDisposable StartRoot()
        {
            switch (_root)
            {
                case IObservable<object> observable:
                    return observable.Subscribe(
                        x => _node.Target = new WeakReference(x != AvaloniaProperty.UnsetValue ? x : null),
                        _ => _finished.OnNext(Unit.Default),
                        () => _finished.OnNext(Unit.Default));
                case WeakReference weak:
                    _node.Target = weak;
                    break;
                default:
                    throw new AvaloniaInternalException("The ExpressionObserver._root member should only be either an observable or WeakReference.");
            }

            return Disposable.Empty;
        }
    }
}
