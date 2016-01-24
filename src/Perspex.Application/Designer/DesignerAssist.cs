using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OmniXaml;
using Perspex.Controls;
using Perspex.Controls.Platform;
using Perspex.Markup.Xaml;
using Perspex.Platform;
using Perspex.Themes.Default;

namespace Perspex.DesignerSupport
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
                Styles = new DefaultTheme();
            }
        }

        public static DesignerApi Api { get; set; }
        
        public static void Init(Dictionary<string, object> shared)
        {
            Design.IsDesignMode = true;
            Api = new DesignerApi(shared) {UpdateXaml = UpdateXaml, SetScalingFactor = SetScalingFactor};
            Application.RegisterPlatformCallback(Application.InitializeWin32Subsystem);

            var plat = (IPclPlatformWrapper) Activator.CreateInstance(Assembly.Load(new AssemblyName("Perspex.Win32"))
                .DefinedTypes.First(typeof (IPclPlatformWrapper).GetTypeInfo().IsAssignableFrom).AsType());
            var app = plat.GetLoadedAssemblies()
                .SelectMany(a => a.DefinedTypes)
                .Where(typeof (Application).GetTypeInfo().IsAssignableFrom).FirstOrDefault(t => t.Assembly != typeof (Application).GetTypeInfo().Assembly);
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
                original =(Control)((XamlXmlLoader)new PerspexXamlLoader()).Load(new MemoryStream(Encoding.UTF8.GetBytes(xaml)));
                window = original as Window;
                if (window == null)
                {
                    window = new Window() {Content = original};
                }
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
