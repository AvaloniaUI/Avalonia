#if !NET5_0_OR_GREATER

namespace System.Diagnostics.CodeAnalysis;

[System.AttributeUsage(
    System.AttributeTargets.Method |
    System.AttributeTargets.Constructor |
    System.AttributeTargets.Class, Inherited = false)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[System.Diagnostics.Conditional("MULTI_TARGETING_SUPPORT_ATTRIBUTES")]
internal sealed class RequiresUnreferencedCodeAttribute : System.Attribute
{
    public RequiresUnreferencedCodeAttribute(string message)
    {
        Message = message;
    }

    public string Message { get; }

    public string? Url { get; set; }
}

#endif