using System;
using Avalonia.Metadata;

namespace Avalonia.Platform.Interop
{
    [Unstable]
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
    }
}
