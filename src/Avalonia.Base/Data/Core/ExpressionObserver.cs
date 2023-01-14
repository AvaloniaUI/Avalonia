using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Avalonia.Data.Core.Parsers;
using Avalonia.Data.Core.Plugins;
using Avalonia.Reactive;

namespace Avalonia.Data.Core
{
    /// <summary>
    /// Observes and sets the value of an expression on an object.
    /// </summary>
    public class ExpressionObserver : LightweightObservableBase<object?>, IDescription
    {
        /// <summary>
        /// An ordered collection of property accessor plugins that can be used to customize
        /// the reading and subscription of property values on a type.
        /// </summary>
        public static readonly List<IPropertyAccessorPlugin> PropertyAccessors =
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
        public static readonly List<IDataValidationPlugin> DataValidators =
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
        public static readonly List<IStreamPlugin> StreamHandlers =
            new List<IStreamPlugin>
            {
                new TaskStreamPlugin(),
                new ObservableStreamPlugin(),
            };
        private readonly ExpressionNode _node;
        private object? _root;
        private Func<object?>? _rootGetter;
        private IDisposable? _rootSubscription;
        private WeakReference<object?>? _value;
        private IReadOnlyList<ITransformNode>? _transformNodes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="root">The root object.</param>
        /// <param name="node">The expression.</param>
        /// <param name="description">
        /// A description of the expression.
        /// </param>
        public ExpressionObserver(
            object? root,
            ExpressionNode node,
            string? description = null)
        {
            _node = node;
            Description = description;
            _root = new WeakReference<object?>(root == AvaloniaProperty.UnsetValue ? null : root);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="rootObservable">An observable which provides the root object.</param>
        /// <param name="node">The expression.</param>
        /// <param name="description">
        /// A description of the expression.
        /// </param>
        public ExpressionObserver(
            IObservable<object?> rootObservable,
            ExpressionNode node,
            string? description)
        {
            _ = rootObservable ??throw new ArgumentNullException(nameof(rootObservable));
            
            _node = node;
            Description = description;
            _root = rootObservable;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="rootGetter">A function which gets the root object.</param>
        /// <param name="node">The expression.</param>
        /// <param name="update">An observable which triggers a re-read of the getter. Generic argument value is not used.</param>
        /// <param name="description">
        /// A description of the expression.
        /// </param>
        public ExpressionObserver(
            Func<object?> rootGetter,
            ExpressionNode node,
            IObservable<ValueTuple> update,
            string? description)
        {
            Description = description;
            _rootGetter = rootGetter ?? throw new ArgumentNullException(nameof(rootGetter));
            _node = node ?? throw new ArgumentNullException(nameof(node));
            _root = update.Select(x => rootGetter());
        }


        /// <summary>
        /// Creates a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="root">The root object.</param>
        /// <param name="expression">The expression.</param>
        /// <param name="enableDataValidation">Whether or not to track data validation</param>
        /// <param name="description">
        /// A description of the expression. If null, <paramref name="expression"/>'s string representation will be used.
        /// </param>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingMessages.ExpressionSafeSupressWarningMessage)]
        public static ExpressionObserver Create<T, U>(
            T? root,
            Expression<Func<T, U>> expression,
            bool enableDataValidation = false,
            string? description = null)
        {
            return new ExpressionObserver(root, Parse(expression, enableDataValidation), description ?? expression.ToString());
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="rootObservable">An observable which provides the root object.</param>
        /// <param name="expression">The expression.</param>
        /// <param name="enableDataValidation">Whether or not to track data validation</param>
        /// <param name="description">
        /// A description of the expression. If null, <paramref name="expression"/>'s string representation will be used.
        /// </param>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingMessages.ExpressionSafeSupressWarningMessage)]
        public static ExpressionObserver Create<T, U>(
            IObservable<T> rootObservable,
            Expression<Func<T, U>> expression,
            bool enableDataValidation = false,
            string? description = null)
        {
            _ = rootObservable ?? throw new ArgumentNullException(nameof(rootObservable));

            return new ExpressionObserver(
                rootObservable.Select(o => (object?)o),
                Parse(expression, enableDataValidation),
                description ?? expression.ToString());
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ExpressionObserver"/> class.
        /// </summary>
        /// <param name="rootGetter">A function which gets the root object.</param>
        /// <param name="expression">The expression.</param>
        /// <param name="update">An observable which triggers a re-read of the getter. Generic argument value is not used.</param>
        /// <param name="enableDataValidation">Whether or not to track data validation</param>
        /// <param name="description">
        /// A description of the expression. If null, <paramref name="expression"/>'s string representation will be used.
        /// </param>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingMessages.ExpressionSafeSupressWarningMessage)]
        public static ExpressionObserver Create<T, U>(
            Func<T> rootGetter,
            Expression<Func<T, U>> expression,
            IObservable<ValueTuple> update,
            bool enableDataValidation = false,
            string? description = null)
        {
            _ = rootGetter ?? throw new ArgumentNullException(nameof(rootGetter));

            return new ExpressionObserver(
                () => rootGetter(),
                Parse(expression, enableDataValidation),
                update,
                description ?? expression.ToString());
        }

        private IReadOnlyList<ITransformNode> GetTransformNodesFromChain()
        {
            LinkedList<ITransformNode> transforms = new LinkedList<ITransformNode>();
            var node = _node;
            while (node != null)
            {
                if (node is ITransformNode transform)
                {
                    transforms.AddFirst(transform);
                }
                node = node.Next;
            }

            return new List<ITransformNode>(transforms);
        }

        private IReadOnlyList<ITransformNode> TransformNodes => (_transformNodes ?? (_transformNodes = GetTransformNodesFromChain()));

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
        public bool SetValue(object? value, BindingPriority priority = BindingPriority.LocalValue)
        {
            if (Leaf is SettableNode settable)
            {
                foreach (var transform in TransformNodes)
                {
                    value = transform.Transform(value);
                    if (value is BindingNotification)
                    {
                        return false;
                    }
                }
                return settable.SetTargetValue(value, priority);
            }
            return false;
        }

        /// <summary>
        /// Gets a description of the expression being observed.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Gets the expression being observed.
        /// </summary>
        public string? Expression { get; }

        /// <summary>
        /// Gets the type of the expression result or null if the expression could not be 
        /// evaluated.
        /// </summary>
        public Type? ResultType => (Leaf as SettableNode)?.PropertyType;

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
            if (_rootGetter is not null)
                _node.Target = new WeakReference<object?>(_rootGetter());
            _node.Subscribe(ValueChanged);
            StartRoot();
        }

        protected override void Deinitialize()
        {
            _rootSubscription?.Dispose();
            _rootSubscription = null;
            _node.Unsubscribe();
        }

        protected override void Subscribed(IObserver<object?> observer, bool first)
        {
            if (!first && _value != null && _value.TryGetTarget(out var value))
            {
                observer.OnNext(value);
            }
        }

        [RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
        private static ExpressionNode Parse(LambdaExpression expression, bool enableDataValidation)
        {
            return ExpressionTreeParser.Parse(expression, enableDataValidation);
        }

        private void StartRoot()
        {
            if (_root is IObservable<object> observable)
            {
                _rootSubscription = observable.Subscribe(
                    new AnonymousObserver<object>(
                        x => _node.Target = new WeakReference<object?>(x != AvaloniaProperty.UnsetValue ? x : null),
                        x => PublishCompleted(),
                        PublishCompleted));
            }
            else
            {
                _node.Target = (WeakReference<object?>)_root!;
            }
        }

        private void ValueChanged(object? value)
        {
            var broken = BindingNotification.ExtractError(value) as MarkupBindingChainException;
            broken?.Commit(Description ?? "{empty}");
            _value = new WeakReference<object?>(value);
            PublishNext(value);
        }
    }
}
