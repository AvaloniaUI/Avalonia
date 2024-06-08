#if !NET5_0_OR_GREATER

namespace System.Diagnostics.CodeAnalysis;

using Targets = AttributeTargets;

[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
[AttributeUsage(
    validOn: Targets.Class |
             Targets.Field |
             Targets.GenericParameter |
             Targets.Interface |
             Targets.Method |
             Targets.Parameter |
             Targets.Property |
             Targets.ReturnValue |
             Targets.Struct,
    Inherited = false)]

sealed class DynamicallyAccessedMembersAttribute :
    Attribute
{
    public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes) =>
        MemberTypes = memberTypes;

    public DynamicallyAccessedMemberTypes MemberTypes { get; }
}

#endif