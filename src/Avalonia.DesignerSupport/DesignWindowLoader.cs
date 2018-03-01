using System;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Avalonia.DesignerSupport
{
    public class DesignWindowLoader
    {
        public static Window LoadDesignerWindow(string xaml, string assemblyPath)
        {
            Window window;
            Control control;
            using (PlatformManager.DesignerMode())
            {
                var loader = new AvaloniaXamlLoader();
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml));


                
                Uri baseUri = null;
                if (assemblyPath != null)
                {
                    //Fabricate fake Uri
                    baseUri =
                        new Uri("resm:Fake.xaml?assembly=" + Path.GetFileNameWithoutExtension(assemblyPath));
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
