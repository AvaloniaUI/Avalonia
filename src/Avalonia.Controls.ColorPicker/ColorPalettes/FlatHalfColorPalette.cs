using Avalonia.Media;
using Avalonia.Utilities;
using FlatColor = Avalonia.Controls.FlatColorPalette.FlatColor;

namespace Avalonia.Controls
{
    /// <summary>
    /// Implements half of the <see cref="FlatColorPalette"/> for improved usability.
    /// </summary>
    /// <inheritdoc cref="FlatColorPalette"/>
    public class FlatHalfColorPalette : IColorPalette
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
                    // Pomegranate
                    {
                        Color.FromUInt32((uint)FlatColor.Pomegranate1),
                        Color.FromUInt32((uint)FlatColor.Pomegranate3),
                        Color.FromUInt32((uint)FlatColor.Pomegranate5),
                        Color.FromUInt32((uint)FlatColor.Pomegranate7),
                        Color.FromUInt32((uint)FlatColor.Pomegranate9),
                    },

                    // Amethyst
                    {
                        Color.FromUInt32((uint)FlatColor.Amethyst1),
                        Color.FromUInt32((uint)FlatColor.Amethyst3),
                        Color.FromUInt32((uint)FlatColor.Amethyst5),
                        Color.FromUInt32((uint)FlatColor.Amethyst7),
                        Color.FromUInt32((uint)FlatColor.Amethyst9),
                    },

                    // Belize Hole
                    {
                        Color.FromUInt32((uint)FlatColor.BelizeHole1),
                        Color.FromUInt32((uint)FlatColor.BelizeHole3),
                        Color.FromUInt32((uint)FlatColor.BelizeHole5),
                        Color.FromUInt32((uint)FlatColor.BelizeHole7),
                        Color.FromUInt32((uint)FlatColor.BelizeHole9),
                    },

                    // Turquoise
                    {
                        Color.FromUInt32((uint)FlatColor.Turquoise1),
                        Color.FromUInt32((uint)FlatColor.Turquoise3),
                        Color.FromUInt32((uint)FlatColor.Turquoise5),
                        Color.FromUInt32((uint)FlatColor.Turquoise7),
                        Color.FromUInt32((uint)FlatColor.Turquoise9),
                    },

                    // Nephritis
                    {
                        Color.FromUInt32((uint)FlatColor.Nephritis1),
                        Color.FromUInt32((uint)FlatColor.Nephritis3),
                        Color.FromUInt32((uint)FlatColor.Nephritis5),
                        Color.FromUInt32((uint)FlatColor.Nephritis7),
                        Color.FromUInt32((uint)FlatColor.Nephritis9),
                    },

                    // Sunflower
                    {
                        Color.FromUInt32((uint)FlatColor.Sunflower1),
                        Color.FromUInt32((uint)FlatColor.Sunflower3),
                        Color.FromUInt32((uint)FlatColor.Sunflower5),
                        Color.FromUInt32((uint)FlatColor.Sunflower7),
                        Color.FromUInt32((uint)FlatColor.Sunflower9),
                    },

                    // Carrot
                    {
                        Color.FromUInt32((uint)FlatColor.Carrot1),
                        Color.FromUInt32((uint)FlatColor.Carrot3),
                        Color.FromUInt32((uint)FlatColor.Carrot5),
                        Color.FromUInt32((uint)FlatColor.Carrot7),
                        Color.FromUInt32((uint)FlatColor.Carrot9),
                    },

                    // Clouds
                    {
                        Color.FromUInt32((uint)FlatColor.Clouds1),
                        Color.FromUInt32((uint)FlatColor.Clouds3),
                        Color.FromUInt32((uint)FlatColor.Clouds5),
                        Color.FromUInt32((uint)FlatColor.Clouds7),
                        Color.FromUInt32((uint)FlatColor.Clouds9),
                    },

                    // Concrete
                    {
                        Color.FromUInt32((uint)FlatColor.Concrete1),
                        Color.FromUInt32((uint)FlatColor.Concrete3),
                        Color.FromUInt32((uint)FlatColor.Concrete5),
                        Color.FromUInt32((uint)FlatColor.Concrete7),
                        Color.FromUInt32((uint)FlatColor.Concrete9),
                    },

                    // Wet Asphalt
                    {
                        Color.FromUInt32((uint)FlatColor.WetAsphalt1),
                        Color.FromUInt32((uint)FlatColor.WetAsphalt3),
                        Color.FromUInt32((uint)FlatColor.WetAsphalt5),
                        Color.FromUInt32((uint)FlatColor.WetAsphalt7),
                        Color.FromUInt32((uint)FlatColor.WetAsphalt9),
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
