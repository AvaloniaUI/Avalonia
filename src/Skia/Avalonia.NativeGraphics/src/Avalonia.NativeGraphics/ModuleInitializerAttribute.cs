using System;
#if !NET6_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    internal class ModuleInitializerAttribute : Attribute
    {
    }
}
#endif