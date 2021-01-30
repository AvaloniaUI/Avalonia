using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.AstNodes
{
    class AvaloniaXamlIlRelativePointAstNode : XamlAstNode, IXamlAstValueNode, IXamlAstILEmitableNode
    {
        private readonly Point _point;
        private readonly IXamlConstructor _constructor;
        private readonly RelativePoint _relativePoint;

        public AvaloniaXamlIlRelativePointAstNode(IXamlLineInfo lineInfo, IXamlType type, IXamlConstructor relativePointConstructor,
            RelativePoint relativePoint)
            : base(lineInfo)
        {
            _constructor = relativePointConstructor;
            _relativePoint = relativePoint;

            Type = new XamlAstClrTypeReference(lineInfo, type, false);
        }

        public IXamlAstTypeReference Type { get; }

        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            codeGen.Ldc_R8(_relativePoint.Point.X);
            codeGen.Ldc_R8(_relativePoint.Point.Y);
            codeGen.Ldc_I4((int)_relativePoint.Unit);
            codeGen.Newobj(_constructor);

            return XamlILNodeEmitResult.Type(0, Type.GetClrType());
        }
    }
}
