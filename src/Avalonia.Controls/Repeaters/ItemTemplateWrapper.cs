using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Controls.Templates;

namespace Avalonia.Controls.Repeaters
{
    internal class ItemTemplateWrapper
    {
        private readonly IDataTemplate _dataTemplate;

        public ItemTemplateWrapper(IDataTemplate dataTemplate) => _dataTemplate = dataTemplate;

        public IControl GetElement(ElementFactoryGetArgs args)
        {
            var selectedTemplate = _dataTemplate;
            var recyclePool = RecyclePool.GetPoolInstance(selectedTemplate);
            IControl element = null;

            if (recyclePool != null)
            {
                // try to get an element from the recycle pool.
                element = recyclePool.TryGetElement(string.Empty, args.Parent);
            }

            if (element == null)
            {
                // no element was found in recycle pool, create a new element
                element = selectedTemplate.Build(args.Data);

                // Associate template with element
                element.SetValue(RecyclePool.OriginTemplateProperty, selectedTemplate);
            }

            return element;
        }

        public void RecycleElement(ElementFactoryRecycleArgs args)
        {
            var element = args.Element;
            var selectedTemplate = _dataTemplate;
            var recyclePool = RecyclePool.GetPoolInstance(selectedTemplate);
            if (recyclePool == null)
            {
                // No Recycle pool in the template, create one.
                recyclePool = new RecyclePool();
                RecyclePool.SetPoolInstance(selectedTemplate, recyclePool);
            }

            recyclePool.PutElement(args.Element, "" /* key */, args.Parent);
        }
    }
}
