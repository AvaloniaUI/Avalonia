using System;
using System.Collections.Generic;
using System.Linq;
using XamlIl.Ast;
using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    public class AvaloniaXamlIlBindingPropertyAssignmentsTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstXamlPropertyValueNode pv 
                && pv.Property.GetClrProperty() is IXamlIlAvaloniaProperty ap
                && pv.Values.Count == 1)
            {
                var types = context.GetAvaloniaTypes();
                
                var vn = pv.Values[0];
                
                // Special handling for markup extensions
                if (vn.Type.IsMarkupExtension)
                {
                    if (XamlIlTransformHelpers
                        .GetMarkupExtensionProvideValueAlternatives(context, vn.Type.GetClrType())
                        .Any(x => types.IBinding.IsAssignableFrom(x.ReturnType)))
                    {
                        if (XamlIlTransformHelpers.TryConvertMarkupExtension(context, pv.Values[0],
                            new AssignBindingProperty(types, ap), out var ext))
                            return ext;
                    }
                }
                else if (types.IBinding.IsAssignableFrom(vn.Type.GetClrType()))
                {
                    pv.Property = new XamlIlAstClrPropertyReference(pv.Property, new AssignBindingProperty(types, ap));
                }
            }

            return node;
        }

        class AssignBindingProperty : IXamlIlAvaloniaProperty
        {
            public AssignBindingProperty(
                AvaloniaXamlIlWellKnownTypes types,
                IXamlIlAvaloniaProperty property
                )
            {
                PropertyType = types.IBinding;
                AvaloniaProperty = property.AvaloniaProperty;
                CustomAttributes = property.CustomAttributes;
                Name = property.Name;
                Setter = new SetterMethod(types, (property.Setter ?? property.Getter).DeclaringType,
                    AvaloniaProperty);
            }

            public bool Equals(IXamlIlProperty other) =>
                other is AssignBindingProperty abp && abp.AvaloniaProperty.Equals(AvaloniaProperty);

            public string Name { get; }
            public IXamlIlType PropertyType { get; }
            public IXamlIlMethod Setter { get; }
            public IXamlIlMethod Getter { get; }
            public IReadOnlyList<IXamlIlCustomAttribute> CustomAttributes { get; }
            public IXamlIlField AvaloniaProperty { get; }

            class SetterMethod : IXamlIlCustomEmitMethod
            {
                private readonly AvaloniaXamlIlWellKnownTypes _types;
                private readonly IXamlIlField _avaloniaProperty;

                public SetterMethod(AvaloniaXamlIlWellKnownTypes types,
                    IXamlIlType declaringType,
                    IXamlIlField avaloniaProperty)
                {
                    _types = types;
                    _avaloniaProperty = avaloniaProperty;
                    Parameters = new[] {types.AvaloniaObject, types.IBinding};
                    ReturnType = types.XamlIlTypes.Void;
                    DeclaringType = declaringType;
                    Name = "Bind_" + avaloniaProperty.Name;
                }

                public bool Equals(IXamlIlMethod other) =>
                    other is SetterMethod sm && sm._avaloniaProperty.Equals(_avaloniaProperty); 

                public string Name { get; }
                public bool IsPublic => true;
                public bool IsStatic => true;
                public IXamlIlType ReturnType { get; }
                public IReadOnlyList<IXamlIlType> Parameters { get; }
                public IXamlIlType DeclaringType { get; }
                public void EmitCall(IXamlIlEmitter emitter)
                {
                    using (var bloc = emitter.LocalsPool.GetLocal(_types.IBinding))
                        emitter
                            .Stloc(bloc.Local)
                            .Ldsfld(_avaloniaProperty)
                            .Ldloc(bloc.Local)
                            // TODO: provide anchor?
                            .Ldnull();
                    emitter.EmitCall(_types.AvaloniaObjectBindMethod, true);
                }
            }
        }
    }
}
