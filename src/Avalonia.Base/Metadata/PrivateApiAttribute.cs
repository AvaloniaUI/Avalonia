using System;

namespace Avalonia.Metadata;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Constructor 
                | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field)]
public sealed class PrivateApiAttribute : Attribute
{

}