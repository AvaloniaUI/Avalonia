// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Data;
using Avalonia.Markup.Xaml.PortableXaml;
using Avalonia.Platform;
using Portable.Xaml;

namespace Avalonia.Markup.Xaml
{
    /// <summary>
    /// Loads XAML for a avalonia application.
    /// </summary>
    public class AvaloniaXamlLoader
    {
        public bool IsDesignMode { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaXamlLoader"/> class.
        /// </summary>
        public AvaloniaXamlLoader()
        {
        }

        /// <summary>
        /// Loads the XAML into a Avalonia component.
        /// </summary>
        /// <param name="obj">The object to load the XAML into.</param>
        public static void Load(object obj)
        {
            Contract.Requires<ArgumentNullException>(obj != null);

            var loader = new AvaloniaXamlLoader();
            loader.Load(obj.GetType(), obj);
        }

        /// <summary>
        /// Loads the XAML for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="rootInstance">
        /// The optional instance into which the XAML should be loaded.
        /// </param>
        /// <returns>The loaded object.</returns>
        public object Load(Type type, object rootInstance = null)
        {
            Contract.Requires<ArgumentNullException>(type != null);

            // HACK: Currently Visual Studio is forcing us to change the extension of xaml files
            // in certain situations, so we try to load .xaml and if that's not found we try .xaml.
            // Ideally we'd be able to use .xaml everywhere
            var assetLocator = AvaloniaLocator.Current.GetService<IAssetLoader>();

            if (assetLocator == null)
            {
                throw new InvalidOperationException(
                    "Could not create IAssetLoader : maybe Application.RegisterServices() wasn't called?");
            }

            foreach (var uri in GetUrisFor(assetLocator, type))
            {
                if (assetLocator.Exists(uri))
                {
                    using (var stream = assetLocator.Open(uri))
                    {
                        var initialize = rootInstance as ISupportInitialize;
                        initialize?.BeginInit();
                        try
                        {
                            return Load(stream, type.Assembly, rootInstance, uri);
                        }
                        finally
                        {
                            initialize?.EndInit();
                        }
                    }
                }
            }

            throw new FileNotFoundException("Unable to find view for " + type.FullName);
        }

        /// <summary>
        /// Loads XAML from a URI.
        /// </summary>
        /// <param name="uri">The URI of the XAML file.</param>
        /// <param name="baseUri">
        /// A base URI to use if <paramref name="uri"/> is relative.
        /// </param>
        /// <param name="rootInstance">
        /// The optional instance into which the XAML should be loaded.
        /// </param>
        /// <returns>The loaded object.</returns>
        public object Load(Uri uri, Uri baseUri = null, object rootInstance = null)
        {
            Contract.Requires<ArgumentNullException>(uri != null);

            var assetLocator = AvaloniaLocator.Current.GetService<IAssetLoader>();

            if (assetLocator == null)
            {
                throw new InvalidOperationException(
                    "Could not create IAssetLoader : maybe Application.RegisterServices() wasn't called?");
            }

            var asset = assetLocator.OpenAndGetAssembly(uri, baseUri);
            using (var stream = asset.stream)
            {
                var absoluteUri = uri.IsAbsoluteUri ? uri : new Uri(baseUri, uri);
                try
                {
                    return Load(stream, asset.assembly, rootInstance, absoluteUri);
                }
                catch (Exception e)
                {
                    throw new XamlLoadException("Error loading xaml at " + absoluteUri + ": " + e.Message, e);
                }
            }
        }

        /// <summary>
        /// Loads XAML from a string.
        /// </summary>
        /// <param name="xaml">The string containing the XAML.</param>
        /// <param name="localAssembly">Default assembly for clr-namespace:</param>
        /// <param name="rootInstance">
        /// The optional instance into which the XAML should be loaded.
        /// </param>
        /// <returns>The loaded object.</returns>
        public object Load(string xaml, Assembly localAssembly = null, object rootInstance = null)
        {
            Contract.Requires<ArgumentNullException>(xaml != null);

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
            {
                return Load(stream, localAssembly, rootInstance);
            }
        }

        /// <summary>
        /// Loads XAML from a stream.
        /// </summary>
        /// <param name="stream">The stream containing the XAML.</param>
        /// <param name="localAssembly">Default assembly for clr-namespace</param>
        /// <param name="rootInstance">
        /// The optional instance into which the XAML should be loaded.
        /// </param>
        /// <param name="uri">The URI of the XAML</param>
        /// <returns>The loaded object.</returns>
        public object Load(Stream stream, Assembly localAssembly, object rootInstance = null, Uri uri = null)
        {
            var readerSettings = new XamlXmlReaderSettings()
            {
                BaseUri = uri,
                LocalAssembly = localAssembly,
                ProvideLineInfo = true,
            };

            var context = IsDesignMode ? AvaloniaXamlSchemaContext.DesignInstance : AvaloniaXamlSchemaContext.Instance;
            var reader = new XamlXmlReader(stream, context, readerSettings);

            object result = LoadFromReader(
                reader,
                AvaloniaXamlContext.For(readerSettings, rootInstance));

            var topLevel = result as TopLevel;

            if (topLevel != null)
            {
                DelayedBinding.ApplyBindings(topLevel);
            }

            return result;
        }

        internal static object LoadFromReader(XamlReader reader, AvaloniaXamlContext context = null, IAmbientProvider parentAmbientProvider = null)
        {
            var writer = AvaloniaXamlObjectWriter.Create(
                                    (AvaloniaXamlSchemaContext)reader.SchemaContext,
                                    context,
                                    parentAmbientProvider);

            XamlServices.Transform(reader, writer);
            writer.ApplyAllDelayedProperties();
            return writer.Result;
        }

        internal static object LoadFromReader(XamlReader reader)
        {
            //return XamlServices.Load(reader);
            return LoadFromReader(reader, null);
        }


        private static readonly DataContractSerializer s_xamlInfoSerializer =
            new DataContractSerializer(typeof(AvaloniaResourceXamlInfo));
        /// <summary>
        /// Gets the URI for a type.
        /// </summary>
        /// <param name="assetLocator"></param>
        /// <param name="type">The type.</param>
        /// <returns>The URI.</returns>
        private static IEnumerable<Uri> GetUrisFor(IAssetLoader assetLocator, Type type)
        {
            var asm = type.GetTypeInfo().Assembly.GetName().Name;
            var xamlInfoUri = new Uri($"avares://{asm}/!AvaloniaResourceXamlInfo");
            var typeName = type.FullName;
            if (typeName == null)
                throw new ArgumentException("Type doesn't have a FullName");
            
            if (assetLocator.Exists(xamlInfoUri))
            {
                using (var xamlInfoStream = assetLocator.Open(xamlInfoUri))
                {
                    var assetDoc = XDocument.Load(xamlInfoStream);
                    XNamespace assetNs = assetDoc.Root.Attribute("xmlns").Value;
                    XNamespace arrayNs = "http://schemas.microsoft.com/2003/10/Serialization/Arrays";
                    Dictionary<string,string> xamlInfo =
                        assetDoc.Root.Element(assetNs + "ClassToResourcePathIndex").Elements(arrayNs + "KeyValueOfstringstring")
                         .ToDictionary(entry =>entry.Element(arrayNs + "Key").Value,
                                entry => entry.Element(arrayNs + "Value").Value);
                    
                    if (xamlInfo.TryGetValue(typeName, out var rv))
                    {
                        yield return new Uri($"avares://{asm}{rv}");
                        yield break;
                    }
                }
            }           
            
            yield return new Uri("resm:" + typeName + ".xaml?assembly=" + asm);
            yield return new Uri("resm:" + typeName + ".paml?assembly=" + asm);
        }
        
        public static object Parse(string xaml, Assembly localAssembly = null)
            => new AvaloniaXamlLoader().Load(xaml, localAssembly);

        public static T Parse<T>(string xaml, Assembly localAssembly = null)
            => (T)Parse(xaml, localAssembly);
    }
}
