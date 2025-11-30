using System.Collections.Generic;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlResolveClassesPropertyTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            var types = context.GetAvaloniaTypes();
            if (node is XamlAstNamePropertyReference prop &&
                prop.Name == "Classes" &&
                prop.TargetType is XamlAstClrTypeReference targetRef &&
                prop.DeclaringType is XamlAstClrTypeReference declaringRef &&
                types.StyledElement.IsAssignableFrom(targetRef.Type) &&
                types.StyledElement.IsAssignableFrom(declaringRef.Type)
                )
            {
                return new XamlAstClrProperty(node, prop.Name, types.StyledElement, types.StyledElementClassesProperty.Getter)
                {
                    Setters = { new ClassesStringSetter(types), new ClassesBindingSetter(types) }
                };
            }
            return node;
        }

        abstract class ClassesSetter(AvaloniaXamlIlWellKnownTypes types, IXamlType parameter)
            : IXamlEmitablePropertySetter<IXamlILEmitter>
        {
            public abstract void Emit(IXamlILEmitter emitter);

            protected AvaloniaXamlIlWellKnownTypes Types { get; } = types;
            public IXamlType TargetType => Types.StyledElement;
            public PropertySetterBinderParameters BinderParameters { get; } = new() { AllowXNull = false };
            public IReadOnlyList<IXamlType> Parameters { get; } = [parameter];
            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes { get; } = [];
        }

        class ClassesStringSetter(AvaloniaXamlIlWellKnownTypes types)
            : ClassesSetter(types, types.XamlIlTypes.String)
        {
            public override void Emit(IXamlILEmitter emitter)
            {
                using (var value = emitter.LocalsPool.GetLocal(Parameters[0]))
                    emitter
                        .Stloc(value.Local)
                        .Ldloc(value.Local);
                emitter.EmitCall(Types.SetClassesMethod);
            }
        }

        class ClassesBindingSetter(AvaloniaXamlIlWellKnownTypes types)
            : ClassesSetter(types, types.BindingBase)
        {
            public override void Emit(IXamlILEmitter emitter)
            {
                using (var value = emitter.LocalsPool.GetLocal(Parameters[0]))
                    emitter
                        .Stloc(value.Local)
                        .Ldloc(value.Local)
                        // TODO: provide anchor?
                        .Ldnull();
                emitter.EmitCall(Types.BindClassesMethod, true);
            }
        }
    }
}
