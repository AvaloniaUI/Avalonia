using System;
using System.Linq;
using XamlIl;
using XamlIl.Ast;
using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AddNameScopeRegistration : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlPropertyAssignmentNode pa)
            {
                if (pa.Property.Name == "Name"
                    && pa.Property.DeclaringType.FullName == "Avalonia.StyledElement")
                {
                    if (context.ParentNodes().FirstOrDefault() is XamlIlManipulationGroupNode mg
                        && mg.Children.OfType<AvaloniaNameScopeRegistrationXamlIlNode>().Any())
                        return node;

                    IXamlIlAstValueNode value = null;
                    for (var c = 0; c < pa.Values.Count; c++)
                        if (pa.Values[c].Type.GetClrType().Equals(context.Configuration.WellKnownTypes.String))
                        {
                            value = pa.Values[c];
                            if (!(value is XamlIlAstTextNode))
                            {
                                var local = new XamlIlAstCompilerLocalNode(value);
                                // Wrap original in local initialization
                                pa.Values[c] = new XamlIlAstLocalInitializationNodeEmitter(value, value, local);
                                // Use local
                                value = local;
                            }

                            break;
                        }

                    if (value != null)
                    {
                        var objectType = context.ParentNodes().OfType<XamlIlAstObjectNode>().FirstOrDefault()?.Type.GetClrType();
                        return new XamlIlManipulationGroupNode(pa)
                        {
                            Children =
                            {
                                pa,
                                new AvaloniaNameScopeRegistrationXamlIlNode(value, context.GetAvaloniaTypes(), objectType)
                            }
                        };
                    }
                }
                else if (pa.Property.CustomAttributes.Select(attr => attr.Type).Intersect(context.Configuration.TypeMappings.DeferredContentPropertyAttributes).Any())
                {
                    pa.Values[pa.Values.Count - 1] =
                        new NestedScopeMetadataNode(pa.Values[pa.Values.Count - 1]);
                }
            }

            return node;
        }
    }

    class NestedScopeMetadataNode : XamlIlValueWithSideEffectNodeBase
    {
        public NestedScopeMetadataNode(IXamlIlAstValueNode value) : base(value, value)
        {
        }
    }

    class AvaloniaNameScopeRegistrationXamlIlNode : XamlIlAstNode, IXamlIlAstManipulationNode, IXamlIlAstEmitableNode
    {
        private readonly AvaloniaXamlIlWellKnownTypes _types;
        public IXamlIlAstValueNode Name { get; set; }
        public IXamlIlType ControlType { get; }

        public AvaloniaNameScopeRegistrationXamlIlNode(IXamlIlAstValueNode name, AvaloniaXamlIlWellKnownTypes types, IXamlIlType controlType) : base(name)
        {
            _types = types;
            ControlType = controlType;
            Name = name;
        }

        public override void VisitChildren(IXamlIlAstVisitor visitor)
            => Name = (IXamlIlAstValueNode)Name.Visit(visitor);

        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            var scopeField = context.RuntimeContext.ContextType.Fields.First(f =>
                f.Name == AvaloniaXamlIlLanguage.ContextNameScopeFieldName);
            
            using (var targetLoc = context.GetLocal(context.Configuration.WellKnownTypes.Object))
            {

                codeGen
                    // var target = {pop}
                    .Stloc(targetLoc.Local)
                    // _context.NameScope.Register(Name, target)
                    .Ldloc(context.ContextLocal)
                    .Ldfld(scopeField);
                    
                context.Emit(Name, codeGen, Name.Type.GetClrType());
                
                codeGen
                    .Ldloc(targetLoc.Local)
                    .EmitCall(_types.INameScopeRegister, true);
            }

            return XamlIlNodeEmitResult.Void(1);
        }
    }
}
