using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Embedding.Offscreen;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Avalonia.DesignerSupport
{
    public class DesignWindowLoader
    {
        public static Window LoadDesignerWindow(string xaml, string assemblyPath, string xamlFileProjectPath)
            => LoadDesignerWindow(xaml, assemblyPath, xamlFileProjectPath, 1.0);

        public static Window LoadDesignerWindow(string xaml, string assemblyPath, string xamlFileProjectPath, double renderScaling)
        {
            Window window;
            Control control;
            using (PlatformManager.DesignerMode())
            {
                var loader = AvaloniaLocator.Current.GetRequiredService<AvaloniaXamlLoader.IRuntimeXamlLoader>();
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml));

                Uri baseUri = null;
                if (assemblyPath != null)
                {
                    if (xamlFileProjectPath == null)
                        xamlFileProjectPath = "/Fake.xaml";
                    //Fabricate fake Uri
                    baseUri =
                        new Uri($"avares://{Path.GetFileNameWithoutExtension(assemblyPath)}{xamlFileProjectPath}");
                }

                var localAsm = assemblyPath != null ? Assembly.LoadFrom(Path.GetFullPath(assemblyPath)) : null;
                var useCompiledBindings = localAsm?.GetCustomAttributes<AssemblyMetadataAttribute>()
                    .FirstOrDefault(a => a.Key == "AvaloniaUseCompiledBindingsByDefault")?.Value;

                var loaded = loader.Load(new RuntimeXamlLoaderDocument(baseUri, stream), new RuntimeXamlLoaderConfiguration
                {
                    LocalAssembly = localAsm,
                    DesignMode = true,
                    UseCompiledBindingsByDefault = bool.TryParse(useCompiledBindings, out var parsedValue) && parsedValue
                });

                if (loaded is IStyle style)
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
                                new TextBlock {Text = "    <Border Padding=\"20\"><!-- YOUR CONTROL FOR PREVIEW HERE --></Border>"},
                                new TextBlock {Text = "</Design.PreviewWith>"},
                                new TextBlock {Text = "before setters in your first Style"}
                            }
                        };
                }
                else if (loaded is ResourceDictionary resources)
                {
                    var substitute = Design.GetPreviewWith(resources);
                    if (substitute != null)
                    {
                        substitute.Resources.MergedDictionaries.Add(resources);
                        control = substitute;
                    }
                    else
                        control = new StackPanel
                        {
                            Children =
                            {
                                new TextBlock {Text = "ResourceDictionaries can't be previewed without Design.PreviewWith. Add"},
                                new TextBlock {Text = "<Design.PreviewWith>"},
                                new TextBlock {Text = "    <Border Padding=\"20\"><!-- YOUR CONTROL FOR PREVIEW HERE --></Border>"},
                                new TextBlock {Text = "</Design.PreviewWith>"},
                                new TextBlock {Text = "in your resource dictionary"}
                            }
                        };
                }
                else if (loaded is IDataTemplate template)
                {
                    if (Design.GetPreviewWith(template) is ContentControl substitute)
                    {
                        substitute.ContentTemplate = template;
                        control = substitute;
                    }
                    else
                        control = new StackPanel
                        {
                            Children =
                            {
                                new TextBlock {Text = "IDataTemplate can't be previewed without Design.PreviewWith."},
                                new TextBlock {Text = "Provide ContentControl with your design data as Content. Previewer will set ContentTemplate from this file."},
                                new TextBlock {Text = "<Design.PreviewWith>"},
                                new TextBlock {Text = "    <ContentControl Content=\"{x:Static YOUR DATA OBJECT HERE}\" ></Border>"},
                                new TextBlock {Text = "</Design.PreviewWith>"}
                            }
                        };
                }
                else if (loaded is Application)
                {
                    control = new TextBlock { Text = "This file cannot be previewed in design view" };
                }
                else if (loaded is AvaloniaObject avObject and not Window
                         && Design.GetPreviewWith(avObject) is { } previewWith)
                {
                    control = previewWith;
                }
                else
                {
                    control = (Control)loaded;
                }

                window = control as Window ?? new Window { Content = control };

                if (window.PlatformImpl is OffscreenTopLevelImplBase offscreenImpl)
                    offscreenImpl.RenderScaling = renderScaling;

                Design.ApplyDesignModeProperties(window, control);

                if (!window.IsSet(Window.SizeToContentProperty))
                {
                    if (double.IsNaN(window.Width))
                    {
                        window.SizeToContent |= SizeToContent.Width;
                    }

                    if (double.IsNaN(window.Height))
                    {
                        window.SizeToContent |= SizeToContent.Height;
                    }
                }
            }
            window.Show();
            return window;
        }
    }
}
