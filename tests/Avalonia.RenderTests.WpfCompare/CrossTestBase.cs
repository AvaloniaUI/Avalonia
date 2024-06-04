using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CrossUI;

namespace Avalonia.RenderTests.WpfCompare;

public class CrossTestBase
{
    private readonly string _groupName;
    public CrossTestBase(string groupName)
    {
        _groupName = groupName;
    }

    protected void RenderAndCompare(CrossControl root, [CallerMemberName] string? testName = null, double dpi = 96)
    {
        var dir = Path.Combine(GetTestsDirectory(), "TestFiles", "CrossTests", _groupName);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, testName + ".wpf.png");
        
        var w = root.Width;
        var h = root.Height;
        var pw = (int)Math.Ceiling(w * dpi / 96);
        var ph = (int)Math.Ceiling(h * dpi / 96);
        
        var control = new WpfCrossControl(root);
        control.Measure(new System.Windows.Size(w, h));
        control.Arrange(new System.Windows.Rect(0, 0, w, h));
        var bmp = new RenderTargetBitmap(pw, ph, dpi, dpi, PixelFormats.Default);
        bmp.Render(control);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bmp));
        using (var f = File.Create(path))
            encoder.Save(f);
    }
    
    static string GetTestsDirectory()
    {
        var path = Directory.GetCurrentDirectory();

        while (path.Length > 0 && Path.GetFileName(path) != "tests")
        {
            path = Path.GetDirectoryName(path)!;
        }

        return path;
    }
    
}
