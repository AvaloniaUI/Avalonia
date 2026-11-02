using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX.Ast;
using XamlX.TypeSystem;
using XamlX.IL;
using XamlX.Emit;
using XamlX.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions
{
    class XamlIlClrPropertyInfoEmitter
    {
        private readonly IXamlTypeBuilder<IXamlILEmitter> _builder;

        private Dictionary<string, List<(IXamlProperty prop, IXamlMethod get)>> _fields
            = new Dictionary<string, List<(IXamlProperty prop, IXamlMethod get)>>();

        private Dictionary<string, List<(IXamlProperty prop, IXamlMethod get)>> _typedFields
            = new Dictionary<string, List<(IXamlProperty prop, IXamlMethod get)>>();

        private IXamlField? _boxedTrue;
        private IXamlField? _boxedFalse;

        public XamlIlClrPropertyInfoEmitter(IXamlTypeBuilder<IXamlILEmitter> builder)
        {
            _builder = builder;
        }

        /// <summary>
        /// Defines two cached boxed booleans on the generated per-assembly helper type, shared by
        /// all bool property getters in the assembly, so they don't allocate on every read (#21065).
        /// </summary>
        private IXamlField GetBooleanBoxField(bool value, XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context)
        {
            if (_boxedTrue is null || _boxedFalse is null)
            {
                var wellKnownTypes = context.Configuration.WellKnownTypes;
                _boxedTrue = _builder.DefineField(wellKnownTypes.Object,
                    context.Configuration.IdentifierGenerator.CreateIdentifierFromPreferredName("BooleanBox!True", 0),
                    XamlVisibility.Private, true);
                _boxedFalse = _builder.DefineField(wellKnownTypes.Object,
                    context.Configuration.IdentifierGenerator.CreateIdentifierFromPreferredName("BooleanBox!False", 0),
                    XamlVisibility.Private, true);

                var cctor = _builder.DefineConstructor(true);
                cctor.Generator
                    .Ldc_I4(1)
                    .Box(wellKnownTypes.Boolean)
                    .Stsfld(_boxedTrue)
                    .Ldc_I4(0)
                    .Box(wellKnownTypes.Boolean)
                    .Stsfld(_boxedFalse)
                    .Ret();
            }

            return value ? _boxedTrue : _boxedFalse;
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

        /// <summary>
        /// Searches a property info cache for a method which was already generated for a property.
        /// </summary>
        /// <param name="fields">The cache to search.</param>
        /// <param name="key">The cache key for the property, as returned by <see cref="GetKey"/>.</param>
        /// <param name="property">The property.</param>
        /// <param name="cached">
        /// When the method returns, contains the cache entries for <paramref name="key"/>. The list is
        /// created and added to <paramref name="fields"/> if it doesn't yet exist.
        /// </param>
        /// <returns>
        /// The method which returns the property info for <paramref name="property"/>, or null if no
        /// such method has been generated yet.
        /// </returns>
        static IXamlMethod? GetCachedPropertyInfoMethod(
            Dictionary<string, List<(IXamlProperty prop, IXamlMethod get)>> fields,
            string key,
            IXamlProperty property,
            out List<(IXamlProperty prop, IXamlMethod get)> cached)
        {
            if (!fields.TryGetValue(key, out cached!))
                fields[key] = cached = new List<(IXamlProperty prop, IXamlMethod get)>();

            foreach (var entry in cached)
            {
                if (
                    ((entry.prop.Getter == null && property.Getter == null) ||
                     entry.prop.Getter?.Equals(property.Getter) == true) &&
                    ((entry.prop.Setter == null && property.Setter == null) ||
                     entry.prop.Setter?.Equals(property.Setter) == true)
                )
                    return entry.get;
            }

            return null;
        }

        private static void EmitFunctionPointer(IXamlILEmitter emitter, IXamlMethod? method)
        {
            if (method == null)
                emitter.Ldc_I4(0).Emit(OpCodes.Conv_I);
            else
                emitter.Ldftn(method);
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

                if (GetCachedPropertyInfoMethod(_fields, key, property, out var lst) is { } cached)
                    return cached;

                var name = context.Configuration.IdentifierGenerator.CreateIdentifierFromPreferredName(key, lst.Count);
                
                var field = _builder.DefineField(types.IPropertyInfo, name.AppendSuffix("!Field"), XamlVisibility.Private, true);

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
                        new[] {types.XamlIlTypes.Object}, name.AppendSuffix("!Getter"), XamlVisibility.Private, true, false);
                if (getter != null)
                {
                    Load(property.Getter!, getter.Generator, !property.Getter!.IsStatic);
                    
                    getter.Generator.EmitCall(property.Getter);
                    var returnType = property.Getter.ReturnType;
                    if (returnType.Equals(context.Configuration.WellKnownTypes.Boolean))
                    {
                        var whenTrue = getter.Generator.DefineLabel();
                        getter.Generator
                            .Brtrue(whenTrue)
                            .Ldsfld(GetBooleanBoxField(false, context))
                            .Ret()
                            .MarkLabel(whenTrue)
                            .Ldsfld(GetBooleanBoxField(true, context));
                    }
                    else if (returnType.IsValueType)
                        getter.Generator.Box(returnType);
                    getter.Generator.Ret();
                }

                var setter = property.Setter == null ?
                    null :
                    _builder.DefineMethod(types.XamlIlTypes.Void,
                        new[] {types.XamlIlTypes.Object, types.XamlIlTypes.Object},
                        name.AppendSuffix("!Setter"), XamlVisibility.Private, true, false);
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
                    name.AppendSuffix("!Property"), XamlVisibility.Public, true, false);


                var factory = types.CompiledPropertyInfoFactory.GetMethod(m =>
                    m.Name == "GetProperty" && m.IsGenericMethod).MakeGenericMethod([property.PropertyType]);
                get.Generator
                    .Emit(OpCodes.Ldsflda, field)
                    .Ldstr(property.Name);

                EmitFunctionPointer(get.Generator, getter);
                EmitFunctionPointer(get.Generator, setter);
                get.Generator
                    .EmitCall(factory)
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

            IXamlMethod Get()
            {
                var key = GetKey(property, null);

                if (GetCachedPropertyInfoMethod(_typedFields, key, property, out var lst) is { } cached)
                    return cached;

                var name = context.Configuration.IdentifierGenerator.CreateIdentifierFromPreferredName(
                    key + "!Typed", lst.Count);

                var field = _builder.DefineField(typedPropertyInfoType, name.AppendSuffix("!Field"), XamlVisibility.Private, true);

                var getter = property.Getter == null
                    ? null
                    : _builder.DefineMethod(valueType, new[] { sourceType }, name.AppendSuffix("!Getter"), XamlVisibility.Private, true, false);
                if (getter != null)
                {
                    if (!property.Getter!.IsStatic)
                        getter.Generator.Ldarg_0();
                    getter.Generator.EmitCall(property.Getter).Ret();
                }

                var setter = property.Setter == null
                    ? null
                    : _builder.DefineMethod(types.XamlIlTypes.Void, new[] { sourceType, valueType }, name.AppendSuffix("!Setter"), XamlVisibility.Private, true, false);
                if (setter != null)
                {
                    if (!property.Setter!.IsStatic)
                        setter.Generator.Ldarg_0();
                    setter.Generator.Ldarg(1);
                    setter.Generator.EmitCall(property.Setter, true).Ret();
                }

                var get = _builder.DefineMethod(typedPropertyInfoType, Array.Empty<IXamlType>(),
                    name.AppendSuffix("!Property"), XamlVisibility.Public, true, false);

                var factory = types.CompiledPropertyInfoFactory.GetMethod(m =>
                    m.Name == "GetTypedProperty" && m.IsGenericMethod).MakeGenericMethod([sourceType, valueType]);
                get.Generator
                    .Emit(OpCodes.Ldsflda, field)
                    .Ldstr(property.Name);

                EmitFunctionPointer(get.Generator, getter);
                EmitFunctionPointer(get.Generator, setter);
                get.Generator
                    .EmitCall(factory)
                    .Ret();

                lst.Add((property, get));
                return get;
            }

            codeGen.EmitCall(Get());
            return typedPropertyInfoType;
        }
    }
}
