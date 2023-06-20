using System;
using System.Reflection;
using Avalonia.Platform;
using Avalonia.Platform.Internal;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests;

public class AssetLoaderTests
{
    private IAssemblyDescriptorResolver _resolver;

    public class MockAssembly : Assembly { }

    private const string AssemblyNameWithWhitespace = "Awesome Library";

    private const string AssemblyNameWithNonAscii = "Какое-то-название";

    public AssetLoaderTests()
    {
        _resolver = Mock.Of<IAssemblyDescriptorResolver>();

        var descriptor = CreateAssemblyDescriptor(AssemblyNameWithWhitespace);
        Mock.Get(_resolver).Setup(x => x.GetAssembly(AssemblyNameWithWhitespace)).Returns(descriptor);

        descriptor = CreateAssemblyDescriptor(AssemblyNameWithNonAscii);
        Mock.Get(_resolver).Setup(x => x.GetAssembly(AssemblyNameWithNonAscii)).Returns(descriptor);
    }

    [Fact]
    public void AssemblyName_With_Whitespace_Should_Load_Resm()
    {
        var uri = new Uri($"resm:Avalonia.Base.UnitTests.Assets.something?assembly={AssemblyNameWithWhitespace}");
        var loader = new StandardAssetLoader(_resolver);

        var assemblyActual = loader.GetAssembly(uri, null);

        Assert.Equal(AssemblyNameWithWhitespace, assemblyActual?.FullName);
    }

    [Fact(Skip = "RegisterResUriParsers breaks this test. See https://github.com/AvaloniaUI/Avalonia/issues/2555.")]
    public void AssemblyName_With_Non_ASCII_Should_Load_Avares()
    {
        var uri = new Uri($"avares://{AssemblyNameWithNonAscii}/Assets/something");
        var loader = new StandardAssetLoader(_resolver);

        var assemblyActual = loader.GetAssembly(uri, null);

        Assert.Equal(AssemblyNameWithNonAscii, assemblyActual?.FullName);
    }

    [Fact]
    public void Invalid_AssemblyName_Should_Yield_Empty_Enumerable()
    {
        var uri = new Uri($"avares://InvalidAssembly");
        var loader = new StandardAssetLoader(_resolver);

        var assemblyActual = loader.GetAssets(uri, null);

        Assert.Empty(assemblyActual);
    }

    private static IAssemblyDescriptor CreateAssemblyDescriptor(string assemblyName)
    {
        var assembly = Mock.Of<MockAssembly>();
        Mock.Get(assembly).Setup(x => x.GetName()).Returns(new AssemblyName(assemblyName));
        Mock.Get(assembly).Setup(x => x.FullName).Returns(assemblyName);

        var descriptor = Mock.Of<IAssemblyDescriptor>();
        Mock.Get(descriptor).Setup(x => x.Assembly).Returns(assembly);
        return descriptor;
    }
}
