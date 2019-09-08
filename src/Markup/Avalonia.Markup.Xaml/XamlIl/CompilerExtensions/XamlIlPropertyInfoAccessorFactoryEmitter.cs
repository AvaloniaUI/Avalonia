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
        private const string IndexerClosureFactoryMethodName = "CreateAccessor";
        private readonly IXamlIlTypeBuilder _indexerClosureTypeBuilder;
        private IXamlIlType _indexerClosureType;
        public XamlIlPropertyInfoAccessorFactoryEmitter(IXamlIlTypeBuilder indexerClosureType)
        {
            _indexerClosureTypeBuilder = indexerClosureType;
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
            var weakReferenceType = context.Configuration.TypeSystem.GetType("System.WeakReference`1").MakeGenericType(context.Configuration.WellKnownTypes.Object);
            FindMethodMethodSignature accessorFactorySignature = new FindMethodMethodSignature(accessorFactoryName, types.IPropertyAccessor, weakReferenceType, types.IPropertyInfo)
            {
                IsStatic = isStatic
            };
            codeGen.Ldftn(type.GetMethod(accessorFactorySignature));
        }

        public IXamlIlType EmitLoadIndexerAccessorFactory(XamlIlEmitContext context, IXamlIlEmitter codeGen, IXamlIlAstValueNode value)
        {
            var intType = context.Configuration.TypeSystem.GetType("System.Int32");
            if (_indexerClosureType is null)
            {
                _indexerClosureType = InitializeClosureType(context);
            }

            context.Emit(value, codeGen, intType);
            codeGen.Newobj(_indexerClosureType.FindConstructor(new List<IXamlIlType> { intType }));
            EmitLoadPropertyAccessorFactory(context, codeGen, _indexerClosureType, IndexerClosureFactoryMethodName, isStatic: false);
            return EmitCreateAccessorFactoryDelegate(context, codeGen);
        }

        private IXamlIlType InitializeClosureType(XamlIlEmitContext context)
        {
            var types = context.GetAvaloniaTypes();
            var intType = context.Configuration.TypeSystem.GetType("System.Int32");
            var weakReferenceType = context.Configuration.TypeSystem.GetType("System.WeakReference`1").MakeGenericType(context.Configuration.WellKnownTypes.Object);
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
            var indexField = _indexerClosureTypeBuilder.DefineField(intType, "_index", false, false);
            var ctor = _indexerClosureTypeBuilder.DefineConstructor(false, intType);
            ctor.Generator
                .Ldarg_0()
                .Ldarg(1)
                .Stfld(indexField)
                .Ret();
            _indexerClosureTypeBuilder.DefineMethod(
                types.IPropertyAccessor,
                new[] { weakReferenceType, types.IPropertyInfo },
                IndexerClosureFactoryMethodName,
                isPublic: true,
                isStatic: false,
                isInterfaceImpl: false)
                .Generator
                .Ldarg(1)
                .Ldarg(2)
                .LdThisFld(indexField)
                .EmitCall(indexAccessorFactoryMethod)
                .Ret();

            return _indexerClosureTypeBuilder.CreateType();
        }

        private IXamlIlType EmitCreateAccessorFactoryDelegate(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            var types = context.GetAvaloniaTypes();
            var weakReferenceType = context.Configuration.TypeSystem.GetType("System.WeakReference`1").MakeGenericType(context.Configuration.WellKnownTypes.Object);
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
