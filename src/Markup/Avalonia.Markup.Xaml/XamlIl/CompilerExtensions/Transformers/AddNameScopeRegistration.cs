using System;
using System.Linq;
using XamlIl.Ast;
using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AddNameScopeRegistration : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlPropertyAssignmentNode pa
                && pa.Property.Name == "Name"
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
                    return new XamlIlManipulationGroupNode(pa)
                    {
                        Children =
                        {
                            pa,
                            new AvaloniaNameScopeRegistrationXamlIlNode(value, context.GetAvaloniaTypes())
                        }
                    };
            }

            return node;
        }

        
    }

    class AvaloniaNameScopeRegistrationXamlIlNode : XamlIlAstNode, IXamlIlAstManipulationNode, IXamlIlAstEmitableNode
    {
        private readonly AvaloniaXamlIlWellKnownTypes _types;
        public IXamlIlAstValueNode Name { get; set; }

        public AvaloniaNameScopeRegistrationXamlIlNode(IXamlIlAstValueNode name, AvaloniaXamlIlWellKnownTypes types) : base(name)
        {
            _types = types;
            Name = name;
        }

        public override void VisitChildren(IXamlIlAstVisitor visitor)
            => Name = (IXamlIlAstValueNode)Name.Visit(visitor);

        public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            using (var targetLoc = context.GetLocal(context.Configuration.WellKnownTypes.Object))
            {
                codeGen
                    // var target = {pop}    
                    .Stloc(targetLoc.Local)
                    // NameScope.Register(context.IntermediateRoot, Name, target)
                    .Ldloc(context.ContextLocal)
                    .Ldfld(context.RuntimeContext.IntermediateRootObjectField)
                    .Castclass(_types.StyledElement);
                context.Emit(Name, codeGen, Name.Type.GetClrType());
                codeGen
                    .Ldloc(targetLoc.Local)
                    .EmitCall(_types.NameScopeStaticRegister, true);
            }

            return XamlIlNodeEmitResult.Void(1);
        }
    }
}
