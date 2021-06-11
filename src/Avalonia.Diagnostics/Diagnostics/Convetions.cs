using System;

namespace Avalonia.Diagnostics
{
    static class Convetions
    {
        public static string DefaultScreenShotRoot =>
             System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures, Environment.SpecialFolderOption.Create),
                "ScreenShot");
    }
}
