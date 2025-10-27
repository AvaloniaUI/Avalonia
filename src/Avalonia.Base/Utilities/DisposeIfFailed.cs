using System;

namespace Avalonia.Utilities;


/// <summary>
/// See <see cref="DisposeIfFailedExtensions.DisposeIfFailed{T}(T)"/> for details
/// </summary>
internal struct DisposeIfFailed<T>(T value) : IDisposable where T : IDisposable
{
    private bool _skipDispose = false;

    public T SuccessReturn()
    {
        _skipDispose = true;
        return Value;
    }

    public TRes SuccessReturn<TRes>(TRes pass)
    {
        _skipDispose = true;
        return pass;
    }

    public T Value => value;
    
    public void Dispose()
    {
        if(!_skipDispose)
            value?.Dispose();
    }
}

internal static class DisposeIfFailedExtensions
{
    /// <summary>
    /// This is a helper class for scenarios like:
    ///
    /// var disposableThing = new (); 
    /// disposableThing.DoStuff();
    /// return disposableThing;
    ///
    /// If an exception is thrown during DoStuff, disposableThing will not be disposed.
    ///
    /// This method provides a wrapper that handles of disposal. The code will look like this:
    ///
    /// using var disposableThing = new ().DisposeIfFailed();
    /// disposableThing.Value.DoStuff();
    /// return disposableThing.SuccessReturn();
    /// </summary>

    public static DisposeIfFailed<T> DisposeIfFailed<T>(this T disposable) where T : IDisposable
    {
        return new DisposeIfFailed<T>(disposable);
    }
}