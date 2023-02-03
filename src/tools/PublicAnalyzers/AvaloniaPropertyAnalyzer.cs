using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Avalonia.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
public partial class AvaloniaPropertyAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "AvaloniaProperty";

    private const string TypeMismatchTag = "TypeMismatch";
    private const string NameCollisionTag = "NameCollision";
    private const string AssociatedPropertyTag = "AssociatedProperty";

    private static readonly DiagnosticDescriptor AssociatedAvaloniaProperty = new(
        "AVP0001",
        "Identify the AvaloniaProperty associated with a CLR property",
        "Associated AvaloniaProperty: {0} {1}",
        Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        "This informational diagnostic identifies which AvaloniaProperty a CLR property is associated with.",
        AssociatedPropertyTag);

    private static readonly DiagnosticDescriptor InappropriatePropertyAssignment = new(
        "AVP1000",
        "Store AvaloniaProperty objects appropriately",
        "Incorrect AvaloniaProperty storage: {0} should be static and readonly",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        "AvaloniaProperty objects have static lifetimes and should be stored accordingly. Do not multiply construct the same property.");

    private static readonly DiagnosticDescriptor OwnerDoesNotMatchOuterType = new(
        "AVP1010",
        "Avaloniaproperty objects should declare their owner to be the type in which they are stored",
        "Type mismatch: AvaloniaProperty owner is {0}, which is not the containing type",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        "The owner of an AvaloniaProperty should generally be the containing type. This ensures that the property can be used as expected in XAML.",
        TypeMismatchTag);

    private static readonly DiagnosticDescriptor DuplicatePropertyName = new(
        "AVP1020",
        "AvaloniaProperty names should be unique within each class",
        "Name collision: {0} has the same name as {1}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        "Querying for an AvaloniaProperty by name requires that each property associated with a type have a unique name.",
        NameCollisionTag);

    private static readonly DiagnosticDescriptor AmbiguousPropertyName = new(
        "AVP1021",
        "Ensure an umabiguous relationship between CLR properties and Avalonia properties within the same class",
        "Name collision: {0} owns multiple Avalonia properties with the name '{1}' {2}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        "It is unclear which AvaloniaProperty this CLR property refers to. Ensure that each AvaloniaProperty associated with a type has a unique name. If you need to change behaviour of a base property in your class, call its AddOwner method and provide new metadata.",
        NameCollisionTag);

    private static readonly DiagnosticDescriptor PropertyNameMismatch = new(
        "AVP1022",
        "Store each AvaloniaProperty object in a field or CLR property which reflects its name",
        "Bad name: An AvaloniaProperty named '{0}' is being assigned to {1}. These names do not relate.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        "An AvaloniaProperty should be stored in a field or property which contains its name. For example, a property named \"Brush\" should be assigned to a field called \"BrushProperty\".\nPrivate symbols are exempt from this diagnostic.",
        NameCollisionTag);

    private static readonly DiagnosticDescriptor AccessorSideEffects = new(
        "AVP1030",
        "Do not add side effects to StyledProperty accessors",
        "Side effects: '{0}' is an AvaloniaProperty which can be {1} without the use of this CLR property. This {2} accessor should do nothing except call {3}.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        "The AvaloniaObject.GetValue and AvaloniaObject.SetValue methods are public, and do not call any user CLR properties. To execute code before or after the property is set, create a Coerce method or a PropertyChanged subscriber.",
        AssociatedPropertyTag);

    private static readonly DiagnosticDescriptor MissingAccessor = new(
        "AVP1031",
        "A CLR property should support the same get/set operations as its associated AvaloniaProperty",
        "Missing accessor: {0} is {1}, but this CLR property lacks a {2} accessor",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        "The AvaloniaObject.GetValue and AvaloniaObject.SetValue methods are public, and do not call CLR properties on the owning type. Not providing both CLR property accessors is ineffective.",
        AssociatedPropertyTag);

    private static readonly DiagnosticDescriptor InconsistentAccessibility = new(
        "AVP1032",
        "A CLR property and its accessors should be equally accessible as its associated AvaloniaProperty",
        "Inconsistent accessibility: CLR {0} accessiblity does not match accessibility of {1}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        "The AvaloniaObject.GetValue and AvaloniaObject.SetValue methods are public, and do not call CLR properties on the owning type. Defining a CLR property with different acessibility from its associated AvaloniaProperty is ineffective.",
        AssociatedPropertyTag);

    private static readonly DiagnosticDescriptor PropertyTypeMismatch = new(
        "AVP1040",
        "CLR property type should match associated AvaloniaProperty type",
        "Type mismatch: CLR property type differs from the value type of {0} {1}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        "The AvaloniaObject.GetValue and AvaloniaObject.SetValue methods are public, and do not call CLR properties on the owning type. A CLR property changing the value type (even when an implicit cast is possible) is ineffective and can lead to InvalidCastException to be thrown.",
        TypeMismatchTag, AssociatedPropertyTag);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        AssociatedAvaloniaProperty,
        InappropriatePropertyAssignment,
        OwnerDoesNotMatchOuterType,
        DuplicatePropertyName,
        AmbiguousPropertyName,
        PropertyNameMismatch,
        AccessorSideEffects,
        MissingAccessor,
        InconsistentAccessibility,
        PropertyTypeMismatch);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(c =>
        {
            if (c.Compilation.GetTypeByMetadataName("Avalonia.AvaloniaObject") is { } avaloniaObjectType)
            {
                new CompileAnalyzer(c, avaloniaObjectType);
            }
        });
    }

    private static bool IsAvaloniaPropertyType(INamedTypeSymbol type, params INamedTypeSymbol[] propertyTypes) => IsAvaloniaPropertyType(type, propertyTypes.AsEnumerable());
    
    private static bool IsAvaloniaPropertyType(INamedTypeSymbol type, IEnumerable<INamedTypeSymbol> propertyTypes)
    {
        if (type.IsGenericType)
        {
            type = type.ConstructUnboundGenericType().OriginalDefinition;
        }

        return propertyTypes.Any(t => SymbolEquals(type, t));
    }

    private static bool DerivesFrom(ITypeSymbol? type, ITypeSymbol baseType)
    {
        while (type != null)
        {
            if (SymbolEquals(type, baseType))
            {
                return true;
            }

            type = type.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Follows assignments and conversions back to their source.
    /// </summary>
    private static IOperation ResolveOperationSource(IOperation operation)
    {
        while (true)
        {
            switch (operation)
            {
                case IConversionOperation conversion:
                    operation = conversion.Operand;
                    break;
                case ISimpleAssignmentOperation assignment:
                    operation = assignment.Value;
                    break;
                default:
                    return operation;
            }
        }
    }

    private static bool IsValidAvaloniaPropertyStorage(IFieldSymbol field) => field.IsStatic && field.IsReadOnly;
    private static bool IsValidAvaloniaPropertyStorage(IPropertySymbol field) => field.IsStatic && field.IsReadOnly;

    private static bool SymbolEquals(ISymbol? x, ISymbol? y, bool includeNullability = false)
    {
        // The current version of Microsoft.CodeAnalysis includes an "IncludeNullability" comparer,
        // but it overshoots the target and tries to compare EVERYTHING. This leads to two symbols for
        // the same type not being equal if they were imported into different compile units (i.e. assemblies).
        // So for now, we will just discard this parameter.
        _ = includeNullability; 

        return SymbolEqualityComparer.Default.Equals(x, y);
    }

    private class AvaloniaPropertyDescription
    {
        /// <summary>
        /// Gets the name that was assigned to this property when it was registered.
        /// </summary>
        /// <remarks>
        /// If the property was not registered within the current compile context, this value will be inferred from 
        /// the name of the field (or CLR property) in which the AvaloniaProperty object is stored.
        /// </remarks>
        public string Name { get; set; }

        /// <summary>
        /// Gets the type of the AvaloniaProperty itself: Styled, Direct, or Attached
        /// </summary>
        public INamedTypeSymbol PropertyType { get; }

        /// <summary>
        /// Gets the TValue type that the property stores.
        /// </summary>
        public INamedTypeSymbol ValueType { get; }

        /// <summary>
        /// Gets the type which registered the property, and all types which have added themselves as owners.
        /// </summary>
        public IReadOnlyCollection<TypeReference> OwnerTypes { get; private set; }
        private ConcurrentBag<TypeReference>? _ownerTypes = new();

        /// <summary>
        /// Gets a dictionary which maps fields and properties which were initialized with this AvaloniaProperty to the TOwner specified at each assignment.
        /// </summary>
        public IReadOnlyDictionary<ISymbol, TypeReference> AssignedTo { get; private set; }
        private ConcurrentDictionary<ISymbol, TypeReference>? _assignedTo = new(SymbolEqualityComparer.Default);

        /// <summary>
        /// Gets properties which provide convenient access to the AvaloniaProperty on an instance of an AvaloniaObject.
        /// </summary>
        public IReadOnlyCollection<IPropertySymbol> PropertyWrappers { get; private set; }
        private ConcurrentBag<IPropertySymbol>? _propertyWrappers = new();

        public AvaloniaPropertyDescription(string name, INamedTypeSymbol propertyType, INamedTypeSymbol valueType)
        {
            Name = name;
            PropertyType = propertyType;
            ValueType = valueType;

            OwnerTypes = _ownerTypes;
            PropertyWrappers = _propertyWrappers;
            AssignedTo = _assignedTo;
        }

        private const string SealedError = "PropertyDescription has been sealed.";

        public void AddOwner(TypeReference owner) => (_ownerTypes ?? throw new InvalidOperationException(SealedError)).Add(owner);

        public void AddPropertyWrapper(IPropertySymbol property) => (_propertyWrappers ?? throw new InvalidOperationException(SealedError)).Add(property);

        public void AddAssignment(ISymbol assignmentTarget, TypeReference ownerType) => (_assignedTo ?? throw new InvalidOperationException(SealedError)).TryAdd(assignmentTarget, ownerType);

        public AvaloniaPropertyDescription Seal()
        {
            if (_ownerTypes == null || _propertyWrappers == null || _assignedTo == null)
            {
                return this;
            }

            OwnerTypes = _ownerTypes.ToImmutableHashSet();
            _ownerTypes = null;

            PropertyWrappers = _propertyWrappers.ToImmutableHashSet<IPropertySymbol>(SymbolEqualityComparer.Default);
            _propertyWrappers = null;

            AssignedTo = new ReadOnlyDictionary<ISymbol, TypeReference>(_assignedTo);
            _assignedTo = null;

            return this;
        }

        /// <summary>
        /// Searches the inheritance hierarchy of the given type for a field or property to which this AvaloniaProperty is assigned.
        /// </summary>
        public ISymbol? ClosestAssignmentFor(ITypeSymbol? type)
        {
            var assignmentsByType = AssignedTo.Keys.ToLookup(s => s.ContainingType, SymbolEqualityComparer.Default);

            while (type != null)
            {
                if (assignmentsByType.Contains(type))
                {
                    return assignmentsByType[type].First();
                }
                type = type.BaseType;
            }

            return null;
        }
    }

    private readonly struct TypeReference
    {
        public INamedTypeSymbol Type { get; }
        public Location Location { get; }

        public TypeReference(INamedTypeSymbol type, Location location)
        {
            Type = type;
            Location = location;
        }
    }

    private class SymbolEqualityComparer<T> : IEqualityComparer<T> where T : ISymbol
    {
        public bool Equals(T x, T y) => SymbolEqualityComparer.Default.Equals(x, y);
        public int GetHashCode(T obj) => SymbolEqualityComparer.Default.GetHashCode(obj);

        public static SymbolEqualityComparer<T> Default { get; } = new();
    }
}

[Serializable]
public class AvaloniaAnalysisException : Exception
{
    public AvaloniaAnalysisException(string message, Exception? innerException = null) : base(message, innerException)
    {
    }

    protected AvaloniaAnalysisException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
