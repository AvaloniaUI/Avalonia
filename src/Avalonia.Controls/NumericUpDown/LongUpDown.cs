namespace Avalonia.Controls
{
    /// <inheritdoc />
    public class LongUpDown : CommonNumericUpDown<long>
    {
        /// <summary>
        /// Initializes static members of the <see cref="LongUpDown"/> class.
        /// </summary>
        static LongUpDown() => UpdateMetadata(typeof(LongUpDown), 1L, long.MinValue, long.MaxValue);

        /// <summary>
        /// Initializes new instance of the <see cref="LongUpDown"/> class.
        /// </summary>
        public LongUpDown() : base(long.TryParse, decimal.ToInt64, (v1, v2) => v1 < v2, (v1, v2) => v1 > v2)
        {
        }

        /// <inheritdoc />
        protected override long IncrementValue(long value, long increment) => value + increment;

        /// <inheritdoc />
        protected override long DecrementValue(long value, long increment) => value - increment;
    }
}