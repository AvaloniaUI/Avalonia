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
        if (valueNode.Manipulation is not XamlObjectInitializationNode
            {
                Manipulation: XamlPropertyAssignmentNode { Property: { Name: "Source" } } sourceProperty
            })
        {
            return context.ParseError($"Source property must be set on the \"{nodeTypeName}\" node.", node);
        }

        if (sourceProperty.Values.OfType<XamlAstNewClrObjectNode>().FirstOrDefault() is not { } sourceUriNode
            || sourceUriNode.Type.GetClrType() != context.GetAvaloniaTypes().Uri
            || sourceUriNode.Arguments.FirstOrDefault() is not XamlConstantNode { Constant: string originalAssetPath })
        {
            // TODO: make it a compiler warning
            // Source value can be set with markup extension instead of the Uri object node, we don't support it here yet.
            return node;
        }

        if (originalAssetPath.StartsWith("avares://"))
        {
        }
        else if (originalAssetPath.StartsWith("/"))
        {
            var baseUrl = context.CurrentDocument.Uri ?? throw new InvalidOperationException("CurrentDocument URI is null.");
            originalAssetPath = baseUrl.Substring(0, baseUrl.LastIndexOf('/')) + originalAssetPath;
        }
        else
        {
            return context.ParseError(
                $"Avalonia supports only \"avares://\" sources or relative sources starting with \"/\" on the \"{nodeTypeName}\" node.",
                node);
        }
        
        AssetLoader.RegisterResUriParsers();

        originalAssetPath = Uri.UnescapeDataString(new Uri(originalAssetPath).AbsoluteUri);
        var assetPath = originalAssetPath.Replace("avares://", "");
        var assemblyNameSeparator = assetPath.IndexOf('/');
        var assembly = assetPath.Substring(0, assemblyNameSeparator);
        var fullTypeName = Path.GetFileNameWithoutExtension(assetPath.Replace('/', '.'));

        if (context.Documents.FirstOrDefault(d => string.Equals(d.Uri, originalAssetPath, StringComparison.InvariantCultureIgnoreCase)) is {} targetDocument)
        {
            if (targetDocument.ClassType is not null)
            {
                return FromType(context, targetDocument.ClassType, node);
            }

            if (targetDocument.BuildMethod is null)
            {
                return context.ParseError($"\"{originalAssetPath}\" cannot be instantiated.", node);
            }

            return FromMethod(context, targetDocument.BuildMethod, node);
        }


        if (context.Configuration.TypeSystem.FindAssembly(assembly) is not { } assetAssembly)
        {
            return context.ParseError($"Assembly \"{assembly}\" was not found from the \"{originalAssetPath}\" source.", node);
        }

        if (assetAssembly.FindType(fullTypeName) is { } type
            && type.FindMethod(m => m.Name == "!XamlIlPopulate") is not null)
        {
            return FromType(context, type, node);
        }
        else
        {
            var avaResType = assetAssembly.FindType("CompiledAvaloniaXaml.!AvaloniaResources");
            if (avaResType is null)
            {
                return context.ParseError(
                    $"Unable to resolve \"!AvaloniaResources\" type on \"{assembly}\" assembly.", node);
            }

            var relativeName = "Build:" + assetPath.Substring(assemblyNameSeparator);
            var buildMethod = avaResType.FindMethod(m => m.Name == relativeName);
            if (buildMethod is null)
            {
                return context.ParseError(
                    $"Unable to resolve build method \"{relativeName}\" resource on the \"{assembly}\" assembly.",
                    node);
            }

            return FromMethod(context, buildMethod, node);
        }
    }

    private static IXamlAstNode FromType(AstTransformationContext context, IXamlType type, IXamlLineInfo li)
    {
        IXamlAstNode newObjNode = new XamlAstObjectNode(li, new XamlAstClrTypeReference(li, type, false));
        newObjNode = new AvaloniaXamlIlConstructorServiceProviderTransformer().Transform(context, newObjNode);
        newObjNode = new ConstructableObjectTransformer().Transform(context, newObjNode);
        return new NewObjectTransformer().Transform(context, newObjNode);
    }

    private static IXamlAstNode FromMethod(AstTransformationContext context, IXamlMethod method, IXamlLineInfo li)
    {
        var sp = context.Configuration.TypeMappings.ServiceProvider;
        return new XamlStaticOrTargetedReturnMethodCallNode(li, method,
            new[] { new AvaloniaXamlIlConstructorServiceProviderTransformer.InjectServiceProviderNode(sp, li, false) });
    }
}
