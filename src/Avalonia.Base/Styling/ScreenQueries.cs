using System;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.Styling.Activators;

namespace Avalonia.Styling
{

    internal sealed class OrientationMediaQuery : MediaQuery<MediaOrientation>
    {
        public OrientationMediaQuery(Query? previous, MediaOrientation argument) : base(previous, argument)
        {
        }

        internal override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
        {
            if (!(control is Visual visual))
            {
                return SelectorMatch.NeverThisType;
            }

            if (subscribe)
            {
                return new SelectorMatch(new OrientationActivator(visual, Argument));
            }

            if (visual.VisualRoot is IMediaProviderHost mediaProviderHost && mediaProviderHost.MediaProvider is { } mediaProvider)
            {
                return Evaluate(mediaProvider, Argument);
            }

            return SelectorMatch.NeverThisInstance;
        }

        internal static SelectorMatch Evaluate(IMediaProvider mediaProvider, MediaOrientation argument)
        {
            return mediaProvider.GetDeviceOrientation() == argument ? SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;
        }

        public override string ToString() => "orientation";

        public override string ToString(Media? owner)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class WidthMediaQuery : MediaQuery<(QueryComparisonOperator @operator, double value)>
    {
        public WidthMediaQuery(Query? previous, QueryComparisonOperator @operator, double value) : base(previous, (@operator, value))
        {
        }

        internal override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
        {
            if (!(control is Visual visual))
            {
                return SelectorMatch.NeverThisType;
            }

            if (subscribe)
            {
                return new SelectorMatch(new WidthActivator(visual, Argument));
            }

            if (visual.VisualRoot is IMediaProviderHost mediaProviderHost && mediaProviderHost.MediaProvider is { } mediaProvider)
            {
                return Evaluate(mediaProvider, Argument);
            }

            return SelectorMatch.NeverThisInstance;
        }

        internal static SelectorMatch Evaluate(IMediaProvider screenSizeProvider, (QueryComparisonOperator @operator, double value) argument)
        {
            var width = screenSizeProvider.GetScreenWidth();

            bool IsTrue(QueryComparisonOperator comparisonOperator, double value)
            {
                switch (comparisonOperator)
                {
                    case QueryComparisonOperator.None:
                        return true;
                    case QueryComparisonOperator.Equals:
                        return width == value;
                    case QueryComparisonOperator.LessThan:
                        return width < value;
                    case QueryComparisonOperator.GreaterThan:
                        return width > value;
                    case QueryComparisonOperator.LessThanOrEquals:
                        return width <= value;
                    case QueryComparisonOperator.GreaterThanOrEquals:
                        return width >= value;
                }

                return false;
            }

            return IsTrue(argument.@operator, argument.value)  ? 
                SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;
        }

        public override string ToString() => "width";

        public override string ToString(Media? owner)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class HeightMediaQuery : MediaQuery<(QueryComparisonOperator @operator, double value)>
    {
        public HeightMediaQuery(Query? previous, QueryComparisonOperator @operator, double value) : base(previous, (@operator, value))
        {
        }

        internal override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
        {
            if (!(control is Visual visual))
            {
                return SelectorMatch.NeverThisType;
            }

            if (subscribe)
            {
                return new SelectorMatch(new HeightActivator(visual, Argument));
            }

            if (visual.VisualRoot is IMediaProviderHost mediaProviderHost && mediaProviderHost.MediaProvider is { } mediaProvider)
            {
                return Evaluate(mediaProvider, Argument);
            }

            return SelectorMatch.NeverThisInstance;
        }

        internal static SelectorMatch Evaluate(IMediaProvider screenSizeProvider, (QueryComparisonOperator @operator, double value) argument)
        {
            var height = screenSizeProvider.GetScreenHeight();

            var isvalueTrue = IsTrue(argument.@operator, argument.value);

            bool IsTrue(QueryComparisonOperator comparisonOperator, double value)
            {
                switch (comparisonOperator)
                {
                    case QueryComparisonOperator.None:
                        return true;
                    case QueryComparisonOperator.Equals:
                        return height == value;
                    case QueryComparisonOperator.LessThan:
                        return height < value;
                    case QueryComparisonOperator.GreaterThan:
                        return height > value;
                    case QueryComparisonOperator.LessThanOrEquals:
                        return height <= value;
                    case QueryComparisonOperator.GreaterThanOrEquals:
                        return height >= value;
                }

                return false;
            }

            return IsTrue(argument.@operator, argument.value) ?
                SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;
        }

        public override string ToString() => "height";

        public override string ToString(Media? owner)
        {
            throw new NotImplementedException();
        }
    }
}
