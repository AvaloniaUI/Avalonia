using System;
using System.Linq;
using XamlIl;
using XamlIl.Ast;
using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlRootObjectScope : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
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
}
