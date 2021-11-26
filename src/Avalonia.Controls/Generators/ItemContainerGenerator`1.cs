using Avalonia.Data;

#nullable enable

namespace Avalonia.Controls.Generators
{
    /// <summary>
    /// Creates containers of type <typeparamref name="T"/> for an <see cref="ItemsControl"/>.
    /// </summary>
    public class ItemContainerGenerator<T> : ItemContainerGenerator
        where T : ContentControl, new()
    {
        public ItemContainerGenerator(ItemsControl owner)
            : base(owner)
        {
        }

        protected override ContentControl CreateContainer(IControl parent, int index, object? item)
        {
            var result = new T
            { 
                DataContext = item,
                ContentTemplate = Owner.ItemTemplate,
            };

            result.Bind(
                ContentControl.ContentProperty,
                result.GetBindingObservable(StyledElement.DataContextProperty),
                BindingPriority.TemplatedParent);
            return result;
        }
    }
}
