namespace Avalonia.Controls
{
    /// <inheritdoc />
    public class ShortUpDown : CommonNumericUpDown<short>
    {
        /// <summary>
        /// Initializes static members of the <see cref="ShortUpDown"/> class.
        /// </summary>
        static ShortUpDown() => UpdateMetadata(typeof(ShortUpDown), 1, short.MinValue, short.MaxValue);

        /// <summary>
        /// Initializes new instance of the <see cref="ShortUpDown"/> class.
        /// </summary>
        public ShortUpDown() : base(short.TryParse, decimal.ToInt16, (v1, v2) => v1 < v2, (v1, v2) => v1 > v2)
        {
        }

        /// <inheritdoc />
        protected override short IncrementValue(short value, short increment) => (short)(value + increment);

        /// <inheritdoc />
        protected override short DecrementValue(short value, short increment) => (short)(value - increment);
    }
}