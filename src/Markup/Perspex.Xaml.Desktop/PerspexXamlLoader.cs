namespace Perspex.Xaml.Desktop
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Resources;
    using OmniXaml;

    public class PerspexXamlLoader : XamlLoader
    {
        public PerspexXamlLoader()
            : this(new PerspexInflatableTypeFactory())
        {
        }

        public PerspexXamlLoader(ITypeFactory typeFactory) 
            : base(new PerspexParserFactory(typeFactory))
        {            
        }

        public void Load(Type type)
        {
            this.Load(GetUriFor(type));
        }

        public void Load(string path)
        {
            var assembly = Assembly.GetEntryAssembly();
            var resourceName = assembly.GetName().Name + ".g";
            var manager = new ResourceManager(resourceName, assembly);

            using (ResourceSet resourceSet = manager.GetResourceSet(CultureInfo.CurrentCulture, true, true))
            {
                var s = (Stream)resourceSet.GetObject(path, true);

                if (s == null)
                {
                    throw new IOException($"The requested resource could not be found: {path}");
                }

                this.Load(s);
            }
        }

        private static string GetUriFor(Type type)
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

                return replace + type.Name + ".xaml";
            }

            return null;
        }
    }
}