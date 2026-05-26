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
    
    

    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class GetProcAddressAttribute : Attribute
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

    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class GenerateEnumValueDictionaryAttribute : Attribute
    {
    }
    

    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class GenerateEnumValueListAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    internal sealed class GenerateCrossThreadProxyAttribute : Attribute
    {
        public GenerateCrossThreadProxyAttribute(Type priorityType, string defaultPriorityExpression)
        {
            PriorityType = priorityType;
            DefaultPriorityExpression = defaultPriorityExpression;
        }

        public Type PriorityType { get; }
        public string DefaultPriorityExpression { get; }

        /// <summary>
        /// Optional. Name of the generated proxy class. Defaults to the
        /// interface name with the leading 'I' stripped (if present) and
        /// "Proxy" appended (e.g. <c>IFoo</c> → <c>FooProxy</c>).
        /// </summary>
        public string? GeneratedClassName { get; set; }
    }

    /// <summary>
    /// When applied to a void-returning method on an interface marked with
    /// <see cref="GenerateCrossThreadProxyAttribute"/>, the generated proxy
    /// returns <see cref="System.Threading.Tasks.Task"/> instead of being
    /// fire-and-forget. Has no effect on non-void methods (which are always
    /// wrapped into <see cref="System.Threading.Tasks.Task{TResult}"/>).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class GenerateCrossThreadProxyReturnTaskAttribute : Attribute
    {
    }
}
