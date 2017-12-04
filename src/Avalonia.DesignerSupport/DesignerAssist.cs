using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Themes.Default;

namespace Avalonia.DesignerSupport
{
    class DesignerAssist
    {
        class DesignerApp : Application
        {
            public override void Initialize()
            {
                Styles.Add(new DefaultTheme());

                var loader = new AvaloniaXamlLoader();
                var baseLight = (IStyle)loader.Load(
                    new Uri("resm:Avalonia.Themes.Default.Accents.BaseLight.xaml?assembly=Avalonia.Themes.Default"));
                Styles.Add(baseLight);
            }
        }

        public static DesignerApi Api { get; set; }
        
        public static void Init(Dictionary<string, object> shared)
        {
            Design.IsDesignMode = true;
            Api = new DesignerApi(shared) {UpdateXaml = UpdateXaml, UpdateXaml2 = UpdateXaml2, SetScalingFactor = SetScalingFactor};

            var runtimeAssembly = Assembly.Load(new AssemblyName("Avalonia.DotNetFrameworkRuntime"));

            var plat = (IRuntimePlatform) Activator.CreateInstance(runtimeAssembly
                .DefinedTypes.First(typeof (IRuntimePlatform).GetTypeInfo().IsAssignableFrom).AsType());
            
            TypeInfo app = null;
            var asms = plat.GetLoadedAssemblies();
            foreach (var asm in asms)
            {
                if(Equals(asm, typeof(Application).GetTypeInfo().Assembly) || Equals(asm, typeof(DesignerApp).GetTypeInfo().Assembly))
                    continue;
                try
                {
                    app = asm.DefinedTypes.Where(typeof (Application).GetTypeInfo().IsAssignableFrom).FirstOrDefault();
                    if (app != null)
                        break;

                }
                catch
                {
                    //Ignore, Assembly.DefinedTypes threw an exception, we can't do anything about that
                }
            }

            var builderType = runtimeAssembly.GetType("Avalonia.AppBuilder");

            var builder = (dynamic)Activator.CreateInstance(builderType,
                app == null ? new DesignerApp() : (Application) Activator.CreateInstance(app.AsType()));
            builder
                .UseWindowingSubsystem("Avalonia.Win32")
                .UseRenderingSubsystem("Avalonia.Direct2D1")
                .SetupWithoutStarting();
        }

        private static void SetScalingFactor(double factor)
        {
            PlatformManager.SetDesignerScalingFactor(factor);
            s_currentWindow?.PlatformImpl?.Resize(s_currentWindow.ClientSize);
        }

        static Window s_currentWindow;

        private static void UpdateXaml(string xaml) => UpdateXaml2(new DesignerApiXamlFileInfo
        {
            Xaml = xaml
        }.Dictionary);

        private static void UpdateXaml2(Dictionary<string, object> dic)
        {
            var xamlInfo = new DesignerApiXamlFileInfo(dic);
            Window window = DesignWindowLoader.LoadDesignerWindow(xamlInfo.Xaml, xamlInfo.AssemblyPath);

            s_currentWindow?.Close();
            s_currentWindow = window;
            
            // ReSharper disable once PossibleNullReferenceException
            // Always not null at this point
            Api.OnWindowCreated?.Invoke(window.PlatformImpl.Handle.Handle);
            Api.OnResize?.Invoke();
        }
    }
}
