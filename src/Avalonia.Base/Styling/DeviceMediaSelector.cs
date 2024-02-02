using System;
using Avalonia.Platform;
using Avalonia.Styling.Activators;

namespace Avalonia.Styling
{
    internal sealed class PlatformMediaQuery : MediaQuery<string>
    {
        public PlatformMediaQuery(Query? previous, string argument) : base(previous, argument)
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
                return new SelectorMatch(new PlatformActivator(visual, Argument));
            }

            if (visual.VisualRoot is IMediaProviderHost mediaProviderHost && mediaProviderHost.MediaProvider is { } mediaProvider)
            {
                return Evaluate(mediaProvider, Argument);
            }

            return SelectorMatch.NeverThisInstance;
        }

        internal static SelectorMatch Evaluate(IMediaProvider mediaProvider, string argument)
        {
            return mediaProvider.GetPlatform() == argument ? SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;
        }

        public override string ToString() => "platform";

        public override string ToString(Media? owner)
        {
            throw new NotImplementedException();
        }
    }
}
