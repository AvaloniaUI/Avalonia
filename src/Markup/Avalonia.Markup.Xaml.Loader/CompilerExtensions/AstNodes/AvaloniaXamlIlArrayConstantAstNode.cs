using System.Collections.Generic;
using System.Reflection.Emit;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.AstNodes
{
    class AvaloniaXamlIlArrayConstantAstNode : XamlAstNode, IXamlAstValueNode, IXamlAstILEmitableNode
    {
        private readonly IXamlType _elementType;
        private readonly IReadOnlyList<IXamlAstValueNode> _values;

        public AvaloniaXamlIlArrayConstantAstNode(IXamlLineInfo lineInfo, IXamlType arrayType, IXamlType elementType, IReadOnlyList<IXamlAstValueNode> values) : base(lineInfo)
        {
            _elementType = elementType;
            _values = values;
            
            Type = new XamlAstClrTypeReference(lineInfo, arrayType, false);
        }

        public IXamlAstTypeReference Type { get; }
        
        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            codeGen.Ldc_I4(_values.Count)
                .Newarr(_elementType);

            for (var index = 0; index < _values.Count; index++)
            {
                var value = _values[index];

                codeGen
                    .Dup()
                    .Ldc_I4(index);

                context.Emit(value, codeGen, _elementType);

                if (value.Type.GetClrType() is { IsValueType: true } valTypeInObjArr)
                {
                    if (!_elementType.IsValueType)
                    {
                        codeGen.Box(valTypeInObjArr);
                    }
                    // It seems like ASM codegen for "stelem valuetype" and "stelem.i4" is identical,
                    // so we don't need to try to optimize it here.
                    codeGen.Emit(OpCodes.Stelem, valTypeInObjArr);
                }
                else
                {
                    codeGen.Stelem_ref();
                }
            }

            return XamlILNodeEmitResult.Type(0, Type.GetClrType());
        }
    }
}
