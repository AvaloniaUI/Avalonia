namespace Avalonia.Controls
{
    /// <inheritdoc />
    public class IntegerUpDown : CommonNumericUpDown<int>
    {
        /// <summary>
        /// Initializes static members of the <see cref="IntegerUpDown"/> class.
        /// </summary>
        static IntegerUpDown() => UpdateMetadata(typeof(IntegerUpDown), 1, int.MinValue, int.MaxValue);

        /// <summary>
        /// Initializes new instance of the <see cref="IntegerUpDown"/> class.
        /// </summary>
        public IntegerUpDown() : base(int.TryParse, decimal.ToInt32, (v1, v2) => v1 < v2, (v1, v2) => v1 > v2)
        {
        }

        /// <inheritdoc />
        protected override int IncrementValue(int value, int increment) => value + increment;

        /// <inheritdoc />
        protected override int DecrementValue(int value, int increment) => value - increment;
    }
}