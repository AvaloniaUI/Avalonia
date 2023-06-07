using System;

namespace Avalonia.Reactive;

internal interface IAvaloniaSubject<T> : IObserver<T>, IObservable<T> /*, ISubject<T> */
{
    
}
