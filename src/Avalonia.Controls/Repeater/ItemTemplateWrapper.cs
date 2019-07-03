// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using Avalonia.Controls.Templates;

namespace Avalonia.Controls
{
    internal class ItemTemplateWrapper
    {
        private readonly IDataTemplate _dataTemplate;

        public ItemTemplateWrapper(IDataTemplate dataTemplate) => _dataTemplate = dataTemplate;

        public IControl GetElement(IControl parent, object data)
        {
            var selectedTemplate = _dataTemplate;
            var recyclePool = RecyclePool.GetPoolInstance(selectedTemplate);
            IControl element = null;

            if (recyclePool != null)
            {
                // try to get an element from the recycle pool.
                element = recyclePool.TryGetElement(string.Empty, parent);
            }

            if (element == null)
            {
                // no element was found in recycle pool, create a new element
                element = selectedTemplate.Build(data);

                // Associate template with element
                element.SetValue(RecyclePool.OriginTemplateProperty, selectedTemplate);
            }

            return element;
        }

        public void RecycleElement(IControl parent, IControl element)
        {
            var selectedTemplate = _dataTemplate;
            var recyclePool = RecyclePool.GetPoolInstance(selectedTemplate);
            if (recyclePool == null)
            {
                // No Recycle pool in the template, create one.
                recyclePool = new RecyclePool();
                RecyclePool.SetPoolInstance(selectedTemplate, recyclePool);
            }

            recyclePool.PutElement(element, "" /* key */, parent);
        }
    }
}
