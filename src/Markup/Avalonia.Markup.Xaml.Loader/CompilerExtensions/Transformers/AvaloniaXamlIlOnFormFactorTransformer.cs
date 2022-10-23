using System;
using System.Collections.Generic;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;

internal class AvaloniaXamlIlOnFormFactorTransformer : IXamlAstTransformer
{
    private const string OnFormFactorFqn = "Avalonia.Markup.Xaml:Avalonia.Markup.Xaml.MarkupExtensions.OnFormFactorExtension";

    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        if (node is XamlAstXamlPropertyValueNode targetPropertyNode
            && targetPropertyNode.Values.OfType<XamlMarkupExtensionNode>().FirstOrDefault() is
            {
                Value: XamlAstObjectNode { Type: XamlAstClrTypeReference { Type: { } type } } objectNode
            }
            && type.GetFqn().StartsWith(OnFormFactorFqn))
        {
            var typeArgument = type.GenericArguments?.FirstOrDefault();

            OnFormFactorDefaultNode defaultValue = null;
            var values = new List<OnFormFactorBranchNode>();

            var directives = objectNode.Children.OfType<XamlAstXmlDirective>().ToArray();

            foreach (var child in objectNode.Arguments.Take(1))
            {
                defaultValue = new OnFormFactorDefaultNode(new XamlAstXamlPropertyValueNode(child, targetPropertyNode.Property, child));
            }

            foreach (var extProp in objectNode.Children.OfType<XamlAstXamlPropertyValueNode>())
            {
                var propName = extProp.Property.GetClrProperty().Name.Trim();
                var transformed = TransformNode(targetPropertyNode.Property, extProp.Values,
                    typeArgument, directives, extProp);
                if (propName.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase))
                {
                    defaultValue = new OnFormFactorDefaultNode(transformed);
                }
                else if (propName != "CONTENT")
                {
                    values.Add(new OnFormFactorBranchNode(
                        ConvertPlatformNode(propName, extProp),
                        transformed));
                }
            }

            return new AvaloniaXamlIlConditionalNode(defaultValue, values, node);
        }

        return node;

        XamlConstantNode ConvertPlatformNode(string propName, IXamlLineInfo li)
        {
            var enumType = context.GetAvaloniaTypes().FormFactorType;

            if (TypeSystemHelpers.TryGetEnumValueNode(enumType, propName, li, false, out var enumConstantNode))
            {
                return enumConstantNode;
            }

            throw new XamlParseException($"Unable to parse form factor name: \"{propName}\"", li);
        }

        XamlAstXamlPropertyValueNode TransformNode(
            IXamlAstPropertyReference property,
            IReadOnlyCollection<IXamlAstValueNode> values,
            IXamlType suggestedType,
            IReadOnlyCollection<XamlAstXmlDirective> directives,
            IXamlLineInfo line)
        {
            if (suggestedType is not null)
            {
                values = values
                    .Select(v => XamlTransformHelpers
                        .TryGetCorrectlyTypedValue(context, v, suggestedType, out var converted)
                        ? converted : v)
                    .ToArray();
            }

            if (directives.Any())
            {
                foreach (var value in values)
                {
                    if (value is XamlAstObjectNode xamlAstObjectNode)
                    {
                        xamlAstObjectNode.Children.AddRange(directives);
                    }
                }
            }

            return new XamlAstXamlPropertyValueNode(line, property, values);
        }
    }

    private sealed class OnFormFactorBranchNode : AvaloniaXamlIlConditionalBranchNode
    {
        private IXamlAstNode _platform;
        private IXamlAstNode _value;

        public OnFormFactorBranchNode(
            IXamlAstNode platform,
            IXamlAstNode value)
            : base(value)
        {
            _platform = platform;
            _value = value;
        }

        public override void VisitChildren(IXamlAstVisitor visitor)
        {
            _platform = _platform.Visit(visitor);
            _value = _value.Visit(visitor);
        }

        public override void EmitCondition(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            var enumType = context.GetAvaloniaTypes().FormFactorType;
            var isOnFormFactorMethod = context.GetAvaloniaTypes().IsOnFormFactorMethod;
            codeGen.Ldloc(context.ContextLocal);
            context.Emit(_platform, codeGen, enumType);
            codeGen.EmitCall(isOnFormFactorMethod);
        }

        public override void EmitBody(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            context.Emit(_value, codeGen, null);
        }
    }

    private sealed class OnFormFactorDefaultNode : AvaloniaXamlIlConditionalDefaultNode
    {
        private IXamlAstNode _value;

        public OnFormFactorDefaultNode(
            IXamlAstNode value)
            : base(value)
        {
            _value = value;
        }

        public override void VisitChildren(IXamlAstVisitor visitor)
        {
            _value = _value.Visit(visitor);
        }

        public override void EmitBody(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            context.Emit(_value, codeGen, null);
        }
    }
}
