using System;
using Avalonia.Data.Core;

namespace Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings
{
    internal class StrongTypeCastNode : TypeCastNode
    {
        private Func<object, object> _cast;

        public StrongTypeCastNode(Type type, Func<object, object> cast) : base(type)
        {
            _cast = cast;
        }

        protected override object Cast(object value)
            => _cast(value);
    }
}
