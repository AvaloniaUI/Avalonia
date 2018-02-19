using Avalonia.Logging;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Platform;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.Reflection;

[assembly:ExportAvaloniaModule("StylesLoader", typeof(StylesModule))]

namespace Avalonia.Markup.Xaml.Styling
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public sealed class AutoLoadStylesAttribute : Attribute
    {
        public AutoLoadStylesAttribute(string embeddedResourcesPath)
        {
            StylesXamlPath = embeddedResourcesPath;
        }

        public string StylesXamlPath { get; }
    }

    class StylesModule
    {
        public StylesModule()
        {
            var runtimePlatform = AvaloniaLocator.Current.GetService<IRuntimePlatform>();
            var globalStyles = AvaloniaLocator.Current.GetService<IGlobalStyles>();

            var xamlLoader = new AvaloniaXamlLoader();
            foreach (var assembly in runtimePlatform.GetLoadedAssemblies())
            {
                foreach (var stylesToLoad in assembly.GetCustomAttributes<AutoLoadStylesAttribute>())
                {
                    try
                    {
                        globalStyles.Styles.AddRange((IEnumerable<IStyle>)xamlLoader.Load(new Uri(stylesToLoad.StylesXamlPath)));
                    }
                    catch (Exception)
                    {
                        Logger.Warning("Styling", this, "Could not load styles from {0} in assembly {1}",
                            stylesToLoad.StylesXamlPath,
                            assembly.FullName);
                    }
                }
            }
        }
    }
}
