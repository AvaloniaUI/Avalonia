using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MicroComGenerator.Ast;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
// ReSharper disable CoVariantArrayConversion

namespace MicroComGenerator
{
    public partial class CSharpGen
    {
        private readonly AstIdlNode _idl;
        private List<string> _extraUsings;
        private string _namespace;
        private SyntaxKind _visibility;
        private LocalInteropHelper _localInterop = new LocalInteropHelper();

        public CSharpGen(AstIdlNode idl)
        {
            _idl = idl;
            _extraUsings = _idl.Attributes.Where(u => u.Name == "clr-using").Select(u => u.Value).ToList();
            _namespace = _idl.Attributes.FirstOrDefault(x => x.Name == "clr-namespace")?.Value;
            if (_namespace == null)
                throw new CodeGenException("Missing clr-namespace attribute");
            var visibilityString = _idl.Attributes.FirstOrDefault(x => x.Name == "clr-access")?.Value;
            if (visibilityString == null)
                throw new CodeGenException("Missing clr-visibility attribute");
            if (visibilityString == "internal")
                _visibility = SyntaxKind.InternalKeyword;
            else if (visibilityString == "public")
                _visibility = SyntaxKind.PublicKeyword;
            else
                throw new CodeGenException("Invalid clr-access attribute");
        }

        public string Generate()
        {
            var ns = NamespaceDeclaration(ParseName(_namespace));
            var implNs = NamespaceDeclaration(ParseName(_namespace + ".Impl"));
            ns = GenerateEnums(ns);
            ns = GenerateStructs(ns);
            foreach (var i in _idl.Interfaces)
                GenerateInterface(ref ns, ref implNs, i);

            implNs = implNs.AddMembers(_localInterop.Class);
            var unit = Unit().AddMembers(ns, implNs);

            return Format(unit);
        }

        NamespaceDeclarationSyntax GenerateEnums(NamespaceDeclarationSyntax ns)
        {
            return ns.AddMembers(_idl.Enums.Select(e =>
                EnumDeclaration(e.Name)
                    .WithModifiers(TokenList(Token(_visibility)))
                    .WithMembers(SeparatedList(e.Select(m =>
                    {
                        var member = EnumMemberDeclaration(m.Name);
                        if (m.Value != null)
                            return member.WithEqualsValue(EqualsValueClause(ParseExpression(m.Value)));
                        return member;
                    })))
            ).ToArray());
        }
        
        NamespaceDeclarationSyntax GenerateStructs(NamespaceDeclarationSyntax ns)
        {
            return ns.AddMembers(_idl.Structs.Select(e =>
                StructDeclaration(e.Name)
                    .WithModifiers(TokenList(Token(_visibility)))
                    .AddModifiers(Token(SyntaxKind.UnsafeKeyword))
                    .AddAttributeLists(AttributeList(SingletonSeparatedList(
                        Attribute(ParseName("System.Runtime.InteropServices.StructLayout"),
                            AttributeArgumentList(SingletonSeparatedList(
                                AttributeArgument(
                                    ParseExpression("System.Runtime.InteropServices.LayoutKind.Sequential"))))
                        ))))
                    .WithMembers(new SyntaxList<MemberDeclarationSyntax>(SeparatedList(e.Select(m =>
                        DeclareField(m.Type.ToString(), m.Name, SyntaxKind.PublicKeyword)))))
            ).ToArray());
        }


        
    }
}
