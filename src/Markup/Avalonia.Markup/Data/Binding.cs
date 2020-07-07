using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.LogicalTree;
using Avalonia.Markup.Parsers;
using Avalonia.Reactive;
using Avalonia.VisualTree;

namespace Avalonia.Data
{
    /// <summary>
    /// A XAML binding.
    /// </summary>
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
        public string ElementName { get; set; }

        /// <summary>
        /// Gets or sets the relative source for the binding.
        /// </summary>
        public RelativeSource RelativeSource { get; set; }

        /// <summary>
        /// Gets or sets the source for the binding.
        /// </summary>
        public object Source { get; set; }

        /// <summary>
        /// Gets or sets the binding path.
        /// </summary>
        public string Path { get; set; } = "";

        /// <summary>
        /// Gets or sets a function used to resolve types from names in the binding path.
        /// </summary>
        public Func<string, string, Type> TypeResolver { get; set; }

        protected override ExpressionObserver CreateExpressionObserver(IAvaloniaObject target, AvaloniaProperty targetProperty, object anchor, bool enableDataValidation)
        {
            Contract.Requires<ArgumentNullException>(target != null);
            anchor = anchor ?? DefaultAnchor?.Target;
            
            enableDataValidation = enableDataValidation && Priority == BindingPriority.LocalValue;

            INameScope nameScope = null;
            NameScope?.TryGetTarget(out nameScope);

            var (node, mode) = ExpressionObserverBuilder.Parse(Path, enableDataValidation, TypeResolver, nameScope);

            if (ElementName != null)
            {
                return CreateElementObserver(
                    (target as IStyledElement) ?? (anchor as IStyledElement),
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
                    return CreateSourceObserver(
                        (target as IStyledElement) ?? (anchor as IStyledElement),
                        node);
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
                return CreateSourceObserver(
                    (target as IStyledElement) ?? (anchor as IStyledElement),
                    node);
            }
            else if (RelativeSource.Mode == RelativeSourceMode.TemplatedParent)
            {
                return CreateTemplatedParentObserver(
                    (target as IStyledElement) ?? (anchor as IStyledElement),
                    node);
            }
            else if (RelativeSource.Mode == RelativeSourceMode.FindAncestor)
            {
                if (RelativeSource.Tree == TreeType.Visual && RelativeSource.AncestorType == null)
                {
                    throw new InvalidOperationException("AncestorType must be set for RelativeSourceMode.FindAncestor when searching the visual tree.");
                }

                return CreateFindAncestorObserver(
                    (target as IStyledElement) ?? (anchor as IStyledElement),
                    RelativeSource,
                    node);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
