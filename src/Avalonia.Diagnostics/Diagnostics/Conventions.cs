using System;

namespace Avalonia.Diagnostics
{
    internal static class Conventions
    {
        public static string DefaultScreenshotsRoot =>
             System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures, Environment.SpecialFolderOption.Create),
                "Screenshots");

        public static IScreenshotHandler DefaultScreenshotHandler { get; } =
            new Screenshots.FilePickerHandler();
    }
}
