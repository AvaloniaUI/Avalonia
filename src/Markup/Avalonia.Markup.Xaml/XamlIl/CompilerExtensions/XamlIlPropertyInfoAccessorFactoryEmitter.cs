using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlIl.Ast;
using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions
{
    class XamlIlPropertyInfoAccessorFactoryEmitter
    {
        public XamlIlPropertyInfoAccessorFactoryEmitter(IXamlIlTypeBuilder indexerClosureType)
        {
        }

        public IXamlIlType EmitLoadInpcPropertyAccessorFactory(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            codeGen.Ldnull();
            EmitLoadPropertyAccessorFactory(context, codeGen, context.GetAvaloniaTypes().PropertyInfoAccessorFactory, "CreateInpcPropertyAccessor");
            return EmitCreateAccessorFactoryDelegate(context, codeGen);
        }

        public IXamlIlType EmitLoadAvaloniaPropertyAccessorFactory(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            codeGen.Ldnull();
            EmitLoadPropertyAccessorFactory(context, codeGen, context.GetAvaloniaTypes().PropertyInfoAccessorFactory, "CreateAvaloniaPropertyAccessor");
            return EmitCreateAccessorFactoryDelegate(context, codeGen);
        }

        private void EmitLoadPropertyAccessorFactory(XamlIlEmitContext context, IXamlIlEmitter codeGen, IXamlIlType type, string accessorFactoryName, bool isStatic = true)
        {
            var types = context.GetAvaloniaTypes();
            var weakReferenceType = context.Configuration.TypeSystem.GetType("System.WeakReference");
            FindMethodMethodSignature accessorFactorySignature = new FindMethodMethodSignature(accessorFactoryName, types.IPropertyAccessor, weakReferenceType, types.IPropertyInfo)
            {
                IsStatic = isStatic
            };
            codeGen.Ldftn(type.GetMethod(accessorFactorySignature));
        }

        public IXamlIlType EmitLoadIndexerAccessorFactory(XamlIlEmitContext context, IXamlIlEmitter codeGen, IXamlIlAstValueNode value)
        {
            const string IndexerClosureFactoryMethodName = "CreateAccessor";
            var intType = context.Configuration.TypeSystem.GetType("System.Int32");
            var indexerClosureType = context.GetAvaloniaTypes().IndexerPropertyAccessorHelper;

            context.Emit(value, codeGen, intType);
            codeGen.Newobj(indexerClosureType.FindConstructor(new List<IXamlIlType> { intType }));
            EmitLoadPropertyAccessorFactory(context, codeGen, indexerClosureType, IndexerClosureFactoryMethodName, isStatic: false);
            return EmitCreateAccessorFactoryDelegate(context, codeGen);
        }

        private IXamlIlType EmitCreateAccessorFactoryDelegate(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            var types = context.GetAvaloniaTypes();
            var weakReferenceType = context.Configuration.TypeSystem.GetType("System.WeakReference");
            var funcType = context.Configuration.TypeSystem.GetType("System.Func`3").MakeGenericType(
                            weakReferenceType,
                            types.IPropertyInfo,
                            types.IPropertyAccessor);
            codeGen.Newobj(funcType.Constructors.First(c =>
                                c.Parameters.Count == 2 &&
                                c.Parameters[0].Equals(context.Configuration.WellKnownTypes.Object)));
            return funcType;
        }
    }
}
