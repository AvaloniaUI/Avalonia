using System;

namespace Avalonia.Markup.Xaml
{
    public interface IProvideValueTarget
    {
        object TargetObject { get; }
        object TargetProperty { get; }
    }
    
    public interface IRootObjectProvider
    {
        object RootObject { get; }
    }
    
    public interface IUriContext
    {
        Uri BaseUri { get; set; }
    }
    
    public interface IXamlTypeResolver
    {
        Type Resolve (string qualifiedTypeName);
    }

    
    public class ConstructorArgumentAttribute : Attribute
    {
        public ConstructorArgumentAttribute(string name)
        {
            
        }
    }
}
