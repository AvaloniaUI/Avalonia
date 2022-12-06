using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings
{
    class PropertyInfoAccessorPlugin : IPropertyAccessorPlugin
    {
        private readonly IPropertyInfo _propertyInfo;
        private readonly Func<WeakReference<object>, IPropertyInfo, IPropertyAccessor> _accessorFactory;

        public PropertyInfoAccessorPlugin(IPropertyInfo propertyInfo, Func<WeakReference<object>, IPropertyInfo, IPropertyAccessor> accessorFactory)
        {
            _propertyInfo = propertyInfo;
            _accessorFactory = accessorFactory;
        }

        [RequiresUnreferencedCode(TrimmingMessages.PropertyAccessorsRequiresUnreferencedCodeMessage)]
        public bool Match(object obj, string propertyName)
        {
            throw new InvalidOperationException("The PropertyInfoAccessorPlugin does not support dynamic matching");
        }

        [RequiresUnreferencedCode(TrimmingMessages.PropertyAccessorsRequiresUnreferencedCodeMessage)]
        public IPropertyAccessor Start(WeakReference<object> reference, string propertyName)
        {
            Debug.Assert(_propertyInfo.Name == propertyName);
            return _accessorFactory(reference, _propertyInfo);
        }
    }
}
