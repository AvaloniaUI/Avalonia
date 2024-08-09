using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.AstNodes;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;

class XSharedTransformer : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        var types = context.GetAvaloniaTypes();
        switch (node)
        {
            case XamlPropertyAssignmentNode { Values.Count: 2 } pa when
                    pa.Property.Name == "Content"
                    && pa.Property.DeclaringType == types.ResourceDictionary
                    && pa.Values[1] is XamlAstConstructableObjectNode co
                    && TryGetSharedDirective(co, out var directive)
                    :
                {
                    co.Children.Remove(directive.Node);
                    if (!directive.Value)
                    {
                        pa.Values[1] = new XamlNotSharedDeferredContentNode(pa.Values[1]
                            , types.NotSharedDeferredContentExecutorCustomization
                            , types.XamlIlTypes.Object
                            , context.Configuration);
                        pa.PossibleSetters = new List<IXamlPropertySetter>
                        {
                            new XamlDirectCallPropertySetter(types.ResourceDictionaryNotSharedAdd),
                        };
                    }
                }
                break;
            case XamlPropertyAssignmentNode { Values.Count: 2 } pa when
                    pa.Property.Name == "Resources"
                    && pa.Property.Getter?.ReturnType.Equals(types.IResourceDictionary) == true
                    && pa.Values[1] is XamlAstConstructableObjectNode co
                    && TryGetSharedDirective(co, out var directive)
                    :
                {
                    co.Children.Remove(directive.Node);
                    if (!directive.Value)
                    {
                        pa.Values[1] = new XamlNotSharedDeferredContentNode(pa.Values[1]
                            , types.NotSharedDeferredContentExecutorCustomization
                            , types.XamlIlTypes.Object
                            , context.Configuration);
                        pa.PossibleSetters = new List<IXamlPropertySetter>
                        {
                            new AdderSetter(pa.Property.Getter, types.ResourceDictionaryNotSharedAdd),
                        };

                    }
                }
                break;
            default:
                break;
        }

        return node;

        bool TryGetSharedDirective(XamlAstConstructableObjectNode co, out (IXamlAstNode Node, bool Value) directive)
        {
            directive = default;
            if (co.Children.Find(d => d is XamlAstXmlDirective { Namespace: XamlNamespaces.Xaml2006, Name: "Shared" }) is XamlAstXmlDirective sharedDirective)
            {
                if (sharedDirective.Values.Count == 1 && sharedDirective.Values[0] is XamlAstTextNode text)
                {
                    if (bool.TryParse(text.Text, out var value))
                    {
                        directive = (sharedDirective, value);
                        return true;
                    }
                    else
                    {
                        context?.ReportTransformError("Invalid argument type for x:Shared directive.", node);
                    }
                }
                else
                {
                    context?.ReportTransformError("Invalid number of arguments for x:Shared directive.", node);
                }
            }
            return false;
        }
    }

    class AdderSetter : IXamlILOptimizedEmitablePropertySetter, IEquatable<AdderSetter>
    {
        private readonly IXamlMethod _getter;
        private readonly IXamlMethod _adder;

        public AdderSetter(IXamlMethod getter, IXamlMethod adder)
        {
            _getter = getter;
            _adder = adder;
            TargetType = getter.DeclaringType;
            Parameters = adder.ParametersWithThis().Skip(1).ToList();

            bool allowNull = Parameters.Last().AcceptsNull();
            BinderParameters = new PropertySetterBinderParameters
            {
                AllowMultiple = true,
                AllowXNull = allowNull,
                AllowRuntimeNull = allowNull,
                AllowAttributeSyntax = false,
            };
        }

        public IXamlType TargetType { get; }

        public PropertySetterBinderParameters BinderParameters { get; }

        public IReadOnlyList<IXamlType> Parameters { get; }
        public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => _adder.CustomAttributes;

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
            while (locals.Count > 0)
                using (var loc = locals.Pop())
                    emitter.Ldloc(loc.Local);
            emitter.EmitCall(_adder, true);
        }

        public void EmitWithArguments(
            XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context,
            IXamlILEmitter emitter,
            IReadOnlyList<IXamlAstValueNode> arguments)
        {
            emitter.EmitCall(_getter);

            for (var i = 0; i < arguments.Count; ++i)
                context.Emit(arguments[i], emitter, Parameters[i]);

            emitter.EmitCall(_adder, true);
        }

        public bool Equals(AdderSetter? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return _getter.Equals(other._getter) && _adder.Equals(other._adder);
        }

        public override bool Equals(object? obj)
            => Equals(obj as AdderSetter);

        public override int GetHashCode()
            => (_getter.GetHashCode() * 397) ^ _adder.GetHashCode();
    }
}
