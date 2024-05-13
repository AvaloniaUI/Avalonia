using System;
using System.Threading;

namespace Avalonia.Threading;

/// <summary>
///     A reader-writer lock abstraction, wrapping around the .NET class ReaderWriterLockSlim.
/// </summary>
internal class ReaderWriterLockWrapper
{
    /// <summary>
    ///     Executes an action within a read lock.
    /// </summary>
    /// <param name="action">The action to execute</param>
    public void ExecuteWithinReadLock(Action action)
    {
        try
        {
            _readerWriterLock.EnterReadLock();

            action();
        }
        finally
        {
            if (_readerWriterLock.IsReadLockHeld)
            {
                _readerWriterLock.ExitReadLock();
            }
        }
    }
    
    /// <summary>
    ///     Executes a func within a read lock.
    /// </summary>
    /// <param name="func">The func to execute</param>
    /// <typeparam name="T">The generic type parameter of the func</typeparam>
    /// <returns>The return value from the execution of the func</returns>
    public T ExecuteWithinReadLock<T>(Func<T> func)
    {
        try
        {
            _readerWriterLock.EnterReadLock();

            return func();
        }
        finally
        {
            if (_readerWriterLock.IsReadLockHeld)
            {
                _readerWriterLock.ExitReadLock();
            }
        }
    }
    
    /// <summary>
    ///     Executes an action within a write lock.
    /// </summary>
    /// <param name="action">The action to execute</param>
    public void ExecuteWithinWriteLock(Action action)
    {
        try
        {
            _readerWriterLock.EnterWriteLock();

            action();
        }
        finally
        {
            if (_readerWriterLock.IsWriteLockHeld)
            {
                _readerWriterLock.ExitWriteLock();
            }
        }
    }
    
    /// <summary>
    ///     Executes a func within a write lock.
    /// </summary>
    /// <param name="func">The func to execute</param>
    /// <typeparam name="T">The generic type parameter of the func</typeparam>
    /// <returns>The return value from the execution of the func</returns>
    public T ExecuteWithinWriteLock<T>(Func<T> func)
    {
        try
        {
            _readerWriterLock.EnterWriteLock();

            return func();
        }
        finally
        {
            if (_readerWriterLock.IsWriteLockHeld)
            {
                _readerWriterLock.ExitWriteLock();
            }
        }
    }

    private readonly ReaderWriterLockSlim _readerWriterLock = new(LockRecursionPolicy.SupportsRecursion);
}
