using System.Collections.Generic;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX.Ast;
using XamlX.Transform;

namespace Avalonia.Markup.Xaml.Loader.CompilerExtensions.Transformers
{
    /// <summary>
    /// An XAMLIL AST transformer that injects <see cref="SourceInfo.SourceInfo"/> metadata into the generated XAML code.
    /// </summary>
    /// <remarks>
    /// This transformer runs during XAML compilation and attaches <see cref="SourceInfo.Source.SourceInfoProperty"/> 
    /// values to each created control node, allowing runtime and design-time tools to map visual elements 
    /// back to their original XAML source locations.
    /// <para/>
    /// The transformation is only applied when <see cref="CreateSourceInfo"/> is set to <c>true</c>, which 
    /// typically occurs when the MSBuild property <c>AvaloniaXamlCreateSourceInfo</c> is enabled 
    /// (for example, in Debug or design-time builds).
    /// <para/>
    /// Adding <see cref="SourceInfo.SourceInfo"/> helps tooling like the Avalonia designer or visual inspectors 
    /// jump directly to the defining <c>.axaml</c> file and line number of a selected element.
    /// </remarks>
    internal class AvaloniaXamlIlAddSourceInfoTransformer : IXamlAstTransformer
    {
        /// <summary>
        /// Gets or sets a value indicating whether source information should be generated
        /// and injected into the compiled XAML output.
        /// </summary>
        /// <remarks>
        /// This is usually enabled automatically by the build system when 
        /// <c>AvaloniaXamlCreateSourceInfo</c> is set.
        /// </remarks>
        public bool CreateSourceInfo { get; set; }

        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (CreateSourceInfo && node is XamlAstObjectNode objNode)
            {
                var avaloniaTypes = context.GetAvaloniaTypes();

                var clrType = objNode.Type.GetClrType();
                if (avaloniaTypes.Control.IsAssignableFrom(clrType))
                {
                    var line = node.Line;
                    var col = node.Position;

                    // Create a CLR property representation for the attached SourceInfo property.
                    var sourceProperty = new XamlAstClrProperty(node,
                        "SourceInfo",
                        avaloniaTypes.SourceInfoAttachedType,
                        null,
                        [avaloniaTypes.SourceInfoPropertySetter], null);

                    var sourceInfoTypeRef = new XamlAstClrTypeReference(node, avaloniaTypes.SourceInfoType, false);
                    XamlAstNewClrObjectNode valueNode;

                    if(context.Document != null)
                    {
                        valueNode = new XamlAstNewClrObjectNode(node, sourceInfoTypeRef, avaloniaTypes.SourceInfoConstructorFull,
                        [
                            new XamlConstantNode(node, avaloniaTypes.XamlIlTypes.Int32, line),
                            new XamlConstantNode(node, avaloniaTypes.XamlIlTypes.Int32, col),
                            new XamlConstantNode(node, avaloniaTypes.XamlIlTypes.String, context.Document)
                        ]);
                    }
                    else
                    {
                        valueNode = new XamlAstNewClrObjectNode(node, sourceInfoTypeRef, avaloniaTypes.SourceInfoConstructor,
                        [
                            new XamlConstantNode(node, avaloniaTypes.XamlIlTypes.Int32, line),
                            new XamlConstantNode(node, avaloniaTypes.XamlIlTypes.Int32, col)
                        ]);
                    }

                    var propNode = new XamlAstXamlPropertyValueNode(
                        node,
                        sourceProperty,
                        valueNode,
                        isAttributeSyntax: true);

                    objNode.Children.Add(propNode);
                }
            }

            return node;
        }
    }
}
