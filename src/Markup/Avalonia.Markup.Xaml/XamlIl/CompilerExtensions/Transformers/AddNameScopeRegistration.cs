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
                    && mg.Children.OfType<ScopeRegistrationNode>().Any())
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
                            new ScopeRegistrationNode(value)
                        }
                    };
            }

            return node;
        }

        class ScopeRegistrationNode : XamlIlAstNode, IXamlIlAstManipulationNode, IXamlIlAstEmitableNode
        {
            private readonly IXamlIlType _targetType;
            public IXamlIlAstValueNode Value { get; set; }
            public ScopeRegistrationNode(IXamlIlAstValueNode value) : base(value)
            {
                Value = value;
            }

            public override void VisitChildren(IXamlIlAstVisitor visitor)
                => Value = (IXamlIlAstValueNode)Value.Visit(visitor);

            public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
            {
                var exts = context.Configuration.TypeSystem.GetType("Avalonia.Controls.NameScopeExtensions");
                var findNameScope = exts.FindMethod(m => m.Name == "FindNameScope");
                var registerMethod = findNameScope.ReturnType.FindMethod(m => m.Name == "Register");
                using (var targetLoc = context.GetLocal(context.Configuration.WellKnownTypes.Object))
                using (var nameScopeLoc = context.GetLocal(findNameScope.ReturnType))
                {
                    var exit = codeGen.DefineLabel();
                    codeGen
                        // var target = {pop}    
                        .Stloc(targetLoc.Local)
                        // var scope = target.FindNameScope()
                        .Ldloc(targetLoc.Local)
                        .Castclass(findNameScope.Parameters[0])
                        .EmitCall(findNameScope)
                        .Stloc(nameScopeLoc.Local)
                        // if({scope} != null) goto call;
                        .Ldloc(nameScopeLoc.Local)
                        .Brfalse(exit)
                        .Ldloc(nameScopeLoc.Local);
                    context.Emit(Value, codeGen, Value.Type.GetClrType());
                    codeGen
                        .Ldloc(targetLoc.Local)
                        .EmitCall(registerMethod)
                        .MarkLabel(exit);
                }
                return XamlIlNodeEmitResult.Void(1);
            }
        }
    }
}
