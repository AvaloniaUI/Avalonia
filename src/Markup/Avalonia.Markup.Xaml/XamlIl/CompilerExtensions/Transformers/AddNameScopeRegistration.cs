using XamlIl.Ast;
using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    public class AddNameScopeRegistration : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlPropertyAssignmentNode pa
                && pa.Property.Name == "Name"
                && pa.Property.Setter.DeclaringType.FullName == "Avalonia.StyledElement")
            {
                return new ScopeRegistrationNode(pa);

            }

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

            public override void VisitChildren(XamlIlAstVisitorDelegate visitor)
                => Value = (IXamlIlAstValueNode)Value.Visit(visitor);

            public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
            {
                using (var nameLoc = context.GetLocal(context.Configuration.WellKnownTypes.String))
                using (var targetLoc = context.GetLocal(context.Configuration.WellKnownTypes.Object))

                {
                    var exts = context.Configuration.TypeSystem.GetType("Avalonia.Controls.NameScopeExtensions");
                    var findNameScope = exts.FindMethod(m => m.Name == "FindNameScope");
                    var registerMethod = findNameScope.ReturnType.FindMethod(m => m.Name == "Register");
                    var exit = codeGen.DefineLabel();
                    var call = codeGen.DefineLabel();
                    
                    context.Emit(Value, codeGen, _property.PropertyType);
                    
                    codeGen
                        // var name = {EmitName()}
                        .Stloc(nameLoc.Local)
                        // var target = {target}
                        .Dup()
                        .Stloc(targetLoc.Local)
                        // {target}.Name = name
                        .Dup()
                        .Ldloc(nameLoc.Local)
                        .EmitCall(_property.Setter)
                        // var scope = {target}.FindNameScope()
                        .EmitCall(findNameScope)
                        // if({scope} != null) goto call;
                        .Dup()
                        .Brtrue(call)
                        // goto: exit
                        .Pop()
                        .Br(exit)
                        // call: {scope}.Register(name, target);
                        .MarkLabel(call)
                        .Ldloc(nameLoc.Local)
                        .Ldloc(targetLoc.Local)
                        .EmitCall(registerMethod)
                        .MarkLabel(exit);
                }
                return XamlIlNodeEmitResult.Void;
            }
        }
    }
}
