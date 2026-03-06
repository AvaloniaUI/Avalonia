using System.IO;
using Avalonia.Platform.Internal;
using Xunit;

namespace Avalonia.Base.UnitTests;

public class SlicedStreamTests
{
    [Theory]
    [InlineData(2, SeekOrigin.Begin, 22, 2, 9)]
    [InlineData(2, SeekOrigin.Current, 22, 17, 24)]
    [InlineData(-2, SeekOrigin.End, 22, 40, 47)]
    public void Seek_Works(
        long offset,
        SeekOrigin origin,
        long startingUnderlyingPosition,
        long expectedPosition,
        long expectedUnderlyingPosition)
    {
        var memoryStream = new MemoryStream(new byte[1024]);
        var slicedStream = new SlicedStream(memoryStream, 7, 42);
        memoryStream.Position = startingUnderlyingPosition;

        slicedStream.Seek(offset, origin);

        Assert.Equal(expectedPosition, slicedStream.Position);
        Assert.Equal(expectedUnderlyingPosition, memoryStream.Position);
    }
}
