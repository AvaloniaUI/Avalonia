using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Avalonia.DesignerSupport
{
    public class DesignWindowLoader
    {
        public static Window LoadDesignerWindow(string xaml, string assemblyPath, string xamlFileProjectPath)
        {
            Window window;
            Control control;
            using (PlatformManager.DesignerMode())
            {
                var loader = new AvaloniaXamlLoader() {IsDesignMode = true};
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml));


                
                Uri baseUri = null;
                if (assemblyPath != null)
                {
                    if (xamlFileProjectPath == null)
                        xamlFileProjectPath = "/Designer/Fake.xaml";
                    //Fabricate fake Uri
                    baseUri =
                        new Uri($"avares://{Path.GetFileNameWithoutExtension(assemblyPath)}{xamlFileProjectPath}");
                }

                var localAsm = assemblyPath != null ? Assembly.LoadFile(Path.GetFullPath(assemblyPath)) : null;
                var loaded = loader.Load(stream, localAsm, null, baseUri);
                var style = loaded as IStyle;
                if (style != null)
                {
                    var substitute = Design.GetPreviewWith((AvaloniaObject)style);
                    if (substitute != null)
                    {
                        substitute.Styles.Add(style);
                        control = substitute;
                    }
                    else
                        control = new StackPanel
                        {
                            Children =
                            {
                                new TextBlock {Text = "Styles can't be previewed without Design.PreviewWith. Add"},
                                new TextBlock {Text = "<Design.PreviewWith>"},
                                new TextBlock {Text = "    <Border Padding=20><!-- YOUR CONTROL FOR PREVIEW HERE --></Border>"},
                                new TextBlock {Text = "</Design.PreviewWith>"},
                                new TextBlock {Text = "before setters in your first Style"}
                            }
                        };
                }
                else if (loaded is Application)
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
            window.Show();
            Design.ApplyDesignModeProperties(window, control);
            return window;
        }
    }
}
