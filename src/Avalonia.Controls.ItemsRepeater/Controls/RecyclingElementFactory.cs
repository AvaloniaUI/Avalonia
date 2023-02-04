using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Templates;

namespace Avalonia.Controls
{
    public class SelectTemplateEventArgs : EventArgs
    {
        public string? TemplateKey { get; set; }
        public object? DataContext { get; internal set; }
        public Control? Owner { get; internal set; }
    }

    public class RecyclingElementFactory : ElementFactory
    {
        private RecyclePool? _recyclePool;
        private IDictionary<string, IDataTemplate>? _templates;
        private SelectTemplateEventArgs? _args;

        public RecyclingElementFactory()
        {
            Templates = new Dictionary<string, IDataTemplate>();
        }

        public RecyclePool RecyclePool 
        {
            get => _recyclePool ??= new RecyclePool();
            set => _recyclePool = value ?? throw new ArgumentNullException(nameof(value));
        }

        public IDictionary<string, IDataTemplate> Templates 
        {
            get => _templates ??= new Dictionary<string, IDataTemplate>();
            set => _templates = value ?? throw new ArgumentNullException(nameof(value));
        }

        public event EventHandler<SelectTemplateEventArgs>? SelectTemplateKey;

        protected override Control GetElementCore(ElementFactoryGetArgs args)
        {
            if (_templates == null || _templates.Count == 0)
            {
                throw new InvalidOperationException("Templates cannot be empty.");
            }

            var templateKey = Templates.Count == 1 ?
                Templates.First().Key :
                OnSelectTemplateKeyCore(args.Data, args.Parent);

            if (string.IsNullOrEmpty(templateKey))
            {
                // Note: We could allow null/whitespace, which would work as long as
                // the recycle pool is not shared. in order to make this work in all cases
                // currently we validate that a valid template key is provided.
                throw new InvalidOperationException("Template key cannot be null or empty.");
            }

            // Get an element from the Recycle Pool or create one
            var element = RecyclePool.TryGetElement(templateKey, args.Parent);

            if (element is null)
            {
                // No need to call HasKey if there is only one template.
                if (Templates.Count > 1 && !Templates.ContainsKey(templateKey))
                {
                    var message = $"No templates of key '{templateKey}' were found in the templates collection.";
                    throw new InvalidOperationException(message);
                }

                var dataTemplate = Templates[templateKey];
                element = dataTemplate.Build(args.Data)!;

                // Associate ReuseKey with element
                RecyclePool.SetReuseKey(element, templateKey);
            }

            return element;
        }

        protected override void RecycleElementCore(ElementFactoryRecycleArgs args)
        {
            var element = args.Element!;
            var key = RecyclePool.GetReuseKey(element);
            RecyclePool.PutElement(element, key, args.Parent);
        }

        protected virtual string OnSelectTemplateKeyCore(object? dataContext, Control? owner)
        {
            if (SelectTemplateKey is not null)
            {
                _args ??= new SelectTemplateEventArgs();
                _args.TemplateKey = null;
                _args.DataContext = dataContext;
                _args.Owner = owner;

                try
                {
                    SelectTemplateKey(this, _args);
                }
                finally
                {
                    _args.DataContext = null;
                    _args.Owner = null;
                }
            }

            if (string.IsNullOrEmpty(_args?.TemplateKey))
            {
                throw new InvalidOperationException(
                    "Please provide a valid template identifier in the handler for the SelectTemplateKey event.");
            }

            return _args!.TemplateKey!;
        }
    }
}
