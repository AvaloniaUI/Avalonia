using System;

#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// Defines a style.
    /// </summary>
    public class Style : StyleBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Style"/> class.
        /// </summary>
        public Style()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Style"/> class.
        /// </summary>
        /// <param name="selector">The style selector.</param>
        public Style(Func<Selector?, Selector> selector)
        {
            Selector = selector(null);
        }

        /// <summary>
        /// Gets or sets the style's selector.
        /// </summary>
        public Selector? Selector { get; set; }

        /// <summary>
        /// Returns a string representation of the style.
        /// </summary>
        /// <returns>A string representation of the style.</returns>
        public override string ToString()
        {
            if (Selector != null)
            {
                return "Style: " + Selector.ToString();
            }
            else
            {
                return "Style";
            }
        }

        public override SelectorMatchResult TryAttach(IStyleable target, object? host)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));

            var match = Selector is object ? Selector.Match(target) :
                target == host ? SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;

            if (match.IsMatch && (SettersCore is object || AnimationsCore is object))
            {
                var instance = new StyleInstance(this, target, SettersCore, AnimationsCore, match.Activator);
                target.StyleApplied(instance);
                instance.Start();
            }

            return match.Result;
        }
    }
}
