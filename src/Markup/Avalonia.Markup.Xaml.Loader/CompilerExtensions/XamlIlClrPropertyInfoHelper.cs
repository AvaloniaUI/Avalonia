using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX.Ast;
using XamlX.TypeSystem;
using XamlX.IL;
using XamlX.Emit;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions
{
    class XamlIlClrPropertyInfoEmitter
    {
        private readonly IXamlTypeBuilder<IXamlILEmitter> _builder;

        private Dictionary<string, List<(IXamlProperty prop, IXamlMethod get)>> _fields
            = new Dictionary<string, List<(IXamlProperty prop, IXamlMethod get)>>();

        private Dictionary<string, List<(IXamlProperty prop, IXamlMethod get)>> _typedFields
            = new Dictionary<string, List<(IXamlProperty prop, IXamlMethod get)>>();

        public XamlIlClrPropertyInfoEmitter(IXamlTypeBuilder<IXamlILEmitter> builder)
        {
            _builder = builder;
        }

        static string GetKey(IXamlProperty property, string? indexerArgumentsKey)
        {
            var declaringType = (property.Getter ?? property.Setter)?.DeclaringType
                ?? throw new InvalidOperationException($"Couldn't get declaring type for property {property}");

            var baseKey = declaringType.GetFullName() + "." + property.Name;

            if (indexerArgumentsKey is null)
            {
                return baseKey;
            }

            return baseKey + $"[{indexerArgumentsKey}]";
        }

        public IXamlType Emit(
            XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context,
            IXamlILEmitter codeGen,
            IXamlProperty property,
            IReadOnlyCollection<IXamlAstValueNode>? indexerArguments = null,
            string? indexerArgumentsKey = null)
        {
            indexerArguments ??= [];
            var types = context.GetAvaloniaTypes();
            IXamlMethod Get()
            {
                var key = GetKey(property, indexerArgumentsKey);
                if (!_fields.TryGetValue(key, out var lst))
                    _fields[key] = lst = new List<(IXamlProperty prop, IXamlMethod get)>();

                foreach (var cached in lst)
                {
                    if (
                        ((cached.prop.Getter == null && property.Getter == null) ||
                         cached.prop.Getter?.Equals(property.Getter) == true) &&
                        ((cached.prop.Setter == null && property.Setter == null) ||
                         cached.prop.Setter?.Equals(property.Setter) == true)
                    )
                        return cached.get;
                }

                var name = lst.Count == 0 ? key : key + "_" + context.Configuration.IdentifierGenerator.GenerateIdentifierPart();
                
                var field = _builder.DefineField(types.IPropertyInfo, name + "!Field", XamlVisibility.Private, true);

                void Load(IXamlMethod m, IXamlILEmitter cg, bool passThis)
                {
                    if (passThis)
                    {
                        cg
                            .Ldarg_0();
                        if (m.DeclaringType.IsValueType)
                            cg.Unbox(m.DeclaringType);
                        else
                            cg.Castclass(m.DeclaringType);
                    }

                    foreach (var indexerArg in indexerArguments)
                    {
                        context.Emit(indexerArg, cg, indexerArg.Type.GetClrType());
                    }
                }

                var getter = property.Getter == null ?
                    null :
                    _builder.DefineMethod(types.XamlIlTypes.Object,
                        new[] {types.XamlIlTypes.Object}, name + "!Getter", XamlVisibility.Private, true, false);
                if (getter != null)
                {
                    Load(property.Getter!, getter.Generator, !property.Getter!.IsStatic);
                    
                    getter.Generator.EmitCall(property.Getter);
                    if (property.Getter.ReturnType.IsValueType)
                        getter.Generator.Box(property.Getter.ReturnType);
                    getter.Generator.Ret();
                }

                var setter = property.Setter == null ?
                    null :
                    _builder.DefineMethod(types.XamlIlTypes.Void,
                        new[] {types.XamlIlTypes.Object, types.XamlIlTypes.Object},
                        name + "!Setter", XamlVisibility.Private, true, false);
                if (setter != null)
                {
                    Load(property.Setter!, setter.Generator, !property.Setter!.IsStatic);
                    
                    setter.Generator.Ldarg(1);

                    var valueIndex = indexerArguments.Count;
                    if (property.Setter.Parameters[valueIndex].IsValueType)
                        setter.Generator.Unbox_Any(property.Setter.Parameters[valueIndex]);
                    else
                        setter.Generator.Castclass(property.Setter.Parameters[valueIndex]);
                    setter.Generator
                        .EmitCall(property.Setter, true)
                        .Ret();
                }

                var get = _builder.DefineMethod(types.IPropertyInfo, Array.Empty<IXamlType>(),
                    name + "!Property", XamlVisibility.Public, true, false);


                var ctor = types.ClrPropertyInfo.Constructors.First(c =>
                    c.Parameters.Count == 4 && c.IsStatic == false);
                
                var cacheMiss = get.Generator.DefineLabel();
                get.Generator
                    .Ldsfld(field)
                    .Brfalse(cacheMiss)
                    .Ldsfld(field)
                    .Ret()
                    .MarkLabel(cacheMiss)
                    .Ldstr(property.Name);

                void EmitFunc(IXamlILEmitter emitter, IXamlMethod? method, IXamlType del)
                {
                    if (method == null)
                        emitter.Ldnull();
                    else
                    {
                        emitter
                            .Ldnull()
                            .Ldftn(method)
                            .Newobj(del.Constructors.First(c =>
                                c.Parameters.Count == 2 &&
                                c.Parameters[0].Equals(context.Configuration.WellKnownTypes.Object)));
                    }
                }

                EmitFunc(get.Generator, getter, ctor.Parameters[1]);
                EmitFunc(get.Generator, setter, ctor.Parameters[2]);
                get.Generator
                    .Ldtype(property.PropertyType)
                    .Newobj(ctor)
                    .Stsfld(field)
                    .Ldsfld(field)
                    .Ret();

                lst.Add((property, get));
                return get;
            }

            codeGen.EmitCall(Get());
            return types.IPropertyInfo;
        }

        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2122", Justification = TrimmingMessages.TypesInCoreOrAvaloniaAssembly)]
        public IXamlType EmitTyped(
            XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context,
            IXamlILEmitter codeGen,
            IXamlProperty property)
        {
            var types = context.GetAvaloniaTypes();
            var sourceType = property.Getter?.DeclaringType ?? property.Setter?.DeclaringType
                ?? throw new InvalidOperationException($"Couldn't get declaring type for property {property}");
            var valueType = property.PropertyType;

            var typedPropertyInfoType = types.IPropertyInfoT.MakeGenericType(sourceType, valueType);
            var typedClrPropertyInfoType = types.ClrPropertyInfoT.MakeGenericType(sourceType, valueType);
            var funcType = context.Configuration.WellKnownTypes.GetFuncOfT(2).MakeGenericType(sourceType, valueType);
            var actionType = context.Configuration.WellKnownTypes.GetActionOfT(2).MakeGenericType(sourceType, valueType);

            IXamlMethod Get()
            {
                var key = GetKey(property, null);
                if (!_typedFields.TryGetValue(key, out var lst))
                    _typedFields[key] = lst = new List<(IXamlProperty prop, IXamlMethod get)>();

                foreach (var cached in lst)
                {
                    if (
                        ((cached.prop.Getter == null && property.Getter == null) ||
                         cached.prop.Getter?.Equals(property.Getter) == true) &&
                        ((cached.prop.Setter == null && property.Setter == null) ||
                         cached.prop.Setter?.Equals(property.Setter) == true)
                    )
                        return cached.get;
                }

                var name = lst.Count == 0
                    ? key + "!Typed"
                    : key + "!Typed_" + context.Configuration.IdentifierGenerator.GenerateIdentifierPart();

                var field = _builder.DefineField(typedPropertyInfoType, name + "!Field", XamlVisibility.Private, true);

                var getter = property.Getter == null
                    ? null
                    : _builder.DefineMethod(valueType, new[] { sourceType }, name + "!Getter", XamlVisibility.Private, true, false);
                if (getter != null)
                {
                    if (!property.Getter!.IsStatic)
                        getter.Generator.Ldarg_0();
                    getter.Generator.EmitCall(property.Getter).Ret();
                }

                var setter = property.Setter == null
                    ? null
                    : _builder.DefineMethod(types.XamlIlTypes.Void, new[] { sourceType, valueType }, name + "!Setter", XamlVisibility.Private, true, false);
                if (setter != null)
                {
                    if (!property.Setter!.IsStatic)
                        setter.Generator.Ldarg_0();
                    setter.Generator.Ldarg(1);
                    setter.Generator.EmitCall(property.Setter, true).Ret();
                }

                var get = _builder.DefineMethod(typedPropertyInfoType, Array.Empty<IXamlType>(),
                    name + "!Property", XamlVisibility.Public, true, false);

                var ctor = typedClrPropertyInfoType.Constructors.First(c =>
                    c.Parameters.Count == 3 && !c.IsStatic);

                var cacheMiss = get.Generator.DefineLabel();
                get.Generator
                    .Ldsfld(field)
                    .Brfalse(cacheMiss)
                    .Ldsfld(field)
                    .Ret()
                    .MarkLabel(cacheMiss)
                    .Ldstr(property.Name);

                void EmitFunc(IXamlILEmitter emitter, IXamlMethod? method, IXamlType del)
                {
                    if (method == null)
                        emitter.Ldnull();
                    else
                    {
                        emitter
                            .Ldnull()
                            .Ldftn(method)
                            .Newobj(del.Constructors.First(c =>
                                c.Parameters.Count == 2 &&
                                c.Parameters[0].Equals(context.Configuration.WellKnownTypes.Object)));
                    }
                }

                EmitFunc(get.Generator, getter, funcType);
                EmitFunc(get.Generator, setter, actionType);
                get.Generator
                    .Newobj(ctor)
                    .Stsfld(field)
                    .Ldsfld(field)
                    .Ret();

                lst.Add((property, get));
                return get;
            }

            codeGen.EmitCall(Get());
            return typedPropertyInfoType;
        }
    }
}
