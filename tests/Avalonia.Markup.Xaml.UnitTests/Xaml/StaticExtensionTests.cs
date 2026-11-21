using System;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml;

// https://github.com/AvaloniaUI/Avalonia/issues/16929
public class StaticExtensionTests : XamlTestBase
{
    private static readonly Assembly LocalAssembly = typeof(StaticExtensionTests).Assembly;

    [Theory]
    [InlineData(nameof(InternalStaticResources.StaticProperty), "InternalStaticPropertyValue")]
    [InlineData(nameof(InternalStaticResources.StaticField), "InternalStaticFieldValue")]
    [InlineData(nameof(InternalStaticResources.StaticConstant), "InternalStaticConstantValue")]
    [InlineData(nameof(InternalStaticResources.ProtectedInternalStaticProperty), "ProtectedInternalStaticPropertyValue")]
    [InlineData(nameof(InternalStaticResources.PublicStaticProperty), "PublicStaticPropertyValue")]
    public void XStatic_Resolves_Assembly_Accessible_Members_In_Local_Assembly(string member, string expected)
    {
        var text = LoadText(member, LocalAssembly);

        Assert.Equal(expected, text);
    }

    [Fact]
    public void XStatic_Resolves_Internal_Members_Visible_Via_InternalsVisibleTo()
    {
        var text = LoadText(
            nameof(XamlInternalAccessResources.FriendResource),
            LocalAssembly,
            "clr-namespace:Avalonia.UnitTests;assembly=Avalonia.UnitTests",
            nameof(XamlInternalAccessResources));

        Assert.Equal(XamlInternalAccessResources.FriendResource, text);
    }

    [Theory]
    [InlineData("PrivateStaticProperty")]
    [InlineData("ProtectedStaticProperty")]
    [InlineData("PrivateProtectedStaticProperty")]
    public void XStatic_Does_Not_Resolve_Inaccessible_Members(string member)
    {
        var ex = Assert.ThrowsAny<Exception>(() => LoadText(member, LocalAssembly));

        Assert.Contains("Unable to resolve ", ex.ToString());
        Assert.Contains(member, ex.ToString());
        Assert.Contains("as static field, property, constant or enum value", ex.ToString());
    }

    [Fact]
    public void XStatic_Does_Not_Resolve_Internal_Members_From_Unrelated_Assembly()
    {
        var ex = Assert.ThrowsAny<Exception>(() =>
            LoadText(nameof(InternalStaticResources.StaticProperty), typeof(object).Assembly));

        Assert.Contains("Unable to resolve ", ex.ToString());
        Assert.Contains(nameof(InternalStaticResources.StaticProperty), ex.ToString());
    }

    private static string LoadText(
        string member,
        Assembly localAssembly,
        string xmlns = "clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests",
        string typeName = nameof(InternalStaticResources))
    {
        var xaml = $@"<TextBlock xmlns='https://github.com/avaloniaui'
            xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
            xmlns:local='{xmlns}'
            Text='{{x:Static local:{typeName}.{member}}}' />";

        return AvaloniaRuntimeXamlLoader.Parse<TextBlock>(xaml, localAssembly).Text!;
    }
}

internal class InternalStaticResources
{
    internal static string StaticProperty { get; } = "InternalStaticPropertyValue";
    internal static string StaticField = "InternalStaticFieldValue";
    internal const string StaticConstant = "InternalStaticConstantValue";
    protected internal static string ProtectedInternalStaticProperty { get; } = "ProtectedInternalStaticPropertyValue";
    public static string PublicStaticProperty { get; } = "PublicStaticPropertyValue";
    private static string PrivateStaticProperty { get; } = "PrivateStaticPropertyValue";
    protected static string ProtectedStaticProperty { get; } = "ProtectedStaticPropertyValue";
    private protected static string PrivateProtectedStaticProperty { get; } = "PrivateProtectedStaticPropertyValue";
}
