using System;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Markup.Xaml
{
    public interface IProvideValueTarget
    {
        object TargetObject { get; }
        object TargetProperty { get; }
    }
    
    public interface IRootObjectProvider
    {
        /// <summary>
        /// The root object of the xaml file
        /// </summary>
        object RootObject { get; }
        /// <summary>
        /// The "current" root object, contains either the root of the xaml file
        /// or the root object of the control/data template 
        /// </summary>
        object IntermediateRootObject { get; }
    }
    
    public interface IUriContext
    {
        Uri BaseUri { get; set; }
    }
    
    public interface IXamlTypeResolver
    {
        [RequiresUnreferencedCode(TrimmingMessages.XamlTypeResolvedRequiresUnreferenceCodeMessage)]
        Type Resolve (string qualifiedTypeName);
    }

    // TODO12: Move to Avalonia.Base
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ConstructorArgumentAttribute : Attribute
    {
        public ConstructorArgumentAttribute(string name)
        {
            
        }
    }
}
