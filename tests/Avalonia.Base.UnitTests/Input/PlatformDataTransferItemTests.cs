using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Xunit;

namespace Avalonia.Base.UnitTests.Input;

public sealed class PlatformDataTransferItemTests
{
    [Fact]
    public void TryGetRaw_Should_Return_Null_When_Format_Is_Unknown()
    {
        var format = DataFormat.CreateBytesApplicationFormat("test-format");
        var item = new TestPlatformDataTransferItem([]);

        var value = item.TryGetRaw(format);

        Assert.Null(value);
    }

    [Fact]
    public void TryGetRaw_Should_Return_Expected_Value_When_Format_Is_Known()
    {
        var format = DataFormat.CreateBytesApplicationFormat("test-format");
        var item = new TestPlatformDataTransferItem([format]);

        var value = item.TryGetRaw(format);

        Assert.Same(format, value);
    }

    [Fact]
    public async Task TryGetRawAsync_Should_Return_Null_When_Format_Is_Unknown()
    {
        var format = DataFormat.CreateBytesApplicationFormat("test-format");
        var item = new TestPlatformDataTransferItem([]);

        var value = await item.TryGetRawAsync(format);

        Assert.Null(value);
    }

    [Fact]
    public async Task TryGetRawAsync_Should_Return_Expected_Value_When_Format_Is_Known()
    {
        var format = DataFormat.CreateBytesApplicationFormat("test-format");
        var item = new TestPlatformDataTransferItem([format]);

        var value = await item.TryGetRawAsync(format);

        Assert.Same(format, value);
    }

    private sealed class TestPlatformDataTransferItem(DataFormat[] dataFormats) : PlatformDataTransferItem
    {
        protected override DataFormat[] ProvideFormats()
            => dataFormats;

        protected override object TryGetRawCore(DataFormat format)
            => format;
    }
}
