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
        private bool _indexerClosureTypeInitialized = false;
        private readonly IXamlIlTypeBuilder _indexerClosureType;
        public XamlIlPropertyInfoAccessorFactoryEmitter(IXamlIlTypeBuilder indexerClosureType)
        {
            _indexerClosureType = indexerClosureType;
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

        private void EmitLoadPropertyAccessorFactory(XamlIlEmitContext context, IXamlIlEmitter codeGen, IXamlIlType type, string accessorFactoryName)
        {
            var types = context.GetAvaloniaTypes();
            var weakReferenceType = context.Configuration.TypeSystem.GetType("System.WeakReference");
            FindMethodMethodSignature accessorFactorySignature = new FindMethodMethodSignature(accessorFactoryName, types.IPropertyAccessor, weakReferenceType, types.IPropertyInfo)
            {
                IsStatic = true
            };
            codeGen.Ldftn(type.GetMethod(accessorFactorySignature));
        }

        public IXamlIlType EmitLoadIndexerAccessorFactory(XamlIlEmitContext context, IXamlIlEmitter codeGen, IXamlIlAstValueNode value)
        {
            const string indexerClosureFactoryMethodName = "CreateAccessor";
            var types = context.GetAvaloniaTypes();
            var intType = context.Configuration.TypeSystem.GetType("System.Int32");
            var weakReferenceType = context.Configuration.TypeSystem.GetType("System.WeakReference");
            if (!_indexerClosureTypeInitialized)
            {
                var indexAccessorFactoryMethod = context.GetAvaloniaTypes().PropertyInfoAccessorFactory.GetMethod(
                        new FindMethodMethodSignature(
                            "CreateIndexerPropertyAccessor",
                            types.IPropertyAccessor,
                            weakReferenceType,
                            types.IPropertyInfo,
                            intType)
                        {
                            IsStatic = true
                        });
                var indexField = _indexerClosureType.DefineField(intType, "_index", false, false);
                var ctor = _indexerClosureType.DefineConstructor(false, intType);
                ctor.Generator
                    .Ldarg_0()
                    .Stfld(indexField);
                _indexerClosureType.DefineMethod(
                    types.IPropertyAccessor,
                    new[] { weakReferenceType, types.IPropertyInfo },
                    indexerClosureFactoryMethodName,
                    isPublic: false,
                    isStatic: false,
                    isInterfaceImpl: false)
                    .Generator
                    .Ldarg_0()
                    .Ldarg(1)
                    .Ldfld(indexField)
                    .EmitCall(indexAccessorFactoryMethod);
            }

            context.Emit(value, codeGen, intType);
            codeGen.Newobj(_indexerClosureType.FindConstructor(new List<IXamlIlType> { intType }));
            EmitLoadPropertyAccessorFactory(context, codeGen, _indexerClosureType, indexerClosureFactoryMethodName);
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
