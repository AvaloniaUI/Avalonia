using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia
{
    public class AliasedProperty<TValue> : AvaloniaProperty<TValue>, IAliasedPropertyAccessor
    {
        public AliasedProperty(AvaloniaProperty<TValue> property, string alias)
            : base(property, alias) => Property = property;

        public AvaloniaProperty<TValue> Property { get; }

        AvaloniaProperty IAliasedPropertyAccessor.ResolveAlias()
        {
            if (Property is IAliasedPropertyAccessor accessor)
            {
                return accessor.ResolveAlias();
            }

            return Property;
        }
    }
}
