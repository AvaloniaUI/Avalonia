using System;

namespace Avalonia.Styling
{
    /// <summary>
    /// Defines a style.
    /// </summary>
    public class Style : StyleBase
    {
        private Selector? _selector;

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
        public Selector? Selector 
        {
            get => _selector;
            set => _selector = ValidateSelector(value);
        }

        public override SelectorMatchResult TryAttach(IStyleable target, object? host)
        {
            _ = target ?? throw new ArgumentNullException(nameof(target));

            var result = SelectorMatchResult.NeverThisType;

            if (HasSettersOrAnimations)
            {
                var match = Selector?.Match(target, Parent, true) ??
                    (target == host ?
                        SelectorMatch.AlwaysThisInstance :
                        SelectorMatch.NeverThisInstance);

                if (match.IsMatch)
                    Attach(target, match.Activator);

                result = match.Result;
            }

            var childResult = TryAttachChildren(target, host);

            if (childResult > result)
                result = childResult;

            return result;
        }

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

        internal override void SetParent(StyleBase? parent)
        {
            if (parent is Style parentStyle && parentStyle.Selector is not null)
            {
                if (Selector is null)
                    throw new InvalidOperationException("Child styles must have a selector.");
                Selector.ValidateNestingSelector(false);
            }
            else if (parent is ControlTheme)
            {
                if (Selector is null)
                    throw new InvalidOperationException("Child styles must have a selector.");
                Selector.ValidateNestingSelector(true);
            }

            base.SetParent(parent);
        }

        private static Selector? ValidateSelector(Selector? selector)
        {
            if (selector is TemplateSelector)
                throw new InvalidOperationException(
                    "Invalid selector: Template selector must be followed by control selector.");
            return selector;
        }
    }
}
