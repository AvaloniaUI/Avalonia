using System.Collections.Generic;
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
        /// <remarks>
        /// This is usually enabled automatically by the build system when 
        /// <c>AvaloniaXamlCreateSourceInfo</c> is set.
        /// </remarks>
        public bool CreateSourceInfo { get; set; }

        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (CreateSourceInfo
                && node is XamlAstNewClrObjectNode objNode
                && context.ParentNodes().FirstOrDefault() is not XamlSourceInfoImperativeValueManipulation
                && !objNode.Type.GetClrType().IsValueType)
            {
                var avaloniaTypes = context.GetAvaloniaTypes();

                if(context.Document != null)
                {
                    return new XamlSourceInfoImperativeValueManipulation(
                        avaloniaTypes, objNode, context.Document);
                }
            }

            return node;
        }

        private class XamlSourceInfoImperativeValueManipulation(
            AvaloniaXamlIlWellKnownTypes avaloniaTypes,
            XamlAstNewClrObjectNode objNode, string document)
            : XamlAstNode(objNode), IXamlAstValueNode, IXamlAstImperativeNode, IXamlAstILEmitableNode
        {
            public IXamlAstTypeReference Type { get; } = objNode.Type;

            public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
            {
                context.Emit(objNode, codeGen, objNode.Type.GetClrType());
                // Duplicate the current object reference on the stack
                codeGen.Dup();

                // var local = new XamlSourceInfo(Line, Position, Document);
                codeGen.Ldc_I4(Line);
                codeGen.Ldc_I4(Position);
                codeGen.Ldstr(document);
                codeGen.Newobj(avaloniaTypes.XamlSourceInfoConstructor);

                // Set the XamlSourceInfo property on the current object
                codeGen.EmitCall(avaloniaTypes.XamlSourceInfoSetter);

                return XamlILNodeEmitResult.Type(0, objNode.Type.GetClrType());
            }
        }
    }
}
