using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text;
using Avalonia.Markup.Xaml.XamlIl;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions;

#nullable enable
namespace Avalonia.Markup.Xaml
{
    public static class AvaloniaRuntimeXamlLoader
    {
        /// <summary>
        /// Loads XAML from a string.
        /// </summary>
        /// <param name="xaml">The string containing the XAML.</param>
        /// <param name="localAssembly">Default assembly for clr-namespace:.</param>
        /// <param name="rootInstance">The optional instance into which the XAML should be loaded.</param>
        /// <param name="uri">The URI of the XAML being loaded.</param>
        /// <param name="designMode">Indicates whether the XAML is being loaded in design mode.</param>
        /// <returns>The loaded object.</returns>
        [RequiresUnreferencedCode(XamlX.TrimmingMessages.DynamicXamlReference)]
        public static object Load(string xaml, Assembly? localAssembly = null, object? rootInstance = null, Uri? uri = null, bool designMode = false)
        {
            xaml = xaml ?? throw new ArgumentNullException(nameof(xaml));

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
            {
                return Load(stream, localAssembly, rootInstance, uri, designMode);
            }
        }

        /// <summary>
        /// Loads XAML from a stream.
        /// </summary>
        /// <param name="stream">The stream containing the XAML.</param>
        /// <param name="localAssembly">Default assembly for clr-namespace:</param>
        /// <param name="rootInstance">The optional instance into which the XAML should be loaded.</param>
        /// <param name="uri">The URI of the XAML being loaded.</param>
        /// <param name="designMode">Indicates whether the XAML is being loaded in design mode.</param>
        /// <returns>The loaded object.</returns>
        [RequiresUnreferencedCode(XamlX.TrimmingMessages.DynamicXamlReference)]
        public static object Load(Stream stream, Assembly? localAssembly = null, object? rootInstance = null, Uri? uri = null,
            bool designMode = false)
            => AvaloniaXamlIlRuntimeCompiler.Load(new RuntimeXamlLoaderDocument(uri, rootInstance, stream),
                new RuntimeXamlLoaderConfiguration { DesignMode = designMode, LocalAssembly = localAssembly });

        /// <summary>
        /// Loads XAML from a stream.
        /// </summary>
        /// <param name="document">The stream containing the XAML.</param>
        /// <param name="configuration">Xaml loader configuration.</param>
        /// <returns>The loaded object.</returns>
        [RequiresUnreferencedCode(XamlX.TrimmingMessages.DynamicXamlReference)]
        public static object Load(RuntimeXamlLoaderDocument document, RuntimeXamlLoaderConfiguration? configuration = null)
            => AvaloniaXamlIlRuntimeCompiler.Load(document, configuration ?? new RuntimeXamlLoaderConfiguration());

        /// <summary>
        /// Loads group of XAML files from a stream.
        /// </summary>
        /// <param name="documents">Collection of documents.</param>
        /// <param name="configuration">Xaml loader configuration.</param>
        /// <returns>The loaded objects per each input document. If document was removed, the element by index is null.</returns>
        [RequiresUnreferencedCode(XamlX.TrimmingMessages.DynamicXamlReference)]
        public static IReadOnlyList<object?> LoadGroup(IReadOnlyCollection<RuntimeXamlLoaderDocument> documents, RuntimeXamlLoaderConfiguration? configuration = null)
            => AvaloniaXamlIlRuntimeCompiler.LoadGroup(documents, configuration ?? new RuntimeXamlLoaderConfiguration());

        /// <summary>
        /// Parse XAML from a string.
        /// </summary>
        /// <param name="xaml">The string containing the XAML.</param>
        /// <param name="localAssembly">Default assembly for clr-namespace:.</param>
        /// <returns>The loaded object.</returns>
        [RequiresUnreferencedCode(XamlX.TrimmingMessages.DynamicXamlReference)]
        public static object Parse(string xaml, Assembly? localAssembly = null)
            => Load(xaml, localAssembly);

        /// <summary>
        /// Parse XAML from a string.
        /// </summary>
        /// <typeparam name="T">The type of the returned object.</typeparam>
        /// <param name="xaml">>The string containing the XAML.</param>
        /// <param name="localAssembly">>Default assembly for clr-namespace:.</param>
        /// <returns>The loaded object.</returns>
        [RequiresUnreferencedCode(XamlX.TrimmingMessages.DynamicXamlReference)]
        public static T Parse<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string xaml, Assembly? localAssembly = null)
            => (T)Parse(xaml, localAssembly);
    }
}
