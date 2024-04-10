using System;
using System.IO;
using System.Text;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests;

public class AvaloniaResourcesIndexTests
{
    [Fact]
    public void Should_Write_And_Read_The_Same_Resources()
    {
        using var memoryStream = new MemoryStream();

        var fooBytes = Encoding.UTF8.GetBytes("foo");
        var booBytes = Encoding.UTF8.GetBytes("boo");
        AvaloniaResourcesIndexReaderWriter.WriteResources(memoryStream,
            new[]
            {
                new AvaloniaResourcesEntry
                {
                    Path = "foo.xaml", Size = fooBytes.Length, Open = () => new MemoryStream(fooBytes)
                },
                new AvaloniaResourcesEntry
                {
                    Path = "boo.xaml", Size = booBytes.Length, Open = () => new MemoryStream(booBytes)
                }
            });

        memoryStream.Seek(4, SeekOrigin.Begin); // skip 4 bytes for "index size" field.

        var index = AvaloniaResourcesIndexReaderWriter.ReadIndex(memoryStream);
        var resourcesBasePosition = memoryStream.Position;

        Span<byte> buffer = stackalloc byte[index[0].Size];

        Assert.Equal("foo.xaml", index[0].Path);
        Assert.Equal(0, index[0].Offset);
        Assert.Equal(fooBytes.Length, index[0].Size);

        memoryStream.Seek(resourcesBasePosition + index[0].Offset, SeekOrigin.Begin);
        memoryStream.ReadExactly(buffer);
        Assert.Equal(fooBytes, buffer.ToArray());

        Assert.Equal("boo.xaml", index[1].Path);
        Assert.Equal(fooBytes.Length, index[1].Offset);
        Assert.Equal(booBytes.Length, index[1].Size);

        memoryStream.Seek(resourcesBasePosition + index[1].Offset, SeekOrigin.Begin);
        memoryStream.ReadExactly(buffer);
        Assert.Equal(booBytes, buffer.ToArray());
    }

    [Fact]
    public void Should_Combined_Same_Physical_Path_Resources()
    {
        using var memoryStream = new MemoryStream();

        var resourceBytes = Encoding.UTF8.GetBytes("resource-data");
        AvaloniaResourcesIndexReaderWriter.WriteResources(memoryStream, new[]
        {
            new AvaloniaResourcesEntry
            {
                Path = "app.xaml",
                SystemPath = "app.ico",
                Size = resourceBytes.Length,
                Open = () => new MemoryStream(resourceBytes)
            },
            new AvaloniaResourcesEntry
            {
                Path = "!__AvaloniaDefaultWindowIcon",
                SystemPath = "app.ico",
                Size = resourceBytes.Length,
                Open = () => new MemoryStream(resourceBytes)
            }
        });

        memoryStream.Seek(4, SeekOrigin.Begin); // skip 4 bytes for "index size" field.

        var index = AvaloniaResourcesIndexReaderWriter.ReadIndex(memoryStream);

        Assert.Equal("app.xaml", index[0].Path);
        Assert.Equal(0, index[0].Offset);
        Assert.Equal(resourceBytes.Length, index[0].Size);

        Assert.Equal("!__AvaloniaDefaultWindowIcon", index[1].Path);
        Assert.Equal(0, index[1].Offset);
        Assert.Equal(resourceBytes.Length, index[1].Size);
    }
}
