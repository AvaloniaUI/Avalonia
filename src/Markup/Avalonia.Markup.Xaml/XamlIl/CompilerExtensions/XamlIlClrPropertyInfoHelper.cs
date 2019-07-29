using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlIl.Ast;
using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions
{
    class XamlIlClrPropertyInfoEmitter
    {
        private readonly IXamlIlTypeBuilder _builder;

        private Dictionary<string, List<(IXamlIlProperty prop, IXamlIlMethod get)>> _fields
            = new Dictionary<string, List<(IXamlIlProperty prop, IXamlIlMethod get)>>();
        
        public XamlIlClrPropertyInfoEmitter(IXamlIlTypeBuilder builder)
        {
            _builder = builder;
        }

        static string GetKey(IXamlIlProperty property, string indexerArgumentsKey)
        {
            var baseKey = property.Getter.DeclaringType.GetFullName() + "." + property.Name;

            if (indexerArgumentsKey is null)
            {
                return baseKey;
            }

            return baseKey + $"[{indexerArgumentsKey}]";
        }

        public IXamlIlType Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen, IXamlIlProperty property, IEnumerable<IXamlIlAstValueNode> indexerArguments = null, string indexerArgumentsKey = null)
        {
            indexerArguments = indexerArguments ?? Enumerable.Empty<IXamlIlAstValueNode>();
            var types = context.GetAvaloniaTypes();
            IXamlIlMethod Get()
            {
                var key = GetKey(property, indexerArgumentsKey);
                if (!_fields.TryGetValue(key, out var lst))
                    _fields[key] = lst = new List<(IXamlIlProperty prop, IXamlIlMethod get)>();

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

                var name = lst.Count == 0 ? key : key + "_" + Guid.NewGuid().ToString("N");
                
                var field = _builder.DefineField(types.IPropertyInfo, name + "!Field", false, true);

                void Load(IXamlIlMethod m, IXamlIlEmitter cg)
                {
                    cg
                        .Ldarg_0();
                    if (m.DeclaringType.IsValueType)
                        cg.Unbox(m.DeclaringType);
                    else
                        cg.Castclass(m.DeclaringType);

                    foreach (var indexerArg in indexerArguments)
                    {
                        context.Emit(indexerArg, cg, indexerArg.Type.GetClrType());
                    }
                }

                var getter = property.Getter == null ?
                    null :
                    _builder.DefineMethod(types.XamlIlTypes.Object,
                        new[] {types.XamlIlTypes.Object}, name + "!Getter", false, true, false);
                if (getter != null)
                {
                    Load(property.Getter, getter.Generator);
                    
                    getter.Generator.EmitCall(property.Getter);
                    if (property.Getter.ReturnType.IsValueType)
                        getter.Generator.Box(property.Getter.ReturnType);
                    getter.Generator.Ret();
                }

                var setter = property.Setter == null ?
                    null :
                    _builder.DefineMethod(types.XamlIlTypes.Void,
                        new[] {types.XamlIlTypes.Object, types.XamlIlTypes.Object},
                        name + "!Setter", false, true, false);
                if (setter != null)
                {
                    Load(property.Setter, setter.Generator);
                    
                    setter.Generator.Ldarg(1);
                    if (property.Setter.Parameters[0].IsValueType)
                        setter.Generator.Unbox_Any(property.Setter.Parameters[0]);
                    else
                        setter.Generator.Castclass(property.Setter.Parameters[0]);
                    setter.Generator
                        .EmitCall(property.Setter, true)
                        .Ret();
                }

                var get = _builder.DefineMethod(types.IPropertyInfo, Array.Empty<IXamlIlType>(),
                    name + "!Property", true, true, false);


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

                void EmitFunc(IXamlIlEmitter emitter, IXamlIlMethod method, IXamlIlType del)
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
    }
}
