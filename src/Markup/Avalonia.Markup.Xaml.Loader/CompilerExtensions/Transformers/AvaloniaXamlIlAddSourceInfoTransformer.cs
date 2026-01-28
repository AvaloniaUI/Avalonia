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
    /// This transformer runs during XAML compilation and attaches <see cref="Avalonia.Markup.Xaml.Diagnostics.XamlSourceInfo"/> 
    /// values to each created control node, allowing runtime and design-time tools to map visual elements 
    /// back to their original XAML source locations.
    /// <para/>
    /// The transformation is only applied when <see cref="CreateSourceInfo"/> is set to <c>true</c>, which 
    /// typically occurs when the MSBuild property <c>AvaloniaXamlCreateSourceInfo</c> is enabled 
    /// (for example, in Debug or design-time builds).
    /// <para/>
    /// Adding <see cref="Avalonia.Markup.Xaml.Diagnostics.XamlSourceInfo"/> helps tooling like the Avalonia designer or visual inspectors 
    /// jump directly to the defining <c>.axaml</c> file and line number of a selected element.
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
                && context.ParentNodes().FirstOrDefault() is not XamlSourceInfoValueWithManipulationNode
                && !objNode.Type.GetClrType().IsValueType)
            {
                var avaloniaTypes = context.GetAvaloniaTypes();

                return new XamlSourceInfoValueWithManipulationNode(
                    avaloniaTypes, objNode, context.Document);
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

        private class XamlSourceInfoValueWithManipulationNode(
            AvaloniaXamlIlWellKnownTypes avaloniaTypes,
            XamlAstNewClrObjectNode objNode, string? document)
            : XamlValueWithManipulationNode(objNode, objNode, new XamlSourceInfoValueManipulation(avaloniaTypes, objNode, document)),
                IXamlAstImperativeNode, IXamlAstILEmitableNode, IXamlAstManipulationNode
        {
            public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
            {
                context.Emit(Value, codeGen, objNode.Type.GetClrType());
                context.Emit(Manipulation!, codeGen, null);

                return XamlILNodeEmitResult.Void(0);
            }
        }
    }
}
