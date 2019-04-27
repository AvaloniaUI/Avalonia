using System;
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
                && pa.Property.Setter.DeclaringType.FullName == "Avalonia.StyledElement")
                return new ScopeRegistrationNode(pa);

            return node;
        }

        class ScopeRegistrationNode : XamlIlAstNode, IXamlIlAstManipulationNode, IXamlIlAstEmitableNode
        {
            public IXamlIlAstValueNode Value { get; set; }
            private IXamlIlProperty _property;

            public ScopeRegistrationNode(XamlIlPropertyAssignmentNode pa) : base(pa)
            {
                _property = pa.Property;
                Value = pa.Value;
            }

            public override void VisitChildren(IXamlIlAstVisitor visitor)
                => Value = (IXamlIlAstValueNode)Value.Visit(visitor);

            public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
            {
                var exts = context.Configuration.TypeSystem.GetType("Avalonia.Controls.NameScopeExtensions");
                var findNameScope = exts.FindMethod(m => m.Name == "FindNameScope");
                var registerMethod = findNameScope.ReturnType.FindMethod(m => m.Name == "Register");
                using (var nameLoc = context.GetLocal(context.Configuration.WellKnownTypes.String))
                using (var targetLoc = context.GetLocal(_property.Setter.DeclaringType))
                using (var nameScopeLoc = context.GetLocal(findNameScope.ReturnType))
                {
                    var exit = codeGen.DefineLabel();

                    // var target = {target}
                    codeGen
                        .Castclass(_property.Setter.DeclaringType)
                        .Stloc(targetLoc.Local);
                    
                    // var name = {EmitName()}
                    context.Emit(Value, codeGen, _property.PropertyType);
                    codeGen.Stloc(nameLoc.Local)
                        // target.Name = name
                        .Ldloc(targetLoc.Local)
                        .Ldloc(nameLoc.Local)
                        .EmitCall(_property.Setter)
                        // var scope = target.FindNameScope()
                        .Ldloc(targetLoc.Local)
                        .Castclass(findNameScope.Parameters[0])
                        .EmitCall(findNameScope)
                        .Stloc(nameScopeLoc.Local)
                        // if({scope} != null) goto call;
                        .Ldloc(nameScopeLoc.Local)
                        .Brfalse(exit)
                        .Ldloc(nameScopeLoc.Local)
                        .Ldloc(nameLoc.Local)
                        .Ldloc(targetLoc.Local)
                        .EmitCall(registerMethod)
                        .MarkLabel(exit);
                }
                return XamlIlNodeEmitResult.Void(1);
            }
        }
    }
}
