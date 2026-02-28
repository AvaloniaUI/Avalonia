using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Utilities;

namespace Avalonia.Data.Core.Parsers;

/// <summary>
/// Visits and processes a LINQ expression to build a compiled binding path.
/// </summary>
/// <typeparam name="TIn">The input parameter type for the binding expression.</typeparam>
/// <remarks>
/// This visitor traverses lambda expressions used in compiled bindings and uses
/// <see cref="CompiledBindingPathBuilder"/> to construct a <see cref="CompiledBindingPath"/>, which
/// can then be converted into <see cref="ExpressionNode"/> instances. It supports property access,
/// indexers, AvaloniaProperty access, stream bindings, type casts, and logical operators.
/// </remarks>
[RequiresDynamicCode(TrimmingMessages.ExpressionNodeRequiresDynamicCodeMessage)]
[RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
internal class BindingExpressionVisitor<TIn>(LambdaExpression expression) : ExpressionVisitor
{
    private const string IndexerGetterName = "get_Item";
    private const string MultiDimensionalArrayGetterMethodName = "Get";
    private readonly LambdaExpression _rootExpression = expression;
    private readonly CompiledBindingPathBuilder _builder = new();
    private Expression? _head;

    /// <summary>
    /// Builds a compiled binding path from a lambda expression.
    /// </summary>
    /// <typeparam name="TOut">The output type of the binding expression.</typeparam>
    /// <param name="expression">
    /// The lambda expression to parse and convert into a binding path.
    /// </param>
    /// <returns>
    /// A <see cref="CompiledBindingPath"/> representing the binding path.
    /// </returns>
    /// <exception cref="ExpressionParseException">
    /// Thrown when the expression contains unsupported operations or invalid syntax for binding
    /// expressions.
    /// </exception>
    public static CompiledBindingPath BuildPath<TOut>(Expression<Func<TIn, TOut>> expression)
    {
        var visitor = new BindingExpressionVisitor<TIn>(expression);
        visitor.Visit(expression);
        return visitor._builder.Build();
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        // Indexers require more work since the compiler doesn't generate IndexExpressions:
        // they weren't in System.Linq.Expressions v1 and so must be generated manually.
        if (node.NodeType == ExpressionType.ArrayIndex)
            return Visit(Expression.MakeIndex(node.Left, null, [node.Right]));

        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    protected override Expression VisitIndex(IndexExpression node)
    {
        if (node.Indexer == BindingExpressionVisitorMembers.AvaloniaObjectIndexer)
        {
            var property = GetValue<AvaloniaProperty>(node.Arguments[0]);
            return Add(node.Object, node, x => x.Property(property, CreateAvaloniaPropertyAccessor));
        }
        else if (node.Object?.Type.IsArray == true)
        {
            var indexes = node.Arguments.Select(GetValue<int>).ToArray();
            return Add(node.Object, node, x => x.ArrayElement(indexes, node.Type));
        }
        else if (node.Indexer?.GetMethod is not null &&
            node.Arguments.Count == 1 &&
            node.Arguments[0].Type == typeof(int))
        {
            var getMethod = node.Indexer.GetMethod;
            var setMethod = node.Indexer.SetMethod;
            var index = GetValue<int>(node.Arguments[0]);
            var info = new ClrPropertyInfo(
                CommonPropertyNames.IndexerName,
                x => getMethod.Invoke(x, new object[] { index }),
                setMethod is not null ? (o, v) => setMethod.Invoke(o, new[] { index, v }) : null,
                getMethod.ReturnType);
            return Add(node.Object, node, x => x.Property(
                info,
                (weakRef, propInfo) => CreateIndexerPropertyAccessor(weakRef, propInfo, index)));
        }
        else if (node.Indexer?.GetMethod is not null)
        {
            var getMethod = node.Indexer.GetMethod;
            var setMethod = node.Indexer?.SetMethod;
            var indexes = node.Arguments.Select(GetValue<object>).ToArray();
            var info = new ClrPropertyInfo(
                CommonPropertyNames.IndexerName,
                x => getMethod.Invoke(x, indexes),
                setMethod is not null ? (o, v) => setMethod.Invoke(o, indexes.Append(v).ToArray()) : null,
                getMethod.ReturnType);
            return Add(node.Object, node, x => x.Property(
                info,
                CreateInpcPropertyAccessor));
        }

        throw new ExpressionParseException(0, $"Invalid indexer in binding expression: {node.NodeType}.");
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        return node.Member.MemberType switch
        {
            MemberTypes.Property => AddPropertyNode(node),
            _ => throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}."),
        };
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var method = node.Method;

        if (method.Name == IndexerGetterName && node.Object is not null)
        {
            var property = TryGetPropertyFromMethod(method);
            return Visit(Expression.MakeIndex(node.Object, property, node.Arguments));
        }
        else if (method.Name == MultiDimensionalArrayGetterMethodName &&
                 node.Object is not null)
        {
            var indexes = node.Arguments.Select(GetValue<int>).ToArray();
            return Add(node.Object, node, x => x.ArrayElement(indexes, node.Type));
        }
        else if (method.Name.StartsWith(StreamBindingExtensions.StreamBindingName) &&
                 method.DeclaringType == typeof(StreamBindingExtensions))
        {
            var instance = node.Method.IsStatic ? node.Arguments[0] : node.Object;
            var instanceType = instance?.Type;
            var genericArgs = method.GetGenericArguments();
            var genericArg = genericArgs.Length > 0 ? genericArgs[0] : typeof(object);

            if (instanceType == typeof(Task) ||
                (instanceType?.IsGenericType == true &&
                 instanceType.GetGenericTypeDefinition() == typeof(Task<>) &&
                 genericArg.IsAssignableFrom(instanceType.GetGenericArguments()[0])))
            {
                return Add(instance, node, x => x.StreamTask());
            }
            else if (instanceType is not null && ObservableStreamPlugin.MatchesType(instanceType))
            {
                return Add(instance, node, x => x.StreamObservable());
            }
        }
        else if (method == BindingExpressionVisitorMembers.CreateDelegateMethod)
        {
            var methodInfo = GetValue<MethodInfo>(node.Object!);
            var delegateType = GetValue<Type>(node.Arguments[0]);
            return Add(node.Arguments[1], node, x => x.Method(
                methodInfo.MethodHandle,
                delegateType.TypeHandle,
                acceptsNull: false));
        }

        throw new ExpressionParseException(0, $"Invalid method call in binding expression: '{node.Method.DeclaringType}.{node.Method.Name}'.");
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node == _rootExpression.Parameters[0] && _head is null)
            _head = node;
        return base.VisitParameter(node);
    }
    
    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType == ExpressionType.Not && node.Type == typeof(bool))
        {
            return Add(node.Operand, node, x => x.Not());
        }
        else if (node.NodeType == ExpressionType.Convert)
        {
            // Allow reference type casts (both upcasts and downcasts) but reject value type conversions
            if (!node.Type.IsValueType && !node.Operand.Type.IsValueType &&
                (node.Type.IsAssignableFrom(node.Operand.Type) || node.Operand.Type.IsAssignableFrom(node.Type)))
            {
                return Add(node.Operand, node, x => x.TypeCast(node.Type));
            }
        }
        else if (node.NodeType == ExpressionType.TypeAs)
        {
            return Add(node.Operand, node, x => x.TypeCast(node.Type));
        }

        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    protected override Expression VisitBlock(BlockExpression node)
    {
        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    protected override CatchBlock VisitCatchBlock(CatchBlock node)
    {
        throw new ExpressionParseException(0, "Catch blocks are not allowed in binding expressions.");
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    protected override Expression VisitDynamic(DynamicExpression node)
    {
        throw new ExpressionParseException(0, "Dynamic expressions are not allowed in binding expressions.");
    }

    protected override ElementInit VisitElementInit(ElementInit node)
    {
        throw new ExpressionParseException(0, "Element init expressions are not valid in a binding expression.");
    }

    protected override Expression VisitGoto(GotoExpression node)
    {
        throw new ExpressionParseException(0, "Goto expressions are not supported in binding expressions.");
    }

    protected override Expression VisitInvocation(InvocationExpression node)
    {
        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    protected override Expression VisitLabel(LabelExpression node)
    {
        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    protected override Expression VisitListInit(ListInitExpression node)
    {
        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    protected override Expression VisitLoop(LoopExpression node)
    {
        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
    {
        throw new ExpressionParseException(0, "Member assignments not supported in binding expressions.");
    }

    protected override Expression VisitSwitch(SwitchExpression node)
    {
        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    protected override Expression VisitTry(TryExpression node)
    {
        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    protected override Expression VisitTypeBinary(TypeBinaryExpression node)
    {
        throw new ExpressionParseException(0, $"Invalid expression type in binding expression: {node.NodeType}.");
    }

    private Expression Add(Expression? instance, Expression expression, Action<CompiledBindingPathBuilder> build)
    {
        var visited = Visit(instance);

        if (visited != _head)
        {
            throw new ExpressionParseException(
                0,
                $"Unable to parse '{expression}': expected an instance of '{_head}' but got '{visited}'.");
        }

        build(_builder);
        return _head = expression;
    }

    private Expression AddPropertyNode(MemberExpression node)
    {
        // Check if it's an AvaloniaProperty accessed via CLR wrapper
        if (typeof(AvaloniaObject).IsAssignableFrom(node.Expression?.Type) &&
            AvaloniaPropertyRegistry.Instance.FindRegistered(node.Expression.Type, node.Member.Name) is { } avaloniaProperty)
        {
            return Add(
                node.Expression,
                node,
                x => x.Property(avaloniaProperty, CreateAvaloniaPropertyAccessor));
        }
        else
        {
            var property = (PropertyInfo)node.Member;
            var info = new ClrPropertyInfo(
                property.Name,
                CreateGetter(property),
                CreateSetter(property),
                property.PropertyType);
            return Add(node.Expression, node, x => x.Property(info, CreateInpcPropertyAccessor));
        }
    }

    private static Func<object, object>? CreateGetter(PropertyInfo info)
    {
        if (info.GetMethod == null)
            return null;
        var target = Expression.Parameter(typeof(object), "target");
        return Expression.Lambda<Func<object, object>>(
                Expression.Convert(Expression.Call(Expression.Convert(target, info.DeclaringType!), info.GetMethod),
                    typeof(object)),
                target)
            .Compile();
    }

    private static Action<object, object?>? CreateSetter(PropertyInfo info)
    {
        if (info.SetMethod == null)
            return null;
        var target = Expression.Parameter(typeof(object), "target");
        var value = Expression.Parameter(typeof(object), "value");
        return Expression.Lambda<Action<object, object?>>(
                Expression.Call(Expression.Convert(target, info.DeclaringType!), info.SetMethod,
                    Expression.Convert(value, info.SetMethod.GetParameters()[0].ParameterType)),
                target, value)
            .Compile();
    }

    private static T GetValue<T>(Expression expr)
    {
        if (expr is ConstantExpression constant)
            return (T)constant.Value!;
        return Expression.Lambda<Func<T>>(expr).Compile(preferInterpretation: true)();
    }

    private static PropertyInfo? TryGetPropertyFromMethod(MethodInfo method)
    {
        var type = method.DeclaringType;
        return type?.GetRuntimeProperties().FirstOrDefault(prop => prop.GetMethod == method);
    }

    // Accessor factory methods
    private static IPropertyAccessor CreateInpcPropertyAccessor(WeakReference<object?> target, IPropertyInfo property)
        => new InpcPropertyAccessor(target, property);

    private static IPropertyAccessor CreateAvaloniaPropertyAccessor(WeakReference<object?> target, IPropertyInfo property)
        => new AvaloniaPropertyAccessor(
            new WeakReference<AvaloniaObject?>((AvaloniaObject?)(target.TryGetTarget(out var o) ? o : null)),
            (AvaloniaProperty)property);

    private static IPropertyAccessor CreateIndexerPropertyAccessor(WeakReference<object?> target, IPropertyInfo property, int argument)
        => new IndexerAccessor(target, property, argument);

    // Accessor implementations
    private class AvaloniaPropertyAccessor : PropertyAccessorBase, IWeakEventSubscriber<AvaloniaPropertyChangedEventArgs>
    {
        private readonly WeakReference<AvaloniaObject?> _reference;
        private readonly AvaloniaProperty _property;

        public AvaloniaPropertyAccessor(WeakReference<AvaloniaObject?> reference, AvaloniaProperty property)
        {
            _reference = reference ?? throw new ArgumentNullException(nameof(reference));
            _property = property ?? throw new ArgumentNullException(nameof(property));
        }

        public override Type PropertyType => _property.PropertyType;
        public override object? Value => _reference.TryGetTarget(out var instance) ? instance?.GetValue(_property) : null;

        public override bool SetValue(object? value, BindingPriority priority)
        {
            if (!_property.IsReadOnly && _reference.TryGetTarget(out var instance))
            {
                instance.SetValue(_property, value, priority);
                return true;
            }
            return false;
        }

        public void OnEvent(object? sender, WeakEvent ev, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == _property)
                PublishValue(Value);
        }

        protected override void SubscribeCore()
        {
            if (_reference.TryGetTarget(out var reference) && reference is not null)
            {
                PublishValue(reference.GetValue(_property));
                WeakEvents.AvaloniaPropertyChanged.Subscribe(reference, this);
            }
        }

        protected override void UnsubscribeCore()
        {
            if (_reference.TryGetTarget(out var reference) && reference is not null)
                WeakEvents.AvaloniaPropertyChanged.Unsubscribe(reference, this);
        }
    }

    private class InpcPropertyAccessor : PropertyAccessorBase, IWeakEventSubscriber<PropertyChangedEventArgs>
    {
        protected readonly WeakReference<object?> _reference;
        private readonly IPropertyInfo _property;

        public InpcPropertyAccessor(WeakReference<object?> reference, IPropertyInfo property)
        {
            _reference = reference ?? throw new ArgumentNullException(nameof(reference));
            _property = property ?? throw new ArgumentNullException(nameof(property));
        }

        public override Type PropertyType => _property.PropertyType;
        public override object? Value => _reference.TryGetTarget(out var o) ? _property.Get(o) : null;

        public override bool SetValue(object? value, BindingPriority priority)
        {
            if (_property.CanSet && _reference.TryGetTarget(out var o))
            {
                _property.Set(o, value);
                SendCurrentValue();
                return true;
            }
            return false;
        }

        public void OnEvent(object? sender, WeakEvent ev, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == _property.Name || string.IsNullOrEmpty(e.PropertyName))
                SendCurrentValue();
        }

        protected override void SubscribeCore()
        {
            SendCurrentValue();
            if (_reference.TryGetTarget(out var o) && o is INotifyPropertyChanged inpc)
                WeakEvents.ThreadSafePropertyChanged.Subscribe(inpc, this);
        }

        protected override void UnsubscribeCore()
        {
            if (_reference.TryGetTarget(out var o) && o is INotifyPropertyChanged inpc)
                WeakEvents.ThreadSafePropertyChanged.Unsubscribe(inpc, this);
        }

        protected void SendCurrentValue()
        {
            try
            {
                PublishValue(Value);
            }
            catch (Exception e)
            {
                PublishValue(new BindingNotification(e, BindingErrorType.Error));
            }
        }
    }

    private class IndexerAccessor : InpcPropertyAccessor, IWeakEventSubscriber<NotifyCollectionChangedEventArgs>
    {
        private readonly int _index;

        public IndexerAccessor(WeakReference<object?> target, IPropertyInfo basePropertyInfo, int argument)
            : base(target, basePropertyInfo)
        {
            _index = argument;
        }

        protected override void SubscribeCore()
        {
            base.SubscribeCore();
            if (_reference.TryGetTarget(out var o) && o is INotifyCollectionChanged incc)
                WeakEvents.CollectionChanged.Subscribe(incc, this);
        }

        protected override void UnsubscribeCore()
        {
            base.UnsubscribeCore();
            if (_reference.TryGetTarget(out var o) && o is INotifyCollectionChanged incc)
                WeakEvents.CollectionChanged.Unsubscribe(incc, this);
        }

        public void OnEvent(object? sender, WeakEvent ev, NotifyCollectionChangedEventArgs args)
        {
            if (ShouldNotifyListeners(args))
                SendCurrentValue();
        }

        private bool ShouldNotifyListeners(NotifyCollectionChangedEventArgs e)
        {
            return e.Action switch
            {
                NotifyCollectionChangedAction.Add => _index >= e.NewStartingIndex,
                NotifyCollectionChangedAction.Remove => _index >= e.OldStartingIndex,
                NotifyCollectionChangedAction.Replace => _index >= e.NewStartingIndex &&
                                                          _index < e.NewStartingIndex + e.NewItems!.Count,
                NotifyCollectionChangedAction.Move => (_index >= e.NewStartingIndex &&
                                                         _index < e.NewStartingIndex + e.NewItems!.Count) ||
                                                        (_index >= e.OldStartingIndex &&
                                                         _index < e.OldStartingIndex + e.OldItems!.Count),
                NotifyCollectionChangedAction.Reset => true,
                _ => false
            };
        }
    }
}
