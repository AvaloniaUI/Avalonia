using Avalonia.Generators.Common;
using Xunit;

namespace Avalonia.Generators.Tests;

public class GlobPatternTests
{
    [Theory]
    [InlineData("*", "anything", true)]
    [InlineData("", "anything", false)]
    [InlineData("Views/*", "Views/SignUpView.xaml", true)]
    [InlineData("Views/*", "Extensions/SignUpView.xaml", false)]
    [InlineData("*SignUpView*", "Extensions/SignUpView.xaml", true)]
    [InlineData("*SignUpView.paml", "Extensions/SignUpView.xaml", false)]
    [InlineData("*.xaml", "Extensions/SignUpView.xaml", true)]
    public void Should_Match_Glob_Expressions(string pattern, string value, bool matches)
    {
        Assert.Equal(matches, new GlobPattern(pattern).Matches(value));
    }

    [Theory]
    [InlineData("Views/SignUpView.xaml", true, new[] { "*.xaml", "Extensions/*" })]
    [InlineData("Extensions/SignUpView.paml", true, new[] { "*.xaml", "Extensions/*" })]
    [InlineData("Extensions/SignUpView.paml", false, new[] { "*.xaml", "Views/*" })]
    [InlineData("anything", true, new[] { "*", "*" })]
    [InlineData("anything", false, new[] { "", "" })]
    public void Should_Match_Glob_Pattern_Groups(string value, bool matches, string[] patterns)
    {
        Assert.Equal(matches, new GlobPatternGroup(patterns).Matches(value));
    }
}
