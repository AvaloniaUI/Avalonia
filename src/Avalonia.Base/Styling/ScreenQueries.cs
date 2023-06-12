using System;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.Styling.Activators;

namespace Avalonia.Styling
{
    internal sealed class MinWidthMediaQuery : MediaQuery<double>
    {
        public MinWidthMediaQuery(Query? previous, double argument) : base(previous, argument)
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

        public override string ToString(Media? owner)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class MaxWidthMediaQuery : MediaQuery<double>
    {
        public MaxWidthMediaQuery(Query? previous, double argument) : base(previous, argument)
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

        public override string ToString(Media? owner)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class MinHeightMediaQuery : MediaQuery<double>
    {
        public MinHeightMediaQuery(Query? previous, double argument) : base(previous, argument)
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

        public override string ToString(Media? owner)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class MaxHeightMediaQuery : MediaQuery<double>
    {
        public MaxHeightMediaQuery(Query? previous, double argument) : base(previous, argument)
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

        public override string ToString(Media? owner)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class OrientationMediaQuery : MediaQuery<DeviceOrientation>
    {
        public OrientationMediaQuery(Query? previous, DeviceOrientation argument) : base(previous, argument)
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

        internal static SelectorMatch Evaluate(IMediaProvider mediaProvider, DeviceOrientation argument)
        {
            return mediaProvider.GetDeviceOrientation() == argument ? SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;
        }

        public override string ToString() => "orientation";

        public override string ToString(Media? owner)
        {
            throw new NotImplementedException();
        }
    }
}
