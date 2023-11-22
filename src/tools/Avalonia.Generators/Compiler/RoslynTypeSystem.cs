using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XamlX.TypeSystem;

namespace Avalonia.Generators.Compiler;

internal class RoslynTypeSystem : IXamlTypeSystem
{
    private readonly List<IXamlAssembly> _assemblies = new();

    public RoslynTypeSystem(CSharpCompilation compilation)
    {
        _assemblies.Add(new RoslynAssembly(compilation.Assembly));

        var assemblySymbols = compilation
            .References
            .Select(compilation.GetAssemblyOrModuleSymbol)
            .OfType<IAssemblySymbol>()
            .Select(assembly => new RoslynAssembly(assembly))
            .ToList();

        _assemblies.AddRange(assemblySymbols);
    }

    public IEnumerable<IXamlAssembly> Assemblies => _assemblies;

    public IXamlAssembly FindAssembly(string name) =>
        Assemblies
            .FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));

    [UnconditionalSuppressMessage("Trimming", "IL2092", Justification = TrimmingMessages.Roslyn)]
    public IXamlType FindType(string name) =>
        _assemblies
            .Select(assembly => assembly.FindType(name))
            .FirstOrDefault(type => type != null);

    [UnconditionalSuppressMessage("Trimming", "IL2092", Justification = TrimmingMessages.Roslyn)]
    public IXamlType FindType(string name, string assembly) =>
        _assemblies
            .Select(assemblyInstance => assemblyInstance.FindType(name))
            .FirstOrDefault(type => type != null);
}
    
internal class RoslynAssembly : IXamlAssembly
{
    private readonly IAssemblySymbol _symbol;

    public RoslynAssembly(IAssemblySymbol symbol) => _symbol = symbol;

    public bool Equals(IXamlAssembly other) =>
        other is RoslynAssembly roslynAssembly &&
        SymbolEqualityComparer.Default.Equals(_symbol, roslynAssembly._symbol);

    public string Name => _symbol.Name;

    public IReadOnlyList<IXamlCustomAttribute> CustomAttributes =>
        _symbol.GetAttributes()
            .Select(data => new RoslynAttribute(data, this))
            .ToList();

    public IXamlType FindType(string fullName)
    {
        var type = _symbol.GetTypeByMetadataName(fullName);
        return type is null ? null : new RoslynType(type, this);
    }
}

internal class RoslynAttribute : IXamlCustomAttribute
{
    private readonly AttributeData _data;
    private readonly RoslynAssembly _assembly;

    public RoslynAttribute(AttributeData data, RoslynAssembly assembly)
    {
        _data = data;
        _assembly = assembly;
    }

    public bool Equals(IXamlCustomAttribute other) =>
        other is RoslynAttribute attribute &&
        _data == attribute._data;

    public IXamlType Type => new RoslynType(_data.AttributeClass, _assembly);

    public List<object> Parameters =>
        _data.ConstructorArguments
            .Select(argument => argument.Value)
            .ToList();

    public Dictionary<string, object> Properties =>
        _data.NamedArguments.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Value);
}
    
internal class RoslynType : IXamlType
{
    private static readonly SymbolDisplayFormat SymbolDisplayFormat = new SymbolDisplayFormat(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters |
                         SymbolDisplayGenericsOptions.IncludeTypeConstraints |
                         SymbolDisplayGenericsOptions.IncludeVariance);

    private readonly RoslynAssembly _assembly;
    private readonly INamedTypeSymbol _symbol;

    public RoslynType(INamedTypeSymbol symbol, RoslynAssembly assembly)
    {
        _symbol = symbol;
        _assembly = assembly;
    }

    public bool Equals(IXamlType other) =>
        other is RoslynType roslynType && 
        SymbolEqualityComparer.Default.Equals(_symbol, roslynType._symbol);

    public object Id => _symbol;
        
    public string Name => _symbol.Name;

    public string Namespace => _symbol.ContainingNamespace.ToDisplayString(SymbolDisplayFormat);

    public string FullName => $"{Namespace}.{Name}";

    public IXamlAssembly Assembly => _assembly;

    public IReadOnlyList<IXamlProperty> Properties =>
        _symbol.GetMembers()
            .Where(member => member.Kind == SymbolKind.Property)
            .OfType<IPropertySymbol>()
            .Select(property => new RoslynProperty(property, _assembly))
            .ToList();

    public IReadOnlyList<IXamlEventInfo> Events { get; } = new List<IXamlEventInfo>();
        
    public IReadOnlyList<IXamlField> Fields { get; } = new List<IXamlField>();
        
    public IReadOnlyList<IXamlMethod> Methods { get; } = new List<IXamlMethod>();

    public IReadOnlyList<IXamlConstructor> Constructors =>
        _symbol.Constructors
            .Select(method => new RoslynConstructor(method, _assembly))
            .ToList();
        
    public IReadOnlyList<IXamlCustomAttribute> CustomAttributes { get; } = new List<IXamlCustomAttribute>();

    public IReadOnlyList<IXamlType> GenericArguments { get; private set; } = new List<IXamlType>();

    public bool IsAssignableFrom(IXamlType type) => type == this;

    public IXamlType MakeGenericType(IReadOnlyList<IXamlType> typeArguments)
    {
        GenericArguments = typeArguments;
        return this;
    }

    public IXamlType GenericTypeDefinition => this;
        
    public bool IsArray => false;

    public IXamlType ArrayElementType { get; } = null;
        
    public IXamlType MakeArrayType(int dimensions) => null;

    public IXamlType BaseType => _symbol.BaseType == null ? null : new RoslynType(_symbol.BaseType, _assembly);

    public bool IsValueType { get; } = false;

    public bool IsEnum { get; } = false;

    public IReadOnlyList<IXamlType> Interfaces =>
        _symbol.AllInterfaces
            .Select(abstraction => new RoslynType(abstraction, _assembly))
            .ToList();

    public bool IsInterface => _symbol.IsAbstract;
        
    public IXamlType GetEnumUnderlyingType() => null;

    public IReadOnlyList<IXamlType> GenericParameters { get; } = new List<IXamlType>();
}

internal class RoslynConstructor : IXamlConstructor
{
    private readonly IMethodSymbol _symbol;
    private readonly RoslynAssembly _assembly;

    public RoslynConstructor(IMethodSymbol symbol, RoslynAssembly assembly)
    {
        _symbol = symbol;
        _assembly = assembly;
    }

    public bool Equals(IXamlConstructor other) =>
        other is RoslynConstructor roslynConstructor &&
        SymbolEqualityComparer.Default.Equals(_symbol, roslynConstructor._symbol);

    public bool IsPublic => true;
        
    public bool IsStatic => false;

    public IReadOnlyList<IXamlType> Parameters =>
        _symbol.Parameters
            .Select(parameter => parameter.Type)
            .OfType<INamedTypeSymbol>()
            .Select(type => new RoslynType(type, _assembly))
            .ToList();
}

internal class RoslynProperty : IXamlProperty
{
    private readonly IPropertySymbol _symbol;
    private readonly RoslynAssembly _assembly;

    public RoslynProperty(IPropertySymbol symbol, RoslynAssembly assembly)
    {
        _symbol = symbol;
        _assembly = assembly;
    }

    public bool Equals(IXamlProperty other) =>
        other is RoslynProperty roslynProperty &&
        SymbolEqualityComparer.Default.Equals(_symbol, roslynProperty._symbol);

    public string Name => _symbol.Name;

    public IXamlType PropertyType =>
        _symbol.Type is INamedTypeSymbol namedTypeSymbol
            ? new RoslynType(namedTypeSymbol, _assembly)
            : null;

    public IXamlMethod Getter => _symbol.GetMethod == null ? null : new RoslynMethod(_symbol.GetMethod, _assembly);
        
    public IXamlMethod Setter => _symbol.SetMethod == null ? null : new RoslynMethod(_symbol.SetMethod, _assembly); 
        
    public IReadOnlyList<IXamlCustomAttribute> CustomAttributes { get; } = new List<IXamlCustomAttribute>();

    public IReadOnlyList<IXamlType> IndexerParameters { get; } = new List<IXamlType>();
}

internal class RoslynMethod : IXamlMethod
{
    private readonly IMethodSymbol _symbol;
    private readonly RoslynAssembly _assembly;

    public RoslynMethod(IMethodSymbol symbol, RoslynAssembly assembly)
    {
        _symbol = symbol;
        _assembly = assembly;
    }

    public bool Equals(IXamlMethod other) =>
        other is RoslynMethod roslynMethod &&
        SymbolEqualityComparer.Default.Equals(roslynMethod._symbol, _symbol);

    public string Name => _symbol.Name;

    public bool IsPublic => true;

    public bool IsStatic => false;

    public IXamlType ReturnType => new RoslynType((INamedTypeSymbol) _symbol.ReturnType, _assembly);

    public IReadOnlyList<IXamlType> Parameters =>
        _symbol.Parameters.Select(parameter => parameter.Type)
            .OfType<INamedTypeSymbol>()
            .Select(type => new RoslynType(type, _assembly))
            .ToList();
        
    public IXamlType DeclaringType => new RoslynType((INamedTypeSymbol)_symbol.ReceiverType, _assembly);
        
    public IXamlMethod MakeGenericMethod(IReadOnlyList<IXamlType> typeArguments) => null;

    public IReadOnlyList<IXamlCustomAttribute> CustomAttributes { get; } = new List<IXamlCustomAttribute>();
}
