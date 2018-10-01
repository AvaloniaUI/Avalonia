using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Avalonia.OpenGL
{
    public class GlInterfaceBase
    {
        public GlInterfaceBase(Func<string, bool, IntPtr> getProcAddress)
        {
            foreach (var prop in this.GetType().GetProperties())
            {
                var a = prop.GetCustomAttribute<EntryPointAttribute>();
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
    }
}
