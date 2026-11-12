using Avalonia.Media;
using Avalonia.Platform;
using Xunit;
using static Avalonia.Impeller.UnitTests.ImpellerTestApp;

namespace Avalonia.Impeller.UnitTests;

public class RecordedLayerTests
{
    private static byte[] Read(ImpellerRecordedLayer layer)
    {
        using var locked = layer.Lock();
        var pixels = new byte[locked.Size.Width * locked.Size.Height * 4];
        System.Runtime.InteropServices.Marshal.Copy(locked.Address, pixels, 0, pixels.Length);
        return pixels;
    }

    [Fact]
    public void Partial_Redraws_Keep_Other_Content()
    {
        using var app = Start();
        using var layer = new ImpellerRecordedLayer(new PixelSize(20, 20), new Vector(96, 96));
        using (var context = layer.CreateDrawingContext())
        {
            context.Clear(Colors.Transparent);
            context.DrawRectangle(Brushes.Red, null, new RoundedRect(new Rect(0, 0, 20, 20)));
        }

        // Many dirty-rect redraws, enough to flatten the recordings.
        for (var i = 0; i < 12; i++)
        {
            using var context = layer.CreateDrawingContext();
            context.PushClip(new Rect(10, 10, 10, 10));
            context.Clear(Colors.Transparent);
            context.DrawRectangle(i % 2 == 0 ? Brushes.Lime : Brushes.Blue, null,
                new RoundedRect(new Rect(10, 10, 10, 10)));
            context.PopClip();
        }

        var pixels = Read(layer);
        Assert.Equal((255, 0, 0, 255), Pixel(pixels, 20, 5, 5));
        Assert.Equal((0, 0, 255, 255), Pixel(pixels, 20, 15, 15));
    }

    [Fact]
    public void Clearing_Everything_Replaces_Content()
    {
        using var app = Start();
        using var layer = new ImpellerRecordedLayer(new PixelSize(20, 20), new Vector(96, 96));
        using (var context = layer.CreateDrawingContext())
            context.DrawRectangle(Brushes.Red, null, new RoundedRect(new Rect(0, 0, 20, 20)));
        using (var context = layer.CreateDrawingContext())
        {
            context.Clear(Colors.Transparent);
            context.DrawRectangle(Brushes.Blue, null, new RoundedRect(new Rect(10, 10, 10, 10)));
        }

        var pixels = Read(layer);
        Assert.Equal(0, Pixel(pixels, 20, 5, 5).A);
        Assert.Equal((0, 0, 255, 255), Pixel(pixels, 20, 15, 15));
    }
}
