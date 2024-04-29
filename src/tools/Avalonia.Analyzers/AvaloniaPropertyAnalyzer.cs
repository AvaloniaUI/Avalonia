using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Avalonia.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
public partial class AvaloniaPropertyAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "AvaloniaProperty";

    private const string TypeMismatchTag = "TypeMismatch";
    private const string NameCollisionTag = "NameCollision";
    private const string AssociatedClrPropertyTag = "AssociatedClrProperty";
    private const string InappropriateReadWriteTag = "InappropriateReadWrite";

    private static readonly DiagnosticDescriptor AssociatedAvaloniaProperty = new(
        "AVP0001",
        "Identification of the AvaloniaProperty associated with a CLR property",
        "Associated AvaloniaProperty: {0} {1}",
        Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        "This informational diagnostic identifies which AvaloniaProperty a CLR property is associated with.",
        AssociatedClrPropertyTag);

    private static readonly DiagnosticDescriptor InappropriatePropertyAssignment = new(
        "AVP1000",
        "AvaloniaProperty objects should be stored appropriately",
        "Incorrect AvaloniaProperty storage: {0} should be static and readonly",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        "AvaloniaProperty objects have static lifetimes and should be stored accordingly.");

    private static readonly DiagnosticDescriptor InappropriatePropertyRegistration = new(
        "AVP1001",
        "The same AvaloniaProperty should not be registered twice",
        "Unsafe registration: {0} should be called only in static constructors or static initializers",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        "AvaloniaProperty objects have static lifetimes and should be created only once. To ensure this, only call Register or AddOwner in static constructors or static initializers.");

    private static readonly DiagnosticDescriptor PropertyOwnedByGenericType = new(
        "AVP1002",
        "AvaloniaProperty objects should not be owned by a generic type",
        "Inadvisable registration: Generic types cannot be referenced from XAML. Create a non-generic type to be the owner of this AvaloniaProperty.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        "It is sometimes necessary to refer to an AvaloniaProperty in XAML by providing its class name. This cannot be achieved if property's owner is a generic type." +
        " Additionally, a new AvaloniaProperty object will be generated each time a new version of the generic owner type is constructed, which may be unexpected.");

    private static readonly DiagnosticDescriptor OwnerDoesNotMatchOuterType = new(
        "AVP1010",
        "AvaloniaProperty objects should be owned by the type in which they are stored",
        "Type mismatch: AvaloniaProperty owner is {0}, which is not the containing type",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false, // TODO: autogenerate property metadata preserved in ref assembly
        "The owner of an AvaloniaProperty should generally be the containing type. This ensures that the property can be used as expected in XAML.",
        TypeMismatchTag);

    private static readonly DiagnosticDescriptor UnexpectedPropertyAccess = new(
        "AVP1011",
        "An AvaloniaObject should own each AvaloniaProperty it reads or writes on itself",
        "Unexpected property use: {0} is neither owned by nor attached to {1}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false, // TODO: autogenerate property metadata preserved in ref assembly
        "It is possible to use any AvaloniaProperty with any AvaloniaObject. However, each AvaloniaProperty an object uses on itself should be either owned by that object, or attached to that object.",
        InappropriateReadWriteTag);

    private static readonly DiagnosticDescriptor SettingOwnStyledPropertyValue = new(
        "AVP1012",
        "An AvaloniaObject should use SetCurrentValue when assigning its own StyledProperty or AttachedProperty values",
        "Inappropriate assignment: An AvaloniaObject should use SetCurrentValue when setting its own StyledProperty or AttachedProperty values",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false, // TODO: autogenerate property metadata preserved in ref assembly
        "The standard means of setting an AvaloniaProperty is to call the SetValue method (often via a CLR property setter). This will forcibly overwrite values from sources like styles and templates, " +
        "which is something that should only be done by consumers of the control, not the control itself. Controls which want to set their own values should instead call the SetCurrentValue method, or " +
        "refactor the property into a DirectProperty. An assignment is exempt from this diagnostic in two scenarios: when it is forwarding a constructor parameter, and when the target object is derived " +
        "from UserControl or TopLevel.",
        InappropriateReadWriteTag);

    private static readonly DiagnosticDescriptor SuperfluousAddOwnerCall = new(
        "AVP1013",
        "AvaloniaProperty owners should not be added superfluously",
        "Superfluous owner: {0} is already an owner of {1} via {2}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false, // TODO: autogenerate property metadata preserved in ref assembly
        "Ownership of an AvaloniaProperty is inherited along the type hierarchy. There is no need for a derived type to assert ownership over a base type's properties. This diagnostic can be a symptom of an incorrect property owner elsewhere.",
        InappropriateReadWriteTag);

    private static readonly DiagnosticDescriptor DuplicatePropertyName = new(
        "AVP1020",
        "AvaloniaProperty names should be unique within each class",
        "Name collision: {0} has the same name as {1}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false, // TODO: autogenerate property metadata preserved in ref assembly
        "Querying for an AvaloniaProperty by name requires that each property associated with a type have a unique name.",
        NameCollisionTag);

    private static readonly DiagnosticDescriptor AmbiguousPropertyName = new(
        "AVP1021",
        "There should be an unambiguous relationship between the CLR properties and Avalonia properties of a class",
        "Name collision: {0} owns multiple Avalonia properties with the name '{1}' {2}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false, // TODO: autogenerate property metadata preserved in ref assembly
        "It is unclear which AvaloniaProperty this CLR property refers to. Ensure that each AvaloniaProperty associated with a type has a unique name. If you need to change behaviour of a base property in your class, call its OverrideMetadata or OverrideDefaultValue methods.",
        NameCollisionTag);

    private static readonly DiagnosticDescriptor PropertyNameMismatch = new(
        "AVP1022",
        "An AvaloniaProperty object should be stored in a field or CLR property which reflects its name",
        "Bad name: An AvaloniaProperty named '{0}' is being assigned to {1}. These names do not relate.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false, // TODO: autogenerate property metadata preserved in ref assembly
        "An AvaloniaProperty should be stored in a field or property which contains its name. For example, a property named \"Brush\" should be assigned to a field called \"BrushProperty\".\nPrivate symbols are exempt from this diagnostic.",
        NameCollisionTag);

    private static readonly DiagnosticDescriptor AccessorSideEffects = new(
        "AVP1030",
        "StyledProperty accessors should not have side effects",
        "Side effects: '{0}' is an AvaloniaProperty which can be {1} without the use of this CLR property. This {2} accessor should do nothing except call {3}.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false, // TODO: autogenerate property metadata preserved in ref assembly
        "The AvaloniaObject.GetValue and AvaloniaObject.SetValue methods are public, and do not call any user CLR properties. To execute code before or after the property is set, consider: 1) adding a Coercion method, b) adding a static observer with AvaloniaProperty.Changed.AddClassHandler, and/or c) overriding the AvaloniaObject.OnPropertyChanged method.",
        AssociatedClrPropertyTag);

    private static readonly DiagnosticDescriptor MissingAccessor = new(
        "AVP1031",
        "A CLR property should support the same get/set operations as its associated AvaloniaProperty",
        "Missing accessor: {0} is {1}, but this CLR property lacks a {2} accessor",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false, // TODO: autogenerate property metadata preserved in ref assembly
        "The AvaloniaObject.GetValue and AvaloniaObject.SetValue methods are public, and do not call CLR properties on the owning type. Not providing both CLR property accessors is ineffective.",
        AssociatedClrPropertyTag);

    private static readonly DiagnosticDescriptor InconsistentAccessibility = new(
        "AVP1032",
        "A CLR property and its accessors should be equally accessible as its associated AvaloniaProperty",
        "Inconsistent accessibility: CLR {0} accessibility does not match accessibility of {1}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false, // TODO: autogenerate property metadata preserved in ref assembly
        "The AvaloniaObject.GetValue and AvaloniaObject.SetValue methods are public, and do not call CLR properties on the owning type. Defining a CLR property with different accessibility from its associated AvaloniaProperty is ineffective.",
        AssociatedClrPropertyTag);

    private static readonly DiagnosticDescriptor PropertyTypeMismatch = new(
        "AVP1040",
        "A CLR property type should match the associated AvaloniaProperty type",
        "Type mismatch: CLR property type differs from the value type of {0} {1}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false, // TODO: autogenerate property metadata preserved in ref assembly
        "The AvaloniaObject.GetValue and AvaloniaObject.SetValue methods are public, and do not call CLR properties on the owning type. A CLR property changing the value type (even when an implicit cast is possible) is ineffective and can lead to InvalidCastException to be thrown.",
        TypeMismatchTag, AssociatedClrPropertyTag);

    private static readonly SymbolDisplayFormat TypeQualifiedName = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        AssociatedAvaloniaProperty,
        InappropriatePropertyAssignment,
        InappropriatePropertyRegistration,
        PropertyOwnedByGenericType,
        OwnerDoesNotMatchOuterType,
        UnexpectedPropertyAccess,
        SettingOwnStyledPropertyValue,
        SuperfluousAddOwnerCall,
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

    private static bool IsAvaloniaPropertyType(ITypeSymbol type, params INamedTypeSymbol[] propertyTypes) => IsAvaloniaPropertyType(type, propertyTypes.AsEnumerable());
    
    private static bool IsAvaloniaPropertyType(ITypeSymbol type, IEnumerable<INamedTypeSymbol> propertyTypes)
    {
        type = type.OriginalDefinition;

        return propertyTypes.Any(t => SymbolEquals(type, t));
    }

    private static bool DerivesFrom(ITypeSymbol? type, ITypeSymbol? baseType, bool includeSelf = true)
    {
        if (baseType != null)
        {
            if (!includeSelf)
            {
                type = type?.BaseType;
            }

            while (type != null)
            {
                if (SymbolEquals(type, baseType))
                {
                    return true;
                }

                type = type.BaseType;
            }
        }
        return false;
    }

    /// <summary>
    /// Follows assignments and conversions back to their source.
    /// </summary>
    private static IOperation ResolveOperationSource(IOperation operation, CancellationToken cancellationToken)
    {
        var seen = new HashSet<IOperation>();

        while (true)
        {
            if (!seen.Add(operation)) // https://github.com/AvaloniaUI/Avalonia/issues/12864
            {
                Debug.Fail("Operation recursion detected.");
                return operation;
            }

            cancellationToken.ThrowIfCancellationRequested();

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

    private static IOperation ResolveOperationTarget(IOperation operation, CancellationToken cancellationToken)
    {
        var seen = new HashSet<IOperation>();
        
        while (true)
        {
            if (!seen.Add(operation)) // https://github.com/AvaloniaUI/Avalonia/issues/12864
            {
                Debug.Fail("Operation recursion detected.");
                return operation;
            }
            
            cancellationToken.ThrowIfCancellationRequested();

            switch (operation)
            {
                case IConversionOperation conversion:
                    operation = conversion.Parent!;
                    break;
                case ISimpleAssignmentOperation assignment:
                    operation = assignment.Target;
                    break;
                default:
                    return operation;
            }
        }
    }

    private static ISymbol? GetReferencedFieldOrProperty(IOperation? operation, CancellationToken cancellationToken) => operation == null ? null : ResolveOperationSource(operation, cancellationToken) switch
    {
        IFieldReferenceOperation fieldRef => fieldRef.Field,
        IPropertyReferenceOperation propertyRef => propertyRef.Property,
        IArgumentOperation argument => GetReferencedFieldOrProperty(argument.Value, cancellationToken),
        _ => null,
    };

    private static bool IsValidAvaloniaPropertyStorage(IFieldSymbol field) => field.IsStatic && field.IsReadOnly;
    private static bool IsValidAvaloniaPropertyStorage(IPropertySymbol field) => field.IsStatic && field.IsReadOnly;

    /// <exception cref="AvaloniaAnalysisException"/>
    private static void WrapAndThrowIfNotCancellation(Exception exception, string analysisContextMessage, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException oce && oce.CancellationToken == cancellationToken)
        {
            return;
        }

        throw new AvaloniaAnalysisException(analysisContextMessage, exception);
    }

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
        public ITypeSymbol ValueType { get; }

        /// <summary>
        /// Gets whether the value of this property is inherited from the parent AvaloniaObject.
        /// </summary>
        public bool Inherits { get; set; }

        /// <summary>
        /// Gets the type which registered the property, and all types which have added themselves as owners.
        /// </summary>
        public IReadOnlyCollection<TypeReference> OwnerTypes { get; private set; }
        private ConcurrentBag<TypeReference>? _ownerTypes = new();

        /// <summary>
        /// Gets the type to which an AttachedProperty is attached, or null if the property is StyledProperty or DirectProperty.
        /// </summary>
        public TypeReference? HostType { get; set; }

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

        public AvaloniaPropertyDescription(string name, INamedTypeSymbol propertyType, ITypeSymbol valueType)
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

        public void SetAssignment(ISymbol assignmentTarget, TypeReference ownerType) => (_assignedTo ?? throw new InvalidOperationException(SealedError))[assignmentTarget] = ownerType;

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
        public ITypeSymbol Type { get; }
        public Location Location { get; }

        public TypeReference(ITypeSymbol type, Location location)
        {
            Type = type;
            Location = location;
        }

        public static TypeReference FromInvocationTypeParameter(IInvocationOperation invocation, ITypeParameterSymbol typeParameter)
        {
            var argument = invocation.TargetMethod.TypeArguments[typeParameter.Ordinal];

            var typeArgumentSyntax = invocation.Syntax;
            if (invocation.Language == LanguageNames.CSharp) // type arguments do not appear in the invocation, so search the code for them
            {
                try
                {
                    typeArgumentSyntax = invocation.Syntax.DescendantNodes()
                        .First(n => n.IsKind(SyntaxKind.TypeArgumentList))
                        .DescendantNodes().ElementAt(typeParameter.Ordinal);
                }
                catch
                {
                    // ignore, this is just a nicety
                }
            }

            return new TypeReference(argument, typeArgumentSyntax.GetLocation());
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
