using System.Collections.Generic;
using System.Linq;
using Avalonia.Markup.Parsers;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core
{
    public class PropertyPathGrammarTests
    {
        static void Check(string s, params PropertyPathGrammar.ISyntax[] expected)
        {
            var parsed = PropertyPathGrammar.Parse(s).ToList();
            Assert.Equal(expected.Length, parsed.Count);
            for (var c = 0; c < parsed.Count; c++)
                Assert.Equal(expected[c], parsed[c]);
        }

        [Fact]
        public void PropertyPath_Should_Support_Simple_Properties()
        {
            Check("SomeProperty", new PropertyPathGrammar.PropertySyntax {Name = "SomeProperty"});
        }

        [Fact]
        public void PropertyPath_Should_Ignore_Trailing_Whitespace()
        {
            Check("  SomeProperty   ", new PropertyPathGrammar.PropertySyntax {Name = "SomeProperty"});
        }

        [Fact]
        public void PropertyPath_Should_Support_Qualified_Properties()
        {
            Check(" ( somens:SomeType.SomeProperty ) ",
                new PropertyPathGrammar.TypeQualifiedPropertySyntax()
                {
                    Name = "SomeProperty", TypeName = "SomeType", TypeNamespace = "somens"
                });
        }
        
        [Fact]
        public void PropertyPath_Should_Support_Property_Paths()
        {
            Check(" ( somens:SomeType.SomeProperty ).Child . SubChild ",
                new PropertyPathGrammar.TypeQualifiedPropertySyntax()
                {
                    Name = "SomeProperty", TypeName = "SomeType", TypeNamespace = "somens"
                },
                PropertyPathGrammar.ChildTraversalSyntax.Instance,
                new PropertyPathGrammar.PropertySyntax {Name = "Child"},
                PropertyPathGrammar.ChildTraversalSyntax.Instance,
                new PropertyPathGrammar.PropertySyntax {Name = "SubChild"}
            );
        }
        
        [Fact]
        public void PropertyPath_Should_Support_Casts()
        {
            Check(" ( somens:SomeType.SomeProperty ) :> SomeType.Child as somens:SomeType . SubChild ",
                new PropertyPathGrammar.TypeQualifiedPropertySyntax()
                {
                    Name = "SomeProperty", TypeName = "SomeType", TypeNamespace = "somens"
                },
                new PropertyPathGrammar.CastTypeSyntax
                {
                    TypeName = "SomeType"
                },
                PropertyPathGrammar.ChildTraversalSyntax.Instance,
                new PropertyPathGrammar.PropertySyntax {Name = "Child"},
                new PropertyPathGrammar.CastTypeSyntax
                {
                    TypeName = "SomeType",
                    TypeNamespace = "somens"
                },
                PropertyPathGrammar.ChildTraversalSyntax.Instance,
                new PropertyPathGrammar.PropertySyntax {Name = "SubChild"}
            );
        }
        
        [Fact]
        public void PropertyPath_Should_Support_Ensure_Type()
        {
            Check(" ( somens:SomeType.SomeProperty ) := SomeType.Child := somens:SomeType . SubChild ",
                new PropertyPathGrammar.TypeQualifiedPropertySyntax()
                {
                    Name = "SomeProperty", TypeName = "SomeType", TypeNamespace = "somens"
                },
                new PropertyPathGrammar.EnsureTypeSyntax
                {
                    TypeName = "SomeType"
                },
                PropertyPathGrammar.ChildTraversalSyntax.Instance,
                new PropertyPathGrammar.PropertySyntax {Name = "Child"},
                new PropertyPathGrammar.EnsureTypeSyntax
                {
                    TypeName = "SomeType",
                    TypeNamespace = "somens"
                },
                PropertyPathGrammar.ChildTraversalSyntax.Instance,
                new PropertyPathGrammar.PropertySyntax {Name = "SubChild"}
            );
        }
    }
}
