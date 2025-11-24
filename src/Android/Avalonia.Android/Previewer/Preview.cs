using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using AvDesign = Avalonia.Controls.Design;

namespace Avalonia.Android.Previewer
{
    internal class Preview(PreviewPresentation previewPresentation, TopLevel? topLevel, Assembly? assembly)
    {
        private Control? _parsed;
        private string? _designThemeVariant;

        public async Task UpdateXamlAsync(string xaml)
        {
            if (topLevel == null)
                return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml));
                    var loaderDocument = new RuntimeXamlLoaderDocument(null, stream);
                    var useCompiledBindings = assembly?.GetCustomAttributes<AssemblyMetadataAttribute>()
                        .FirstOrDefault(a => a.Key == "AvaloniaUseCompiledBindingsByDefault")?.Value;
                    var loaderConfig = new RuntimeXamlLoaderConfiguration
                    {
                        DesignMode = true,
                        LocalAssembly = assembly,
                        UseCompiledBindingsByDefault = bool.TryParse(useCompiledBindings, out var parsedValue) && parsedValue
                    };

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
                    if (AvaloniaRuntimeXamlLoader.Load(loaderDocument, loaderConfig) is { } loaded &&
                        ApplyPreview(loaded))
                    {
                        Invalidate();
                    }
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
                }
                catch (Exception)
                {
                    // ignored
                }
            });
        }

        internal void Invalidate()
        {
            if (topLevel != null)
                LayoutHelper.InvalidateSelfAndChildrenMeasure(topLevel);

            if(topLevel?.PlatformImpl is TopLevelImpl topLevelImpl)
            {
                topLevelImpl.DoPaint();
            }
        }

        private bool ApplyPreview(object root)
        {
            _parsed = root as Control;
            if (_parsed != null)
            {
                _designThemeVariant = _parsed.ActualThemeVariant?.Key.ToString() ?? "Default";
            }

            if (root is ResourceDictionary resources &&
                AvDesign.GetPreviewWith(resources) is { } resourcePreview)
            {
                resourcePreview.Resources.MergedDictionaries.Add(resources);
                topLevel!.Content = resourcePreview;
            }
            else if (root is IStyle style && root is AvaloniaObject ao &&
                     AvDesign.GetPreviewWith(ao) is { } stylePreview)
            {
                stylePreview.Styles.Add(style);
                topLevel!.Content = stylePreview;
            }
            else if (root is TopLevel)
            {
                // We can't host toplevels
                topLevel!.Content = null;
            }
            else
            {
                if(root is Visual visual)
                {
                    if (visual.IsSet(AvDesign.DataContextProperty))
                    {
                        visual.DataContext = visual.GetValue(AvDesign.DataContextProperty);
                    }
                }
                topLevel!.Content = root;
            }
            return true;
        }
    }
}
