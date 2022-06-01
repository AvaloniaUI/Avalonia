using System;

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

        internal override bool HasSelector => Selector is not null;

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

        internal override SelectorMatch Match(IStyleable control, object? host, bool subscribe)
        {
            return Selector?.Match(control, Parent, subscribe) ??
                (control == host ?
                    SelectorMatch.AlwaysThisInstance :
                    SelectorMatch.NeverThisInstance);
        }

        internal override void SetParent(StyleBase? parent)
        {
            if (parent is Style parentStyle && parentStyle.Selector is not null)
            {
                if (Selector is null)
                    throw new InvalidOperationException("Child styles must have a selector.");
                if (!Selector.HasValidNestingSelector())
                    throw new InvalidOperationException("Child styles must have a nesting selector.");
            }

            base.SetParent(parent);
        }
    }
}
