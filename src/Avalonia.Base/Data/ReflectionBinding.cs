using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Data.Core;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Data.Core.Parsers;
using Avalonia.Utilities;

namespace Avalonia.Data
{
    /// <summary>
    /// A binding that uses reflection to access members.
    /// </summary>
    [RequiresUnreferencedCode(TrimmingMessages.ReflectionBindingRequiresUnreferencedCodeMessage)]
    public class ReflectionBinding : StandardBindingBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionBinding"/> class.
        /// </summary>
        public ReflectionBinding()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionBinding"/> class.
        /// </summary>
        /// <param name="path">The binding path.</param>
        public ReflectionBinding(string path)
        {
            Path = path;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionBinding"/> class.
        /// </summary>
        /// <param name="path">The binding path.</param>
        /// <param name="mode">The binding mode.</param>
        public ReflectionBinding(string path, BindingMode mode)
        {
            Path = path;
            Mode = mode;
        }

        /// <summary>
        /// Gets or sets the name of the element to use as the binding source.
        /// </summary>
        public string? ElementName { get; set; }

        /// <summary>
        /// Gets or sets the relative source for the binding.
        /// </summary>
        public RelativeSource? RelativeSource { get; set; }

        /// <summary>
        /// Gets or sets the source for the binding.
        /// </summary>
        public object? Source { get; set; } = AvaloniaProperty.UnsetValue;

        /// <summary>
        /// Gets or sets the binding path.
        /// </summary>
        public string Path { get; set; } = "";

        /// <summary>
        /// Gets or sets a function used to resolve types from names in the binding path.
        /// </summary>
        public Func<string?, string, Type>? TypeResolver { get; set; }

        internal override BindingExpressionBase Instance(
            AvaloniaObject target,
            AvaloniaProperty? targetProperty,
            object? anchor)
        {
            List<ExpressionNode>? nodes = null;
            var isRooted = false;
            var enableDataValidation = targetProperty?.GetMetadata(target).EnableDataValidation ?? false;

            // Build the expression nodes from the binding path.
            if (!string.IsNullOrEmpty(Path))
            {
                var reader = new CharacterReader(Path.AsSpan());
                var (astPool, sourceMode) = BindingExpressionGrammar.ParseToPooledList(ref reader);
                nodes = ExpressionNodeFactory.CreateFromAst(
                    astPool,
                    TypeResolver,
                    GetNameScope(),
                    out isRooted);
            }

            // If the binding isn't rooted (i.e. doesn't have a Source or start with $parent, $self,
            // #elementName etc.) then we need to add a source node. The type of source node will
            // depend on the ElementName and RelativeSource properties of the binding and if
            // neither of those are set will default to a data context node.
            if (Source == AvaloniaProperty.UnsetValue && !isRooted && CreateSourceNode(targetProperty) is { } sourceNode)
            {
                nodes ??= new();
                nodes.Insert(0, sourceNode);
            }

            // If the first node is an ISourceNode then allow it to select the source; otherwise
            // use the binding source if specified, falling back to the target.
            var source = nodes?.Count > 0 && nodes[0] is SourceNode sn ?
                sn.SelectSource(Source, target, anchor ?? DefaultAnchor?.Target) :
                Source != AvaloniaProperty.UnsetValue ? Source : target;

            var (mode, trigger) = ResolveDefaultsFromMetadata(target, targetProperty);

            return new BindingExpression(
                source,
                nodes,
                FallbackValue,
                delay: TimeSpan.FromMilliseconds(Delay),
                converter: Converter,
                converterCulture: ConverterCulture,
                converterParameter: ConverterParameter,
                enableDataValidation: enableDataValidation,
                mode: mode,
                priority: Priority,
                stringFormat: StringFormat,
                targetProperty: targetProperty,
                targetNullValue: TargetNullValue,
                targetTypeConverter: TargetTypeConverter.GetReflectionConverter(),
                updateSourceTrigger: trigger);
        }

        private INameScope? GetNameScope()
        {
            INameScope? result = null;
            NameScope?.TryGetTarget(out result);
            return result;
        }

        private ExpressionNode? CreateSourceNode(AvaloniaProperty? targetProperty)
        {
            if (!string.IsNullOrEmpty(ElementName))
            {
                var nameScope = GetNameScope() ?? throw new InvalidOperationException(
                    "Cannot create ElementName binding when NameScope is null");
                return new NamedElementNode(nameScope, ElementName);
            }

            if (RelativeSource is not null)
                return ExpressionNodeFactory.CreateRelativeSource(RelativeSource);

            return ExpressionNodeFactory.CreateDataContext(targetProperty);
        }
    }
}
