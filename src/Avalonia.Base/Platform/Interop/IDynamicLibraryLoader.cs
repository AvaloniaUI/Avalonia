using System;
using Avalonia.Metadata;

namespace Avalonia.Platform.Interop
{
    public interface IDynamicLibraryLoader
    {
        IntPtr LoadLibrary(string dll);
        IntPtr GetProcAddress(IntPtr dll, string proc, bool optional);
    }

    public class DynamicLibraryLoaderException : Exception
    {
        public DynamicLibraryLoaderException(string message) : base(message)
        {
            
        }
        
        public DynamicLibraryLoaderException(string message, Exception innerException) : base(message, innerException)
        {
            
        }
    }
}
