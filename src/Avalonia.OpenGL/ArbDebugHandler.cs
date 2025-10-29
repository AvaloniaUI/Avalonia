using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Avalonia.OpenGL;

public unsafe class ArbDebugHandler
{
    /*        typedef void (APIENTRY *DEBUGPROCARB)(enum source,
                                             enum type,
                                             uint id,
                                             enum severity,
                                             sizei length,
                                             const char* message,
                                             const void* userParam);*/
    
    #if NET6_0_OR_GREATER
    private static IntPtr s_Callback =
        (IntPtr)(delegate* unmanaged[Stdcall]<int, int, uint, int, IntPtr, IntPtr, IntPtr, void>)&DebugCallback;
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    #else
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate void DebugCallbackDelegate(int source, int type, uint id, int severity, IntPtr length, IntPtr message,
        IntPtr userParam);
    private static DebugCallbackDelegate s_ManagedCallback = DebugCallback;
    private static IntPtr s_Callback = Marshal.GetFunctionPointerForDelegate(s_ManagedCallback);
    
    #endif
    static void DebugCallback(int source, int type, uint id, int severity, IntPtr length, IntPtr message, IntPtr userParam)
    {
        string msg = Marshal.PtrToStringAnsi(message) ?? string.Empty;
        Console.WriteLine($"GL DEBUG MESSAGE: Source={source}, Type={type}, ID={id}, Severity={severity}, Message={msg}");
    }
    
    public static void Install(GlInterface gl)
    {
        if (gl.IsDebugMessageCallbackAvailable)
        {
            gl.DebugMessageCallback(s_Callback, IntPtr.Zero);
            gl.DebugMessageControl(GlConsts.GL_DONT_CARE, GlConsts.GL_DONT_CARE, GlConsts.GL_DONT_CARE, IntPtr.Zero,
                null, 1);
        }
    }
}