using System.IO;
using Avalonia.Platform.Internal;
using Xunit;

namespace Avalonia.Base.UnitTests;

public class AssemblyDescriptorResolverTests
{
    [Theory]
    [InlineData("MyApp.Views.dll", "MyApp.Views")]
    [InlineData("MyApp.Views.exe", "MyApp.Views")]
    [InlineData("MyApp.Views", "MyApp.Views")]
    [InlineData("myapp.views.DLL", "MyApp.Views")]
    [InlineData("AvaloniaEdit.resources.dll", "AvaloniaEdit.resources")]
    public void Scope_Name_Of_The_Assembly_Matches(string scopeName, string name)
    {
        Assert.True(AssemblyDescriptorResolver.IsScopeNameOf(scopeName, name));
    }

    [Theory]
    [InlineData("MyApp.Views.Tests.dll", "MyApp.Views")]
    [InlineData("MyApp.Views.Extra.dll", "MyApp.Views")]
    [InlineData("MyApp.ViewsX.dll", "MyApp.Views")]
    [InlineData("MyApp.Views.Tests", "MyApp.Views")]
    [InlineData("MyApp.dll", "MyApp.Views")]
    [InlineData("AvaloniaEdit.resources.dll", "AvaloniaEdit")]
    public void Scope_Name_Of_Another_Assembly_Starting_With_The_Name_Does_Not_Match(string scopeName, string name)
    {
        Assert.False(AssemblyDescriptorResolver.IsScopeNameOf(scopeName, name));
    }

    [Fact]
    public void GetAssembly_Returns_The_Loaded_Assembly_Of_The_Name()
    {
        var resolver = new AssemblyDescriptorResolver();

        var descriptor = resolver.GetAssembly("Avalonia.Base");

        Assert.Same(typeof(AvaloniaObject).Assembly, descriptor.Assembly);
    }

    [Fact]
    public void GetAssembly_Does_Not_Return_A_Loaded_Assembly_Whose_Name_Only_Starts_With_The_Name()
    {
        // Avalonia.Base.UnitTests is loaded, Avalonia.Base.Unit doesn't exist
        var resolver = new AssemblyDescriptorResolver();

        Assert.ThrowsAny<FileNotFoundException>(() => resolver.GetAssembly("Avalonia.Base.Unit"));
    }
}
