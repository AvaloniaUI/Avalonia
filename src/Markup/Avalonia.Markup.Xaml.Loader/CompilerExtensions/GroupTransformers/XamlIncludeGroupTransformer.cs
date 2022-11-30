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
        
        if (valueNode.Manipulation is not XamlObjectInitializationNode
            {
                Manipulation: XamlPropertyAssignmentNode { Property: { Name: "Source" } } sourceProperty
            })
        {
            return context.ParseError($"Source property must be set on the \"{nodeTypeName}\" node.", node);
        }

        // We expect that AvaloniaXamlIlLanguageParseIntrinsics has already parsed the Uri and created node like: `new Uri(assetPath, uriKind)`.
        if (sourceProperty.Values.OfType<XamlAstNewClrObjectNode>().FirstOrDefault() is not { } sourceUriNode
            || sourceUriNode.Type.GetClrType() != context.GetAvaloniaTypes().Uri
            || sourceUriNode.Arguments.FirstOrDefault() is not XamlConstantNode { Constant: string originalAssetPath }
            || sourceUriNode.Arguments.Skip(1).FirstOrDefault() is not XamlConstantNode { Constant: int uriKind })
        {
            // TODO: make it a compiler warning
            // Source value can be set with markup extension instead of the Uri object node, we don't support it here yet.
            return node;
        }

        var uriPath = new Uri(originalAssetPath, (UriKind)uriKind);
        if (!uriPath.IsAbsoluteUri)
        {
            var baseUrl = context.CurrentDocument.Uri ?? throw new InvalidOperationException("CurrentDocument URI is null.");
            uriPath = new Uri(new Uri(baseUrl, UriKind.Absolute), uriPath);
        }
        else if (!uriPath.Scheme.Equals("avares", StringComparison.CurrentCultureIgnoreCase))
        {
            return context.ParseError(
                $"\"{nodeTypeName}.Source\" supports only \"avares://\" absolute or relative uri.",
                sourceUriNode, node);
        }

        var assetPathUri = Uri.UnescapeDataString(uriPath.AbsoluteUri);
        var assetPath = assetPathUri.Replace("avares://", "");
        var assemblyNameSeparator = assetPath.IndexOf('/');
        var assembly = assetPath.Substring(0, assemblyNameSeparator);
        var fullTypeName = Path.GetFileNameWithoutExtension(assetPath.Replace('/', '.'));

        // Search file in the current assembly among other XAML resources.
        if (context.Documents.FirstOrDefault(d => string.Equals(d.Uri, assetPathUri, StringComparison.InvariantCultureIgnoreCase)) is {} targetDocument)
        {
            if (targetDocument.BuildMethod is not null)
            {
                return FromMethod(context, targetDocument.BuildMethod, sourceUriNode, expectedLoadedType, node, assetPathUri, assembly);
            }

            if (targetDocument.ClassType is not null)
            {
                return FromType(context, targetDocument.ClassType, sourceUriNode, expectedLoadedType, node, assetPathUri, assembly);
            }

            return context.ParseError(
                $"Unable to resolve XAML resource \"{assetPathUri}\" in the current assembly.",
                sourceUriNode, node);
        }

        // If resource wasn't found in the current assembly, search in the others.
        if (context.Configuration.TypeSystem.FindAssembly(assembly) is not { } assetAssembly)
        {
            return context.ParseError($"Assembly \"{assembly}\" was not found from the \"{assetPathUri}\" source.", sourceUriNode, node);
        }

        var avaResType = assetAssembly.FindType("CompiledAvaloniaXaml.!AvaloniaResources");
        if (avaResType is null)
        {
            return context.ParseError(
                $"Unable to resolve \"!AvaloniaResources\" type on \"{assembly}\" assembly.", sourceUriNode, node);
        }

        var relativeName = "Build:" + assetPath.Substring(assemblyNameSeparator);
        var buildMethod = avaResType.FindMethod(m => m.Name == relativeName);
        if (buildMethod is not null)
        {
            return FromMethod(context, buildMethod, sourceUriNode, expectedLoadedType, node, assetPathUri, assembly);
        }
        else if (assetAssembly.FindType(fullTypeName) is { } type)
        {
            return FromType(context, type, sourceUriNode, expectedLoadedType, node, assetPathUri, assembly);
        }

        return context.ParseError(
            $"Unable to resolve XAML resource \"{assetPathUri}\" in the \"{assembly}\" assembly.",
            sourceUriNode, node);
    }
    
    private static IXamlAstNode FromType(AstTransformationContext context, IXamlType type, IXamlAstNode li,
        IXamlType expectedLoadedType, IXamlAstNode fallbackNode, string assetPathUri, string assembly)
    {
        if (!expectedLoadedType.IsAssignableFrom(type))
        {
            return context.ParseError(
                $"Resource \"{assetPathUri}\" is defined as \"{type}\" type in the \"{assembly}\" assembly, but expected \"{expectedLoadedType}\".",
                li, fallbackNode);
        }
        
        IXamlAstNode newObjNode = new XamlAstObjectNode(li, new XamlAstClrTypeReference(li, type, false));
        newObjNode = new AvaloniaXamlIlConstructorServiceProviderTransformer().Transform(context, newObjNode);
        newObjNode = new ConstructableObjectTransformer().Transform(context, newObjNode);
        return new NewObjectTransformer().Transform(context, newObjNode);
    }

    private static IXamlAstNode FromMethod(AstTransformationContext context, IXamlMethod method, IXamlAstNode li,
        IXamlType expectedLoadedType, IXamlAstNode fallbackNode, string assetPathUri, string assembly)
    {
        if (!expectedLoadedType.IsAssignableFrom(method.ReturnType))
        {
            return context.ParseError(
                $"Resource \"{assetPathUri}\" is defined as \"{method.ReturnType}\" type in the \"{assembly}\" assembly, but expected \"{expectedLoadedType}\".",
                li, fallbackNode);
        }
        
        var sp = context.Configuration.TypeMappings.ServiceProvider;
        return new XamlStaticOrTargetedReturnMethodCallNode(li, method,
            new[] { new NewServiceProviderNode(sp, li) });
    }
    
    internal class NewServiceProviderNode : XamlAstNode, IXamlAstValueNode,IXamlAstNodeNeedsParentStack,
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
            var method = context.GetAvaloniaTypes().RuntimeHelpers
                .FindMethod(m => m.Name == "CreateRootServiceProviderV2");
            codeGen.EmitCall(method);

            return XamlILNodeEmitResult.Type(0, Type.GetClrType());
        }
    }
}
