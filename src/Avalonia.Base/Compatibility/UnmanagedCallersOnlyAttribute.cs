#pragma warning disable
#nullable enable

#if !NET6_0_OR_GREATER
namespace System.Runtime.InteropServices
{
    [global::System.AttributeUsage(global::System.AttributeTargets.Method, Inherited = false)]
    internal sealed class UnmanagedCallersOnlyAttribute : global::System.Attribute
    {
        public global::System.Type[]? CallConvs;
        public string? EntryPoint;
    }
}
#endif
