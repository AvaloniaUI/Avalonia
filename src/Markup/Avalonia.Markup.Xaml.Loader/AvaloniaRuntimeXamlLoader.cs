using System;
using System.IO;
using System.Reflection;
using System.Text;
using Avalonia.Markup.Xaml.XamlIl;
// ReSharper disable CheckNamespace

namespace Avalonia.Markup.Xaml
{
    public static class AvaloniaRuntimeXamlLoader
    {
        /// <summary>
        /// Loads XAML from a string.
        /// </summary>
        /// <param name="xaml">The string containing the XAML.</param>
        /// <param name="localAssembly">Default assembly for clr-namespace:</param>
        /// <param name="rootInstance">
        /// The optional instance into which the XAML should be loaded.
        /// </param>
        /// <returns>The loaded object.</returns>
        public static object Load(string xaml, Assembly localAssembly = null, object rootInstance = null, Uri uri = null, bool designMode = false)
        {
            Contract.Requires<ArgumentNullException>(xaml != null);

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
            {
                return Load(stream, localAssembly, rootInstance, uri, designMode);
            }
        }

        public static object Load(Stream stream, Assembly localAssembly, object rootInstance = null, Uri uri = null,
            bool designMode = false)
            => AvaloniaXamlIlRuntimeCompiler.Load(stream, localAssembly, rootInstance, uri, designMode);

        public static object Parse(string xaml, Assembly localAssembly = null)
            => Load(xaml, localAssembly);

        public static T Parse<T>(string xaml, Assembly localAssembly = null)
            => (T)Parse(xaml, localAssembly);
            
    }
}
