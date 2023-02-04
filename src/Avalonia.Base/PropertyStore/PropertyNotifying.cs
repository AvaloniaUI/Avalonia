using System;
using System.Diagnostics;

namespace Avalonia.PropertyStore
{
    /// <summary>
    /// Raises <see cref="AvaloniaProperty.Notifying"/> where necessary.
    /// </summary>
    /// <remarks>
    /// Uses the disposable pattern to ensure that the closing Notifying call is made even in the
    /// presence of exceptions. 
    /// </remarks>
    internal readonly struct PropertyNotifying : IDisposable
    {
        private readonly AvaloniaObject _owner;
        private readonly AvaloniaProperty _property;

        private PropertyNotifying(AvaloniaObject owner, AvaloniaProperty property)
        {
            Debug.Assert(property.Notifying is not null);
            _owner = owner;
            _property = property;
            _property.Notifying!(owner, true);
        }

        public void Dispose() => _property.Notifying!(_owner, false);

        public static PropertyNotifying? Start(AvaloniaObject owner, AvaloniaProperty property)
        {
            if (property.Notifying is null)
                return null;
            return new PropertyNotifying(owner, property);
        }
    }
}
