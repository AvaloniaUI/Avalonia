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
    /// <summary>
    /// Transforms ResourceDictionary and IResourceDictionary property assignments
    /// to use Add method calls with deferred content where applicable.
    /// Additionally, handles x:Shared on assignments and injects XamlSourceInfo.
    /// </summary>
    internal class AvaloniaXamlResourceTransformer : IXamlAstTransformer
    {
        /// <summary>
        /// Gets or sets a value indicating whether source information should be generated
        /// and injected into the compiled XAML output.
        /// </summary>
        public bool CreateSourceInfo { get; set; } = true;
        
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (!(node is XamlPropertyAssignmentNode pa) || pa.Values.Count != 2)
                return node;

            var types = context.GetAvaloniaTypes();
            var document = context.Document;

            if (pa.Property.DeclaringType == types.ResourceDictionary && pa.Property.Name == "Content")
            {
                var value = pa.Values[1];
                (var adder, value) = ResolveAdderAndValue(value);

                pa.Values[1] = value;
                pa.PossibleSetters = new List<IXamlPropertySetter>
                {
                    new AdderSetter(adder, CreateSourceInfo, types, value.Line, value.Position, document),
                };
            }
            else if (pa.Property.Name == "Resources" && pa.Property.Getter?.ReturnType.Equals(types.IResourceDictionary) == true)
            {
                var value = pa.Values[1];
                (var adder, value) = ResolveAdderAndValue(value);

                pa.Values[1] = value;
                pa.PossibleSetters = new List<IXamlPropertySetter>
                {
                    new AdderSetter(pa.Property.Getter, adder, CreateSourceInfo, types, value.Line, value.Position, document),
                };
            }

            return node;

            (IXamlMethod adder, IXamlAstValueNode newValue) ResolveAdderAndValue(IXamlAstValueNode valueNode)
            {
                if (ShouldBeDeferred(valueNode))
                {
                    var adder = TryGetSharedValue(valueNode, out var isShared) && !isShared
                        ? types.ResourceDictionaryNotSharedDeferredAdd
                        : types.ResourceDictionaryDeferredAdd;
                    var deferredNode = new XamlDeferredContentNode(valueNode, types.XamlIlTypes.Object, context.Configuration);
                    return (adder, deferredNode);
                }
                else
                {
                    var adder = XamlTransformHelpers.FindPossibleAdders(context, types.IResourceDictionary)
                        .FirstOrDefault() ?? throw new XamlTransformException("No suitable Add method found for IResourceDictionary.", node);
                    return (adder, valueNode);
                }
            }

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
            private readonly IXamlMethod? _getter;
            private readonly IXamlMethod _adder;
            private readonly bool _emitSourceInfo;
            private readonly AvaloniaXamlIlWellKnownTypes _avaloniaTypes;
            private readonly string? _document;
            private readonly int _line, _position;

            /// <summary>
            /// Creates an adder-only setter. Target is assumed to be already on the stack before emit.
            /// For example:
            /// var resourceDictionary = ...
            /// resourceDictionary.Add(key, value);
            /// resourceDictionary.Add(key2, value2);
            /// </summary>
            public AdderSetter(
                IXamlMethod adder,
                bool emitSourceInfo,
                AvaloniaXamlIlWellKnownTypes avaloniaTypes,
                int line, int position, string? document)
            {
                _adder = adder;
                _emitSourceInfo = emitSourceInfo;
                _avaloniaTypes = avaloniaTypes;
                _line = line;
                _position = position;
                _document = document;

                TargetType = adder.ThisOrFirstParameter();
                Parameters = adder.ParametersWithThis().Skip(1).ToList();
                bool allowNull = Parameters.Last().AcceptsNull();
                BinderParameters = new PropertySetterBinderParameters
                {
                    AllowMultiple = true,
                    AllowXNull = allowNull,
                    AllowRuntimeNull = allowNull
                };
            }

            /// <summary>
            /// Explicit target getter - target will be obtained by calling the getter first.
            /// 
            /// </summary>
            public AdderSetter(
                IXamlMethod getter, IXamlMethod adder,
                bool emitSourceInfo,
                AvaloniaXamlIlWellKnownTypes avaloniaTypes,
                int line, int position, string? document)
                : this(adder, emitSourceInfo, avaloniaTypes, line, position, document)
            {
                _getter = getter;
                TargetType = getter.DeclaringType;
                BinderParameters.AllowMultiple = false;
                BinderParameters.AllowAttributeSyntax = false;
            }

            public IXamlType TargetType { get; }

            public PropertySetterBinderParameters BinderParameters { get; }

            public IReadOnlyList<IXamlType> Parameters { get; }
            public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => _adder.CustomAttributes;

            /// <summary>
            /// Emits the setter with arguments already on the stack.
            /// </summary>
            /// <remarks>
            /// If _getter is null - assume target is already on the stack.
            /// In this case, we can just call Emit. Unless _emitSourceInfo is true.
            /// 
            /// If _emitSourceInfo is true - we need to make sure that target and key are on the stack for XamlSourceInfo setting,
            /// so we need to store parameters to locals first regardless.
            /// </remarks>
            public void Emit(IXamlILEmitter emitter)
            {
                using var keyLocal = emitter.LocalsPool.GetLocal(Parameters[0]);

                if (_getter is not null || _emitSourceInfo)
                {
                    var locals = new Stack<XamlLocalsPool.PooledLocal>();
                    // Save all "setter" parameters
                    for (var c = Parameters.Count - 1; c >= 0; c--)
                    {
                        var loc = emitter.LocalsPool.GetLocal(Parameters[c]);
                        locals.Push(loc);
                        emitter.Stloc(loc.Local);

                        if (c == 0 && _emitSourceInfo)
                        {
                            // Store the key argument for XamlSourceInfo later
                            emitter.Ldloc(loc.Local);
                            emitter.Stloc(keyLocal.Local);
                        }
                    }

                    if (_getter is not null)
                    {
                        emitter.EmitCall(_getter);
                    }

                    // Duplicate the target object on stack for setting XamlSourceInfo later
                    emitter.Dup();

                    while (locals.Count > 0)
                        using (var loc = locals.Pop())
                            emitter.Ldloc(loc.Local);
                }

                emitter.EmitCall(_adder, true);

                if (_emitSourceInfo)
                {
                    // Target is already on stack (dup)
                    // Load the key argument from local
                    emitter.Ldloc(keyLocal.Local);
                    EmitSetSourceInfo(emitter);
                }
            }

            /// <summary>
            /// Emits the setter with provided arguments that are not yet on the stack.
            /// </summary>
            /// <remarks>
            /// If _getter is null - assume target is already on the stack.
            /// If _emitSourceInfo is true - we need to make sure that target and key are on the stack for XamlSourceInfo setting.
            /// </remarks>
            public void EmitWithArguments(
                XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context,
                IXamlILEmitter emitter,
                IReadOnlyList<IXamlAstValueNode> arguments)
            {
                using var keyLocal = _emitSourceInfo ? emitter.LocalsPool.GetLocal(Parameters[0]) : null;

                if (_getter is not null)
                {
                    emitter.EmitCall(_getter);
                }

                if (_emitSourceInfo)
                {
                    // Duplicate the target object on stack for setting XamlSourceInfo later
                    emitter.Dup();
                }

                for (var i = 0; i < arguments.Count; ++i)
                {
                    context.Emit(arguments[i], emitter, Parameters[i]);

                    // Store the key argument for XamlSourceInfo later
                    if (i == 0 && _emitSourceInfo)
                    {
                        emitter.Stloc(keyLocal!.Local);
                        emitter.Ldloc(keyLocal.Local);
                    }
                }

                emitter.EmitCall(_adder, true);

                if (_emitSourceInfo)
                {
                    // Target is already on stack (dub)
                    // Load the key argument from local
                    emitter.Ldloc(keyLocal!.Local);

                    EmitSetSourceInfo(emitter);
                }
            }

            private void EmitSetSourceInfo(IXamlILEmitter emitter)
            {
                // Assumes the target object and key are already on the stack

                emitter.Ldc_I4(_line);
                emitter.Ldc_I4(_position);
                if (_document is not null)
                    emitter.Ldstr(_document);
                else
                    emitter.Ldnull();
                emitter.Newobj(_avaloniaTypes.XamlSourceInfoConstructor);

                // Set the XamlSourceInfo property on the current object
                // XamlSourceInfo.SetXamlSourceInfo(@this, key, info);
                emitter.EmitCall(_avaloniaTypes.XamlSourceInfoDictionarySetter);
            }
            
            public bool Equals(AdderSetter? other)
            {
                if (ReferenceEquals(null, other))
                    return false;
                if (ReferenceEquals(this, other))
                    return true;

                return _getter?.Equals(other._getter) == true && _adder.Equals(other._adder);
            }

            public override bool Equals(object? obj)
                => Equals(obj as AdderSetter);

            public override int GetHashCode()
                => (_getter, _adder).GetHashCode();
        }
    }
}
