using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using Avalonia.Platform;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.GroupTransformers;

#nullable enable
internal class AvaloniaXamlIncludeTransformer : IXamlAstGroupTransformer
{
    public IXamlAstNode Transform(AstGroupTransformationContext context, IXamlAstNode node)
    {
        if (node is not XamlValueWithManipulationNode valueNode
            || valueNode.Value is not XamlAstNewClrObjectNode objectNode
            || (objectNode.Type.GetClrType() != context.GetAvaloniaTypes().StyleInclude
                && objectNode.Type.GetClrType() != context.GetAvaloniaTypes().ResourceInclude))
        {
            return node;
        }

        var nodeTypeName = objectNode.Type.GetClrType().Name;
        var expectedLoadedType = objectNode.Type.GetClrType().GetAllProperties()
            .FirstOrDefault(p => p.Name == "Loaded")?.PropertyType;
        if (expectedLoadedType is null)
        {
            throw new InvalidOperationException($"\"{nodeTypeName}\".Loaded property is expected to be defined");
        }

        if (valueNode.Manipulation is not XamlObjectInitializationNode initializationNode)
        {
            throw new InvalidOperationException($"Invalid \"{nodeTypeName}\" node initialization.");
        }

        var additionalProperties = new List<IXamlAstManipulationNode>();
        if (initializationNode.Manipulation is not XamlPropertyAssignmentNode { Property: { Name: "Source" } } sourceProperty)
        {
            if (initializationNode.Manipulation is XamlManipulationGroupNode manipulationGroup
                && manipulationGroup.Children.OfType<XamlPropertyAssignmentNode>()
                    .FirstOrDefault(p => p.Property.Name == "Source") is { } sourceProperty2)
            {
                sourceProperty = sourceProperty2;
                // We need to copy some additional properties from ResourceInclude to ResourceDictionary except the Source one.
                // If there is any missing properties, then XAML compiler will throw an error in the emitter code.
                additionalProperties = manipulationGroup.Children.Where(c => c != sourceProperty2).ToList();
            }
            else
            {
                context.ReportTransformError(
                    $"Source property must be set on the \"{nodeTypeName}\" node.", valueNode);
                return node;
            }
        }

        var (assetPathUri, sourceUriNode) = ResolveSourceFromXamlInclude(context, nodeTypeName, sourceProperty, false);
        if (assetPathUri is null)
        {
            return node;
        }
        else
        {
            sourceUriNode ??= valueNode;
        }

        var assetPath = assetPathUri.Replace("avares://", "");
        var assemblyNameSeparator = assetPath.IndexOf('/');
        var assembly = assetPath.Substring(0, assemblyNameSeparator);
        var fullTypeName = Path.GetFileNameWithoutExtension(assetPath.Replace('/', '.'));

        // Search file in the current assembly among other XAML resources.
        if (context.Documents.FirstOrDefault(d => string.Equals(d.Uri, assetPathUri, StringComparison.InvariantCultureIgnoreCase)) is {} targetDocument)
        {
            if (targetDocument.BuildMethod is not null)
            {
                return FromMethod(context, targetDocument.BuildMethod, sourceUriNode, expectedLoadedType, node, assetPathUri, assembly, additionalProperties);
            }

            if (targetDocument.ClassType is not null)
            {
                return FromType(context, targetDocument.ClassType, sourceUriNode, expectedLoadedType, node, assetPathUri, assembly, additionalProperties);
            }

            context.ReportTransformError(
                $"Unable to resolve XAML resource \"{assetPathUri}\" in the current assembly.",
                sourceUriNode);
            return node;
        }

        // If resource wasn't found in the current assembly, search in the others.
        if (context.Configuration.TypeSystem.FindAssembly(assembly) is not { } assetAssembly)
        {
            context.ReportTransformError($"Assembly \"{assembly}\" was not found from the \"{assetPathUri}\" source.", sourceUriNode);
            return node;
        }

        var avaResType = assetAssembly.FindType("CompiledAvaloniaXaml.!AvaloniaResources");
        if (avaResType is null)
        {
            context.ReportTransformError(
                $"Unable to resolve \"!AvaloniaResources\" type on \"{assembly}\" assembly.", sourceUriNode);
            return node;
        }

        var relativeName = "Build:" + assetPath.Substring(assemblyNameSeparator);
        var buildMethod = avaResType.FindMethod(m => m.Name == relativeName && m.IsPublic);
        if (buildMethod is not null)
        {
            return FromMethod(context, buildMethod, sourceUriNode, expectedLoadedType, node, assetPathUri, assembly, additionalProperties);
        }
        else if (assetAssembly.FindType(fullTypeName) is { } type)
        {
            return FromType(context, type, sourceUriNode, expectedLoadedType, node, assetPathUri, assembly, additionalProperties);
        }

        context.ReportTransformError(
            $"Unable to resolve XAML resource \"{assetPathUri}\" in the \"{assembly}\" assembly. Make sure this file exists and is public.",
            sourceUriNode);
        return node;
    }

    private static IXamlAstNode FromType(AstGroupTransformationContext context, IXamlType type, IXamlAstNode li,
        IXamlType expectedLoadedType, IXamlAstNode fallbackNode, string assetPathUri, string assembly,
        IEnumerable<IXamlAstManipulationNode> manipulationNodes)
    {
        if (!expectedLoadedType.IsAssignableFrom(type))
        {
            context.ReportTransformError(
                $"Resource \"{assetPathUri}\" is defined as \"{type}\" type in the \"{assembly}\" assembly, but expected \"{expectedLoadedType}\".",
                li);
            return fallbackNode;
        }

        IXamlAstNode newObjNode = new XamlAstObjectNode(li, new XamlAstClrTypeReference(li, type, false));
        ((XamlAstObjectNode)newObjNode).Children.AddRange(manipulationNodes);
        newObjNode = new AvaloniaXamlIlConstructorServiceProviderTransformer().Transform(context, newObjNode);
        newObjNode = new ConstructableObjectTransformer().Transform(context, newObjNode);
        return new NewObjectTransformer().Transform(context, newObjNode);
    }

    private static IXamlAstNode FromMethod(AstGroupTransformationContext context, IXamlMethod method, IXamlAstNode li,
        IXamlType expectedLoadedType, IXamlAstNode fallbackNode, string assetPathUri, string assembly,
        IEnumerable<IXamlAstManipulationNode> manipulationNodes)
    {
        if (!expectedLoadedType.IsAssignableFrom(method.ReturnType))
        {
            context.ReportTransformError(
                $"Resource \"{assetPathUri}\" is defined as \"{method.ReturnType}\" type in the \"{assembly}\" assembly, but expected \"{expectedLoadedType}\".",
                li);
            return fallbackNode;
        }
        
        var sp = context.Configuration.TypeMappings.ServiceProvider;

        return new XamlValueWithManipulationNode(li,
            new XamlStaticOrTargetedReturnMethodCallNode(li, method,
                new[] { new NewServiceProviderNode(sp, li) }),
            new XamlManipulationGroupNode(li, manipulationNodes));
    }
    
    internal static (string?, IXamlAstNode?) ResolveSourceFromXamlInclude(
        AstGroupTransformationContext context, string nodeTypeName, XamlPropertyAssignmentNode sourceProperty,
        bool strictSourceValueType)
    {
        void OnInvalidSource(IXamlAstNode? node) =>
            context.ReportDiagnostic(
                AvaloniaXamlDiagnosticCodes.TransformError,
                strictSourceValueType ? XamlDiagnosticSeverity.Error : XamlDiagnosticSeverity.Warning,
                $"\"{nodeTypeName}.Source\" supports only \"avares://\" absolute or relative uri. This {nodeTypeName} will be resolved in runtime instead.",
                node);
        
        // We expect that AvaloniaXamlIlLanguageParseIntrinsics has already parsed the Uri and created node like: `new Uri(assetPath, uriKind)`.
        if (sourceProperty.Values.OfType<XamlAstNewClrObjectNode>().FirstOrDefault() is not { } sourceUriNode
            || sourceUriNode.Type.GetClrType() != context.GetAvaloniaTypes().Uri
            || sourceUriNode.Arguments.FirstOrDefault() is not XamlConstantNode { Constant: string originalAssetPath }
            || sourceUriNode.Arguments.Skip(1).FirstOrDefault() is not XamlConstantNode { Constant: int uriKind })
        {
            // Source value can be set with markup extension instead of the Uri object node, we don't support it here yet.
            var anyPropValue = sourceProperty.Values.FirstOrDefault();
            OnInvalidSource(anyPropValue);
            return (null, anyPropValue);
        }

        var uriPath = new Uri(originalAssetPath, (UriKind)uriKind);
        if (!uriPath.IsAbsoluteUri)
        {
            var baseUrl = context.CurrentDocument.Uri ?? throw new InvalidOperationException("CurrentDocument URI is null.");
            uriPath = new Uri(new Uri(baseUrl, UriKind.Absolute), uriPath);
        }
        else if (!uriPath.Scheme.Equals("avares", StringComparison.CurrentCultureIgnoreCase))
        {
            OnInvalidSource(sourceUriNode);
            return (null, sourceUriNode);
        }

        return (Uri.UnescapeDataString(uriPath.AbsoluteUri), sourceUriNode);
    }
    
    private class NewServiceProviderNode : XamlAstNode, IXamlAstValueNode,IXamlAstNodeNeedsParentStack,
        IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public NewServiceProviderNode(IXamlType type, IXamlLineInfo lineInfo) : base(lineInfo)
        {
            Type = new XamlAstClrTypeReference(lineInfo, type, false);
        }

        public IXamlAstTypeReference Type { get; }
        public bool NeedsParentStack => true;
        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            codeGen.Ldloc(context.ContextLocal);
            var method = context.GetAvaloniaTypes().RuntimeHelpers
                .FindMethod(m => m.Name == "CreateRootServiceProviderV3");
            codeGen.EmitCall(method);

            return XamlILNodeEmitResult.Type(0, Type.GetClrType());
        }
    }
}
