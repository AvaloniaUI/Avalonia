using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Data.Core;
using Avalonia.Markup.Parsers;

namespace Avalonia.Data
{
    /// <summary>
    /// A XAML binding.
    /// </summary>
    [RequiresUnreferencedCode(TrimmingMessages.ReflectionBindingRequiresUnreferencedCodeMessage)]
    public class Binding : BindingBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Binding"/> class.
        /// </summary>
        public Binding()
            :base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Binding"/> class.
        /// </summary>
        /// <param name="path">The binding path.</param>
        /// <param name="mode">The binding mode.</param>
        public Binding(string path, BindingMode mode = BindingMode.Default)
            : base(mode)
        {
            Path = path;
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
        public object? Source { get; set; }

        /// <summary>
        /// Gets or sets the binding path.
        /// </summary>
        public string Path { get; set; } = "";

        /// <summary>
        /// Gets or sets a function used to resolve types from names in the binding path.
        /// </summary>
        public Func<string, string, Type>? TypeResolver { get; set; }

        protected override ExpressionObserver CreateExpressionObserver(AvaloniaObject target, AvaloniaProperty? targetProperty, object? anchor, bool enableDataValidation)
        {
            _ = target ?? throw new ArgumentNullException(nameof(target));

            anchor ??= DefaultAnchor?.Target;
            enableDataValidation = enableDataValidation && Priority == BindingPriority.LocalValue;

            INameScope? nameScope = null;
            NameScope?.TryGetTarget(out nameScope);

            var (node, mode) = ExpressionObserverBuilder.Parse(Path, enableDataValidation, TypeResolver, nameScope);

            if (node is null)
            {
                throw new InvalidOperationException("Could not parse binding expression.");
            }

            StyledElement GetSource()
            {
                return target as StyledElement ??
                    anchor as StyledElement ??
                    throw new ArgumentException("Could not find binding source: either target or anchor must be an StyledElement.");
            }

            if (ElementName != null)
            {
                return CreateElementObserver(
                    GetSource(),
                    ElementName,
                    node);
            }
            else if (Source != null)
            {
                return CreateSourceObserver(Source, node);
            }
            else if (RelativeSource == null)
            {
                if (mode == SourceMode.Data)
                {
                    return CreateDataContextObserver(
                        target,
                        node,
                        targetProperty == StyledElement.DataContextProperty,
                        anchor);
                }
                else
                {
                    return CreateSourceObserver(GetSource(), node);
                }
            }
            else if (RelativeSource.Mode == RelativeSourceMode.DataContext)
            {
                return CreateDataContextObserver(
                    target,
                    node,
                    targetProperty == StyledElement.DataContextProperty,
                    anchor);
            }
            else if (RelativeSource.Mode == RelativeSourceMode.Self)
            {
                return CreateSourceObserver(GetSource(), node);
            }
            else if (RelativeSource.Mode == RelativeSourceMode.TemplatedParent)
            {
                return CreateTemplatedParentObserver(GetSource(), node);
            }
            else if (RelativeSource.Mode == RelativeSourceMode.FindAncestor)
            {
                if (RelativeSource.Tree == TreeType.Visual && RelativeSource.AncestorType == null)
                {
                    throw new InvalidOperationException("AncestorType must be set for RelativeSourceMode.FindAncestor when searching the visual tree.");
                }

                return CreateFindAncestorObserver(GetSource(), RelativeSource, node);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
