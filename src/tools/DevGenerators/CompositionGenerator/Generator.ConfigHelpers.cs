using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Avalonia.SourceGenerator.CompositionGenerator;

public partial class Generator
{
    private sealed class GeneratorTypeInfo
    {
        public TypeSyntax RoslynType { get; set; } = null!;
        public string FilteredTypeName { get; set; } = null!;
        public bool IsObject { get; set; }
        public bool IsPassthrough { get; set; }
        public string ServerType { get; set; } = null!;
        public bool IsNullable { get; set; }
    }

    private readonly Dictionary<string, GeneratorTypeInfo> _typeInfoCache = new();

    private GeneratorTypeInfo GetTypeInfo(string type)
    {
        if (_typeInfoCache.TryGetValue(type, out var cached))
            return cached;
        
        var propType = ParseTypeName(type);
        var filteredType = type.TrimEnd('?');
        var isObject = _objects.Contains(filteredType);
        var isNullable = type.EndsWith("?");
        bool isPassthrough = false;
                
        var serverType = ((isObject ? "Server" : "") + type);
        if (_manuals.TryGetValue(filteredType, out var manual))
        {
            if (manual.Passthrough)
            {
                isPassthrough = true;
                serverType = type;
            }

            if (manual.ServerName != null)
                serverType = manual.ServerName + (isNullable ? "?" : "");
        }

        return _typeInfoCache[type] = new GeneratorTypeInfo
        {
            RoslynType = propType,
            FilteredTypeName = filteredType,
            IsObject = isObject,
            IsPassthrough = isPassthrough,
            ServerType = serverType,
            IsNullable = isNullable
        };
    }
}
