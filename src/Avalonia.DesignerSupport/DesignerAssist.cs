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
            Window window;
            Control control;

            using (PlatformManager.DesignerMode())
            {
                var loader = new AvaloniaXamlLoader();
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(xamlInfo.Xaml));


                
                Uri baseUri = null;
                if (xamlInfo.AssemblyPath != null)
                {
                    //Fabricate fake Uri
                    baseUri =
                        new Uri("resm:Fake.xaml?assembly=" + Path.GetFileNameWithoutExtension(xamlInfo.AssemblyPath));
                }

                var loaded = loader.Load(stream, null, baseUri);
                var styles = loaded as Styles;
                if (styles != null)
                {
                    var substitute = Design.GetPreviewWith(styles) ??
                                     styles.Select(Design.GetPreviewWith).FirstOrDefault(s => s != null);
                    if (substitute != null)
                    {
                        substitute.Styles.AddRange(styles);
                        control = substitute;
                    }
                    else
                        control = new StackPanel
                        {
                            Children =
                            {
                                new TextBlock {Text = "Styles can't be previewed without Design.PreviewWith. Add"},
                                new TextBlock {Text = "<Design.PreviewWith>"},
                                new TextBlock {Text = "    <Border Padding=20><!-- YOUR CONTROL FOR PREVIEW HERE--></Border>"},
                                new TextBlock {Text = "<Design.PreviewWith>"},
                                new TextBlock {Text = "before setters in your first Style"}
                            }
                        };
                }
                if (loaded is Application)
                    control = new TextBlock {Text = "Application can't be previewed in design view"};
                else
                    control = (Control) loaded;

                window = control as Window;
                if (window == null)
                {
                    window = new Window() {Content = (Control)control};
                }

                if (!window.IsSet(Window.SizeToContentProperty))
                    window.SizeToContent = SizeToContent.WidthAndHeight;
            }

            s_currentWindow?.Close();
            s_currentWindow = window;
            window.Show();
            Design.ApplyDesignerProperties(window, control);
            // ReSharper disable once PossibleNullReferenceException
            // Always not null at this point
            Api.OnWindowCreated?.Invoke(window.PlatformImpl.Handle.Handle);
            Api.OnResize?.Invoke();
        }
    }
}
