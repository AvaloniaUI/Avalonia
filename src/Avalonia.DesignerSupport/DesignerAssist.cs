using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using OmniXaml;
using OmniXaml.ObjectAssembler;
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
            public DesignerApp()
            {
                RegisterServices();
                //For now we only support windows
                InitializeSubsystems(2);
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
            Api = new DesignerApi(shared) {UpdateXaml = UpdateXaml, SetScalingFactor = SetScalingFactor};
            Application.RegisterPlatformCallback(Application.InitializeWin32Subsystem);

            var plat = (IPclPlatformWrapper) Activator.CreateInstance(Assembly.Load(new AssemblyName("Avalonia.Win32"))
                .DefinedTypes.First(typeof (IPclPlatformWrapper).GetTypeInfo().IsAssignableFrom).AsType());
            TypeInfo app = null;
            foreach (var asm in plat.GetLoadedAssemblies())
            {
                if(Equals(asm, typeof(Application).GetTypeInfo().Assembly))
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
            if (app == null)
                new DesignerApp();
            else
                Activator.CreateInstance(app.AsType());
        }

        private static void SetScalingFactor(double factor)
        {
            PlatformManager.SetDesignerScalingFactor(factor);
            if (s_currentWindow != null)
                s_currentWindow.PlatformImpl.ClientSize = s_currentWindow.ClientSize;
        }

        static Window s_currentWindow;

        private static void UpdateXaml(string xaml)
        {
            Window window;
            Control original;

            using (PlatformManager.DesignerMode())
            {
                var loader = new AvaloniaXamlLoader();
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml));

                original = (Control)loader.Load(stream);
                window = original as Window;

                if (window == null)
                {
                    window = new Window() {Content = original};
                }

                if (!window.IsSet(Window.SizeToContentProperty))
                    window.SizeToContent = SizeToContent.WidthAndHeight;
            }

            s_currentWindow?.Close();
            s_currentWindow = window;
            window.Show();
            Design.ApplyDesignerProperties(window, original);
            Api.OnWindowCreated?.Invoke(window.PlatformImpl.Handle.Handle);
            Api.OnResize?.Invoke();
        }
    }
}
