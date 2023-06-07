using System;
using System.IO;

namespace Avalonia.Diagnostics
{
    internal static class Conventions
    {
        public static string DefaultScreenshotsRoot
        {
            get
            {
                var dir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static IScreenshotHandler DefaultScreenshotHandler { get; } =
            new Screenshots.FilePickerHandler();
    }
}
