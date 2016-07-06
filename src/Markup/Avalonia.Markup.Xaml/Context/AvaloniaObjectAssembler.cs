// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using OmniXaml;
using OmniXaml.ObjectAssembler;
using OmniXaml.ObjectAssembler.Commands;
using OmniXaml.TypeConversion;
using Avalonia.Markup.Xaml.Templates;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Avalonia.Markup.Xaml.Context
{
    public class AvaloniaObjectAssembler : IObjectAssembler
    {
        private readonly TemplateHostingObjectAssembler objectAssembler;
        private readonly ObjectAssembler assembler;

        public AvaloniaObjectAssembler(
            IRuntimeTypeSource typeSource, 
            ITopDownValueContext topDownValueContext, 
            Settings settings = null)
        {
            var mapping = new DeferredLoaderMapping();
            mapping.Map<Template>(x => x.Content, new TemplateLoader());
            mapping.Map<ControlTemplate>(x => x.Content, new TemplateLoader());
            mapping.Map<DataTemplate>(x => x.Content, new TemplateLoader());
            mapping.Map<FocusAdornerTemplate>(x => x.Content, new TemplateLoader());
            mapping.Map<TreeDataTemplate>(x => x.Content, new TemplateLoader());
            mapping.Map<ItemsPanelTemplate>(x => x.Content, new TemplateLoader());

            var parsingDictionary = GetDictionary(settings);
            var valueContext = new ValueContext(typeSource, topDownValueContext, parsingDictionary);
            assembler = new ObjectAssembler(typeSource, valueContext, settings);
            objectAssembler = new TemplateHostingObjectAssembler(assembler, mapping);
        }


        public object Result => objectAssembler.Result;

        public EventHandler<XamlSetValueEventArgs> XamlSetValueHandler { get; set; }

        public IRuntimeTypeSource TypeSource => assembler.TypeSource;

        public ITopDownValueContext TopDownValueContext => assembler.TopDownValueContext;

        public IInstanceLifeCycleListener LifecycleListener
        {
            get { throw new NotImplementedException(); }
        }

        public void Process(Instruction node)
        {
            objectAssembler.Process(node);
        }

        public void OverrideInstance(object instance)
        {
            objectAssembler.OverrideInstance(instance);
        }

        private static IReadOnlyDictionary<string, object> GetDictionary(Settings settings)
        {
            IReadOnlyDictionary<string, object> dict;

            if (settings != null)
            {
                dict = settings.ParsingContext;
            }
            else
            {
                dict = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());
            }

            return dict;
        }
    }
}