using System;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Styling.Activators;

namespace Avalonia.Styling
{
    internal sealed class WidthQuery : ValueStyleQuery<(StyleQueryComparisonOperator @operator, double value)>
    {
        public WidthQuery(StyleQuery? previous, StyleQueryComparisonOperator @operator, double value) : base(previous, (@operator, value))
        {
        }

        internal override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe, string? containerName = null)
        {
            if (control is not Visual visual)
            {
                return SelectorMatch.NeverThisType;
            }

            if (subscribe)
            {
                return new SelectorMatch(new WidthActivator(visual, Argument, containerName));
            }

            if (ContainerQueryActivatorBase.GetContainer(visual, containerName) is { } container
                && container is Layoutable layoutable
                && Container.GetQueryProvider(layoutable) is { } queryProvider
                && Container.GetSizing(layoutable) == Styling.ContainerSizing.WidthAndHeight)
            {
                return Evaluate(queryProvider, Argument);
            }

            return SelectorMatch.NeverThisInstance;
        }

        internal static SelectorMatch Evaluate(VisualQueryProvider queryProvider, (StyleQueryComparisonOperator @operator, double value) argument)
        {
            var width = queryProvider.Width;
            if (double.IsNaN(width))
            {
                return SelectorMatch.NeverThisInstance;
            }

            bool IsTrue(StyleQueryComparisonOperator comparisonOperator, double value)
            {
                switch (comparisonOperator)
                {
                    case StyleQueryComparisonOperator.None:
                        return true;
                    case StyleQueryComparisonOperator.Equals:
                        return width == value;
                    case StyleQueryComparisonOperator.LessThan:
                        return width < value;
                    case StyleQueryComparisonOperator.GreaterThan:
                        return width > value;
                    case StyleQueryComparisonOperator.LessThanOrEquals:
                        return width <= value;
                    case StyleQueryComparisonOperator.GreaterThanOrEquals:
                        return width >= value;
                }

                return false;
            }

            return IsTrue(argument.@operator, argument.value) ?
                SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;
        }

        public override string ToString() => ToString(null);

        public override string ToString(ContainerQuery? owner)
        {
            var prop = Argument.@operator switch
            {
                StyleQueryComparisonOperator.None => "",
                StyleQueryComparisonOperator.Equals => "width",
                StyleQueryComparisonOperator.LessThan => "",
                StyleQueryComparisonOperator.GreaterThan => "",
                StyleQueryComparisonOperator.LessThanOrEquals => "max-width",
                StyleQueryComparisonOperator.GreaterThanOrEquals => "min-width",
                _ => throw new NotImplementedException(),
            };

            return $"{prop}:{Argument.value}";
        }
    }

    internal sealed class HeightQuery : ValueStyleQuery<(StyleQueryComparisonOperator @operator, double value)>
    {
        public HeightQuery(StyleQuery? previous, StyleQueryComparisonOperator @operator, double value) : base(previous, (@operator, value))
        {
        }

        internal override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe, string? containerName = null)
        {
            if (control is not Visual visual)
            {
                return SelectorMatch.NeverThisType;
            }

            if (subscribe)
            {
                return new SelectorMatch(new HeightActivator(visual, Argument, containerName));
            }

            if (ContainerQueryActivatorBase.GetContainer(visual, containerName) is { } container
                && container is Layoutable layoutable
                && Container.GetQueryProvider(layoutable) is { } queryProvider
                && Container.GetSizing(layoutable) == Styling.ContainerSizing.WidthAndHeight)
            {
                return Evaluate(queryProvider, Argument);
            }

            return SelectorMatch.NeverThisInstance;
        }

        internal static SelectorMatch Evaluate(VisualQueryProvider queryProvider, (StyleQueryComparisonOperator @operator, double value) argument)
        {
            var height = queryProvider.Height;
            if (double.IsNaN(height))
            {
                return SelectorMatch.NeverThisInstance;
            }

            bool IsTrue(StyleQueryComparisonOperator comparisonOperator, double value)
            {
                switch (comparisonOperator)
                {
                    case StyleQueryComparisonOperator.None:
                        return true;
                    case StyleQueryComparisonOperator.Equals:
                        return height == value;
                    case StyleQueryComparisonOperator.LessThan:
                        return height < value;
                    case StyleQueryComparisonOperator.GreaterThan:
                        return height > value;
                    case StyleQueryComparisonOperator.LessThanOrEquals:
                        return height <= value;
                    case StyleQueryComparisonOperator.GreaterThanOrEquals:
                        return height >= value;
                }

                return false;
            }

            return IsTrue(argument.@operator, argument.value) ?
                SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;
        }

        public override string ToString() => ToString(null);

        public override string ToString(ContainerQuery? owner)
        {
            var prop = Argument.@operator switch
            {
                StyleQueryComparisonOperator.None => "",
                StyleQueryComparisonOperator.Equals => "height",
                StyleQueryComparisonOperator.LessThan => "",
                StyleQueryComparisonOperator.GreaterThan => "",
                StyleQueryComparisonOperator.LessThanOrEquals => "max-height",
                StyleQueryComparisonOperator.GreaterThanOrEquals => "min-height",
                _ => throw new NotImplementedException(),
            };

            return $"{prop}:{Argument.value}";
        }
    }
}
