using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Visitors;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    internal class AvaloniaXamlIlDeferredResourceTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (!(node is XamlPropertyAssignmentNode pa) || pa.Values.Count != 2)
                return node;

            var types = context.GetAvaloniaTypes();

            if (pa.Property.DeclaringType == types.ResourceDictionary && pa.Property.Name == "Content"
                && ShouldBeDeferred(pa.Values[1]))
            {
                IXamlMethod addMethod = TryGetSharedValue(pa.Values[1], out var isShared) && !isShared
                    ? types.ResourceDictionaryNotSharedDeferredAdd
                    : types.ResourceDictionaryDeferredAdd;

                pa.Values[1] = new XamlDeferredContentNode(pa.Values[1], types.XamlIlTypes.Object, context.Configuration);
                pa.PossibleSetters = new List<IXamlPropertySetter>
                {
                    new XamlDirectCallPropertySetter(addMethod),
                };
            }
            else if (pa.Property.Name == "Resources" && pa.Property.Getter?.ReturnType.Equals(types.IResourceDictionary) == true
                && ShouldBeDeferred(pa.Values[1]))
            {
                IXamlMethod addMethod = TryGetSharedValue(pa.Values[1], out var isShared) && !isShared
                    ? types.ResourceDictionaryNotSharedDeferredAdd
                    : types.ResourceDictionaryDeferredAdd;

                pa.Values[1] = new XamlDeferredContentNode(pa.Values[1], types.XamlIlTypes.Object, context.Configuration);
                pa.PossibleSetters = new List<IXamlPropertySetter>
                {
                    new AdderSetter(pa.Property.Getter, addMethod),
                };
            }

            return node;

            bool TryGetSharedValue(IXamlAstValueNode valueNode, out bool value)
            {
                value = default;
                if (valueNode is XamlAstConstructableObjectNode co)
                {
                    // Try find x:Share directive
                    if (co.Children.Find(d => d is XamlAstXmlDirective { Namespace: XamlNamespaces.Xaml2006, Name: "Shared" }) is XamlAstXmlDirective sharedDirective)
                    {
                        if (sharedDirective.Values.Count == 1 && sharedDirective.Values[0] is XamlAstTextNode text)
                        {
                            if (bool.TryParse(text.Text, out var parseValue))
                            {
                                // If the parser succeeds, remove the x:Share directive
                                co.Children.Remove(sharedDirective);
                                return true;
                            }
                            else
                            {
                                context.ReportTransformError("Invalid argument type for x:Shared directive.", node);
                            }
                        }
                        else
                        {
                            context.ReportTransformError("Invalid number of arguments for x:Shared directive.", node);
                        }
                    }
                }
                return false;
            }
        }

        private static bool ShouldBeDeferred(IXamlAstValueNode node)
        {
            var clrType = node.Type.GetClrType();

            // XAML compiler is currently strict about value types, allowing them to be created only through converters.
            // At the moment it should be safe to not defer structs.
            if (clrType.IsValueType)
            {
                return false;
            }

            // Never defer strings.
            if (clrType.FullName == "System.String")
            {
                return false;
            }

            // Do not defer resources, if it has any x:Name registration, as it cannot be delayed.
            // This visitor will count x:Name registrations, ignoring nested NestedScopeMetadataNode scopes.
            // We set target scope level to 0, assuming that this resource node is a scope of itself.
            var nameRegistrationsVisitor = new NameScopeRegistrationVisitor(
                targetMetadataScopeLevel: 0);
            node.Visit(nameRegistrationsVisitor);
            if (nameRegistrationsVisitor.Count > 0)
            {
                return false;
            }

            return true;
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
                while (locals.Count>0)
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
}
