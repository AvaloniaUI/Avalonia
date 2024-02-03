using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;

internal class AvaloniaXamlIlOptionMarkupExtensionTransformer : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        if (node is XamlMarkupExtensionNode
            {
                Value: XamlAstObjectNode { Type: XamlAstClrTypeReference { Type: { } type } } objectNode
            } markupExtensionNode
            && type.FindMethods(m => m.IsPublic && m.Parameters.Count is 1 or 2 && m.ReturnType == context.Configuration.WellKnownTypes.Boolean && m.Name == "ShouldProvideOption").ToArray() is { } methods
            && methods.Any())
        {
            var optionAttribute = context.GetAvaloniaTypes().MarkupExtensionOptionAttribute;
            var defaultOptionAttribute = context.GetAvaloniaTypes().MarkupExtensionDefaultOptionAttribute;

            var typeArgument = type.GenericArguments?.FirstOrDefault();

            IXamlAstValueNode defaultValue = null;
            var values = new List<OptionsMarkupExtensionBranch>();

            if (objectNode.Arguments.FirstOrDefault() is { } argument)
            {
                var hasDefaultProp = objectNode.Type.GetClrType().GetAllProperties().Any(p =>
                    p.CustomAttributes.Any(a => a.Type == defaultOptionAttribute));
                if (hasDefaultProp)
                {
                    if (objectNode.Arguments.Count > 1)
                    {
                        throw new XamlTransformException("Options MarkupExtensions allow only single argument", objectNode);
                    }

                    defaultValue = TransformNode(new[] { argument }, typeArgument, objectNode);
                    objectNode.Arguments.Remove(argument);
                }
            }

            foreach (var extProp in objectNode.Children.OfType<XamlAstXamlPropertyValueNode>().ToArray())
            {
                if (!extProp.Values.Any())
                {
                    continue;
                }

                var shouldRemoveProp = false;
                var onObjs = extProp.Values.OfType<XamlAstObjectNode>()
                    .Where(o => o.Type.GetClrType() == context.GetAvaloniaTypes().OnExtensionType).ToArray();
                if (onObjs.Any())
                {
                    shouldRemoveProp = true;
                    foreach (var onObj in onObjs)
                    {
                        var optionsPropNode = onObj.Children.OfType<XamlAstXamlPropertyValueNode>()
                            .SingleOrDefault(v => v.Property.GetClrProperty().Name == "Options")
                            ?.Values.Single();
                        var options = (optionsPropNode as XamlAstTextNode)?.Text?.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            ?? Array.Empty<string>();
                        if (options.Length == 0)
                        {
                            throw new XamlTransformException("On.Options string must be set", onObj);
                        }

                        var content = onObj.Children.OfType<XamlAstXamlPropertyValueNode>()
                            .SingleOrDefault(v => v.Property.GetClrProperty().Name == "Content");
                        if (content is null)
                        {
                            throw new XamlTransformException("On content object must be set", onObj);
                        }

                        var propertiesSet = options
                            .Select(o => type.GetAllProperties()
                                .FirstOrDefault(p => o.Equals(p.Name, StringComparison.Ordinal))
                                ?? throw new XamlTransformException($"Property \"{o}\" wasn't found on the \"{type.Name}\" type", onObj))
                            .ToArray();
                        foreach (var propertySet in propertiesSet)
                        {
                            AddBranchNode(content.Values, propertySet.CustomAttributes, content);
                        }
                    }
                }
                else
                {
                    shouldRemoveProp = AddBranchNode(extProp.Values, extProp.Property.GetClrProperty().CustomAttributes, extProp);
                }

                if (shouldRemoveProp)
                {
                    objectNode.Children.Remove(extProp);
                }
            }

            if (defaultValue is null && !values.Any())
            {
                throw new XamlTransformException("Options markup extension requires at least one option to be set", objectNode);
            }

            return new OptionsMarkupExtensionNode(
                markupExtensionNode, values.ToArray(), defaultValue,
                context.Configuration.TypeMappings.ServiceProvider);

            bool AddBranchNode(
                IReadOnlyCollection<IXamlAstValueNode> valueNodes,
                IReadOnlyCollection<IXamlCustomAttribute> propAttributes,
                IXamlLineInfo li)
            {
                var transformed = TransformNode(valueNodes, typeArgument, li);
                if (propAttributes.FirstOrDefault(a => a.Type == defaultOptionAttribute) is { } defAttr)
                {
                    defaultValue = transformed;
                    return true;
                }
                else if (propAttributes.FirstOrDefault(a => a.Type == optionAttribute) is { } optAttr)
                {
                    var option = optAttr.Parameters.Single();
                    if (option is null)
                    {
                        throw new XamlTransformException("MarkupExtension option must not be null", li);
                    }

                    var optionAsString = option.ToString();
                    IXamlAstValueNode optionNode = null;
                    foreach (var method in methods)
                    {
                        try
                        {
                            var targetType = method.Parameters.Last();
                            if (targetType.FullName == "System.Type")
                            {
                                if (option is IXamlType typeOption)
                                {
                                    optionNode = new XamlTypeExtensionNode(li,
                                        new XamlAstClrTypeReference(li, typeOption, false), targetType);
                                }
                            }
                            else if (targetType == context.Configuration.WellKnownTypes.String)
                            {
                                optionNode = new XamlConstantNode(li, targetType, optionAsString);
                            }
                            else if (targetType.IsEnum)
                            {
                                if (TypeSystemHelpers.TryGetEnumValueNode(targetType, optionAsString, li, false,
                                        out var enumConstantNode))
                                {
                                    optionNode = enumConstantNode;
                                }
                            }
                            else if (TypeSystemHelpers.ParseConstantIfTypeAllows(optionAsString, targetType, li,
                                         out var constantNode))
                            {
                                optionNode = constantNode;
                            }
                        }
                        catch (FormatException)
                        {
                            // try next method overload
                        }

                        if (optionNode is not null)
                        {
                            values.Add(new OptionsMarkupExtensionBranch(optionNode, transformed, method));
                            return true;
                        }
                    }

                    throw new XamlTransformException($"Option value \"{optionAsString}\" is not assignable to any of existing ShouldProvideOption methods", li);
                }

                return false;
            }
        }

        return node;

        IXamlAstValueNode TransformNode(
            IReadOnlyCollection<IXamlAstValueNode> values,
            IXamlType suggestedType,
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

            if (values.Count > 1)
            {
                throw new XamlTransformException("Options markup extension supports only a singular value", line);
            }

            return values.Single();
        }
    }

    internal sealed class OptionsMarkupExtensionNode : XamlMarkupExtensionNode, IXamlAstValueNode
    {
        private readonly IXamlType _contextParameter;

        public OptionsMarkupExtensionNode(
            XamlMarkupExtensionNode original,
            OptionsMarkupExtensionBranch[] branches,
            IXamlAstValueNode defaultNode,
            IXamlType contextParameter)
            : base(
            original.Value,
            new OptionsMarkupExtensionMethod(new OptionsMarkupExtensionNodesContainer(branches, defaultNode), original.Value.Type.GetClrType(), contextParameter),
            original.Value)
        {
            _contextParameter = contextParameter;
        }

        public new OptionsMarkupExtensionMethod ProvideValue => (OptionsMarkupExtensionMethod)base.ProvideValue;

        IXamlAstTypeReference IXamlAstValueNode.Type => new XamlAstClrTypeReference(this, ProvideValue.ReturnType, false);

        public override void VisitChildren(IXamlAstVisitor visitor)
        {
            ProvideValue.ExtensionNodeContainer.Visit(visitor);
            base.VisitChildren(visitor);
        }

        public bool ConvertToReturnType(AstTransformationContext context, IXamlType type, out OptionsMarkupExtensionNode res)
        {
            IXamlAstValueNode convertedDefaultNode = null;

            if (ProvideValue.ExtensionNodeContainer.DefaultNode is { } defaultNode)
            {
                if (!XamlTransformHelpers.TryGetCorrectlyTypedValue(context, defaultNode, type, out convertedDefaultNode))
                {
                    res = null;
                    return false;
                }
            }

            var convertedBranches = ProvideValue.ExtensionNodeContainer.Branches.Select(b => XamlTransformHelpers
                .TryGetCorrectlyTypedValue(context, b.Value, type, out var convertedValue) ?
                new OptionsMarkupExtensionBranch(b.Option, convertedValue, b.ConditionMethod) :
                null).ToArray();
            if (convertedBranches.Any(b => b is null))
            {
                res = null;
                return false;
            }

            res = new OptionsMarkupExtensionNode(this, convertedBranches, convertedDefaultNode, _contextParameter);
            return true;
        }
    }

    internal sealed class OptionsMarkupExtensionNodesContainer : XamlAstNode
    {
        public OptionsMarkupExtensionNodesContainer(
            OptionsMarkupExtensionBranch[] branches,
            IXamlAstValueNode defaultNode) : base(branches.FirstOrDefault()?.Value ?? defaultNode)
        {
            Branches = branches;
            DefaultNode = defaultNode;
        }

        public OptionsMarkupExtensionBranch[] Branches { get; }
        public IXamlAstValueNode DefaultNode { get; private set; }

        public override void VisitChildren(IXamlAstVisitor visitor)
        {
            VisitList(Branches, visitor);
            DefaultNode = (IXamlAstValueNode)DefaultNode?.Visit(visitor);
        }

        public IXamlType GetReturnType()
        {
            var types = Branches.Select(b => b.Value.Type);
            if (DefaultNode?.Type is { } type)
            {
                types = types.Concat(new [] { type });
            }
            return types.Select(t => t.GetClrType()).ToArray().GetCommonBaseClass();
        }
    }

    internal sealed class OptionsMarkupExtensionBranch : XamlAstNode
    {
        public OptionsMarkupExtensionBranch(IXamlAstValueNode option, IXamlAstValueNode value, IXamlMethod conditionMethod) : base(value)
        {
            Option = option;
            Value = value;
            ConditionMethod = conditionMethod;
        }

        public IXamlAstValueNode Option { get; set; }
        public IXamlAstValueNode Value { get; set; }
        public IXamlMethod ConditionMethod { get; }

        public bool HasContext => ConditionMethod.Parameters.Count > 1;

        public override void VisitChildren(IXamlAstVisitor visitor)
        {
            Option = (IXamlAstValueNode)Option.Visit(visitor);
            Value = (IXamlAstValueNode)Value.Visit(visitor);
        }
    }

    internal sealed class OptionsMarkupExtensionMethod : IXamlCustomEmitMethodWithContext<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public OptionsMarkupExtensionMethod(
            OptionsMarkupExtensionNodesContainer extensionNodeContainer,
            IXamlType declaringType,
            IXamlType contextParameter)
        {
            ExtensionNodeContainer = extensionNodeContainer;
            DeclaringType = declaringType;
            Parameters = extensionNodeContainer.Branches.Any(c => c.HasContext) ?
                new[] { contextParameter } :
                Array.Empty<IXamlType>();
        }

        public OptionsMarkupExtensionNodesContainer ExtensionNodeContainer { get; }

        public string Name => "ProvideValue";
        public bool IsPublic => true;
        public bool IsStatic => false;
        public IXamlType ReturnType => ExtensionNodeContainer.GetReturnType();
        public IReadOnlyList<IXamlType> Parameters { get; }
        public IXamlType DeclaringType { get; }
        public IXamlMethod MakeGenericMethod(IReadOnlyList<IXamlType> typeArguments) => throw new NotImplementedException();
        public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => Array.Empty<IXamlCustomAttribute>();

        public void EmitCall(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            // At this point this extension will be called from MarkupExtensionEmitter.
            // Since it's a "fake" method, we share stack and locals with parent method.
            // Real ProvideValue method would pop 2 parameters from the stack and return one. This method should do the same.
            // At this point we will have on stack:
            // - context (if parameters > 1)
            // - markup ext "@this" instance (always)
            // We always pop context from the stack, as this method decide by itself either context is needed.
            // We store "@this" as a local variable. But only if any conditional method is an instance method.
            IXamlLocal @this = null;
            if (Parameters.Count > 0)
            {
                codeGen.Pop();
            }
            if (ExtensionNodeContainer.Branches.Any(b => !b.ConditionMethod.IsStatic))
            {
                codeGen.Stloc(@this = codeGen.DefineLocal(DeclaringType));
            }
            else
            {
                codeGen.Pop();
            }

            // Iterate over all branches and push prepared locals into the stack if needed.
            var ret = codeGen.DefineLabel();
            foreach (var branch in ExtensionNodeContainer.Branches)
            {
                var next = codeGen.DefineLabel();
                codeGen.Emit(OpCodes.Nop);
                if (branch.HasContext)
                {
                    codeGen.Ldloc(context.ContextLocal);
                }
                if (!branch.ConditionMethod.IsStatic)
                {
                    codeGen.Ldloc(@this);
                }
                context.Emit(branch.Option, codeGen, branch.Option.Type.GetClrType());
                codeGen.EmitCall(branch.ConditionMethod);
                codeGen.Brfalse(next);

                context.Emit(branch.Value, codeGen, branch.Value.Type.GetClrType());
                codeGen.Br(ret);
                codeGen.MarkLabel(next);
            }

            if (ExtensionNodeContainer.DefaultNode is {} defaultNode)
            {
                // Nop is needed, otherwise Label wouldn't be set on nested CALL op (limitation of our IL validator).
                codeGen.Emit(OpCodes.Nop);
                context.Emit(defaultNode, codeGen, defaultNode.Type.GetClrType());
            }
            else
            {
                codeGen.EmitDefault(ReturnType);
            }

            codeGen.MarkLabel(ret);
        }

        public bool Equals(IXamlMethod other) => ReferenceEquals(this, other);
    }
}

