using System;
using System.Linq;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AddNameScopeRegistration : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlPropertyAssignmentNode pa
                && pa.Property.Name == "Name"
                && pa.Property.DeclaringType.FullName == "Avalonia.StyledElement")
            {
                if (context.ParentNodes().FirstOrDefault() is XamlManipulationGroupNode mg
                    && mg.Children.OfType<AvaloniaNameScopeRegistrationXamlIlNode>().Any())
                    return node;
                
                IXamlAstValueNode value = null;
                for (var c = 0; c < pa.Values.Count; c++)
                    if (pa.Values[c].Type.GetClrType().Equals(context.Configuration.WellKnownTypes.String))
                    {
                        value = pa.Values[c];
                        if (!(value is XamlAstTextNode))
                        {
                            var local = new XamlAstCompilerLocalNode(value);
                            // Wrap original in local initialization
                            pa.Values[c] = new XamlAstLocalInitializationNodeEmitter(value, value, local);
                            // Use local
                            value = local;
                        }

                        break;
                    }

                if (value != null)
                    return new XamlManipulationGroupNode(pa)
                    {
                        Children =
                        {
                            pa,
                            new AvaloniaNameScopeRegistrationXamlIlNode(value)
                        }
                    };
            }

            if (!context.ParentNodes().Any()
                && node is XamlValueWithManipulationNode mnode)
            {
                mnode.Manipulation = new XamlManipulationGroupNode(mnode,
                    new[]
                    {
                        mnode.Manipulation,
                        new HandleRootObjectScopeNode(mnode, context.GetAvaloniaTypes())
                    });
            }
            return node;
        }

        class HandleRootObjectScopeNode : XamlAstNode, IXamlAstManipulationNode, IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
        {
            private readonly AvaloniaXamlIlWellKnownTypes _types;

            public HandleRootObjectScopeNode(IXamlLineInfo lineInfo,
                AvaloniaXamlIlWellKnownTypes types) : base(lineInfo)
            {
                _types = types;
            }

            public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
            {
                var next = codeGen.DefineLabel();
                var scopeField = context.RuntimeContext.ContextType.Fields.First(f =>
                    f.Name == AvaloniaXamlIlLanguage.ContextNameScopeFieldName);
                using (var local = codeGen.LocalsPool.GetLocal(_types.StyledElement))
                {
                    codeGen
                        .Isinst(_types.StyledElement)
                        .Dup()
                        .Stloc(local.Local)
                        .Brfalse(next)
                        .Ldloc(local.Local)
                        .Ldloc(context.ContextLocal)
                        .Ldfld(scopeField)
                        .EmitCall(_types.NameScopeSetNameScope, true)
                        .MarkLabel(next)
                        .Ldloc(context.ContextLocal)
                        .Ldfld(scopeField)
                        .EmitCall(_types.INameScopeComplete, true);
                }

                return XamlILNodeEmitResult.Void(1);

            }
        }
    }

    class AvaloniaNameScopeRegistrationXamlIlNode : XamlAstNode, IXamlAstManipulationNode
    {
        public IXamlAstValueNode Name { get; set; }

        public AvaloniaNameScopeRegistrationXamlIlNode(IXamlAstValueNode name) : base(name)
        {
            Name = name;
        }

        public override void VisitChildren(IXamlAstVisitor visitor)
            => Name = (IXamlAstValueNode)Name.Visit(visitor);
    }

    class AvaloniaNameScopeRegistrationXamlIlNodeEmitter : IXamlAstLocalsNodeEmitter<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public XamlILNodeEmitResult Emit(IXamlAstNode node, XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            if (node is AvaloniaNameScopeRegistrationXamlIlNode registration)
            {

                var scopeField = context.RuntimeContext.ContextType.Fields.First(f =>
                    f.Name == AvaloniaXamlIlLanguage.ContextNameScopeFieldName);

                using (var targetLoc = context.GetLocalOfType(context.Configuration.WellKnownTypes.Object))
                {

                    codeGen
                        // var target = {pop}
                        .Stloc(targetLoc.Local)
                        // _context.NameScope.Register(Name, target)
                        .Ldloc(context.ContextLocal)
                        .Ldfld(scopeField);

                    context.Emit(registration.Name, codeGen, registration.Name.Type.GetClrType());

                    codeGen
                        .Ldloc(targetLoc.Local)
                        .EmitCall(context.GetAvaloniaTypes().INameScopeRegister, true);
                }

                return XamlILNodeEmitResult.Void(1);
            }
            return default;
        }
    }
}
