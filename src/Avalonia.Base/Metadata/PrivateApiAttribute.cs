using System;

namespace Avalonia.Metadata;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method)]
public sealed class PrivateApiAttribute : Attribute
{

}