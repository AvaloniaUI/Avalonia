using System;
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
            _idl = idl.Clone();
            new AstRewriter(_idl.Attributes.Where(a => a.Name == "clr-map")
                .Select(x => x.Value.Trim().Split(' '))
                .ToDictionary(x => x[0], x => x[1])
            ).VisitAst(_idl);
            
            _extraUsings = _idl.Attributes.Where(u => u.Name == "clr-using").Select(u => u.Value).ToList();
            _namespace = _idl.GetAttribute("clr-namespace");
            var visibilityString = _idl.GetAttribute("clr-access");

            if (visibilityString == "internal")
                _visibility = SyntaxKind.InternalKeyword;
            else if (visibilityString == "public")
                _visibility = SyntaxKind.PublicKeyword;
            else
                throw new CodeGenException("Invalid clr-access attribute");
        }

        class AstRewriter : AstVisitor
        {
            private readonly Dictionary<string, string> _typeMap = new Dictionary<string, string>();

            public AstRewriter(Dictionary<string, string> typeMap)
            {
                _typeMap = typeMap;
            }

            void ConvertIntPtr(AstTypeNode type)
            {
                if (type.Name == "void" && type.PointerLevel > 0)
                {
                    type.Name = "IntPtr";
                    type.PointerLevel--;
                }
            }
            
            protected override void VisitStructMember(AstStructMemberNode member)
            {
                if (member.HasAttribute("intptr"))
                    ConvertIntPtr(member.Type);
                base.VisitStructMember(member);
            }

            protected override void VisitType(AstTypeNode type)
            {
                if (type.IsLink)
                {
                    type.PointerLevel++;
                    type.IsLink = false;
                }

                if (_typeMap.TryGetValue(type.Name, out var mapped))
                    type.Name = mapped;
                
                base.VisitType(type);
            }

            protected override void VisitArgument(AstInterfaceMemberArgumentNode argument)
            {
                if (argument.HasAttribute("intptr"))
                {
                    if(argument.Name == "retOut")
                        Console.WriteLine();
                    ConvertIntPtr(argument.Type);
                }

                base.VisitArgument(argument);
            }

            protected override void VisitInterfaceMember(AstInterfaceMemberNode member)
            {
                if (member.HasAttribute("intptr"))
                    ConvertIntPtr(member.ReturnType);
                if (member.HasAttribute("propget") && !member.Name.StartsWith("Get"))
                    member.Name = "Get" + member.Name;
                if (member.HasAttribute("propput") && !member.Name.StartsWith("Set"))
                    member.Name = "Set" + member.Name;
                base.VisitInterfaceMember(member);
            }
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
            {
                var dec =  EnumDeclaration(e.Name)
                    .WithModifiers(TokenList(Token(_visibility)))
                    .WithMembers(SeparatedList(e.Select(m =>
                    {
                        var member = EnumMemberDeclaration(m.Name);
                        if (m.Value != null)
                            return member.WithEqualsValue(EqualsValueClause(ParseExpression(m.Value)));
                        return member;
                    })));
                if (e.HasAttribute("flags"))
                    dec = dec.AddAttribute("System.Flags");
                return dec;
            }).ToArray());
        }
        
        NamespaceDeclarationSyntax GenerateStructs(NamespaceDeclarationSyntax ns)
        {
            return ns.AddMembers(_idl.Structs.Select(e =>
                StructDeclaration(e.Name)
                    .WithModifiers(TokenList(Token(_visibility)))
                    .AddAttribute("System.Runtime.InteropServices.StructLayout", "System.Runtime.InteropServices.LayoutKind.Sequential")
                    .AddModifiers(Token(SyntaxKind.UnsafeKeyword))
                    .WithMembers(new SyntaxList<MemberDeclarationSyntax>(SeparatedList(e.Select(m =>
                        DeclareField(m.Type.ToString(), m.Name, SyntaxKind.PublicKeyword)))))
            ).ToArray());
        }


        
    }
}
