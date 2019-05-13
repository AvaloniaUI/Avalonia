using System.Linq;
using XamlIl.Ast;
using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class AvaloniaXamlIlConstructorServiceProviderTransformer : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstObjectNode on && on.Arguments.Count == 0)
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

        class InjectServiceProviderNode : XamlIlAstNode, IXamlIlAstValueNode,IXamlIlAstNodeNeedsParentStack,
            IXamlIlAstEmitableNode
        {
            public InjectServiceProviderNode(IXamlIlType type, IXamlIlLineInfo lineInfo) : base(lineInfo)
            {
                Type = new XamlIlAstClrTypeReference(lineInfo, type, false);
            }

            public IXamlIlAstTypeReference Type { get; }
            public bool NeedsParentStack => true;
            public XamlIlNodeEmitResult Emit(XamlIlEmitContext context, IXamlIlEmitter codeGen)
            {
                codeGen.Ldloc(context.ContextLocal);
                return XamlIlNodeEmitResult.Type(0, Type.GetClrType());
            }
        }
    }
}
