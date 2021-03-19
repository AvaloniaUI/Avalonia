using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlLazyResourceTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (!(node is XamlPropertyAssignmentNode pa) || pa.Values.Count != 2)
                return node;

            var types = context.GetAvaloniaTypes();

            if (pa.Property.DeclaringType == types.ResourceDictionary && pa.Property.Name == "Content")
            {
                pa.Values[1] = new XamlDeferredContentNode(pa.Values[1], context.Configuration);
                pa.PossibleSetters = new List<IXamlPropertySetter>
                {
                    new XamlDirectCallPropertySetter(types.ResourceDictionaryLazyAdd),
                };
            }
            else if ((pa.Property.DeclaringType == types.StyledElement ||
                      pa.Property.DeclaringType == types.Style ||
                      pa.Property.DeclaringType == types.Styles) && 
                pa.Property.Name == "Resources")
            {
                pa.Values[1] = new XamlDeferredContentNode(pa.Values[1], context.Configuration);
                pa.PossibleSetters = new List<IXamlPropertySetter>
                {
                    new AdderSetter(pa.Property.Getter, types.ResourceDictionaryLazyAdd),
                };
            }

            return node;
        }
        
        class AdderSetter : IXamlPropertySetter, IXamlEmitablePropertySetter<IXamlILEmitter>
        {
            private readonly IXamlMethod _getter;
            private readonly IXamlMethod _adder;

            public AdderSetter(IXamlMethod getter, IXamlMethod adder)
            {
                _getter = getter;
                _adder = adder;
                TargetType = getter.DeclaringType;
                Parameters = adder.ParametersWithThis().Skip(1).ToList();
            }

            public IXamlType TargetType { get; }

            public PropertySetterBinderParameters BinderParameters { get; } = new PropertySetterBinderParameters
            {
                AllowMultiple = true
            };
            
            public IReadOnlyList<IXamlType> Parameters { get; }
            public void Emit(IXamlILEmitter emitter)
            {
                var locals = new Stack<XamlLocalsPool.PooledLocal>();
                // Save all "setter" parameters
                for (var c = Parameters.Count - 1; c >= 0; c--)
                {
                    var loc = emitter.LocalsPool.GetLocal(Parameters[c]);
                    locals.Push(loc);
                    emitter.Stloc(loc.Local);
                }

                emitter.EmitCall(_getter);
                while (locals.Count>0)
                    using (var loc = locals.Pop())
                        emitter.Ldloc(loc.Local);
                emitter.EmitCall(_adder, true);
            }
        }
    }
}
