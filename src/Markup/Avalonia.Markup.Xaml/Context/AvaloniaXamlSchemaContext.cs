using System;
using System.Collections.Generic;
using System.Linq;
using System.Xaml;

namespace Avalonia.Markup.Xaml.Context
{
    internal class AvaloniaXamlSchemaContext : XamlSchemaContext
    {
        private IRuntimeTypeProvider _typeProvider;
        private Dictionary<Type, AvaloniaXamlType> _typeCache = new Dictionary<Type, AvaloniaXamlType>();

        public AvaloniaXamlSchemaContext(IRuntimeTypeProvider typeProvider)
        {
            Contract.Requires<ArgumentNullException>(typeProvider != null);

            _typeProvider = typeProvider;
        }

        public override XamlType GetXamlType(Type type)
        {
            if (!_typeCache.TryGetValue(type, out var result))
            {
                result = typeof(IAvaloniaObject).IsAssignableFrom(type) ?
                    new AvaloniaObjectXamlType(type, this) :
                    new AvaloniaXamlType(type, this);
                _typeCache.Add(type, result);
            }

            return result;
        }

        protected override XamlType GetXamlType(string xamlNamespace, string name, params XamlType[] typeArguments)
        {
            var result = _typeProvider.FindType(xamlNamespace, name, typeArguments?.Select(x => x.UnderlyingType));
            return result != null ?
                GetXamlType(result) :
                base.GetXamlType(xamlNamespace, name, typeArguments);
        }
    }
}
