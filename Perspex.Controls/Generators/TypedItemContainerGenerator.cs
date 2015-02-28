// -----------------------------------------------------------------------
// <copyright file="TypedItemContainerGenerator.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Generators
{
    using Perspex.Controls.Templates;

    public class TypedItemContainerGenerator<T> : ItemContainerGenerator where T : ContentControl, new()
    {
        public TypedItemContainerGenerator(ItemsControl owner)
            : base(owner)
        {
        }

        protected override Control CreateContainerOverride(object item)
        {
            T result = item as T;

            if (result == null)
            {
                result = new T();
                result.Content = this.Owner.MaterializeDataTemplate(item);
            }

            return result;
        }
    }
}
