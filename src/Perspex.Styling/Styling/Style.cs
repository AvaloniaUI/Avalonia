





namespace Perspex.Styling
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Defines a style.
    /// </summary>
    public class Style : IStyle
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
        public Style(Func<Selector, Selector> selector)
        {
            this.Selector = selector(new Selector());
        }

        /// <summary>
        /// Gets or sets style's selector.
        /// </summary>
        public Selector Selector { get; set; }

        /// <summary>
        /// Gets or sets style's setters.
        /// </summary>
        public IEnumerable<ISetter> Setters { get; set; } = new List<ISetter>();

        /// <summary>
        /// Attaches the style to a control if the style's selector matches.
        /// </summary>
        /// <param name="control">The control to attach to.</param>
        public void Attach(IStyleable control)
        {
            var description = "Style " + this.Selector.ToString();
            var match = this.Selector.Match(control);

            if (match.ImmediateResult != false)
            {
                foreach (var setter in this.Setters)
                {
                    setter.Apply(this, control, match.ObservableResult);
                }
            }
        }

        /// <summary>
        /// Returns a string representation of the style.
        /// </summary>
        /// <returns>A string representation of the style.</returns>
        public override string ToString()
        {
            return "Style: " + this.Selector.ToString();
        }
    }
}
