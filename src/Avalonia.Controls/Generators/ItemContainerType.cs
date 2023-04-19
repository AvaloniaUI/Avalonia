using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Controls.Generators
{
    public sealed class ItemContainerType
    {
        public ItemContainerType() => Name = string.Empty;
        public ItemContainerType(string name) => Name = name;
        public string Name { get; }
        public static ItemContainerType Default { get; } = new("Default");
        public static ItemContainerType ItemIsOwnContainer { get; } = new("ItemIsOwnContainer");
    }
}
