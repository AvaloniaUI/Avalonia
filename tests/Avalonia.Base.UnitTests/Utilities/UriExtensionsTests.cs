using System;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Utilities;

public class UriExtensionsTests
{
    [Fact]
    public void Assembly_Name_From_Query_Parsed()
    {
        const string key = "assembly";
        const string value = "Avalonia.Themes.Simple";

        var uri = new Uri($"resm:Avalonia.Themes.Simple.Accents.BaseLight.xaml?{key}={value}");
        var name = uri.GetAssemblyNameFromQuery();

        Assert.Equal(value, name);
    }

    [Fact]
    public void Assembly_Name_From_Empty_Query_Not_Parsed()
    {
        var uri = new Uri("resm:Avalonia.Themes.Simple.Accents.BaseLight.xaml");
        var name = uri.GetAssemblyNameFromQuery();

        Assert.Equal(string.Empty, name);
    }
    
    [Theory]
    [InlineData("/home/Projects.txt")]
    [InlineData("/home/Stahování/Požární kniha 2.txt")]
    [InlineData("C:\\%51.txt")]
    [InlineData("/home/asd#xcv.txt")]
    [InlineData("C:\\\\Work\\Projects.txt")]
    public void Should_Convert_File_Path_To_Uri_And_Back(string path)
    {
        var uri = StorageProviderHelpers.FilePathToUri(path);

        Assert.Equal(path, uri.LocalPath);
    }
}
