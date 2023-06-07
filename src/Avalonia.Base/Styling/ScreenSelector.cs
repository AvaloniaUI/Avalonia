using System;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.Styling.Activators;

namespace Avalonia.Styling
{
    internal sealed class MinWidthMediaSelector : MediaSelector<double>
    {
        public MinWidthMediaSelector(Selector? previous, double argument) : base(previous, argument)
        {
        }

        private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
        {
            if (!(control is Visual visual))
            {
                return SelectorMatch.NeverThisType;
            }

            if (subscribe)
            {
                return new SelectorMatch(new MinWidthActivator(visual, Argument));
            }

            if (visual.VisualRoot is IMediaProviderHost mediaProviderHost && mediaProviderHost.MediaProvider is { } mediaProvider)
            {
                return Evaluate(mediaProvider, Argument);
            }

            return SelectorMatch.NeverThisInstance;
        }

        internal static SelectorMatch Evaluate(IMediaProvider screenSizeProvider, double argument)
        {
            return screenSizeProvider.GetScreenWidth() >= argument ? SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;
        }

        public override string ToString() => "min-width";

        public override string ToString(Style? owner)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class MaxWidthMediaSelector : MediaSelector<double>
    {
        public MaxWidthMediaSelector(Selector? previous, double argument) : base(previous, argument)
        {
        }

        private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
        {
            if (!(control is Visual visual))
            {
                return SelectorMatch.NeverThisType;
            }

            if (subscribe)
            {
                return new SelectorMatch(new MaxWidthActivator(visual, Argument));
            }

            if (visual.VisualRoot is IMediaProviderHost mediaProviderHost && mediaProviderHost.MediaProvider is { } mediaProvider)
            {
                return Evaluate(mediaProvider, Argument);
            }

            return SelectorMatch.NeverThisInstance;
        }

        internal static SelectorMatch Evaluate(IMediaProvider screenSizeProvider, double argument)
        {
            return screenSizeProvider.GetScreenWidth() <= argument ? SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;
        }

        public override string ToString() => "max-width";

        public override string ToString(Style? owner)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class MinHeightMediaSelector : MediaSelector<double>
    {
        public MinHeightMediaSelector(Selector? previous, double argument) : base(previous, argument)
        {
        }

        private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
        {
            if (!(control is Visual visual))
            {
                return SelectorMatch.NeverThisType;
            }

            if (subscribe)
            {
                return new SelectorMatch(new MinHeightActivator(visual, Argument));
            }

            if (visual.VisualRoot is IMediaProviderHost mediaProviderHost && mediaProviderHost.MediaProvider is { } mediaProvider)
            {
                return Evaluate(mediaProvider, Argument);
            }

            return SelectorMatch.NeverThisInstance;
        }

        internal static SelectorMatch Evaluate(IMediaProvider screenSizeProvider, double argument)
        {
            return screenSizeProvider.GetScreenHeight() >= argument ? SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;
        }

        public override string ToString() => "min-height";

        public override string ToString(Style? owner)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class MaxHeightMediaSelector : MediaSelector<double>
    {
        public MaxHeightMediaSelector(Selector? previous, double argument) : base(previous, argument)
        {
        }

        private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
        {
            if (!(control is Visual visual))
            {
                return SelectorMatch.NeverThisType;
            }

            if (subscribe)
            {
                return new SelectorMatch(new MaxHeightActivator(visual, Argument));
            }

            if (visual.VisualRoot is IMediaProviderHost mediaProviderHost && mediaProviderHost.MediaProvider is { } mediaProvider)
            {
                return Evaluate(mediaProvider, Argument);
            }

            return SelectorMatch.NeverThisInstance;
        }

        internal static SelectorMatch Evaluate(IMediaProvider screenSizeProvider, double argument)
        {
            return screenSizeProvider.GetScreenHeight() <= argument ? SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;
        }

        public override string ToString() => "max-height";

        public override string ToString(Style? owner)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class OrientationMediaSelector : MediaSelector<DeviceOrientation>
    {
        public OrientationMediaSelector(Selector? previous, DeviceOrientation argument) : base(previous, argument)
        {
        }

        private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
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

        internal static SelectorMatch Evaluate(IMediaProvider mediaProvider, DeviceOrientation argument)
        {
            return mediaProvider.GetDeviceOrientation() == argument ? SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;
        }

        public override string ToString() => "orientation";

        public override string ToString(Style? owner)
        {
            throw new NotImplementedException();
        }
    }
}
