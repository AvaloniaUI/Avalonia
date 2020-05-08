using System;

namespace Avalonia.OpenGL
{
    public interface IGlEntryPointAttribute
    {
        IntPtr GetProcAddress(Func<string, IntPtr> getProcAddress);
    }
    
    public interface IGlEntryPointAttribute<in TContext>
    {
        IntPtr GetProcAddress(TContext context, Func<string, IntPtr> getProcAddress);
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class GlOptionalEntryPoint : Attribute
    {
        
    }
    
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class GlEntryPointAttribute : Attribute, IGlEntryPointAttribute
    {
        public string[] EntryPoints { get; }

        public GlEntryPointAttribute(string entryPoint)
        {
            EntryPoints = new []{entryPoint};
        }
/*
        public GlEntryPointAttribute(params string[] entryPoints)
        {
            EntryPoints = entryPoints;
        }
*/
        public IntPtr GetProcAddress(Func<string, IntPtr> getProcAddress)
        {
            foreach (var name in EntryPoints)
            {
                var rv = getProcAddress(name);
                if (rv != IntPtr.Zero)
                    return rv;
            }
            return IntPtr.Zero;
        }
    }
    
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class GlMinVersionEntryPoint : Attribute, IGlEntryPointAttribute<GlInterface.GlContextInfo>
    {
        private readonly string _entry;
        private readonly GlProfileType? _profile;
        private readonly int _minVersionMajor;
        private readonly int _minVersionMinor;

        public GlMinVersionEntryPoint(string entry, GlProfileType profile, int minVersionMajor,
            int minVersionMinor)
        {
            _entry = entry;
            _profile = profile;
            _minVersionMajor = minVersionMajor;
            _minVersionMinor = minVersionMinor;
        }
        
        public GlMinVersionEntryPoint(string entry, int minVersionMajor,
            int minVersionMinor)
        {
            _entry = entry;
            _minVersionMajor = minVersionMajor;
            _minVersionMinor = minVersionMinor;
        }
        
        public IntPtr GetProcAddress(GlInterface.GlContextInfo context, Func<string, IntPtr> getProcAddress)
        {
            if(_profile.HasValue && context.Version.Type != _profile)
                return IntPtr.Zero;
            if(context.Version.Major<_minVersionMajor)
                return IntPtr.Zero;
            if (context.Version.Major == _minVersionMajor && context.Version.Minor < _minVersionMinor)
                return IntPtr.Zero;
            return getProcAddress(_entry);
        }
    }
    
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class GlExtensionEntryPoint : Attribute, IGlEntryPointAttribute<GlInterface.GlContextInfo>
    {
        private readonly string _entry;
        private readonly GlProfileType? _profile;
        private readonly string _extension;

        public GlExtensionEntryPoint(string entry, GlProfileType profile, string extension)
        {
            _entry = entry;
            _profile = profile;
            _extension = extension;
        }
        
        public GlExtensionEntryPoint(string entry, string extension)
        {
            _entry = entry;
            _extension = extension;
        }
        
        public IntPtr GetProcAddress(GlInterface.GlContextInfo context, Func<string, IntPtr> getProcAddress)
        {
            // Ignore different profile type
            if (_profile.HasValue && _profile != context.Version.Type)
                return IntPtr.Zero;

            // Check if extension is supported by the current context
            if (!context.Extensions.Contains(_extension))
                return IntPtr.Zero;

            return getProcAddress(_entry);
        }
    }
}
