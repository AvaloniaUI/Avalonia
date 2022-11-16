using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;

internal class AvaloniaXamlIlAssetIncludeTransformer : IXamlAstTransformer
{
    private const string StyleIncludeName = "StyleInclude";
    private const string ResourceIncludeName = "ResourceInclude";

    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        if (node is not XamlAstObjectNode objectNode
            || objectNode.Type.GetClrType() is not {Name: StyleIncludeName or ResourceIncludeName} objectNodeType)
        {
            return node;
        }

        var sourceProperty = objectNode.Children.OfType<XamlAstXamlPropertyValueNode>().FirstOrDefault(n => n.Property.GetClrProperty().Name == "Source");
        var directives = objectNode.Children.OfType<XamlAstXmlDirective>().ToList();
        if (sourceProperty is null
            || objectNode.Children.Count != (directives.Count + 1))
        {
            // Don't transform node with any other property, as we don't know how to transform them.
            return node;
        }

        if (sourceProperty.Values.OfType<XamlAstTextNode>().FirstOrDefault() is not { } sourceTextNode)
        {
            // Source value can be set with markup extension instead of a text node, we don't support it here yet.
            return node;
        }

        var originalAssetPath = sourceTextNode.Text;
        if (!(originalAssetPath.StartsWith("avares://") || originalAssetPath.StartsWith("/")))
        {
            // Only "avares" protocol supported or relative paths.
            return node;
        }

        var runtimeHelpers = context.Configuration.TypeSystem.FindType("Avalonia.Markup.Xaml.XamlIl.Runtime.XamlIlRuntimeHelpers");
        var markerMethodName = "Resolve" + objectNodeType.Name;
        var markerMethod = runtimeHelpers.FindMethod(m => m.Name == markerMethodName && m.Parameters.Count == 3);
        if (markerMethod is null)
        {
            throw new XamlParseException($"Marker method \"{markerMethodName}\" was not found for the \"{objectNodeType.Name}\" node", node);
        }

        return new XamlValueWithManipulationNode(
            node,
            new AssetIncludeMethodNode(node, markerMethod, originalAssetPath),
            new XamlManipulationGroupNode(node, directives));
    }

    private class AssetIncludeMethodNode : XamlAstNode, IXamlAstValueNode, IXamlAstILEmitableNode
    {
        private readonly IXamlMethod _method;
        private readonly string _originalAssetPath;

        public AssetIncludeMethodNode(
            IXamlAstNode original, IXamlMethod method, string originalAssetPath)
            : base(original)
        {
            _method = method;
            _originalAssetPath = originalAssetPath;
        }

        public IXamlAstTypeReference Type => new XamlAstClrTypeReference(this, _method.ReturnType, false);

        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            var absoluteSource = _originalAssetPath;
            if (absoluteSource.StartsWith("/"))
            {
                // Avoid Uri class here to avoid potential problems with escaping.
                // Keeping string as close to the original as possible.
                var absoluteBaseUrl =  context.RuntimeContext.BaseUrl;
                absoluteSource = absoluteBaseUrl.Substring(0, absoluteBaseUrl.LastIndexOf('/')) + absoluteSource;
            }

            codeGen.Ldstr(absoluteSource);
            codeGen.Ldc_I4(Line);
            codeGen.Ldc_I4(Position);
            codeGen.EmitCall(_method);

            return XamlILNodeEmitResult.Type(0, _method.ReturnType);
        }
    }
}
