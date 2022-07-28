using System;

namespace Avalonia.SourceGenerator
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    internal sealed class SubtypesFactoryAttribute : Attribute
    {
        public SubtypesFactoryAttribute(Type baseType, string @namespace)
        {
            BaseType = baseType;
            Namespace = @namespace;
        }

        public string Namespace { get; }
        public Type BaseType { get; }
    }
    
    
    internal class GetProcAddressAttribute : Attribute
    {
        public GetProcAddressAttribute(string proc)
        {
            
        }
        
        public GetProcAddressAttribute(string proc, bool optional = false)
        {

        }

        public GetProcAddressAttribute(bool optional)
        {

        }

        public GetProcAddressAttribute()
        {

        }
    }
}
