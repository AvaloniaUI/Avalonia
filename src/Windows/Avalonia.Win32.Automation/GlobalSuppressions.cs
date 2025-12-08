using System.Diagnostics.CodeAnalysis;

// Until https://github.com/dotnet/runtime/issues/90922 is resolved, this is the only way
// to suppress CA2256 in the generated code.
[assembly: SuppressMessage(
    "Usage",
    "CA2256:All members declared in parent interfaces must have an implementation in a DynamicInterfaceCastableImplementation-attributed interface",
    Justification = "Generated code and analyzer ignores that it is generated AND partial",
    Scope = "type",
    Target = "~T:InterfaceImplementation")
]
