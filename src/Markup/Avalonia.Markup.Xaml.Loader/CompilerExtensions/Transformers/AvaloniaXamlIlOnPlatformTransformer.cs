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

            OnPlatformDefaultNode defaultValue = null;
            var values = new List<OnPlatformBranchNode>();

            var directives = objectNode.Children.OfType<XamlAstXmlDirective>().ToArray();

            foreach (var child in objectNode.Arguments.Take(1))
            {
                defaultValue = new OnPlatformDefaultNode(new XamlAstXamlPropertyValueNode(child, targetPropertyNode.Property, child));
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
                            values.Add( new OnPlatformBranchNode(
                                ConvertPlatformNode(platform.Trim().ToUpperInvariant(), onObj),
                                transformed));
                        }
                    }
                }
                else
                {
                    var propName = extProp.Property.GetClrProperty().Name.Trim().ToUpperInvariant();
                    var transformed = TransformNode(targetPropertyNode.Property, extProp.Values,
                        typeArgument, directives, extProp);
                    if (propName.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase))
                    {
                        defaultValue = new OnPlatformDefaultNode(transformed);
                    }
                    else if (propName != "CONTENT")
                    {
                        values.Add(new OnPlatformBranchNode(
                            ConvertPlatformNode(propName, extProp),
                            transformed));
                    }
                }
            }

            return new AvaloniaXamlIlConditionalNode(defaultValue, values, node);
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

    private sealed class OnPlatformBranchNode : AvaloniaXamlIlConditionalBranchNode
    {
        private IXamlAstNode _platform;
        private IXamlAstNode _value;

        public OnPlatformBranchNode(
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
            var osTypeEnum = context.GetAvaloniaTypes().OperatingSystemType;
            var isOSPlatformMethod = context.GetAvaloniaTypes().IsOnPlatformMethod;
            codeGen.Ldloc(context.ContextLocal);
            context.Emit(_platform, codeGen, osTypeEnum);
            codeGen.EmitCall(isOSPlatformMethod);
        }

        public override void EmitBody(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            context.Emit(_value, codeGen, null);
        }
    }

    private sealed class OnPlatformDefaultNode : AvaloniaXamlIlConditionalDefaultNode
    {
        private IXamlAstNode _value;

        public OnPlatformDefaultNode(
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

