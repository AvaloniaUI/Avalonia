using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace Avalonia.SourceGenerator.CompositionGenerator
{
    public static class Extensions
    {
        public static ClassDeclarationSyntax AddModifiers(this ClassDeclarationSyntax cl, params SyntaxKind[]? modifiers)
        {
            if (modifiers is null)
                return cl;
            return cl.AddModifiers(modifiers.Select(x => SyntaxFactory.Token(x)).ToArray());
        }
        
        public static MethodDeclarationSyntax AddModifiers(this MethodDeclarationSyntax cl, params SyntaxKind[]? modifiers)
        {
            if (modifiers is null)
                return cl;
            return cl.AddModifiers(modifiers.Select(x => SyntaxFactory.Token(x)).ToArray());
        }
        
        public static PropertyDeclarationSyntax AddModifiers(this PropertyDeclarationSyntax cl, params SyntaxKind[]? modifiers)
        {
            if (modifiers is null)
                return cl;
            return cl.AddModifiers(modifiers.Select(x => SyntaxFactory.Token(x)).ToArray());
        }

        public static ConstructorDeclarationSyntax AddModifiers(this ConstructorDeclarationSyntax cl, params SyntaxKind[]? modifiers)
        {
            if (modifiers is null)
                return cl;
            return cl.AddModifiers(modifiers.Select(x => SyntaxFactory.Token(x)).ToArray());
        }
        
        public static AccessorDeclarationSyntax AddModifiers(this AccessorDeclarationSyntax cl, params SyntaxKind[]? modifiers)
        {
            if (modifiers is null)
                return cl;
            return cl.AddModifiers(modifiers.Select(x => SyntaxFactory.Token(x)).ToArray());
        }
        
        public static EnumDeclarationSyntax AddModifiers(this EnumDeclarationSyntax cl, params SyntaxKind[]? modifiers)
        {
            if (modifiers is null)
                return cl;
            return cl.AddModifiers(modifiers.Select(x => SyntaxFactory.Token(x)).ToArray());
        }

        public static string WithLowerFirst(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }

        public static ExpressionSyntax MemberAccess(params string[]? identifiers)
        {
            if (identifiers == null || identifiers.Length == 0)
                throw new ArgumentException();
            var expr = (ExpressionSyntax)IdentifierName(identifiers[0]);
            for (var c = 1; c < identifiers.Length; c++)
                expr = MemberAccess(expr, identifiers[c]);
            return expr;
        }
        
        public static ExpressionSyntax MemberAccess(ExpressionSyntax expr, params string[] identifiers)
        {
            foreach (var i in identifiers)
                expr = MemberAccess(expr, i);
            return expr;
        }
        
        public static MemberAccessExpressionSyntax MemberAccess(ExpressionSyntax expr, string identifier) =>
            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expr, IdentifierName(identifier));

        public static ExpressionSyntax ConditionalMemberAccess(ExpressionSyntax expr, string member, bool checkNull)
        {
            if (checkNull)
                return ConditionalAccessExpression(expr, MemberBindingExpression(IdentifierName(member)));
            return MemberAccess(expr, member);
        }
        
        public static ClassDeclarationSyntax WithBaseType(this ClassDeclarationSyntax cl, string bt)
        {
            return cl.AddBaseListTypes(SimpleBaseType(SyntaxFactory.ParseTypeName(bt)));
        }

        public static string StripPrefix(this string s, string prefix) => string.IsNullOrEmpty(s)
            ? s
            : s.StartsWith(prefix)
                ? s.Substring(prefix.Length)
                : s;
    }
}
