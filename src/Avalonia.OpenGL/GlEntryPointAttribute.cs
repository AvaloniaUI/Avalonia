using System;

namespace Avalonia.OpenGL
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    sealed class GlMinVersionEntryPoint : Attribute
    {
        public GlMinVersionEntryPoint(string entry, int minVersionMajor, int minVersionMinor)
        {
        }
        
        public GlMinVersionEntryPoint(string entry, int minVersionMajor, int minVersionMinor, GlProfileType profile)
        {
        }

        
        public static IntPtr GetProcAddress(Func<string, IntPtr> getProcAddress, GlInterface.GlContextInfo context,
            string entry, int minVersionMajor, int minVersionMinor, GlProfileType? profile = null)
        {
            if(profile.HasValue && context.Version.Type != profile)
                return IntPtr.Zero;
            if(context.Version.Major<minVersionMajor)
                return IntPtr.Zero;
            if (context.Version.Major == minVersionMajor && context.Version.Minor < minVersionMinor)
                return IntPtr.Zero;
            return getProcAddress(entry);
        }
    }
    
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    sealed class GlExtensionEntryPoint : Attribute
    {
        public GlExtensionEntryPoint(string entry, string extension)
        {
        }
        
        public GlExtensionEntryPoint(string entry, string extension, GlProfileType profile)
        {
        }
        
        public static IntPtr GetProcAddress(Func<string, IntPtr> getProcAddress, GlInterface.GlContextInfo context,
            string entry, string extension, GlProfileType? profile = null)
        {
            // Ignore different profile type
            if (profile.HasValue && profile != context.Version.Type)
                return IntPtr.Zero;

            // Check if extension is supported by the current context
            if (!context.Extensions.Contains(extension))
                return IntPtr.Zero;

            return getProcAddress(entry);
        }
    }
}
