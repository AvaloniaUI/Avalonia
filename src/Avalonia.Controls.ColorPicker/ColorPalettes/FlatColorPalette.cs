using Avalonia.Media;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    /// <summary>
    /// Implements a reduced flat design or flat UI color palette.
    /// </summary>
    /// <remarks>
    /// See:
    ///  - https://htmlcolorcodes.com/color-chart/
    ///  - https://htmlcolorcodes.com/color-chart/flat-design-color-chart/
    ///  - http://designmodo.github.io/Flat-UI/
    ///
    /// The GitHub project is licensed as MIT: https://github.com/designmodo/Flat-UI.
    ///
    /// </remarks>
    public class FlatColorPalette : IColorPalette
    {
        // The full Flat UI color chart has 10 rows and 20 columns
        // See: https://htmlcolorcodes.com/assets/downloads/flat-design-colors/flat-design-color-chart.png
        // This is a reduced palette for usability
        private static Color[,] colorChart = new Color[,]
        {
            // Pomegranate
            {
                Color.FromArgb(0xFF, 0xF9, 0xEB, 0xEA),
                Color.FromArgb(0xFF, 0xE6, 0xB0, 0xAA),
                Color.FromArgb(0xFF, 0xCD, 0x61, 0x55),
                Color.FromArgb(0xFF, 0xA9, 0x32, 0x26),
                Color.FromArgb(0xFF, 0x7B, 0x24, 0x1C),
            },

            // Amethyst
            {
                Color.FromArgb(0xFF, 0xF5, 0xEE, 0xF8),
                Color.FromArgb(0xFF, 0xD7, 0xBD, 0xE2),
                Color.FromArgb(0xFF, 0xAF, 0x7A, 0xC5),
                Color.FromArgb(0xFF, 0x88, 0x4E, 0xA0),
                Color.FromArgb(0xFF, 0x63, 0x39, 0x74),
            },

            // Belize Hole
            {
                Color.FromArgb(0xFF, 0xEA, 0xF2, 0xF8),
                Color.FromArgb(0xFF, 0xA9, 0xCC, 0xE3),
                Color.FromArgb(0xFF, 0x54, 0x99, 0xC7),
                Color.FromArgb(0xFF, 0x24, 0x71, 0xA3),
                Color.FromArgb(0xFF, 0x1A, 0x52, 0x76),
            },

            // Turquoise
            {
                Color.FromArgb(0xFF, 0xE8, 0xF8, 0xF5),
                Color.FromArgb(0xFF, 0xA3, 0xE4, 0xD7),
                Color.FromArgb(0xFF, 0x48, 0xC9, 0xB0),
                Color.FromArgb(0xFF, 0x17, 0xA5, 0x89),
                Color.FromArgb(0xFF, 0x11, 0x78, 0x64),
            },

            // Nephritis
            {
                Color.FromArgb(0xFF, 0xE9, 0xF7, 0xEF),
                Color.FromArgb(0xFF, 0xA9, 0xDF, 0xBF),
                Color.FromArgb(0xFF, 0x52, 0xBE, 0x80),
                Color.FromArgb(0xFF, 0x22, 0x99, 0x54),
                Color.FromArgb(0xFF, 0x19, 0x6F, 0x3D),
            },

            // Sunflower
            {
                Color.FromArgb(0xFF, 0xFE, 0xF9, 0xE7),
                Color.FromArgb(0xFF, 0xF9, 0xE7, 0x9F),
                Color.FromArgb(0xFF, 0xF4, 0xD0, 0x3F),
                Color.FromArgb(0xFF, 0xD4, 0xAC, 0x0D),
                Color.FromArgb(0xFF, 0x9A, 0x7D, 0x0A),
            },

            // Carrot
            {
                Color.FromArgb(0xFF, 0xFD, 0xF2, 0xE9),
                Color.FromArgb(0xFF, 0xF5, 0xCB, 0xA7),
                Color.FromArgb(0xFF, 0xEB, 0x98, 0x4E),
                Color.FromArgb(0xFF, 0xCA, 0x6F, 0x1E),
                Color.FromArgb(0xFF, 0x93, 0x51, 0x16),
            },

            // Clouds
            {
                Color.FromArgb(0xFF, 0xFD, 0xFE, 0xFE),
                Color.FromArgb(0xFF, 0xF7, 0xF9, 0xF9),
                Color.FromArgb(0xFF, 0xF0, 0xF3, 0xF4),
                Color.FromArgb(0xFF, 0xD0, 0xD3, 0xD4),
                Color.FromArgb(0xFF, 0x97, 0x9A, 0x9A),
            },

            // Concrete
            {
                Color.FromArgb(0xFF, 0xF4, 0xF6, 0xF6),
                Color.FromArgb(0xFF, 0xD5, 0xDB, 0xDB),
                Color.FromArgb(0xFF, 0xAA, 0xB7, 0xB8),
                Color.FromArgb(0xFF, 0x83, 0x91, 0x92),
                Color.FromArgb(0xFF, 0x5F, 0x6A, 0x6A),
            },

            // Wet Asphalt
            {
                Color.FromArgb(0xFF, 0xEB, 0xED, 0xEF),
                Color.FromArgb(0xFF, 0xAE, 0xB6, 0xBF),
                Color.FromArgb(0xFF, 0x5D, 0x6D, 0x7E),
                Color.FromArgb(0xFF, 0x2E, 0x40, 0x53),
                Color.FromArgb(0xFF, 0x21, 0x2F, 0x3C),
            },
        };

        /// <summary>
        /// Gets the index of the default shade of colors in this palette.
        /// </summary>
        public const int DefaultShadeIndex = 2;

        /// <summary>
        /// The index in the color palette of the 'Pomegranate' color.
        /// This index can correspond to multiple color shades.
        /// </summary>
        public const int PomegranateIndex = 0;

        /// <summary>
        /// The index in the color palette of the 'Amethyst' color.
        /// This index can correspond to multiple color shades.
        /// </summary>
        public const int AmethystIndex = 1;

        /// <summary>
        /// The index in the color palette of the 'BelizeHole' color.
        /// This index can correspond to multiple color shades.
        /// </summary>
        public const int BelizeHoleIndex = 2;

        /// <summary>
        /// The index in the color palette of the 'Turquoise' color.
        /// This index can correspond to multiple color shades.
        /// </summary>
        public const int TurquoiseIndex = 3;

        /// <summary>
        /// The index in the color palette of the 'Nephritis' color.
        /// This index can correspond to multiple color shades.
        /// </summary>
        public const int NephritisIndex = 4;

        /// <summary>
        /// The index in the color palette of the 'Sunflower' color.
        /// This index can correspond to multiple color shades.
        /// </summary>
        public const int SunflowerIndex = 5;

        /// <summary>
        /// The index in the color palette of the 'Carrot' color.
        /// This index can correspond to multiple color shades.
        /// </summary>
        public const int CarrotIndex = 6;

        /// <summary>
        /// The index in the color palette of the 'Clouds' color.
        /// This index can correspond to multiple color shades.
        /// </summary>
        public const int CloudsIndex = 7;

        /// <summary>
        /// The index in the color palette of the 'Concrete' color.
        /// This index can correspond to multiple color shades.
        /// </summary>
        public const int ConcreteIndex = 8;

        /// <summary>
        /// The index in the color palette of the 'WetAsphalt' color.
        /// This index can correspond to multiple color shades.
        /// </summary>
        public const int WetAsphaltIndex = 9;

        /// <inheritdoc/>
        public int ColorCount
        {
            // Table is transposed compared to the reference chart
            get => colorChart.GetLength(0);
        }

        /// <inheritdoc/>
        public int ShadeCount
        {
            // Table is transposed compared to the reference chart
            get => colorChart.GetLength(1);
        }

        /// <summary>
        /// Gets the palette defined color that has an ARGB value of #FFC0392B.
        /// </summary>
        public static Color Pomegranate
        {
            get => colorChart[PomegranateIndex, DefaultShadeIndex];
        }

        /// <summary>
        /// Gets the palette defined color that has an ARGB value of #FF9B59B6.
        /// </summary>
        public static Color Amethyst
        {
            get => colorChart[AmethystIndex, DefaultShadeIndex];
        }

        /// <summary>
        /// Gets the palette defined color that has an ARGB value of #FF2980B9.
        /// </summary>
        public static Color BelizeHole
        {
            get => colorChart[BelizeHoleIndex, DefaultShadeIndex];
        }

        /// <summary>
        /// Gets the palette defined color that has an ARGB value of #FF1ABC9C.
        /// </summary>
        public static Color Turquoise
        {
            get => colorChart[TurquoiseIndex, DefaultShadeIndex];
        }

        /// <summary>
        /// Gets the palette defined color that has an ARGB value of #FF27AE60.
        /// </summary>
        public static Color Nephritis
        {
            get => colorChart[NephritisIndex, DefaultShadeIndex];
        }

        /// <summary>
        /// Gets the palette defined color that has an ARGB value of #FFF1C40F.
        /// </summary>
        public static Color Sunflower
        {
            get => colorChart[SunflowerIndex, DefaultShadeIndex];
        }

        /// <summary>
        /// Gets the palette defined color that has an ARGB value of #FFE67E22.
        /// </summary>
        public static Color Carrot
        {
            get => colorChart[CarrotIndex, DefaultShadeIndex];
        }

        /// <summary>
        /// Gets the palette defined color that has an ARGB value of #FFECF0F1.
        /// </summary>
        public static Color Clouds
        {
            get => colorChart[CloudsIndex, DefaultShadeIndex];
        }

        /// <summary>
        /// Gets the palette defined color that has an ARGB value of #FF95A5A6.
        /// </summary>
        public static Color Concrete
        {
            get => colorChart[ConcreteIndex, DefaultShadeIndex];
        }

        /// <summary>
        /// Gets the palette defined color that has an ARGB value of #FF34495E.
        /// </summary>
        public static Color WetAsphalt
        {
            get => colorChart[WetAsphaltIndex, DefaultShadeIndex];
        }

        /// <inheritdoc/>
        public Color GetColor(int colorIndex, int shadeIndex)
        {
            // Table is transposed compared to the reference chart
            return colorChart[
                MathUtilities.Clamp(colorIndex, 0, colorChart.GetLength(0) - 1),
                MathUtilities.Clamp(shadeIndex, 0, colorChart.GetLength(1) - 1)];
        }
    }
}
