namespace Avalonia.Controls
{
    /// <inheritdoc />
    public class ByteUpDown : CommonNumericUpDown<byte>
    {
        /// <summary>
        /// Initializes static members of the <see cref="ByteUpDown"/> class.
        /// </summary>
        static ByteUpDown() => UpdateMetadata(typeof(ByteUpDown), 1, byte.MinValue, byte.MaxValue);

        /// <summary>
        /// Initializes new instance of the <see cref="ByteUpDown"/> class.
        /// </summary>
        public ByteUpDown() : base(byte.TryParse, decimal.ToByte, (v1, v2) => v1 < v2, (v1, v2) => v1 > v2)
        {
        }

        /// <inheritdoc />
        protected override byte IncrementValue(byte value, byte increment) => (byte)(value + increment);

        /// <inheritdoc />
        protected override byte DecrementValue(byte value, byte increment) => (byte)(value - increment);
    }
}