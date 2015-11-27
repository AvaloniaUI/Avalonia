using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OmniXaml;
using Perspex.Controls;
using Perspex.Controls.Platform;
using Perspex.Markup.Xaml;
using Perspex.Themes.Default;

namespace Perspex.Designer
{
    class Designer
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
            Api = new DesignerApi(shared) {UpdateXaml = UpdateXaml, SetScalingFactor = SetScalingFactor};
            new DesignerApp();
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
            using (PlatformManager.DesignerMode())
            {
                var obj = ((XamlXmlLoader)new PerspexXamlLoader()).Load(new MemoryStream(Encoding.UTF8.GetBytes(xaml)));
                window = obj as Window;
                if (window == null)
                {
                    window = new Window() {Content = obj};
                }
            }
            s_currentWindow?.Close();
            s_currentWindow = window;
            window.Show();
            Api.OnWindowCreated?.Invoke(window.PlatformImpl.Handle.Handle);
            Api.OnResize?.Invoke();
        }
    }
}
