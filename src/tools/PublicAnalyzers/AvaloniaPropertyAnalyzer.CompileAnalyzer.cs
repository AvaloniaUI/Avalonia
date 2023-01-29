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
        private readonly ConcurrentDictionary<ISymbol, AvaloniaPropertyDescription> _avaloniaProperyDescriptions = new(SymbolEqualityComparer.Default);

        private readonly ConcurrentDictionary<IPropertySymbol, ImmutableArray<AvaloniaPropertyDescription>> _clrPropertyToAvaloniaProperties = new(SymbolEqualityComparer.Default);

        private readonly INamedTypeSymbol _avaloniaObjectType;
        private readonly ImmutableHashSet<IMethodSymbol> _getValueMethods;
        private readonly ImmutableHashSet<IMethodSymbol> _setValueMethods;
        private readonly INamedTypeSymbol _avaloniaPropertyType;
        private readonly INamedTypeSymbol _styledPropertyType;
        private readonly INamedTypeSymbol _attachedPropertyType;
        private readonly INamedTypeSymbol _directPropertyType;
        private readonly ImmutableHashSet<IMethodSymbol> _avaloniaPropertyRegisterMethods;
        private readonly ImmutableHashSet<IMethodSymbol> _avaloniaPropertyAddOwnerMethods;

        public CompileAnalyzer(CompilationStartAnalysisContext context)
        {
            _avaloniaObjectType = GetTypeOrThrow("Avalonia.AvaloniaObject");
            _getValueMethods = _avaloniaObjectType.GetMembers("GetValue").OfType<IMethodSymbol>().ToImmutableHashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            _setValueMethods = _avaloniaObjectType.GetMembers("SetValue").OfType<IMethodSymbol>().ToImmutableHashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

            _avaloniaPropertyType = GetTypeOrThrow("Avalonia.AvaloniaProperty");
            _styledPropertyType = GetTypeOrThrow("Avalonia.StyledProperty`1");
            _attachedPropertyType = GetTypeOrThrow("Avalonia.AttachedProperty`1");
            _directPropertyType = GetTypeOrThrow("Avalonia.DirectProperty`2");

            _avaloniaPropertyRegisterMethods = _avaloniaPropertyType.GetMembers()
                .OfType<IMethodSymbol>().Where(m => m.Name.StartsWith("Register")).ToImmutableHashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

            _avaloniaPropertyAddOwnerMethods = new[] { _styledPropertyType, _attachedPropertyType, _directPropertyType }
                .SelectMany(t => t.GetMembers("AddOwner").OfType<IMethodSymbol>()).ToImmutableHashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

            FindAvaloniaPropertySymbols(context.Compilation, context.CancellationToken);

            context.RegisterOperationAction(AnalyzeFieldInitializer, OperationKind.FieldInitializer);
            context.RegisterOperationAction(AnalyzePropertyInitializer, OperationKind.PropertyInitializer);

            context.RegisterSymbolStartAction(StartPropertySymbolAnalysis, SymbolKind.Property);

            if (context.Compilation.Language == LanguageNames.CSharp)
            {
                context.RegisterCodeBlockAction(AnalyzePropertyMethods);
            }

            INamedTypeSymbol GetTypeOrThrow(string name) => context.Compilation.GetTypeByMetadataName(name) ?? throw new KeyNotFoundException($"Could not locate {name} in the compilation context.");
        }

        private void FindAvaloniaPropertySymbols(Compilation compilation, CancellationToken cancellationToken)
        {
            var namespaceStack = new Stack<INamespaceSymbol>();
            namespaceStack.Push(compilation.GlobalNamespace);

            var types = new List<INamedTypeSymbol>();

            while (namespaceStack.Count > 0)
            {
                var current = namespaceStack.Pop();

                foreach (var type in current.GetTypeMembers())
                {
                    if (DerivesFrom(type, _avaloniaObjectType))
                    {
                        types.Add(type);
                    }
                }

                foreach (var child in current.GetNamespaceMembers())
                {
                    namespaceStack.Push(child);
                }
            }

            var references = new ConcurrentBag<(ISymbol symbol, Func<ISymbolInitializerOperation, IEnumerable<ISymbol>> getInits)>();

            var parallelOptions = new ParallelOptions() { CancellationToken = cancellationToken };

            Parallel.ForEach(types, parallelOptions, type =>
            {
                foreach (var member in type.GetMembers())
                {
                    switch (member)
                    {
                        case IFieldSymbol fieldSymbol when IsValidAvaloniaPropertyStorage(fieldSymbol):
                            references.Add((fieldSymbol, so => ((IFieldInitializerOperation)so).InitializedFields));
                            break;
                        case IPropertySymbol propertySymbol when IsValidAvaloniaPropertyStorage(propertySymbol):
                            references.Add((propertySymbol, so => ((IPropertyInitializerOperation)so).InitializedProperties));
                            break;
                    }
                }
            });

            // key initializes value
            var fieldInitializations = new ConcurrentDictionary<ISymbol, ISymbol>(SymbolEqualityComparer.Default);

            Parallel.ForEach(references, parallelOptions, tuple =>
            {
                foreach (var syntaxRef in tuple.symbol.DeclaringSyntaxReferences)
                {
                    var node = syntaxRef.GetSyntax(cancellationToken);
                    if (!compilation.ContainsSyntaxTree(node.SyntaxTree))
                    {
                        continue;
                    }

                    var model = compilation.GetSemanticModel(node.SyntaxTree);
                    var operation = node.ChildNodes().Select(n => model.GetOperation(n, cancellationToken)).OfType<ISymbolInitializerOperation>().FirstOrDefault();

                    if (operation == null)
                    {
                        return;
                    }

                    var operationValue = operation.Value;

                    while (operationValue is IConversionOperation conversion)
                    {
                        operationValue = conversion.Operand;
                    }

                    switch (operationValue)
                    {
                        case IInvocationOperation invocation:
                            RegisterInitializer_Invocation(tuple.getInits(operation), invocation, tuple.symbol);
                            break;
                        case IFieldReferenceOperation fieldRef when IsValidAvaloniaPropertyStorage(fieldRef.Field):
                            fieldInitializations[fieldRef.Field] = tuple.symbol;
                            break;
                        case IPropertyReferenceOperation propRef when IsValidAvaloniaPropertyStorage(propRef.Property):
                            fieldInitializations[propRef.Property] = tuple.symbol;
                            break;
                    }
                }
            });

            // we have recorded every Register and AddOwner call. Now follow assignment chains.
            foreach (var root in fieldInitializations.Keys.Intersect(_avaloniaProperyDescriptions.Keys, SymbolEqualityComparer.Default).ToArray())
            {
                var propertyDescription = _avaloniaProperyDescriptions[root];
                var owner = propertyDescription.AssignedTo[root];

                var current = root;
                do
                {
                    var target = fieldInitializations[current];

                    propertyDescription.AssignedTo[target] = owner; // This loop handles simple assignment operations, so do NOT change the owner
                    _avaloniaProperyDescriptions[target] = propertyDescription;

                    fieldInitializations.TryGetValue(target, out current);
                }
                while(current != null);
            }
        }

        private void RegisterInitializer_Invocation(IEnumerable<ISymbol> initializedSymbols, IInvocationOperation invocation, ISymbol target)
        {
            try
            {
                if (invocation.TargetMethod.ReturnType is not INamedTypeSymbol propertyType)
                {
                    return;
                }

                if (_avaloniaPropertyRegisterMethods.Contains(invocation.TargetMethod.OriginalDefinition)) // This is a call to one of the AvaloniaProperty.Register* methods
                {
                    if (!invocation.TargetMethod.IsGenericMethod)
                    {
                        return;
                    }

                    var typeParamLookup = invocation.TargetMethod.TypeParameters.Select((s, i) => (param: s, index: i))
                        .ToDictionary(t => t.param.Name, t => (INamedTypeSymbol)invocation.TargetMethod.TypeArguments[t.index]);

                    if (!typeParamLookup.TryGetValue("TOwner", out var ownerType) && // if it's NOT a generic parameter, try to work out the runtime value
                        invocation.TargetMethod.Parameters.FirstOrDefault(p => p.Name == "ownerType") is INamedTypeSymbol ownerTypeParam &&
                        invocation.Arguments.FirstOrDefault(a => SymbolEquals(a.Parameter, ownerTypeParam)) is IArgumentOperation argument)
                    {
                        switch (argument.Value)
                        {
                            case ITypeOfOperation typeOf:
                                ownerType = (INamedTypeSymbol)typeOf.Type!;
                                break;
                        }
                    }

                    if (ownerType == null || !typeParamLookup.TryGetValue("TValue", out var propertyValueType))
                    {
                        return;
                    }

                    foreach (var symbol in initializedSymbols)
                    {
                        string name;
                        switch (invocation.Arguments[0].Value)
                        {
                            case INameOfOperation nameof when nameof.Argument is IPropertyReferenceOperation propertyReference:
                                name = propertyReference.Property.Name;
                                break;
                            case IAssignmentOperation assignment when assignment.ConstantValue is { HasValue: true } stringLiteral:
                                name = (string)stringLiteral.Value!;
                                break;
                            default:
                                return;
                        }

                        var description = _avaloniaProperyDescriptions.GetOrAdd(symbol, s => new AvaloniaPropertyDescription(name, propertyType, propertyValueType));
                        description.Name = name;
                        description.AssignedTo[symbol] = ownerType;
                        description.OwnerTypes.Add(ownerType);
                    }
                }
                else if (_avaloniaPropertyAddOwnerMethods.Contains(invocation.TargetMethod.OriginalDefinition)) // This is a call to one of the AddOwner methods
                {
                    if (invocation.TargetMethod.TypeArguments[0] is not INamedTypeSymbol ownerType)
                    {
                        return;
                    }

                    ISymbol sourceSymbol;
                    switch (invocation.Instance)
                    {
                        case IFieldReferenceOperation fieldReference:
                            sourceSymbol = fieldReference.Field;
                            break;
                        case IPropertyReferenceOperation propertyReference:
                            sourceSymbol = propertyReference.Property;
                            break;
                        default:
                            return;
                    }

                    var propertyValueType = AvaloniaPropertyType_GetValueType(propertyType);

                    foreach (var symbol in initializedSymbols)
                    {
                        var description = _avaloniaProperyDescriptions.GetOrAdd(symbol, s =>
                        {
                            string inferredName = target.Name;

                            var match = Regex.Match(target.Name, "(?<name>.*)Property$");
                            if (match.Success)
                            {
                                inferredName = match.Groups["name"].Value;
                            }
                            return new AvaloniaPropertyDescription(inferredName, (INamedTypeSymbol)invocation.TargetMethod.ReturnType, propertyValueType);
                        });

                        description.AssignedTo[symbol] = ownerType;
                        description.OwnerTypes.Add(ownerType);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new AvaloniaAnalysisException($"Failed to register the initializer of '{target}'.", ex);
            }
        }

        private void AnalyzeFieldInitializer(OperationAnalysisContext context)
        {
            var operation = (IFieldInitializerOperation)context.Operation;

            foreach (var field in operation.InitializedFields)
            {
                try
                {
                    if (!_avaloniaProperyDescriptions.TryGetValue(field, out var description))
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

        private void AnalyzePropertyInitializer(OperationAnalysisContext context)
        {
            var operation = (IPropertyInitializerOperation)context.Operation;

            foreach (var property in operation.InitializedProperties)
            {
                try
                {
                    if (!_avaloniaProperyDescriptions.TryGetValue(property, out var description))
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

        private void AnalyzeInitializer_Shared(OperationAnalysisContext context, ISymbol assignmentSymbol, AvaloniaPropertyDescription description)
        {
            if (!assignmentSymbol.Name.Contains(description.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(PropertyNameMismatch, assignmentSymbol.Locations[0],
                    description.Name, assignmentSymbol));
            }

            try
            {
                var ownerType = description.AssignedTo[assignmentSymbol];

                if (!IsAvaloniaPropertyType(description.PropertyType, _attachedPropertyType) && !SymbolEquals(ownerType, assignmentSymbol.ContainingType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(OwnerDoesNotMatchOuterType, assignmentSymbol.Locations[0], ownerType));
                }
            }
            catch (KeyNotFoundException)
            {
                return; // WIP
                throw new KeyNotFoundException($"Assignment operation for {assignmentSymbol} was not recorded.");
            }
        }

        private void StartPropertySymbolAnalysis(SymbolStartAnalysisContext context)
        {
            var property = (IPropertySymbol)context.Symbol;
            try
            {
                var avaloniaPropertyDescriptions = GetAvaloniaPropertiesForType(property.ContainingType).ToLookup(d => d.Name);

                var candidateTargetProperties = avaloniaPropertyDescriptions[property.Name].ToImmutableArray();

                switch (candidateTargetProperties.Length)
                {
                    case 0:
                        return; // does not refer to an AvaloniaProperty
                    case 1:
                        candidateTargetProperties[0].PropertyWrappers.Add(property);
                        break;
                }

                _clrPropertyToAvaloniaProperties[property] = candidateTargetProperties;

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
                    var argumentValue = operation.Arguments[0].Value;
                    if (argumentValue is IConversionOperation conversion)
                    {
                        argumentValue = conversion.Operand;
                    }

                    switch (argumentValue)
                    {
                        case IFieldReferenceOperation fieldRef when avaloniaPropertyDescription.AssignedTo.ContainsKey(fieldRef.Field):
                        case IPropertyReferenceOperation propertyRef when avaloniaPropertyDescription.AssignedTo.ContainsKey(propertyRef.Property):
                            break; // the argument is a reference to the correct AvaloniaProperty object
                        default:
                            ReportSideEffects(argumentValue.Syntax.GetLocation());
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

        private INamedTypeSymbol AvaloniaPropertyType_GetValueType(INamedTypeSymbol type)
        {
            var compareType = type.IsGenericType ? type.ConstructUnboundGenericType().OriginalDefinition : type;

            if (SymbolEquals(compareType, _styledPropertyType) || SymbolEquals(compareType, _attachedPropertyType))
            {
                return (INamedTypeSymbol)type.TypeArguments[0];
            }
            else if (SymbolEquals(compareType, _directPropertyType))
            {
                return (INamedTypeSymbol)type.TypeArguments[1];
            }

            throw new ArgumentException($"{type} is not a recognised AvaloniaProperty ({_styledPropertyType}, {_attachedPropertyType}, {_directPropertyType}).", nameof(type));
        }

        private ImmutableHashSet<AvaloniaPropertyDescription> GetAvaloniaPropertiesForType(ITypeSymbol type)
        {
            var properties = new List<AvaloniaPropertyDescription>();

            var current = type;
            while (current != null)
            {
                properties.AddRange(current.GetMembers().Intersect(_avaloniaProperyDescriptions.Keys, SymbolEqualityComparer.Default).Select(s => _avaloniaProperyDescriptions[s]));
                current = current.BaseType;
            }

            return properties.ToImmutableHashSet();
        }
    }
}
