using System;

namespace Avalonia.Controls
{
    public static class AndroidSceneCategory
    {
        public const string Default = "android.intent.category.DEFAULT";
        public const string Browsable = "android.intent.category.BROWSABLE";
        public const string Launcher = "android.intent.category.LAUNCHER";
        public const string LeanbackLauncher = "android.intent.category.LEANBACK_LAUNCHER";
    }

    public enum AndroidSceneLaunchMode
    {
        Multiple,
        SingleTask,
        SingleTop,
        SingleInstance,
        SingleInstancePerTask
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class AndroidSceneAttribute : Attribute
    {

        // ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode | ConfigChanges.SmallestScreenSize
        public const int DefaultConfigChanges = 128 | 1024 | 512 | 2048;
        public AndroidSceneAttribute(string label = "",
                                     string androidTheme = "",
                                     string iconResource = "",
                                     bool isExported = false,
                                     bool isLauncher = false,
                                     int configChanges = DefaultConfigChanges,
                                     AndroidSceneLaunchMode launchMode = AndroidSceneLaunchMode.Multiple,
                                     string[]? actions = null,
                                     string[]? categories = null,
                                     string[]? dataMimeTypes = null,
                                     string[]? dataPaths = null,
                                     string[]? dataSchemes = null)
        {
            Label = label;
            AndroidTheme = androidTheme;
            IconResource = iconResource;
            IsExported = isExported;
            IsLauncher = isLauncher;
            LaunchMode = launchMode;
            Actions = actions;
            Categories = categories;
            DataMimeTypes = dataMimeTypes;
            DataPaths = dataPaths;
            DataSchemes = dataSchemes;
        }

        public string Label { get; }
        public string AndroidTheme { get; }
        public string IconResource { get; }
        public bool IsExported { get; }
        public bool IsLauncher { get; }
        public AndroidSceneLaunchMode LaunchMode { get; }
        public string[]? Actions { get; }
        public string[]? Categories { get; }
        public string[]? DataMimeTypes { get; }
        public string[]? DataPaths { get; }
        public string[]? DataSchemes { get; }
    }
}
