namespace Avalonia.Controls
{
    /// <inheritdoc />
    public class DecimalUpDown : CommonNumericUpDown<decimal>
    {
        /// <summary>
        /// Initializes static members of the <see cref="DecimalUpDown"/> class.
        /// </summary>
        static DecimalUpDown() => UpdateMetadata(typeof(DecimalUpDown), 1m, decimal.MinValue, decimal.MaxValue);

        /// <summary>
        /// Initializes new instance of the <see cref="DecimalUpDown"/> class.
        /// </summary>
        public DecimalUpDown() : base(decimal.TryParse, d => d, (v1, v2) => v1 < v2, (v1, v2) => v1 > v2)
        {
        }

        /// <inheritdoc />
        protected override decimal IncrementValue(decimal value, decimal increment) => value + increment;

        /// <inheritdoc />
        protected override decimal DecrementValue(decimal value, decimal increment) => value - increment;
    }
}