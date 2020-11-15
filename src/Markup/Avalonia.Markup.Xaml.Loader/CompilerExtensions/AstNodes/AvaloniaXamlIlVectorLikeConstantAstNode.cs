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

        public AvaloniaXamlIlVectorLikeConstantAstNode(IXamlLineInfo lineInfo, IXamlType type, IXamlConstructor constructor, double[] values) : base(lineInfo)
        {
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
