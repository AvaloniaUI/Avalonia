using System;
using System.Collections.Generic;
using System.Xml;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.GroupTransformers;

internal class AstGroupTransformationContext : AstTransformationContext
{
    public AstGroupTransformationContext(
        IReadOnlyCollection<IXamlDocumentResource> documents,
        TransformerConfiguration configuration)
        : base(configuration, null)
    {
        Documents = documents;
    }

    public override string Document => CurrentDocument?.FileSource?.FilePath ?? "{unknown document}";
    public IXamlDocumentResource CurrentDocument { get; set; }
    
    public IReadOnlyCollection<IXamlDocumentResource> Documents { get; }

    class Visitor : ContextXamlAstVisitor
    {
        private readonly IXamlAstGroupTransformer _transformer;

        public Visitor(AstGroupTransformationContext context, IXamlAstGroupTransformer transformer) : base(context)
        {
            _transformer = transformer;
        }

        public override string GetTransformerInfo() => _transformer.GetType().Name;

        public override IXamlAstNode VisitCore(AstTransformationContext context, IXamlAstNode node) =>
            _transformer.Transform((AstGroupTransformationContext)context, node);
    }
    
    public IXamlAstNode Visit(IXamlAstNode root, IXamlAstGroupTransformer transformer)
    {
        root = root.Visit(new Visitor(this, transformer));
        return root;
    }

    public void VisitChildren(IXamlAstNode root, IXamlAstGroupTransformer transformer)
    {
        root.VisitChildren(new Visitor(this, transformer));
    }
}

internal interface IXamlAstGroupTransformer
{
    IXamlAstNode Transform(AstGroupTransformationContext context, IXamlAstNode node);
}
