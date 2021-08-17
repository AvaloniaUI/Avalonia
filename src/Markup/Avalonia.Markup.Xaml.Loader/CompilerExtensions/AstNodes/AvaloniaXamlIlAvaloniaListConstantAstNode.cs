using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.AstNodes
{
    class AvaloniaXamlIlAvaloniaListConstantAstNode : XamlAstNode, IXamlAstValueNode, IXamlAstILEmitableNode
    {
        private readonly IXamlType _elementType;
        private readonly IReadOnlyList<IXamlAstValueNode> _values;
        private readonly IXamlConstructor _constructor;
        private readonly IXamlMethod _listAddMethod;
        private readonly IXamlMethod _listSetCapacityMethod;

        public AvaloniaXamlIlAvaloniaListConstantAstNode(IXamlLineInfo lineInfo, AvaloniaXamlIlWellKnownTypes types, IXamlType listType, IXamlType elementType, IReadOnlyList<IXamlAstValueNode> values) : base(lineInfo)
        {
            _constructor = listType.GetConstructor();
            _listAddMethod = listType.GetMethod(new FindMethodMethodSignature("Add", types.XamlIlTypes.Void, elementType));
            _listSetCapacityMethod = listType.GetMethod(new FindMethodMethodSignature("set_Capacity", types.XamlIlTypes.Void, types.Int));

            _elementType = elementType;
            _values = values;

            Type = new XamlAstClrTypeReference(lineInfo, listType, false);
        }

        public IXamlAstTypeReference Type { get; }
        
        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            codeGen.Newobj(_constructor);

            codeGen
                .Dup()
                .Ldc_I4(_values.Count)
                .EmitCall(_listSetCapacityMethod);
            
            foreach (var value in _values)
            {
                codeGen.Dup();
                
                context.Emit(value, codeGen, _elementType);
            
                codeGen.EmitCall(_listAddMethod);
            }
            
            return XamlILNodeEmitResult.Type(0, Type.GetClrType());
        }
    }
}
