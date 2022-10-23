using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.IL.Emitters;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;

internal class AvaloniaXamlIlOnPlatformTransformer : IXamlAstTransformer
{
    private const string OnPlatformFqn = "Avalonia.Markup.Xaml:Avalonia.Markup.Xaml.MarkupExtensions.OnPlatformExtension";
    private const string OnFqn = "Avalonia.Markup.Xaml:Avalonia.Markup.Xaml.MarkupExtensions.On";

    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        if (node is XamlAstXamlPropertyValueNode targetPropertyNode
            && targetPropertyNode.Values.OfType<XamlMarkupExtensionNode>().FirstOrDefault() is
            {
                Value: XamlAstObjectNode { Type: XamlAstClrTypeReference { Type: { } type } } objectNode
            }
            && type.GetFqn().StartsWith(OnPlatformFqn))
        {
            var typeArgument = type.GenericArguments?.FirstOrDefault();
            var targetType = typeArgument ?? objectNode.Type.GetClrType();
            if (targetType is null)
            {
                throw new XamlParseException(
                    "Unable to find OnPlatform property type. Try to set x:TypeArguments on the markup extension.",
                    node);
            }

            IXamlAstNode defaultValue = null;
            var values = new Dictionary<XamlConstantNode, IXamlAstNode>();

            var directives = objectNode.Children.OfType<XamlAstXmlDirective>().ToArray();

            foreach (var child in objectNode.Arguments.Take(1))
            {
                defaultValue = new XamlAstXamlPropertyValueNode(child, targetPropertyNode.Property, child);
            }

            foreach (var extProp in objectNode.Children.OfType<XamlAstXamlPropertyValueNode>())
            {
                var onObjs = extProp.Values.OfType<XamlAstObjectNode>()
                    .Where(o => o.Type.GetClrType().GetFqn() == OnFqn).ToArray();
                if (onObjs.Any())
                {
                    foreach (var onObj in onObjs)
                    {
                        var platformStr = (onObj.Children.OfType<XamlAstXamlPropertyValueNode>()
                                .SingleOrDefault(v => v.Property.GetClrProperty().Name == "Platform")
                                ?.Values.Single() as XamlAstTextNode)?
                            .Text;
                        if (string.IsNullOrWhiteSpace(platformStr))
                        {
                            throw new XamlParseException("On.Platform string must be set", onObj);
                        }

                        var content = onObj.Children.OfType<XamlAstXamlPropertyValueNode>()
                            .SingleOrDefault(v => v.Property.GetClrProperty().Name == "Content");
                        if (content is null)
                        {
                            throw new XamlParseException("On content object must be set", onObj);
                        }

                        var transformed = TransformNode(targetPropertyNode.Property, content.Values,
                            typeArgument, directives, content);
                        foreach (var platform in platformStr.Split(new[] { ',' },
                                     StringSplitOptions.RemoveEmptyEntries))
                        {
                            values.Add(ConvertPlatformNode(platform.Trim().ToUpperInvariant(), onObj), transformed);
                        }
                    }
                }
                else
                {
                    var platformStr = extProp.Property.GetClrProperty().Name.Trim().ToUpperInvariant();
                    var transformed = TransformNode(targetPropertyNode.Property, extProp.Values,
                        typeArgument, directives, extProp);
                    if (platformStr.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase))
                    {
                        defaultValue = transformed;
                    }
                    else if (platformStr != "CONTENT")
                    {
                        values.Add(ConvertPlatformNode(platformStr, extProp), transformed);
                    }
                }
            }

            return new XamlIlOnPlatformExtensionNode(
                defaultValue, values,
                new XamlAstClrTypeReference(node, targetType, false),
                node);
        }

        return node;

        XamlConstantNode ConvertPlatformNode(string platform, IXamlLineInfo li)
        {
            var osTypeEnum = context.GetAvaloniaTypes().OperatingSystemType;
            if (platform.Equals("MACOS", StringComparison.OrdinalIgnoreCase))
            {
                platform = "OSX";
            }
            if (platform.Equals("WINDOWS", StringComparison.OrdinalIgnoreCase))
            {
                platform = "WINNT";
            }

            if (TypeSystemHelpers.TryGetEnumValueNode(osTypeEnum, platform, li, true, out var enumConstantNode))
            {
                return enumConstantNode;
            }

            throw new XamlParseException($"Unable to parse platform name: \"{platform}\"", li);
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

    private sealed class XamlIlOnPlatformExtensionNode : XamlAstNode, IXamlAstValueNode,
        IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>, IXamlAstManipulationNode
    {
        private IXamlAstNode _defaultValue;
        private readonly IXamlAstNode[] _values;
        private readonly XamlConstantNode[] _valuePlatforms;

        public XamlIlOnPlatformExtensionNode(
            IXamlAstNode defaultValue,
            IDictionary<XamlConstantNode, IXamlAstNode> values,
            IXamlAstTypeReference targetType,
            IXamlLineInfo info) : base(info)
        {
            _defaultValue = defaultValue;
            _values = values.Values.ToArray();
            _valuePlatforms = values.Keys.ToArray();
            Type = targetType;
        }

        public override void VisitChildren(IXamlAstVisitor visitor)
        {
            _defaultValue = _defaultValue?.Visit(visitor);
            VisitList(_values, visitor);
            VisitList(_valuePlatforms, visitor);
        }

        public IXamlAstTypeReference Type { get; }

        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context,
            IXamlILEmitter codeGen)
        {
            var osTypeEnum = context.GetAvaloniaTypes().OperatingSystemType;
            var isOSPlatformMethod = context.GetAvaloniaTypes().IsOnPlatformMethod;

            var ret = codeGen.DefineLabel();

            for (var index = 0; index < _valuePlatforms.Length; index++)
            {
                var platform = _valuePlatforms[index];
                var propertyNode = _values[index];

                var next = codeGen.DefineLabel();
                codeGen.Ldloc(context.ContextLocal);
                context.Emit(platform, codeGen, osTypeEnum);
                codeGen.EmitCall(isOSPlatformMethod);
                codeGen.Brfalse(next);
                context.Emit(propertyNode, codeGen, null);
                codeGen.Br(ret);
                codeGen.MarkLabel(next);
            }

            if (_defaultValue is not null)
            {
                codeGen.Emit(OpCodes.Nop);
                context.Emit(_defaultValue, codeGen, null);
            }
            else
            {
                codeGen.Pop();
            }

            codeGen.MarkLabel(ret);

            return XamlILNodeEmitResult.Void(1);
        }
    }
}
