using System;
using System.Collections.Generic;
using System.Xml;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.GroupTransformers;

internal class AstGroupTransformationContext : AstTransformationContext
{
    public AstGroupTransformationContext(IReadOnlyCollection<IXamlDocumentResource> documents, TransformerConfiguration configuration, bool strictMode = true)
        : base(configuration, null, strictMode)
    {
        Documents = documents;
    }

    public IXamlDocumentResource CurrentDocument { get; set; }
    
    public IReadOnlyCollection<IXamlDocumentResource> Documents { get; }

    public new IXamlAstNode ParseError(string message, IXamlAstNode node) =>
        Error(node, new XamlDocumentParseException(CurrentDocument?.FileSource?.FilePath, message, node));

    public new IXamlAstNode ParseError(string message, IXamlAstNode offender, IXamlAstNode ret) =>
        Error(ret, new XamlDocumentParseException(CurrentDocument?.FileSource?.FilePath, message, offender));

    class Visitor : IXamlAstVisitor
    {
        private readonly AstGroupTransformationContext _context;
        private readonly IXamlAstGroupTransformer _transformer;

        public Visitor(AstGroupTransformationContext context, IXamlAstGroupTransformer transformer)
        {
            _context = context;
            _transformer = transformer;
        }
            
        public IXamlAstNode Visit(IXamlAstNode node)
        {
#if Xaml_DEBUG
                return _transformer.Transform(_context, node);
#else
            try
            {
                return _transformer.Transform(_context, node);
            }
            catch (Exception e) when (!(e is XmlException))
            {
                throw new XamlDocumentParseException(
                    _context.CurrentDocument?.FileSource?.FilePath,
                    "Internal compiler error while transforming node " + node + ":\n" + e,
                    node);
            }
#endif
        }

        public void Push(IXamlAstNode node) => _context.PushParent(node);

        public void Pop() => _context.PopParent();
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
