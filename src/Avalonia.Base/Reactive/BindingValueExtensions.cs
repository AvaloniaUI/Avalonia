using System;
using System.Reactive.Subjects;
using Avalonia.Data;

#nullable enable

namespace Avalonia.Reactive
{
    public static class BindingValueExtensions
    {
        public static IObservable<BindingValue<T>> ToBindingValue<T>(this IObservable<T> source)
        {
            source = source ?? throw new ArgumentNullException(nameof(source));
            return new BindingValueAdapter<T>(source);
        }
    }
}
