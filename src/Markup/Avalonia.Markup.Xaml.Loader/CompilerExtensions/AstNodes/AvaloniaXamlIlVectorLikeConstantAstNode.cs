using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.AstNodes
{
    class AvaloniaXamlIlVectorLikeConstantAstNode : XamlAstNode, IXamlAstValueNode, IXamlAstILEmitableNode
    {
        private readonly IXamlConstructor _constructor;
        private readonly double[] _values;

        public AvaloniaXamlIlVectorLikeConstantAstNode(IXamlLineInfo lineInfo, AvaloniaXamlIlWellKnownTypes types, IXamlType type, IXamlConstructor constructor, double[] values) : base(lineInfo)
        {
            var parameters = constructor.Parameters;

            if (parameters.Count != values.Length)
            {
                throw new XamlTypeSystemException($"Constructor that takes {values.Length} parameters is expected, got {parameters.Count} instead.");
            }

            var elementType = types.XamlIlTypes.Double;

            foreach (var parameter in parameters)
            {
                if (parameter != elementType)
                {
                    throw new XamlTypeSystemException($"Expected parameter of type {elementType}, got {parameter} instead.");
                }
            }
            
            _constructor = constructor;
            _values = values;
            
            Type = new XamlAstClrTypeReference(lineInfo, type, false);
        }
        
        public IXamlAstTypeReference Type { get; }
        
        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            foreach (var value in _values)
            {
                codeGen.Ldc_R8(value);
            }
            
            codeGen.Newobj(_constructor);
            
            return XamlILNodeEmitResult.Type(0, Type.GetClrType());
        }
    }
}
