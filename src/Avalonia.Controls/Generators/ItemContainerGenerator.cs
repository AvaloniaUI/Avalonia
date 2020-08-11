using System;
using Avalonia.Controls.Presenters;
using Avalonia.Data;

#nullable enable

namespace Avalonia.Controls.Generators
{
    /// <summary>
    /// Creates containers for items and maintains a list of created containers.
    /// </summary>
    public class ItemContainerGenerator : IItemContainerGenerator
    {
        private RecyclePool _recyclePool = new RecyclePool();

        public ItemContainerGenerator(ItemsControl owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public ItemsControl Owner { get; }
        public bool SupportsRecycling => false;

        public bool Match(object data) => true;

        public IControl Build(object param)
        {
            return GetElement(new ElementFactoryGetArgs { Data = param });
        }

        public IControl GetElement(ElementFactoryGetArgs args)
        {
            var result = _recyclePool.TryGetElement(string.Empty, args.Parent);

            if (result is null)
            {
                result = CreateContainer(args);

                if (result.Parent == null)
                {
                    ((ISetLogicalParent)result).SetParent(Owner);
                }
            }

            return result;
        }

        public void RecycleElement(ElementFactoryRecycleArgs args)
        {
            _recyclePool.PutElement(args.Element, string.Empty, args.Parent);
        }

        protected virtual IControl CreateContainer(ElementFactoryGetArgs args)
        {
            if (args.Data is IControl c)
            {
                return c;
            }

            var result = new ContentPresenter();
            result.Bind(
                ContentPresenter.ContentProperty,
                result.GetBindingObservable(Control.DataContextProperty),
                BindingPriority.Style);

            if (Owner.ItemTemplate is object)
            {
                result.SetValue(
                    ContentPresenter.ContentTemplateProperty,
                    Owner.ItemTemplate,
                    BindingPriority.TemplatedParent);
            }

            return result;
        }
    }
}
