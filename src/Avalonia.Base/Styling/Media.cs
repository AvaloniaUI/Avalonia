using System;
using Avalonia.Controls;
using Avalonia.PropertyStore;

namespace Avalonia.Styling
{
    /// <summary>
    /// Defines a media.
    /// </summary>
    public class Media : StyleBase
    {
        private Query? _query;

        /// <summary>
        /// Initializes a new instance of the <see cref="Media"/> class.
        /// </summary>
        public Media()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Media"/> class.
        /// </summary>
        /// <param name="selector">The media selector.</param>
        public Media(Func<Query?, Query> selector)
        {
            Query = selector(null);
        }

        /// <summary>
        /// Gets or sets the media's selector.
        /// </summary>
        public Query? Query 
        {
            get => _query;
            set => _query = value;
        }

        /// <summary>
        /// Returns a string representation of the media.
        /// </summary>
        /// <returns>A string representation of the media.</returns>
        public override string ToString() => Query?.ToString(this) ?? "Media";

        internal override void SetParent(StyleBase? parent)
        {
            throw new InvalidOperationException("Media cannot be added as a nested style.");
        }

        internal SelectorMatchResult TryAttach(StyledElement target, object? host, FrameType type)
        {
            _ = target ?? throw new ArgumentNullException(nameof(target));

            var result = SelectorMatchResult.NeverThisType;

            if (HasChildren)
            {
                var match = Query?.Match(target, Parent, true) ??
                    (target == host ?
                        SelectorMatch.AlwaysThisInstance :
                        SelectorMatch.NeverThisInstance);

                if (match.IsMatch)
                {
                    Attach(target, match.Activator, type);
                }

                result = match.Result;
            }

            return result;
        }
    }
}
