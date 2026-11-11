using System;

namespace Avalonia.Controls
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class AndroidSceneAttribute : Attribute
    {
        // ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode | ConfigChanges.SmallestScreenSize
        public const int DefaultConfigChanges = 128 | 1024 | 512 | 2048;
        public AndroidSceneAttribute(string label = "", string androidTheme = "", string iconResource = "", bool isExported = false, bool isLauncher = false, int configChanges = DefaultConfigChanges)
        {
            Label = label;
            AndroidTheme = androidTheme;
            IconResource = iconResource;
            IsExported = isExported;
            IsLauncher = isLauncher;
        }

        public string Label { get; }
        public string AndroidTheme { get; }
        public string IconResource { get; }
        public bool IsExported { get; }
        public bool IsLauncher { get; }
    }
}
