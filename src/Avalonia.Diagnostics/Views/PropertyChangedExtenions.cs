using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reflection;

namespace Avalonia.Diagnostics.Views
{
    internal static class PropertyChangedExtenions
    {
        public static IObservable<T> GetObservable<T>(this INotifyPropertyChanged source, string propertyName)
        {
            Contract.Requires<ArgumentNullException>(source != null);
            Contract.Requires<ArgumentNullException>(propertyName != null);

            var property = source.GetType().GetTypeInfo().GetDeclaredProperty(propertyName);

            if (property == null)
            {
                throw new ArgumentException($"Property '{propertyName}' not found on '{source}.");
            }

            return Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                e => source.PropertyChanged += e,
                e => source.PropertyChanged -= e)
                    .Where(e => e.EventArgs.PropertyName == propertyName)
                    .Select(_ => (T)property.GetValue(source))
                    .StartWith((T)property.GetValue(source));
        }
    }
}
