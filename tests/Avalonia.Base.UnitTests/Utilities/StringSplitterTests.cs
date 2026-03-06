using System;
using System.Collections.Generic;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Utilities;

public class StringSplitterTests
{
    #region Tests without brackets - should match string.Split behavior

    [Fact]
    public void SplitRespectingBrackets_WithoutBrackets_NullReturnsEmptyArray()
    {
        var result = StringSplitter.SplitRespectingBrackets(null, ',');
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    [InlineData("abc")]
    [InlineData("a,b,c")]
    [InlineData("a,,c")]
    [InlineData("a,,,b")]
    [InlineData(",a,b,")]
    [InlineData(" a , b , c ")]
    [InlineData(" a ,,,, c ")]
    [InlineData(" a ,  , c ")]
    [InlineData(" a, b ,c ")]
    [InlineData(" , a , b , ")]
    [InlineData("  a  ,  b  ,  c  ")]
    [InlineData("First,Second,Third")]
    [InlineData("Header\nBody\nFooter\n", '\n')]
    [InlineData("Width;Height;Margin;Padding", ';')]
    [InlineData("Avalonia.Utilities.StringSplitter", '.')]
    public void SplitRespectingBrackets_WithoutBrackets_SingleSeparator(string input, char separator = ',')
    {
        foreach (var options in EnumerateStringSplitOptionsCombinations())
        {
            var result = StringSplitter.SplitRespectingBrackets(input, separator, options: options);
            var expected = input.Split(separator, options);
            Assert.Equal(expected, result);
        }
    }

    [Theory]
    [InlineData("a,b;c,d")]
    [InlineData("a,b;,;c,d")]
    [InlineData(" a , b ; c , d ")]
    [InlineData(" a , b ; ; c , d ")]
    [InlineData(" a , b ;,; c , d ")]
    [InlineData(" ; a , b ; c , d ; ")]
    public void SplitRespectingBrackets_WithoutBrackets_MultipleSeparators(string input)
    {
        char[] separators = [',', ';'];
        foreach (var options in EnumerateStringSplitOptionsCombinations())
        {
            var result = StringSplitter.SplitRespectingBrackets(input, separators, options: options);
            var expected = input.Split(separators, options);
            Assert.Equal(expected, result);
        }
    }

    #endregion

    #region Tests with brackets - should respect bracket pairs

    [Theory]
    [InlineData("(a)(b,c)", new[] { "(a)(b,c)" })]
    [InlineData("a,(),b", new[] { "a", "()", "b" })]
    [InlineData("a,(b,c),d", new[] { "a", "(b,c)", "d" })]
    [InlineData("a,(b,(c,d)),e", new[] { "a", "(b,(c,d))", "e" })]
    [InlineData(",a,(b,c),d,", new[] { "", "a", "(b,c)", "d", "" })]
    [InlineData("(a,b),(c,d),(e,f)", new[] { "(a,b)", "(c,d)", "(e,f)" })]
    [InlineData("a,(b,(c,(d,e))),f", new[] { "a", "(b,(c,(d,e)))", "f" })]
    [InlineData("Button,TextBox(Width=100,Height=50),Label", new[] { "Button", "TextBox(Width=100,Height=50)", "Label" })]
    [InlineData("string,List(int),Dictionary(string,object)", new[] { "string", "List(int)", "Dictionary(string,object)" })]
    [InlineData("FirstItem,Item(param1,param2,param3),x,VeryLongItemName(a,b),Short", new[] { "FirstItem", "Item(param1,param2,param3)", "x", "VeryLongItemName(a,b)", "Short" })]
    [InlineData("BindingPath,Converter(Type=MyConverter,Parameter=Value123),Mode=TwoWay", new[] { "BindingPath", "Converter(Type=MyConverter,Parameter=Value123)", "Mode=TwoWay" })]
    [InlineData("Observable(List(Dictionary(string,int))),SimpleType,AnotherObservable(string)", new[] { "Observable(List(Dictionary(string,int)))", "SimpleType", "AnotherObservable(string)" })]
    [InlineData("OuterType(InnerType(DeepType(VeryDeepValue1,VeryDeepValue2),InnerValue),OuterValue)", new[] { "OuterType(InnerType(DeepType(VeryDeepValue1,VeryDeepValue2),InnerValue),OuterValue)" })]
    [InlineData("0 4 6 -1 #FF000000,0 2 4 -1 rgba(0,0,0,0.06),inset 0 1 2 0 rgba(255,255,255,0.1)", new[] { "0 4 6 -1 #FF000000", "0 2 4 -1 rgba(0,0,0,0.06)", "inset 0 1 2 0 rgba(255,255,255,0.1)" })]
    public void SplitRespectingBrackets_WithBrackets_DefaultBrackets(string input, string[] expected)
    {
        var result = StringSplitter.SplitRespectingBrackets(input, ',');
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("a,(b,c;d);e", new[] { "a", "(b,c;d)", "e" })]
    [InlineData("Width=100,Height=200;Margin(10;20;30;40),Padding=5", new[] { "Width=100", "Height=200", "Margin(10;20;30;40)", "Padding=5" })]
    public void SplitRespectingBrackets_WithBrackets_MultipleSeparators(string input, string[] expected)
    {
        var result = StringSplitter.SplitRespectingBrackets(input, [',', ';']);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("a,(b,c),d", '[', ']', new[] { "a", "(b", "c)", "d" })]
    [InlineData("a,[b,c],d", '[', ']', new[] { "a", "[b,c]", "d" })]
    [InlineData("x,<y,z>,w", '<', '>', new[] { "x", "<y,z>", "w" })]
    [InlineData("Property1,Property2[Index1,Index2],Property3", '[', ']', new[] { "Property1", "Property2[Index1,Index2]", "Property3" })]
    public void SplitRespectingBrackets_WithBrackets_CustomBrackets(string input, char openingBracket, char closingBracket, string[] expected)
    {
        var result = StringSplitter.SplitRespectingBrackets(input, ',', openingBracket, closingBracket);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("a,,(b,c),,d", StringSplitOptions.None, new[] { "a", "", "(b,c)", "", "d" })]
    [InlineData("a,,(b,c),,d", StringSplitOptions.RemoveEmptyEntries, new[] { "a", "(b,c)", "d" })]
    [InlineData(",a,(b,c),d,", StringSplitOptions.None, new[] { "", "a", "(b,c)", "d", "" })]
    [InlineData(",a,(b,c),d,", StringSplitOptions.RemoveEmptyEntries, new[] { "a", "(b,c)", "d" })]
    [InlineData(" a , (b, c) , d ", StringSplitOptions.None, new[] { " a ", " (b, c) ", " d " })]
    [InlineData(" a , (b, c) , d ", StringSplitOptions.TrimEntries, new[] { "a", "(b, c)", "d" })]
    [InlineData(" a ,  , (b, c) ,  , d ", StringSplitOptions.None, new[] { " a ", "  ", " (b, c) ", "  ", " d " })]
    [InlineData(" a ,  , (b, c) ,  , d ", StringSplitOptions.TrimEntries, new[] { "a", "", "(b, c)", "", "d" })]
    [InlineData(" a ,  , (b, c) ,  , d ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries, new[] { "a", "(b, c)", "d" })]
    [InlineData(" , a , ( b , ( c , d ) ) , , e , ", StringSplitOptions.None, new[] { " ", " a ", " ( b , ( c , d ) ) ", " ", " e ", " " })]
    [InlineData(" , a , ( b , ( c , d ) ) , , e , ", StringSplitOptions.TrimEntries, new[] { "", "a", "( b , ( c , d ) )", "", "e", "" })]
    [InlineData(" , a , ( b , ( c , d ) ) , , e , ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries, new[] { "a", "( b , ( c , d ) )", "e" })]
    public void SplitRespectingBrackets_WithBrackets_WithOptions(string input, StringSplitOptions options, string[] expected)
    {
        var result = StringSplitter.SplitRespectingBrackets(input, ',', options: options);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Tests for mismatched brackets - should throw exceptions

    [Theory]
    [InlineData("(")]
    [InlineData(")")]
    [InlineData(")a,b(")]
    [InlineData("a,b),c")]
    [InlineData("a,(b,c")]
    [InlineData("a,b))c")]
    [InlineData("a,((b,c)")]
    [InlineData("a,(b,(c)),d)")]
    [InlineData("x,[y,z", '[', ']')]
    [InlineData("x,y],z", '[', ']')]
    [InlineData("Type1,Type2(Inner1,Inner2)),Type3")]
    [InlineData("Property1,Property2(Parameter1,Parameter2,Property3")]
    [InlineData("OuterType(InnerType(DeepType(Value1,Value2),MiddleType(Value3)")]
    public void SplitRespectingBrackets_UnmatchedBrackets_ThrowsFormatException(string input, char openingBracket = '(', char closingBracket = ')')
    {
        Assert.Throws<FormatException>(() =>
            StringSplitter.SplitRespectingBrackets(input, ',', openingBracket, closingBracket));
    }

    [Theory]
    [InlineData('(', '(')]
    [InlineData('[', '[')]
    [InlineData('.', '.')]
    public void SplitRespectingBrackets_SameOpeningAndClosingBracket_ThrowsArgumentException(char bracket1, char bracket2)
    {
        var input = "a,b,c";

        Assert.Throws<ArgumentException>(() =>
            StringSplitter.SplitRespectingBrackets(input, ',', bracket1, bracket2));
    }

    #endregion

    private static IEnumerable<StringSplitOptions> EnumerateStringSplitOptionsCombinations()
    {
        yield return StringSplitOptions.None;
        yield return StringSplitOptions.RemoveEmptyEntries;
        yield return StringSplitOptions.TrimEntries;
        yield return StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries;
    }
}
