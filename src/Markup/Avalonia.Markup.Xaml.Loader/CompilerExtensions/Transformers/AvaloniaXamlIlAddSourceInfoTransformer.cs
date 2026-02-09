using System.Linq;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;

namespace Avalonia.Markup.Xaml.Loader.CompilerExtensions.Transformers
{
    /// <summary>
    /// An XAMLIL AST transformer that injects <see cref="Avalonia.Markup.Xaml.Diagnostics.XamlSourceInfo"/> metadata into the generated XAML code.
    /// </summary>
    /// <remarks>
    /// This transformer wraps object creation nodes with a manipulation node that adds source information.
    /// This source information includes line number, position, and document name, which can be useful for debugging and diagnostics.
    /// Note: ResourceDictionary source info is handled separately in <see cref="AvaloniaXamlResourceTransformer"/>.
    /// </remarks>
    internal class AvaloniaXamlIlAddSourceInfoTransformer : IXamlAstTransformer
    {
        /// <summary>
        /// Gets or sets a value indicating whether source information should be generated
        /// and injected into the compiled XAML output.
        /// </summary>
        public bool CreateSourceInfo { get; set; }

        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (CreateSourceInfo
                && node is XamlAstNewClrObjectNode objNode
                && context.ParentNodes().FirstOrDefault() is not XamlValueWithManipulationNode { Manipulation: XamlSourceInfoValueManipulation }
                && !objNode.Type.GetClrType().IsValueType)
            {
                var avaloniaTypes = context.GetAvaloniaTypes();

                return new XamlValueWithManipulationNode(
                    objNode, objNode,
                    new XamlSourceInfoValueManipulation(avaloniaTypes, objNode, context.Document));
            }

            return node;
        }

        private class XamlSourceInfoValueManipulation(
            AvaloniaXamlIlWellKnownTypes avaloniaTypes,
            XamlAstNewClrObjectNode objNode, string? document)
            : XamlAstNode(objNode), IXamlAstManipulationNode, IXamlAstILEmitableNode
        {
            public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
            {
                // Target object is already on stack.

                // var info = new XamlSourceInfo(Line, Position, Document);
                codeGen.Ldc_I4(Line);
                codeGen.Ldc_I4(Position);
                if (document is not null)
                    codeGen.Ldstr(document);
                else
                    codeGen.Ldnull();
                codeGen.Newobj(avaloniaTypes.XamlSourceInfoConstructor);

                // Set the XamlSourceInfo property on the current object
                // XamlSourceInfo.SetValue(@this, info);
                codeGen.EmitCall(avaloniaTypes.XamlSourceInfoSetter);

                return XamlILNodeEmitResult.Void(1);
            }
        }
    }
}
