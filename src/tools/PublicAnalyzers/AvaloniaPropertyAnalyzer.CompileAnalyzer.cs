using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Avalonia.Analyzers;

public partial class AvaloniaPropertyAnalyzer
{
    public class CompileAnalyzer
    {
        /// <summary>
        /// A dictionary that maps field/property symbols to the AvaloniaProperty objects assigned to them.
        /// </summary>
        private ImmutableDictionary<ISymbol, AvaloniaPropertyDescription> _avaloniaPropertyDescriptions = null!;

        /// <summary>
        /// Maps properties onto all AvaloniaProperty objects that they may be intended to represent.
        /// </summary>
        private ImmutableDictionary<IPropertySymbol, ImmutableArray<AvaloniaPropertyDescription>> _clrPropertyToAvaloniaProperties = null!;

        private readonly INamedTypeSymbol _stringType;
        private readonly INamedTypeSymbol _avaloniaObjectType;
        private readonly ImmutableHashSet<IMethodSymbol> _getValueMethods;
        private readonly ImmutableHashSet<IMethodSymbol> _setValueMethods;
        private readonly ImmutableHashSet<IMethodSymbol> _allGetSetMethods;
        private readonly INamedTypeSymbol _avaloniaPropertyType;
        private readonly INamedTypeSymbol _styledPropertyType;
        private readonly INamedTypeSymbol _attachedPropertyType;
        private readonly INamedTypeSymbol _directPropertyType;
        private readonly ImmutableArray<INamedTypeSymbol> _allAvaloniaPropertyTypes;
        private readonly ImmutableDictionary<INamedTypeSymbol, ITypeParameterSymbol> _propertyValueTypeParams;
        private readonly ImmutableHashSet<IMethodSymbol> _avaloniaPropertyRegisterMethods;
        private readonly ImmutableHashSet<IMethodSymbol> _avaloniaPropertyAddOwnerMethods;
        private readonly ImmutableHashSet<IMethodSymbol> _allAvaloniaPropertyMethods;
        private readonly ImmutableDictionary<IMethodSymbol, ITypeParameterSymbol> _ownerTypeParams;
        private readonly ImmutableDictionary<IMethodSymbol, ITypeParameterSymbol> _valueTypeParams;
        private readonly ImmutableDictionary<IMethodSymbol, ITypeParameterSymbol> _hostTypeParams;
        private readonly ImmutableDictionary<IMethodSymbol, IParameterSymbol> _inheritsParams;
        private readonly ImmutableDictionary<IMethodSymbol, IParameterSymbol> _ownerParams;

        public CompileAnalyzer(CompilationStartAnalysisContext context, INamedTypeSymbol avaloniaObjectType)
        {
            var methodComparer = SymbolEqualityComparer<IMethodSymbol>.Default;

            _stringType = GetTypeOrThrow("System.String");
            _avaloniaObjectType = avaloniaObjectType;
            _getValueMethods = _avaloniaObjectType.GetMembers("GetValue").OfType<IMethodSymbol>().ToImmutableHashSet(methodComparer);
            _setValueMethods = _avaloniaObjectType.GetMembers("SetValue").OfType<IMethodSymbol>().ToImmutableHashSet(methodComparer);
            _allGetSetMethods = _getValueMethods.Concat(_setValueMethods).ToImmutableHashSet(methodComparer);

            _avaloniaPropertyType = GetTypeOrThrow("Avalonia.AvaloniaProperty");
            _styledPropertyType = GetTypeOrThrow("Avalonia.StyledProperty`1");
            _attachedPropertyType = GetTypeOrThrow("Avalonia.AttachedProperty`1");
            _directPropertyType = GetTypeOrThrow("Avalonia.DirectProperty`2");

            _avaloniaPropertyRegisterMethods = _avaloniaPropertyType.GetMembers()
                .OfType<IMethodSymbol>().Where(m => m.Name.StartsWith("Register")).ToImmutableHashSet(methodComparer);

            _allAvaloniaPropertyTypes = new[] { _styledPropertyType, _attachedPropertyType, _directPropertyType }.ToImmutableArray();

            _propertyValueTypeParams = _allAvaloniaPropertyTypes.Select(p => p.TypeParameters.First(t => t.Name == "TValue"))
                .Where(p => p != null).Cast<ITypeParameterSymbol>()
                .ToImmutableDictionary(p => p.ContainingType, SymbolEqualityComparer<INamedTypeSymbol>.Default);

            _avaloniaPropertyAddOwnerMethods = _allAvaloniaPropertyTypes
                .SelectMany(t => t.GetMembers("AddOwner").OfType<IMethodSymbol>()).ToImmutableHashSet(methodComparer);

            _allAvaloniaPropertyMethods = _avaloniaPropertyRegisterMethods.Concat(_avaloniaPropertyAddOwnerMethods).ToImmutableHashSet(SymbolEqualityComparer<IMethodSymbol>.Default);

            _ownerTypeParams = GetParamDictionary("TOwner", m => m.TypeParameters);
            _valueTypeParams = GetParamDictionary("TValue", m => m.TypeParameters);
            _hostTypeParams = GetParamDictionary("THost", m => m.TypeParameters);
            _inheritsParams = GetParamDictionary("inherits", m => m.Parameters);
            _ownerParams = GetParamDictionary("ownerType", m => m.Parameters);

            RegisterAvaloniaPropertySymbols(context.Compilation, context.CancellationToken);

            context.RegisterOperationAction(AnalyzeFieldInitializer, OperationKind.FieldInitializer);
            context.RegisterOperationAction(AnalyzePropertyInitializer, OperationKind.PropertyInitializer);
            context.RegisterOperationAction(AnalyzeAssignment, OperationKind.SimpleAssignment);
            context.RegisterOperationAction(AnalyzeMethodInvocation, OperationKind.Invocation);

            context.RegisterSymbolStartAction(StartPropertySymbolAnalysis, SymbolKind.Property);

            if (context.Compilation.Language == LanguageNames.CSharp)
            {
                context.RegisterCodeBlockAction(AnalyzePropertyMethods);
            }

            INamedTypeSymbol GetTypeOrThrow(string name) => context.Compilation.GetTypeByMetadataName(name) ?? throw new KeyNotFoundException($"Could not locate {name} in the compilation context.");

            ImmutableDictionary<IMethodSymbol, TSymbol> GetParamDictionary<TSymbol>(string name, Func<IMethodSymbol, IEnumerable<TSymbol>> methodSymbolSelector) where TSymbol : ISymbol => _allAvaloniaPropertyMethods
                .Select(m => methodSymbolSelector(m).SingleOrDefault(p => p.Name == name))
                .Where(p => p != null).Cast<TSymbol>()
                .ToImmutableDictionary(p => (IMethodSymbol)p.ContainingSymbol, SymbolEqualityComparer<IMethodSymbol>.Default);
        }

        private bool IsAvaloniaPropertyStorage(IFieldSymbol symbol) => symbol.Type is INamedTypeSymbol namedType && IsAvaloniaPropertyType(namedType, _allAvaloniaPropertyTypes);
        private bool IsAvaloniaPropertyStorage(IPropertySymbol symbol) => symbol.Type is INamedTypeSymbol namedType && IsAvaloniaPropertyType(namedType, _allAvaloniaPropertyTypes);

        private void RegisterAvaloniaPropertySymbols(Compilation compilation, CancellationToken cancellationToken)
        {
            var namespaceStack = new Stack<INamespaceSymbol>();
            namespaceStack.Push(compilation.GlobalNamespace);

            var types = new List<INamedTypeSymbol>();

            while (namespaceStack.Count > 0)
            {
                var current = namespaceStack.Pop();

                types.AddRange(current.GetTypeMembers());

                foreach (var child in current.GetNamespaceMembers())
                {
                    namespaceStack.Push(child);
                }
            }

            var avaloniaPropertyStorageSymbols = new ConcurrentBag<ISymbol>();

            var propertyDescriptions = new ConcurrentDictionary<ISymbol, AvaloniaPropertyDescription>(SymbolEqualityComparer.Default);

            // key initializes value
            var fieldInitializations = new ConcurrentDictionary<ISymbol, ISymbol>(SymbolEqualityComparer.Default);

            var parallelOptions = new ParallelOptions() { CancellationToken = cancellationToken };

            var semanticModels = new ConcurrentDictionary<SyntaxTree, SemanticModel>();

            Parallel.ForEach(types, parallelOptions, type =>
            {
                try
                {
                    foreach (var member in type.GetMembers())
                    {
                        switch (member)
                        {
                            case IFieldSymbol fieldSymbol when IsAvaloniaPropertyStorage(fieldSymbol):
                                avaloniaPropertyStorageSymbols.Add(fieldSymbol);
                                break;
                            case IPropertySymbol propertySymbol when IsAvaloniaPropertyStorage(propertySymbol):
                                avaloniaPropertyStorageSymbols.Add(propertySymbol);
                                break;
                        }
                    }

                    foreach (var constructor in type.StaticConstructors)
                    {
                        foreach (var syntaxRef in constructor.DeclaringSyntaxReferences.Where(sr => compilation.ContainsSyntaxTree(sr.SyntaxTree)))
                        {
                            var (node, model) = GetNodeAndModel(syntaxRef);

                            foreach (var descendant in node.DescendantNodes().Where(n => n.IsKind(SyntaxKind.SimpleAssignmentExpression)))
                            {
                                var assignmentOperation = (IAssignmentOperation)model.GetOperation(descendant, cancellationToken)!;

                                if (GetReferencedFieldOrProperty(assignmentOperation.Target) is { } target)
                                {
                                    RegisterAssignment(target, assignmentOperation.Value);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    throw new AvaloniaAnalysisException($"Failed to find AvaloniaProperty objects in {type}.", ex);
                }
            });

            Parallel.ForEach(avaloniaPropertyStorageSymbols, parallelOptions, symbol =>
            {
                foreach (var syntaxRef in symbol.DeclaringSyntaxReferences.Where(sr => compilation.ContainsSyntaxTree(sr.SyntaxTree)))
                {
                    var (node, model) = GetNodeAndModel(syntaxRef);

                    var operation = node.ChildNodes().Select(n => model.GetOperation(n, cancellationToken)).OfType<ISymbolInitializerOperation>().FirstOrDefault();

                    if (operation == null)
                    {
                        return;
                    }

                    RegisterAssignment(symbol, operation.Value);
                }
            });

            // we have recorded every Register and AddOwner call. Now follow assignment chains.
            Parallel.ForEach(fieldInitializations.Keys.Intersect(propertyDescriptions.Keys, SymbolEqualityComparer.Default).ToArray(), root =>
            {
                var propertyDescription = propertyDescriptions[root];
                var owner = propertyDescription.AssignedTo[root];

                var current = root;
                do
                {
                    var target = fieldInitializations[current];

                    propertyDescription.AddAssignment(target, new(owner.Type, target.Locations[0])); // This loop handles simple assignment operations, so do NOT change the owner type
                    propertyDescriptions[target] = propertyDescription;

                    fieldInitializations.TryGetValue(target, out current);
                }
                while (current != null);
            });

            var clrPropertyWrapCandidates = new ConcurrentBag<(IPropertySymbol, AvaloniaPropertyDescription)>();

            var propertyDescriptionsByName = propertyDescriptions.Values.ToLookup(p => p.Name, p => (property: p, owners: p.OwnerTypes.Select(t => t.Type).ToImmutableHashSet(SymbolEqualityComparer.Default)));

            // Detect CLR properties that provide syntatic wrapping around an AvaloniaProperty (or potentially multiple, which leads to a warning diagnostic)
            Parallel.ForEach(propertyDescriptions.Values, propertyDescription =>
            {
                var nameMatches = propertyDescriptionsByName[propertyDescription.Name];

                foreach (var ownerType in propertyDescription.OwnerTypes.Select(o => o.Type).Distinct(SymbolEqualityComparer<ITypeSymbol>.Default))
                {
                    if (ownerType.GetMembers(propertyDescription.Name).OfType<IPropertySymbol>().SingleOrDefault() is not { IsStatic: false } clrProperty)
                    {
                        continue;
                    }

                    propertyDescription.AddPropertyWrapper(clrProperty);
                    clrPropertyWrapCandidates.Add((clrProperty, propertyDescription));

                    var current = ownerType.BaseType;
                    while (current != null)
                    {
                        foreach (var otherProp in nameMatches.Where(t => t.owners.Contains(current)).Select(t => t.property))
                        {
                            clrPropertyWrapCandidates.Add((clrProperty, otherProp));
                        }

                        current = current.BaseType;
                    }
                }
            });

            // convert our dictionaries to immutable form
            _clrPropertyToAvaloniaProperties = clrPropertyWrapCandidates.ToLookup(t => t.Item1, t => t.Item2, SymbolEqualityComparer<IPropertySymbol>.Default)
                .ToImmutableDictionary(g => g.Key, g => g.Distinct().ToImmutableArray(), SymbolEqualityComparer<IPropertySymbol>.Default);
            _avaloniaPropertyDescriptions = propertyDescriptions.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.Seal(), SymbolEqualityComparer.Default);

            void RegisterAssignment(ISymbol target, IOperation value)
            {
                switch (ResolveOperationSource(value))
                {
                    case IInvocationOperation invocation:
                        RegisterInitializer_Invocation(invocation, target, propertyDescriptions);
                        break;
                    case IFieldReferenceOperation fieldRef when IsAvaloniaPropertyStorage(fieldRef.Field):
                        fieldInitializations[fieldRef.Field] = target;
                        break;
                    case IPropertyReferenceOperation propRef when IsAvaloniaPropertyStorage(propRef.Property):
                        fieldInitializations[propRef.Property] = target;
                        break;
                }
            }

            (SyntaxNode, SemanticModel) GetNodeAndModel(SyntaxReference syntaxRef) =>
                (syntaxRef.GetSyntax(cancellationToken), semanticModels.GetOrAdd(syntaxRef.SyntaxTree, st => compilation.GetSemanticModel(st)));
        }

        // This method handles registration of a new AvaloniaProperty, and calls to AddOwner.
        private void RegisterInitializer_Invocation(IInvocationOperation invocation, ISymbol target, ConcurrentDictionary<ISymbol, AvaloniaPropertyDescription> propertyDescriptions)
        {
            try
            {
                if (invocation.TargetMethod.ReturnType is not INamedTypeSymbol propertyType)
                {
                    return;
                }

                var originalMethod = invocation.TargetMethod.OriginalDefinition;

                if (_avaloniaPropertyRegisterMethods.Contains(originalMethod)) // This is a call to one of the AvaloniaProperty.Register* methods
                {
                    TypeReference ownerTypeRef;

                    if (_ownerTypeParams.TryGetValue(originalMethod, out var ownerTypeParam))
                    {
                        ownerTypeRef = TypeReference.FromInvocationTypeParameter(invocation, ownerTypeParam);
                    }
                    else if (_ownerParams.TryGetValue(originalMethod, out var ownerParam) && // try extracting the runtime argument
                        ResolveOperationSource(invocation.Arguments[ownerParam.Ordinal].Value) is ITypeOfOperation { Type: ITypeSymbol type } typeOf)
                    {
                        ownerTypeRef = new TypeReference(type, typeOf.Syntax.GetLocation());
                    }
                    else
                    {
                        return;
                    }

                    TypeReference valueTypeRef;
                    if (_valueTypeParams.TryGetValue(originalMethod, out var valueTypeParam))
                    {
                        valueTypeRef = TypeReference.FromInvocationTypeParameter(invocation, valueTypeParam);
                    }
                    else
                    {
                        return;
                    }

                    string name;
                    switch (ResolveOperationSource(invocation.Arguments[0].Value))
                    {
                        case ILiteralOperation literal when SymbolEquals(literal.Type, _stringType):
                            name = (string)literal.ConstantValue.Value!;
                            break;
                        case INameOfOperation nameof when nameof.Argument is IPropertyReferenceOperation propertyReference:
                            name = propertyReference.Property.Name;
                            break;
                        case IFieldReferenceOperation fieldRef when SymbolEquals(fieldRef.Type, _stringType) && fieldRef.ConstantValue is { HasValue: true } constantValue:
                            name = (string)fieldRef.ConstantValue.Value!;
                            break;
                        default:
                            return;
                    }

                    var inherits = false;
                    if (_inheritsParams.TryGetValue(originalMethod, out var inheritsParam) &&
                        invocation.Arguments[inheritsParam.Ordinal].Value is ILiteralOperation literalOp &&
                        literalOp.ConstantValue.Value is bool constValue)
                    {
                        inherits = constValue;
                    }

                    TypeReference? hostTypeRef = null;
                    if (SymbolEquals(propertyType.OriginalDefinition, _attachedPropertyType))
                    {
                        if (_hostTypeParams.TryGetValue(originalMethod, out var hostTypeParam))
                        {
                            hostTypeRef = TypeReference.FromInvocationTypeParameter(invocation, hostTypeParam);
                        }
                        else
                        {
                            hostTypeRef = new(_avaloniaObjectType, Location.None);
                        }
                    }

                    var description = propertyDescriptions.GetOrAdd(target, s => new AvaloniaPropertyDescription(name, propertyType, valueTypeRef.Type));
                    description.Name = name;
                    description.HostType = hostTypeRef;
                    description.Inherits = inherits;
                    description.AddAssignment(target, ownerTypeRef);
                    description.AddOwner(ownerTypeRef);
                }
                else if (_avaloniaPropertyAddOwnerMethods.Contains(invocation.TargetMethod.OriginalDefinition)) // This is a call to one of the AddOwner methods
                {
                    if (!_ownerTypeParams.TryGetValue(invocation.TargetMethod.OriginalDefinition, out var ownerTypeParam))
                    {
                        return;
                    }

                    if (GetReferencedFieldOrProperty(invocation.Instance) is not { } sourceSymbol)
                    {
                        return;
                    }

                    var description = propertyDescriptions.GetOrAdd(sourceSymbol, s =>
                    {
                        string inferredName = s.Name;

                        var match = Regex.Match(s.Name, "(?<name>.*)Property$");
                        if (match.Success)
                        {
                            inferredName = match.Groups["name"].Value;
                        }

                        if (!_propertyValueTypeParams.TryGetValue(propertyType.OriginalDefinition, out var propertyValueType))
                        {
                            throw new InvalidOperationException($"{propertyType} is not a recognised AvaloniaProperty ({_styledPropertyType}, {_attachedPropertyType}, {_directPropertyType}).");
                        }
                        
                        var valueType = propertyType.TypeArguments[propertyValueType.Ordinal];

                        TypeReference? hostTypeRef = null;
                        if (SymbolEquals(propertyType.OriginalDefinition, _attachedPropertyType))
                        {
                            hostTypeRef = new(_avaloniaObjectType, Location.None); // assume that an attached property applies everywhere until we find its registration
                        }

                        return new AvaloniaPropertyDescription(inferredName, propertyType, valueType) { HostType = hostTypeRef };
                    });

                    var ownerTypeRef = TypeReference.FromInvocationTypeParameter(invocation, ownerTypeParam);
                    description.AddAssignment(target, ownerTypeRef);
                    description.AddOwner(ownerTypeRef);
                }
            }
            catch (Exception ex)
            {
                throw new AvaloniaAnalysisException($"Failed to register the initializer of '{target}'.", ex);
            }
        }

        /// <seealso cref="InappropriatePropertyAssignment"/>
        private void AnalyzeFieldInitializer(OperationAnalysisContext context)
        {
            var operation = (IFieldInitializerOperation)context.Operation;

            foreach (var field in operation.InitializedFields)
            {
                try
                {
                    if (!_avaloniaPropertyDescriptions.TryGetValue(field, out var description))
                    {
                        continue;
                    }

                    if (!IsValidAvaloniaPropertyStorage(field))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(InappropriatePropertyAssignment, field.Locations[0], field));
                    }

                    AnalyzeInitializer_Shared(context, field, description);

                }
                catch (Exception ex)
                {
                    throw new AvaloniaAnalysisException($"Failed to process initialization of field '{field}'.", ex);
                }
            }
        }

        /// <seealso cref="InappropriatePropertyAssignment"/>
        private void AnalyzePropertyInitializer(OperationAnalysisContext context)
        {
            var operation = (IPropertyInitializerOperation)context.Operation;

            foreach (var property in operation.InitializedProperties)
            {
                try
                {
                    if (!_avaloniaPropertyDescriptions.TryGetValue(property, out var description))
                    {
                        continue;
                    }

                    if (!IsValidAvaloniaPropertyStorage(property))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(InappropriatePropertyAssignment, property.Locations[0], property));
                    }

                    AnalyzeInitializer_Shared(context, property, description);
                }
                catch (Exception ex)
                {
                    throw new AvaloniaAnalysisException($"Failed to process initialization of property '{property}'.", ex);
                }
            }
        }

        /// <seealso cref="InappropriatePropertyAssignment"/>
        private void AnalyzeAssignment(OperationAnalysisContext context)
        {
            var operation = (IAssignmentOperation)context.Operation;

            try
            {
                var (target, isValid) = ResolveOperationSource(operation.Target) switch
                {
                    IFieldReferenceOperation fieldRef => (fieldRef.Field, IsValidAvaloniaPropertyStorage(fieldRef.Field)),
                    IPropertyReferenceOperation propertyRef => (propertyRef.Property, IsValidAvaloniaPropertyStorage(propertyRef.Property)),
                    _ => (default(ISymbol), false),
                };

                if (target == null || !_avaloniaPropertyDescriptions.TryGetValue(target, out var description))
                {
                    return;
                }

                if (!isValid)
                {
                    context.ReportDiagnostic(Diagnostic.Create(InappropriatePropertyAssignment, target.Locations[0], target));
                }

                AnalyzeInitializer_Shared(context, target, description);
            }
            catch (Exception ex)
            {
                throw new AvaloniaAnalysisException($"Failed to process assignment '{operation}'.", ex);
            }
        }

        /// <seealso cref="PropertyNameMismatch"/>
        /// <seealso cref="OwnerDoesNotMatchOuterType"/>
        private void AnalyzeInitializer_Shared(OperationAnalysisContext context, ISymbol assignmentSymbol, AvaloniaPropertyDescription description)
        {
            if (!assignmentSymbol.Name.Contains(description.Name) && assignmentSymbol.DeclaredAccessibility != Accessibility.Private)
            {
                context.ReportDiagnostic(Diagnostic.Create(PropertyNameMismatch, assignmentSymbol.Locations[0],
                    description.Name, assignmentSymbol));
            }

            try
            {
                var ownerType = description.AssignedTo[assignmentSymbol];

                if (!IsAvaloniaPropertyType(description.PropertyType, _attachedPropertyType) && !SymbolEquals(ownerType.Type, assignmentSymbol.ContainingType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(OwnerDoesNotMatchOuterType, ownerType.Location, ownerType.Type));
                }
            }
            catch (KeyNotFoundException)
            {
                throw new KeyNotFoundException($"Assignment operation for {assignmentSymbol} was not recorded.");
            }
        }

        /// <seealso cref="UnexpectedPropertyAccess"/>
        /// <seealso cref="InappropriatePropertyRegistration"/>
        private void AnalyzeMethodInvocation(OperationAnalysisContext context)
        {
            var invocation = (IInvocationOperation)context.Operation;

            var originalMethod = invocation.TargetMethod.OriginalDefinition;

            if (_allGetSetMethods.Contains(originalMethod))
            {
                var avaloniaPropertyOperation = invocation.Arguments[0].Value;

                var propertyStorageSymbol = GetReferencedFieldOrProperty(ResolveOperationSource(avaloniaPropertyOperation));

                if (propertyStorageSymbol == null || !_avaloniaPropertyDescriptions.TryGetValue(propertyStorageSymbol, out var propertyDescription))
                {
                    return;
                }

                TypeReference ownerOrHostType;
                if (SymbolEquals(propertyDescription.PropertyType.OriginalDefinition, _attachedPropertyType))
                {
                    ownerOrHostType = propertyDescription.HostType!.Value;
                }
                else if (!propertyDescription.AssignedTo.TryGetValue(propertyStorageSymbol, out ownerOrHostType))
                {
                    return;
                }

                if (invocation.Instance is IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance } &&
                    !DerivesFrom(context.ContainingSymbol.ContainingType, ownerOrHostType.Type))
                {
                    context.ReportDiagnostic(Diagnostic.Create(UnexpectedPropertyAccess, invocation.Arguments[0].Syntax.GetLocation(),
                        GetReferencedFieldOrProperty(avaloniaPropertyOperation), context.ContainingSymbol.ContainingType));
                }
            }
            else if (!IsStaticConstructorOrInitializer() && _allAvaloniaPropertyMethods.Contains(originalMethod))
            {
                context.ReportDiagnostic(Diagnostic.Create(InappropriatePropertyRegistration, invocation.Syntax.GetLocation(),
                    originalMethod.ToDisplayString(TypeQualifiedName)));
            }

            bool IsStaticConstructorOrInitializer() =>
                context.ContainingSymbol is IMethodSymbol { MethodKind: MethodKind.StaticConstructor } ||
                ResolveOperationTarget(invocation.Parent!) switch
                {
                    IFieldInitializerOperation fieldInit when fieldInit.InitializedFields.All(f => f.IsStatic) => true,
                    IPropertyInitializerOperation propInit when propInit.InitializedProperties.All(p => p.IsStatic) => true,
                    _ => false,
                };
        }

        /// <seealso cref="AmbiguousPropertyName"/>
        /// <seealso cref="PropertyTypeMismatch"/>
        /// <seealso cref="AssociatedAvaloniaProperty"/>
        /// <seealso cref="InconsistentAccessibility"/>
        /// <seealso cref="MissingAccessor"/>
        private void StartPropertySymbolAnalysis(SymbolStartAnalysisContext context)
        {
            var property = (IPropertySymbol)context.Symbol;
            try
            {
                if (!_clrPropertyToAvaloniaProperties.TryGetValue(property, out var candidateTargetProperties))
                {
                    return; // does not refer to an AvaloniaProperty
                }

                context.RegisterSymbolEndAction(context =>
                {
                    if (candidateTargetProperties.Length > 1)
                    {
                        var candidateSymbols = candidateTargetProperties.Select(d => d.ClosestAssignmentFor(property.ContainingType)).Where(s => s != null);
                        context.ReportDiagnostic(Diagnostic.Create(AmbiguousPropertyName, property.Locations[0], candidateSymbols.SelectMany(s => s!.Locations),
                            property.ContainingType, property.Name, $"\n\t{string.Join("\n\t", candidateSymbols)}"));
                        return;
                    }

                    var avaloniaPropertyDescription = candidateTargetProperties[0];
                    var avaloniaPropertyStorage = avaloniaPropertyDescription.ClosestAssignmentFor(property.ContainingType);

                    if (avaloniaPropertyStorage == null)
                    {
                        return;
                    }

                    context.ReportDiagnostic(Diagnostic.Create(AssociatedAvaloniaProperty, property.Locations[0], new[] { avaloniaPropertyStorage.Locations[0] },
                        avaloniaPropertyDescription.PropertyType.Name, avaloniaPropertyStorage));

                    if (!SymbolEquals(property.Type, avaloniaPropertyDescription.ValueType, includeNullability: true))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(PropertyTypeMismatch, property.Locations[0],
                            avaloniaPropertyStorage, $"\t\n{string.Join("\t\n", avaloniaPropertyDescription.ValueType, property.Type)}"));
                    }

                    if (property.DeclaredAccessibility != avaloniaPropertyStorage.DeclaredAccessibility)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(InconsistentAccessibility, property.Locations[0], "property", avaloniaPropertyStorage));
                    }

                    VerifyAccessor(property.GetMethod, "readable", "get");

                    if (!IsAvaloniaPropertyType(avaloniaPropertyDescription.PropertyType, _directPropertyType))
                    {
                        VerifyAccessor(property.SetMethod, "writeable", "set");
                    }

                    void VerifyAccessor(IMethodSymbol? method, string verb, string methodName)
                    {
                        if (method == null)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(MissingAccessor, property.Locations[0], avaloniaPropertyStorage, verb, methodName));
                        }
                        else if (method.DeclaredAccessibility != avaloniaPropertyStorage.DeclaredAccessibility && method.DeclaredAccessibility != property.DeclaredAccessibility)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(InconsistentAccessibility, method.Locations[0], "property accessor", avaloniaPropertyStorage));
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                throw new AvaloniaAnalysisException($"Failed to analyse property '{property}'.", ex);
            }
        }

        /// <seealso cref="AccessorSideEffects"/>
        private void AnalyzePropertyMethods(CodeBlockAnalysisContext context)
        {
            if (context.OwningSymbol is not IMethodSymbol { AssociatedSymbol: IPropertySymbol property } method)
            {
                return;
            }

            try
            {
                if (!_clrPropertyToAvaloniaProperties.TryGetValue(property, out var candidateTargetProperties) ||
                    candidateTargetProperties.Length != 1) // a diagnostic about multiple candidates will have already been reported
                {
                    return;
                }

                var avaloniaPropertyDescription = candidateTargetProperties.Single();

                if (IsAvaloniaPropertyType(avaloniaPropertyDescription.PropertyType, _directPropertyType))
                {
                    return;
                }

                if (!SymbolEquals(property.Type, avaloniaPropertyDescription.ValueType))
                {
                    return; // a diagnostic about this will have already been reported, and if the cast is implicit then this message would be confusing anyway
                }

                var bodyNode = context.CodeBlock.ChildNodes().Single();

                var operation = bodyNode.DescendantNodes()
                    .Where(n => n.IsKind(SyntaxKind.InvocationExpression)) // this line is specific to C#
                    .Select(n => (IInvocationOperation)context.SemanticModel.GetOperation(n)!)
                    .FirstOrDefault();

                var isGetMethod = method.MethodKind == MethodKind.PropertyGet;

                var expectedInvocations = isGetMethod ? _getValueMethods : _setValueMethods;

                if (operation == null || bodyNode.ChildNodes().Count() != 1 || !expectedInvocations.Contains(operation.TargetMethod.OriginalDefinition))
                {
                    ReportSideEffects();
                    return;
                }

                if (operation.Arguments.Length != 0)
                {
                    switch (ResolveOperationSource(operation.Arguments[0].Value))
                    {
                        case IFieldReferenceOperation fieldRef when avaloniaPropertyDescription.AssignedTo.ContainsKey(fieldRef.Field):
                        case IPropertyReferenceOperation propertyRef when avaloniaPropertyDescription.AssignedTo.ContainsKey(propertyRef.Property):
                            break; // the argument is a reference to the correct AvaloniaProperty object
                        default:
                            ReportSideEffects(operation.Arguments[0].Value.Syntax.GetLocation());
                            return;
                    }
                }

                if (!isGetMethod &&
                    operation.Arguments.Length >= 2 &&
                    operation.Arguments[1].Value.Kind != OperationKind.ParameterReference) // passing something other than `value` to SetValue
                {
                    ReportSideEffects(operation.Arguments[1].Syntax.GetLocation());
                }

                void ReportSideEffects(Location? locationOverride = null)
                {
                    var propertySourceName = avaloniaPropertyDescription.ClosestAssignmentFor(method.ContainingType)?.Name ?? "[unknown]";

                    context.ReportDiagnostic(Diagnostic.Create(AccessorSideEffects, locationOverride ?? context.CodeBlock.GetLocation(),
                        avaloniaPropertyDescription.Name,
                        isGetMethod ? "read" : "written to",
                        isGetMethod ? "get" : "set",
                        isGetMethod ? $"GetValue({propertySourceName})" : $"SetValue({propertySourceName}, value)"));
                }
            }
            catch (Exception ex)
            {
                throw new AvaloniaAnalysisException($"Failed to process property accessor '{method}'.", ex);
            }
        }
    }
}
