using Avalonia.Media;
using Avalonia.Utilities;
using MaterialColor = Avalonia.Controls.MaterialColorPalette.MaterialColor;

namespace Avalonia.Controls
{
    /// <summary>
    /// Implements half of the <see cref="MaterialColorPalette"/> for improved usability.
    /// </summary>
    /// <inheritdoc cref="MaterialColorPalette"/>
    public class MaterialHalfColorPalette : IColorPalette
    {
        private static Color[,]? _colorChart = null;
        private static readonly object _colorChartMutex = new();

        /// <summary>
        /// Initializes all color chart colors.
        /// </summary>
        protected void InitColorChart()
        {
            lock (_colorChartMutex)
            {
                if (_colorChart != null)
                {
                    return;
                }

                _colorChart = new Color[,]
                {
                    // Red
                    {
                        Color.FromUInt32((uint)MaterialColor.Red50),
                        Color.FromUInt32((uint)MaterialColor.Red200),
                        Color.FromUInt32((uint)MaterialColor.Red400),
                        Color.FromUInt32((uint)MaterialColor.Red600),
                        Color.FromUInt32((uint)MaterialColor.Red800),
                    },

                    // Purple
                    {
                        Color.FromUInt32((uint)MaterialColor.Purple50),
                        Color.FromUInt32((uint)MaterialColor.Purple200),
                        Color.FromUInt32((uint)MaterialColor.Purple400),
                        Color.FromUInt32((uint)MaterialColor.Purple600),
                        Color.FromUInt32((uint)MaterialColor.Purple800),
                    },

                    // Indigo
                    {
                        Color.FromUInt32((uint)MaterialColor.Indigo50),
                        Color.FromUInt32((uint)MaterialColor.Indigo200),
                        Color.FromUInt32((uint)MaterialColor.Indigo400),
                        Color.FromUInt32((uint)MaterialColor.Indigo600),
                        Color.FromUInt32((uint)MaterialColor.Indigo800),
                    },

                    // Light Blue
                    {
                        Color.FromUInt32((uint)MaterialColor.LightBlue50),
                        Color.FromUInt32((uint)MaterialColor.LightBlue200),
                        Color.FromUInt32((uint)MaterialColor.LightBlue400),
                        Color.FromUInt32((uint)MaterialColor.LightBlue600),
                        Color.FromUInt32((uint)MaterialColor.LightBlue800),
                    },

                    // Teal
                    {
                        Color.FromUInt32((uint)MaterialColor.Teal50),
                        Color.FromUInt32((uint)MaterialColor.Teal200),
                        Color.FromUInt32((uint)MaterialColor.Teal400),
                        Color.FromUInt32((uint)MaterialColor.Teal600),
                        Color.FromUInt32((uint)MaterialColor.Teal800),
                    },

                    // Light Green
                    {
                        Color.FromUInt32((uint)MaterialColor.LightGreen50),
                        Color.FromUInt32((uint)MaterialColor.LightGreen200),
                        Color.FromUInt32((uint)MaterialColor.LightGreen400),
                        Color.FromUInt32((uint)MaterialColor.LightGreen600),
                        Color.FromUInt32((uint)MaterialColor.LightGreen800),
                    },

                    // Yellow
                    {
                        Color.FromUInt32((uint)MaterialColor.Yellow50),
                        Color.FromUInt32((uint)MaterialColor.Yellow200),
                        Color.FromUInt32((uint)MaterialColor.Yellow400),
                        Color.FromUInt32((uint)MaterialColor.Yellow600),
                        Color.FromUInt32((uint)MaterialColor.Yellow800),
                    },

                    // Orange
                    {
                        Color.FromUInt32((uint)MaterialColor.Orange50),
                        Color.FromUInt32((uint)MaterialColor.Orange200),
                        Color.FromUInt32((uint)MaterialColor.Orange400),
                        Color.FromUInt32((uint)MaterialColor.Orange600),
                        Color.FromUInt32((uint)MaterialColor.Orange800),
                    },

                    // Brown
                    {
                        Color.FromUInt32((uint)MaterialColor.Brown50),
                        Color.FromUInt32((uint)MaterialColor.Brown200),
                        Color.FromUInt32((uint)MaterialColor.Brown400),
                        Color.FromUInt32((uint)MaterialColor.Brown600),
                        Color.FromUInt32((uint)MaterialColor.Brown800),
                    },

                    // Blue Gray
                    {
                        Color.FromUInt32((uint)MaterialColor.BlueGray50),
                        Color.FromUInt32((uint)MaterialColor.BlueGray200),
                        Color.FromUInt32((uint)MaterialColor.BlueGray400),
                        Color.FromUInt32((uint)MaterialColor.BlueGray600),
                        Color.FromUInt32((uint)MaterialColor.BlueGray800),
                    },
                };
            }

            return;
        }

        /// <inheritdoc/>
        public int ColorCount
        {
            get => 10;
        }

        /// <inheritdoc/>
        public int ShadeCount
        {
            get => 5;
        }

        /// <inheritdoc/>
        public Color GetColor(int colorIndex, int shadeIndex)
        {
            if (_colorChart == null)
            {
                InitColorChart();
            }

            return _colorChart![
                MathUtilities.Clamp(colorIndex, 0, ColorCount - 1),
                MathUtilities.Clamp(shadeIndex, 0, ShadeCount - 1)];
        }
    }
}
