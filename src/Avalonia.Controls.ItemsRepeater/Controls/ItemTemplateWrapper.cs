// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using Avalonia.Controls.Templates;

namespace Avalonia.Controls
{
    internal class ItemTemplateWrapper : IElementFactory
    {
        private readonly IDataTemplate _dataTemplate;

        public ItemTemplateWrapper(IDataTemplate dataTemplate) => _dataTemplate = dataTemplate;

        public Control Build(object? param) => GetElement(null, param);
        public bool Match(object? data) => _dataTemplate.Match(data);

        public Control GetElement(ElementFactoryGetArgs args)
        {
            return GetElement(args.Parent, args.Data);
        }

        public void RecycleElement(ElementFactoryRecycleArgs args)
        {
            RecycleElement(args.Parent, args.Element!);
        }

        private Control GetElement(Control? parent, object? data)
        {
            var selectedTemplate = _dataTemplate;
            var recyclePool = RecyclePool.GetPoolInstance(selectedTemplate);
            Control? element = null;

            if (recyclePool != null)
            {
                // try to get an element from the recycle pool.
                element = recyclePool.TryGetElement(string.Empty, parent);
            }

            if (element == null)
            {
                // no element was found in recycle pool, create a new element
                element = selectedTemplate.Build(data)!;

                // Associate template with element
                element.SetValue(RecyclePool.OriginTemplateProperty, selectedTemplate);
            }

            return element;
        }

        private void RecycleElement(Control? parent, Control element)
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
