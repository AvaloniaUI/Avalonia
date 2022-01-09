using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;

namespace Sandbox;

public class ImageSource
{
    public ImageSource(string fileName)
    {
        FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
    }

    public string FileName { get; }

    public Bitmap Bitmap => new Bitmap(FileName);

    public async Task CopyImage()
    {
        await Application.Current!.Clipboard!.SetBitmapAsync(Bitmap);
    }
}
