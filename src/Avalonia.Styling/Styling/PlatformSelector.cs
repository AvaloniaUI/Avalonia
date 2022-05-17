

#nullable enable

using System;
using System.Runtime.InteropServices;
using Avalonia.Styling.Activators;
namespace Avalonia.Styling
{
    /// <summary>
    /// The `:osx()`, `:windows()`, `:linux()` style selectors.
    /// </summary>
    internal class PlatformSelector : Selector
    {
        private readonly Selector? _previous;
        private readonly Selector _argument;
        private readonly string _platform;
        private string? _selectorString;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformSelector"/> class.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <param name="argument">The selector to be not-ed.</param>
        public PlatformSelector(Selector? previous, Selector argument, string platform)
        {
            if(platform != "osx" && platform != "windows" && platform != "linux")
            {
                throw new ArgumentException("Invalid platform selector.");
            }
            
            _previous = previous;
            _argument = argument ?? throw new InvalidOperationException("Platform selector must have a selector argument.");
            _platform = platform;
        }

        /// <inheritdoc/>
        public override bool InTemplate => _argument.InTemplate;

        /// <inheritdoc/>
        public override bool IsCombinator => false;

        /// <inheritdoc/>
        public override Type? TargetType => _previous?.TargetType;

        /// <inheritdoc/>
        public override string ToString()
        {
            if (_selectorString == null)
            {
                _selectorString = $"{_previous?.ToString()}:{_platform}({_argument})";
            }

            return _selectorString;
        }

        private bool IsPlatform()
        {
            if (_platform == "osx")
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            }

            if (_platform == "windows")
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            }

            if (_platform == "linux")
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            }
            return false;
        }
        
        protected override SelectorMatch Evaluate(IStyleable control, bool subscribe)
        {
            if (!IsPlatform())
                return SelectorMatch.NeverThisInstance;
            
            return _argument.Match(control, subscribe);
        }

        protected override Selector? MovePrevious() => _previous;
    }
}
