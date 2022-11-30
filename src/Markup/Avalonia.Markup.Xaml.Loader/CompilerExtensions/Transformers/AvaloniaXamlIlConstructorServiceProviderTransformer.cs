using System.Linq;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlConstructorServiceProviderTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstObjectNode on && on.Arguments.Count == 0)
            {
                var ctors = on.Type.GetClrType().Constructors;
                if (!ctors.Any(c => c.IsPublic && !c.IsStatic && c.Parameters.Count == 0))
                {
                    var sp = context.Configuration.TypeMappings.ServiceProvider;
                    if (ctors.Any(c =>
                        c.IsPublic && !c.IsStatic && c.Parameters.Count == 1 && c.Parameters[0]
                            .Equals(sp)))
                    {
                        on.Arguments.Add(new InjectServiceProviderNode(sp, on));
                    }
                }
            }

            return node;
        }

        class InjectServiceProviderNode : XamlAstNode, IXamlAstValueNode,IXamlAstNodeNeedsParentStack,
            IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
        {
            public InjectServiceProviderNode(IXamlType type, IXamlLineInfo lineInfo) : base(lineInfo)
            {
                Type = new XamlAstClrTypeReference(lineInfo, type, false);
            }

            public IXamlAstTypeReference Type { get; }
            public bool NeedsParentStack => true;
            public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
            {
                codeGen.Ldloc(context.ContextLocal);
                return XamlILNodeEmitResult.Type(0, Type.GetClrType());
            }
        }
    }
}
