using System;

namespace Avalonia.Platform
{
    public interface IForeignBitmapImpl : IBitmapImpl
    {
        /// <summary>
        /// Locks the bitmap, so underlying surface can be safely updated from the current thread
        /// </summary>
        /// <returns>IDisposable object for unlocking the bitmap</returns>
        IDisposable Lock();
    }
}
