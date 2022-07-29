using System;
using Avalonia.Controls.Templates;
using Avalonia.Data;

namespace Avalonia.Controls.Generators
{
    /// <summary>
    /// Creates containers for items and maintains a list of created containers.
    /// </summary>
    /// <typeparam name="T">The type of the container.</typeparam>
    public class ItemContainerGenerator<T> : ItemContainerGenerator where T : class, IControl, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemContainerGenerator{T}"/> class.
        /// </summary>
        /// <param name="owner">The owner control.</param>
        /// <param name="contentProperty">The container's Content property.</param>
        /// <param name="contentTemplateProperty">The container's ContentTemplate property.</param>
        public ItemContainerGenerator(
            IControl owner, 
            AvaloniaProperty contentProperty,
            AvaloniaProperty? contentTemplateProperty)
            : base(owner)
        {
            ContentProperty = contentProperty ?? throw new ArgumentNullException(nameof(contentProperty));
            ContentTemplateProperty = contentTemplateProperty;
        }

        /// <inheritdoc/>
        public override Type ContainerType => typeof(T);

        /// <summary>
        /// Gets the container's Content property.
        /// </summary>
        protected AvaloniaProperty ContentProperty { get; }

        /// <summary>
        /// Gets the container's ContentTemplate property.
        /// </summary>
        protected AvaloniaProperty? ContentTemplateProperty { get; }

        /// <inheritdoc/>
        protected override IControl? CreateContainer(object item)
        {
            var container = item as T;

            if (container is null)
            {
                container = new T();

                if (ContentTemplateProperty != null)
                {
                    container.SetValue(ContentTemplateProperty, ItemTemplate, BindingPriority.Style);
                }

                container.SetValue(ContentProperty, item, BindingPriority.Style);

                if (!(item is IControl))
                {
                    container.DataContext = item;
                }
            }

            if (ItemContainerTheme != null)
            {
                container.SetValue(StyledElement.ThemeProperty, ItemContainerTheme, BindingPriority.Style);
            }

            return container;
        }

        /// <inheritdoc/>
        public override bool TryRecycle(int oldIndex, int newIndex, object item)
        {
            var container = ContainerFromIndex(oldIndex);

            if (container == null)
            {
                throw new IndexOutOfRangeException("Could not recycle container: not materialized.");
            }

            container.SetValue(ContentProperty, item);

            if (!(item is IControl))
            {
                container.DataContext = item;
            }

            var info = MoveContainer(oldIndex, newIndex, item);
            RaiseRecycled(new ItemContainerEventArgs(info));

            return true;
        }
    }
}
