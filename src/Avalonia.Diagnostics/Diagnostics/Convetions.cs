using System;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics
{
    static class Convetions
    {
        public static string DefaultScreenshotsRoot =>
             System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures, Environment.SpecialFolderOption.Create),
                "Screenshots");

        public static IScreenshotHandler DefaultScreenshotHandler { get; } =
            new Screenshots.FilePickerHandler();
    }
}
