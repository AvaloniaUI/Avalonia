using System.Collections.Generic;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.IL.Emitters;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    /// <summary>
    /// Handles x:Immutable directive on objects, which have an immutable counterpart.
    /// SolidColorBrush is directly constructed as an immutable brush.
    /// Other types are created as usual mutable objects and converted with their ToImmutable method.
    /// </summary>
    class AvaloniaXamlIlImmutableTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is not XamlAstObjectNode obj
                || obj.Children.Find(d => d is XamlAstXmlDirective { Namespace: XamlNamespaces.Xaml2006, Name: "Immutable" })
                    is not XamlAstXmlDirective directive)
            {
                return node;
            }

            obj.Children.Remove(directive);

            if (!(directive.Values.Count == 1
                  && directive.Values[0] is XamlAstTextNode text
                  && bool.TryParse(text.Text, out var isImmutable)))
            {
                return context.ReportTransformError("The value of x:Immutable must be a literal boolean value.", directive, node);
            }

            var types = context.GetAvaloniaTypes();
            var type = obj.Type.GetClrType();
            var toImmutable = FindToImmutableMethod(type);
            var isSolidColorBrush = type.Equals(types.SolidColorBrush);

            if (!isSolidColorBrush && toImmutable is null)
            {
                return context.ReportTransformError($"x:Immutable is not supported on type {type.GetFqn()}.", directive, node);
            }

            if (!isImmutable)
            {
                return node;
            }

            var visitor = new MarkupExtensionFinder(types.BindingBase);
            obj.VisitChildren(visitor);
            if (visitor.Binding is { } binding)
            {
                return context.ReportTransformError(
                    $"Bindings and dynamic resources are not supported inside of an x:Immutable object ({binding.Type.GetClrType().GetFqn()}).",
                    binding, node);
            }

            if (isSolidColorBrush && TryCreateSolidColorBrush(context, types, obj) is { } created)
            {
                var directives = obj.Children.OfType<XamlAstXmlDirective>().ToList();

                // Immutable objects with compile-time known values are cheaper to create than to defer.
                // Markup extensions, such as StaticResource, are still resolved lazily.
                // Respect x:Shared="False", which requires deferral.
                var canSkipDeferral = !visitor.HasMarkupExtensions
                    && !directives.Any(d => d is { Namespace: XamlNamespaces.Xaml2006, Name: "Shared" });
                return new AvaloniaXamlIlImmutableObjectNode(created, directives, canSkipDeferral);
            }

            if (toImmutable is null)
            {
                return context.ReportTransformError(
                    $"Unable to create immutable {type.GetFqn()} from the specified properties.", directive, node);
            }

            // Text content is converted to a value by the type converter, which might return an immutable object already.
            if (obj.Children.Any(IsTextContent))
            {
                return context.ReportTransformError(
                    $"Text content is not supported on x:Immutable {type.GetFqn()} object.", directive, node);
            }

            // ToImmutable is called on a fully initialized mutable object, which might still be deferred as any other resource.
            return new AvaloniaXamlIlToImmutableNode(obj, toImmutable);
        }

        private static IXamlAstValueNode? TryCreateSolidColorBrush(
            AstTransformationContext context, AvaloniaXamlIlWellKnownTypes types, XamlAstObjectNode obj)
        {
            // ImmutableSolidColorBrush(Color color, double opacity, ImmutableTransform? transform, ImmutableTransform? relativeTransform)
            var ctor = types.ImmutableSolidColorBrushConstructor;
            var arguments = new IXamlAstValueNode?[4];

            foreach (var child in obj.Children)
            {
                if (child is XamlAstXmlDirective || IsWhitespace(child))
                {
                    continue;
                }

                var (index, value) = child switch
                {
                    XamlAstTextNode textContent => (0, textContent),
                    XamlAstXamlPropertyValueNode { Property: XamlAstClrProperty property } propertyNode
                        when GetSingleValue(propertyNode) is { } propertyValue => (property.Name switch
                        {
                            "Color" => 0,
                            "Opacity" => 1,
                            "Transform" => 2,
                            "RelativeTransform" => 3,
                            _ => -1
                        }, propertyValue),
                    _ => (-1, null)
                };

                if (index < 0 || arguments[index] is not null
                    || ConvertArgument(context, value!, ctor.Parameters[index]) is not { } argument)
                {
                    return null;
                }

                arguments[index] = argument;
            }

            if (arguments[0] is null)
            {
                return null;
            }

            arguments[1] ??= new XamlConstantNode(obj, types.XamlIlTypes.Double, 1.0);
            arguments[2] ??= new XamlNullExtensionNode(obj);
            arguments[3] ??= new XamlNullExtensionNode(obj);

            return new XamlAstNewClrObjectNode(obj,
                new XamlAstClrTypeReference(obj, types.ImmutableSolidColorBrush, false), ctor, arguments.ToList()!);
        }

        private static IXamlAstValueNode? ConvertArgument(
            AstTransformationContext context, IXamlAstValueNode value, IXamlType parameterType)
        {
            var valueType = value.Type.GetClrType();
            var targetType = parameterType.GenericTypeDefinition?.Equals(context.Configuration.WellKnownTypes.NullableT) == true
                ? parameterType.GenericArguments[0]
                : parameterType;

            // Nested objects, such as transforms, are created as usual and converted with their ToImmutable method.
            if (value is XamlAstObjectNode nested)
            {
                return FindToImmutableMethod(valueType) is { } toImmutable
                       && targetType.IsAssignableFrom(toImmutable.ReturnType)
                    ? new AvaloniaXamlIlToImmutableNode(nested, toImmutable)
                    : null;
            }

            // Markup extensions, which return an object, are casted on emit.
            if (targetType.IsValueType && valueType.Equals(context.Configuration.WellKnownTypes.Object))
            {
                return value;
            }

            return XamlTransformHelpers.TryGetCorrectlyTypedValue(context, value, targetType, out var converted)
                ? converted
                : null;
        }

        private static IXamlAstValueNode? GetSingleValue(XamlAstXamlPropertyValueNode propertyNode)
        {
            var values = propertyNode.Values.Where(v => !IsWhitespace(v)).ToList();
            return values.Count == 1 ? values[0] : null;
        }

        private static bool IsWhitespace(IXamlAstNode node) =>
            node is XamlAstTextNode text && WhitespaceNormalization.IsWhitespace(text.Text);

        private static bool IsTextContent(IXamlAstNode node) =>
            node is XamlAstTextNode text && !WhitespaceNormalization.IsWhitespace(text.Text);

        private static IXamlMethod? FindToImmutableMethod(IXamlType type)
        {
            return type.FindMethod(m => m.Name == "ToImmutable"
                                        && m.IsPublic
                                        && !m.IsStatic
                                        && m.Parameters.Count == 0
                                        && m.ReturnType.FullName != "System.Void");
        }

        private class MarkupExtensionFinder : IXamlAstVisitor
        {
            private readonly IXamlType _bindingBase;

            public MarkupExtensionFinder(IXamlType bindingBase) => _bindingBase = bindingBase;

            public IXamlAstValueNode? Binding { get; private set; }

            public bool HasMarkupExtensions { get; private set; }

            public IXamlAstNode Visit(IXamlAstNode node)
            {
                if (node is XamlMarkupExtensionNode)
                {
                    HasMarkupExtensions = true;
                }

                if (Binding is null
                    && node is IXamlAstValueNode value
                    && value.Type is XamlAstClrTypeReference { Type: { } type }
                    && _bindingBase.IsAssignableFrom(type))
                {
                    Binding = value;
                }

                return node;
            }

            public void Push(IXamlAstNode node) { }

            public void Pop() { }
        }
    }

    /// <summary>
    /// Directly constructed immutable object, with its x:Key and x:Shared directives preserved.
    /// </summary>
    sealed class AvaloniaXamlIlImmutableObjectNode : XamlValueWithManipulationNode
    {
        public AvaloniaXamlIlImmutableObjectNode(IXamlAstValueNode value, List<XamlAstXmlDirective> directives, bool canSkipDeferral)
            : base(value, new ParentStackScopeNode(value), new XamlManipulationGroupNode(value, directives))
        {
            CanSkipDeferral = canSkipDeferral;
        }

        public bool CanSkipDeferral { get; }

        /// <summary>
        /// Markup extensions in constructor arguments verify that the parent stack was analyzed for them.
        /// It's normally done by the object initialization of a parent, which immutable objects don't have,
        /// so the whole subtree is analyzed before emitting it.
        /// </summary>
        private sealed class ParentStackScopeNode : XamlValueWithSideEffectNodeBase,
            IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
        {
            public ParentStackScopeNode(IXamlAstValueNode value) : base(value, value)
            {
            }

            public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
            {
                context.GetOrCreateItem<XamlNeedsParentStackCache>().NeedsParentStack(Value);
                context.Emit(Value, codeGen, Value.Type.GetClrType());
                return XamlILNodeEmitResult.Type(0, Value.Type.GetClrType());
            }
        }
    }

    /// <summary>
    /// Calls ToImmutable method on the created mutable object.
    /// </summary>
    sealed class AvaloniaXamlIlToImmutableNode : XamlValueWithSideEffectNodeBase,
        IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public AvaloniaXamlIlToImmutableNode(IXamlAstValueNode value, IXamlMethod toImmutableMethod)
            : base(value, value)
        {
            ToImmutableMethod = toImmutableMethod;
        }

        public IXamlMethod ToImmutableMethod { get; }

        public override IXamlAstTypeReference Type => new XamlAstClrTypeReference(this, ToImmutableMethod.ReturnType, false);

        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            context.Emit(Value, codeGen, Value.Type.GetClrType());
            codeGen.EmitCall(ToImmutableMethod);
            return XamlILNodeEmitResult.Type(0, ToImmutableMethod.ReturnType);
        }
    }
}
