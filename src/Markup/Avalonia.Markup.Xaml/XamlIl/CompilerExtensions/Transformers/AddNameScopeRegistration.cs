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

            if (!context.ParentNodes().Any()
                && node is XamlIlValueWithManipulationNode mnode)
            {
                mnode.Manipulation = new XamlIlManipulationGroupNode(mnode,
                    new[]
                    {
                        mnode.Manipulation,
                        new HandleRootObjectScopeNode(mnode, context.GetAvaloniaTypes())
                    });
            }
            return node;
        }

        class HandleRootObjectScopeNode : XamlIlAstNode, IXamlIlAstManipulationNode, IXamlIlAstEmitableNode
        {
            private readonly AvaloniaXamlIlWellKnownTypes _types;

            public HandleRootObjectScopeNode(IXamlIlLineInfo lineInfo,
                AvaloniaXamlIlWellKnownTypes types) : base(lineInfo)
            {
                _types = types;
            }

            public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
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

                return XamlIlNodeEmitResult.Void(1);

            }
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
