using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia.Platform.Interop;

namespace Avalonia.OpenGL
{
    public class GlInterfaceBase : GlInterfaceBase<object>
    {
        public GlInterfaceBase(Func<string, IntPtr> getProcAddress) : base(getProcAddress, null)
        {
        }

        public GlInterfaceBase(Func<Utf8Buffer, IntPtr> nativeGetProcAddress) : base(nativeGetProcAddress, null)
        {
        }
    }

    public class GlInterfaceBase<TContext>
    {
        private readonly Func<string, IntPtr> _getProcAddress;
        public GlInterfaceBase(Func<string, IntPtr> getProcAddress, TContext context)
        {
            _getProcAddress = getProcAddress;
            foreach (var prop in this.GetType().GetProperties())
            {
                var attrs = prop.GetCustomAttributes()
                    .Where(a =>
                        a is IGlEntryPointAttribute || a is IGlEntryPointAttribute<TContext>)
                    .ToList();
                if(attrs.Count == 0)
                    continue;
                
                var isOptional = prop.GetCustomAttribute<GlOptionalEntryPoint>() != null;
                
                var fieldName = $"<{prop.Name}>k__BackingField";
                var field = prop.DeclaringType.GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (field == null)
                    throw new InvalidProgramException($"Expected property {prop.Name} to have {fieldName}");
                
                
                IntPtr proc = IntPtr.Zero;
                foreach (var attr in attrs)
                {
                    if (attr is IGlEntryPointAttribute<TContext> typed)
                        proc = typed.GetProcAddress(context, getProcAddress);
                    else if (attr is IGlEntryPointAttribute untyped)
                        proc = untyped.GetProcAddress(getProcAddress);
                    if (proc != IntPtr.Zero)
                        break;
                }
                
                if (proc != IntPtr.Zero)
                    field.SetValue(this, Marshal.GetDelegateForFunctionPointer(proc, prop.PropertyType));
                else if (!isOptional)
                    throw new OpenGlException("Unable to find a suitable GL function for " + prop.Name);
            }
        }

        protected static Func<string, IntPtr> ConvertNative(Func<Utf8Buffer, IntPtr> func) =>
            (proc) =>
            {
                using (var u = new Utf8Buffer(proc))
                {
                    var rv = func(u);
                    return rv;
                }
            };
        
        public GlInterfaceBase(Func<Utf8Buffer, IntPtr> nativeGetProcAddress, TContext context) : this(ConvertNative(nativeGetProcAddress), context)
        {
            
        }
        
        public IntPtr GetProcAddress(string proc) => _getProcAddress(proc);
    }
}
