namespace Perspex.Xaml.Desktop
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Resources;
    using OmniXaml.AppServices;

    public class InflatableResourceTranslator : IInflatableTranslator
    {
        public Stream GetStream(Type type)
        {
            var uri = GetUriFor(type);
            var assembly = Assembly.GetEntryAssembly();
            var resourceName = assembly.GetName().Name + ".g";
            var manager = new ResourceManager(resourceName, assembly);

            using (ResourceSet resourceSet = manager.GetResourceSet(CultureInfo.CurrentCulture, true, true))
            {
                Stream s = (Stream)resourceSet.GetObject(uri.ToString(), true);

                if (s == null)
                {
                    throw new IOException(
                        "The requested resource could not be found: " +
                        uri.OriginalString);
                }

                return s;
            }
        }

        private static Uri GetUriFor(Type type)
        {
            if (type.Namespace != null)
            {
                var toRemove = type.Assembly.GetName().Name;
                var substracted = toRemove.Length < type.Namespace.Length ? type.Namespace.Remove(0, toRemove.Length + 1) : "";
                var replace = substracted.Replace('.', Path.PathSeparator);
                if (replace != string.Empty)
                {
                    replace = replace + "/";
                }
                return new Uri(replace + type.Name + ".xaml", UriKind.Relative);
            }

            return null;
        }

        public Type GetTypeFor(Uri uri)
        {
            var withExt = uri.OriginalString;
            var lastSlash = withExt.LastIndexOf("/", StringComparison.Ordinal);
            var innerNs = withExt.Substring(0, lastSlash);
            var fileName = withExt.Substring(lastSlash + 1, withExt.Length - lastSlash - 1);

            var className = fileName.Substring(0, fileName.LastIndexOf('.'));


            var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            var type = from assembly in assemblies
                       let t = assembly.GetType(GetName(assembly, innerNs, className))
                       where t != null
                       select t;

            return type.First();
        }

        private static string GetName(Assembly assembly, string innerNs, string className)
        {
            var ns = assembly.GetName().Name + "." + innerNs;
            var fullLocator = ns + "." + className;
            return fullLocator;
        }
    }
}