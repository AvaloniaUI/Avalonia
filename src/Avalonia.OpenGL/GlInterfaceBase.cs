using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia.Platform.Interop;

namespace Avalonia.OpenGL
{
    public class GlInterfaceBase
    {
        
        private readonly Func<string, bool, IntPtr> _getProcAddress;
        public GlInterfaceBase(Func<string, bool, IntPtr> getProcAddress)
        {
            _getProcAddress = getProcAddress;
            foreach (var prop in this.GetType().GetProperties())
            {
                var a = prop.GetCustomAttribute<GlEntryPointAttribute>();
                if (a != null)
                {
                    var fieldName = $"<{prop.Name}>k__BackingField";
                    var field = prop.DeclaringType.GetField(fieldName,
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    if (field == null)
                        throw new InvalidProgramException($"Expected property {prop.Name} to have {fieldName}");
                    var proc = getProcAddress(a.EntryPoint, a.Optional);
                    if (proc != IntPtr.Zero)
                        field.SetValue(this, Marshal.GetDelegateForFunctionPointer(proc, prop.PropertyType));
                }
            }
        }

        protected static Func<string, bool, IntPtr> ConvertNative(Func<Utf8Buffer, IntPtr> func) =>
            (proc, optional) =>
            {
                using (var u = new Utf8Buffer(proc))
                {
                    var rv = func(u);
                    if (rv == IntPtr.Zero && !optional)
                        throw new OpenGlException("Missing function " + proc);
                    return rv;
                }
            };
        
        public GlInterfaceBase(Func<Utf8Buffer, IntPtr> nativeGetProcAddress) : this(ConvertNative(nativeGetProcAddress))
        {
            
        }
        
        public IntPtr GetProcAddress(string proc) => _getProcAddress(proc, true);
        public IntPtr GetProcAddress(string proc, bool optional) => _getProcAddress(proc, optional);

    }
}
