// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using OmniXaml;
using Perspex.Platform;

namespace Perspex.Markup.Xaml
{
    using Context;
    using Controls;
    using Data;
    using OmniXaml.ObjectAssembler;

    /// <summary>
    /// Loads XAML for a perspex application.
    /// </summary>
    public class PerspexXamlLoader : XmlLoader
    {
        private static PerspexParserFactory s_parserFactory;
        private static IInstanceLifeCycleListener s_lifeCycleListener = new PerspexLifeCycleListener();

        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexXamlLoader"/> class.
        /// </summary>
        public PerspexXamlLoader()
            : this(GetParserFactory())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexXamlLoader"/> class.
        /// </summary>
        /// <param name="xamlParserFactory">The parser factory to use.</param>
        public PerspexXamlLoader(IParserFactory xamlParserFactory)
            : base(xamlParserFactory)
        {
        }

        /// <summary>
        /// Loads the XAML into a Perspex component.
        /// </summary>
        /// <param name="obj">The object to load the XAML into.</param>
        public static void Load(object obj)
        {
            Contract.Requires<ArgumentNullException>(obj != null);

            var loader = new PerspexXamlLoader();
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
            // in certain situations, so we try to load .xaml and if that's not found we try .paml.
            // Ideally we'd be able to use .xaml everywhere
            var assetLocator = PerspexLocator.Current.GetService<IAssetLoader>();

            if (assetLocator == null)
            {
                throw new InvalidOperationException(
                    "Could not create IAssetLoader : maybe Application.RegisterServices() wasn't called?");
            }

            foreach (var uri in GetUrisFor(type))
            {
                if (assetLocator.Exists(uri))
                {
                    using (var stream = assetLocator.Open(uri))
                    {
                        var initialize = rootInstance as ISupportInitialize;
                        initialize?.BeginInit();
                        return Load(stream, rootInstance);
                    }
                }
            }

            throw new FileNotFoundException("Unable to find view for " + type.FullName);
        }

        /// <summary>
        /// Loads XAML from a URI.
        /// </summary>
        /// <param name="uri">The URI of the XAML file.</param>
        /// <param name="rootInstance">
        /// The optional instance into which the XAML should be loaded.
        /// </param>
        /// <returns>The loaded object.</returns>
        public object Load(Uri uri, object rootInstance = null)
        {
            Contract.Requires<ArgumentNullException>(uri != null);

            var assetLocator = PerspexLocator.Current.GetService<IAssetLoader>();

            if (assetLocator == null)
            {
                throw new InvalidOperationException(
                    "Could not create IAssetLoader : maybe Application.RegisterServices() wasn't called?");
            }

            using (var stream = assetLocator.Open(uri))
            {
                return Load(stream, rootInstance);
            }
        }

        /// <summary>
        /// Loads XAML from a string.
        /// </summary>
        /// <param name="xaml">The string containing the XAML.</param>
        /// <param name="rootInstance">
        /// The optional instance into which the XAML should be loaded.
        /// </param>
        /// <returns>The loaded object.</returns>
        public object Load(string xaml, object rootInstance = null)
        {
            Contract.Requires<ArgumentNullException>(xaml != null);

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
            {
                return Load(stream, rootInstance);
            }
        }

        private static PerspexParserFactory GetParserFactory()
        {
            if (s_parserFactory == null)
            {
                s_parserFactory = new PerspexParserFactory();
            }

            return s_parserFactory;
        }

        /// <summary>
        /// Gets the URI for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The URI.</returns>
        private static IEnumerable<Uri> GetUrisFor(Type type)
        {
            var asm = type.GetTypeInfo().Assembly.GetName().Name;
            var typeName = type.FullName;
            yield return new Uri("resm:" + typeName + ".xaml?assembly=" + asm);
            yield return new Uri("resm:" + typeName + ".paml?assembly=" + asm);

        }

        private object Load(Stream stream, object rootInstance)
        {
            var result = base.Load(stream, new Settings
            {
                RootInstance = rootInstance,
                InstanceLifeCycleListener = s_lifeCycleListener,
            });

            var control = result as IControl;

            if (control != null)
            {
                DelayedBinding.ApplyBindings(control);
            }

            return result;
        }
    }
}
