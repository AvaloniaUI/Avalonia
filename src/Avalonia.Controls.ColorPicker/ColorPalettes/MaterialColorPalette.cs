using Avalonia.Media;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    /// <summary>
    /// Implements a reduced version of the 2014 Material Design color palette.
    /// </summary>
    /// <remarks>
    /// This palette is based on the one outlined here:
    ///
    ///   https://material.io/design/color/the-color-system.html#tools-for-picking-colors
    ///
    /// In order to make the palette uniform and rectangular the following
    /// alterations were made:
    ///
    ///  1. The A100-A700 shades of each color are excluded.
    ///     These shades do not exist for all colors (brown/gray).
    ///  2. Black/White are stand-alone and are also excluded.
    ///
    /// </remarks>
    public class MaterialColorPalette : IColorPalette
    {
        /// <summary>
        /// Defines all colors in the <see cref="MaterialColorPalette"/>.
        /// </summary>
        /// <remarks>
        /// This is done in an enum to ensure it is compiled into the assembly improving
        /// startup performance.
        /// </remarks>
        public enum MaterialColor : uint
        {
            // Red
            Red50  = 0xFFFFEBEE,
            Red100 = 0xFFFFCDD2,
            Red200 = 0xFFEF9A9A,
            Red300 = 0xFFE57373,
            Red400 = 0xFFEF5350,
            Red500 = 0xFFF44336,
            Red600 = 0xFFE53935,
            Red700 = 0xFFD32F2F,
            Red800 = 0xFFC62828,
            Red900 = 0xFFB71C1C,

            RedA100 = 0xFFFF8A80,
            RedA200 = 0xFFFF5252,
            RedA400 = 0xFFFF1744,
            RedA700 = 0xFFD50000,

            // Pink
            Pink50  = 0xFFFCE4EC,
            Pink100 = 0xFFF8BBD0,
            Pink200 = 0xFFF48FB1,
            Pink300 = 0xFFF06292,
            Pink400 = 0xFFEC407A,
            Pink500 = 0xFFE91E63,
            Pink600 = 0xFFD81B60,
            Pink700 = 0xFFC2185B,
            Pink800 = 0xFFAD1457,
            Pink900 = 0xFF880E4F,

            PinkA100 = 0xFFFF80AB,
            PinkA200 = 0xFFFF4081,
            PinkA400 = 0xFFF50057,
            PinkA700 = 0xFFC51162,

            // Purple
            Purple50  = 0xFFF3E5F5,
            Purple100 = 0xFFE1BEE7,
            Purple200 = 0xFFCE93D8,
            Purple300 = 0xFFBA68C8,
            Purple400 = 0xFFAB47BC,
            Purple500 = 0xFF9C27B0,
            Purple600 = 0xFF8E24AA,
            Purple700 = 0xFF7B1FA2,
            Purple800 = 0xFF6A1B9A,
            Purple900 = 0xFF4A148C,

            PurpleA100 = 0xFFEA80FC,
            PurpleA200 = 0xFFE040FB,
            PurpleA400 = 0xFFD500F9,
            PurpleA700 = 0xFFAA00FF,

            // Deep Purple
            DeepPurple50  = 0xFFEDE7F6,
            DeepPurple100 = 0xFFD1C4E9,
            DeepPurple200 = 0xFFB39DDB,
            DeepPurple300 = 0xFF9575CD,
            DeepPurple400 = 0xFF7E57C2,
            DeepPurple500 = 0xFF673AB7,
            DeepPurple600 = 0xFF5E35B1,
            DeepPurple700 = 0xFF512DA8,
            DeepPurple800 = 0xFF4527A0,
            DeepPurple900 = 0xFF311B92,

            DeepPurpleA100 = 0xFFB388FF,
            DeepPurpleA200 = 0xFF7C4DFF,
            DeepPurpleA400 = 0xFF651FFF,
            DeepPurpleA700 = 0xFF6200EA,

            // Indigo
            Indigo50  = 0xFFE8EAF6,
            Indigo100 = 0xFFC5CAE9,
            Indigo200 = 0xFF9FA8DA,
            Indigo300 = 0xFF7986CB,
            Indigo400 = 0xFF5C6BC0,
            Indigo500 = 0xFF3F51B5,
            Indigo600 = 0xFF3949AB,
            Indigo700 = 0xFF303F9F,
            Indigo800 = 0xFF283593,
            Indigo900 = 0xFF1A237E,

            IndigoA100 = 0xFF8C9EFF,
            IndigoA200 = 0xFF536DFE,
            IndigoA400 = 0xFF3D5AFE,
            IndigoA700 = 0xFF304FFE,

            // Blue
            Blue50  = 0xFFE3F2FD,
            Blue100 = 0xFFBBDEFB,
            Blue200 = 0xFF90CAF9,
            Blue300 = 0xFF64B5F6,
            Blue400 = 0xFF42A5F5,
            Blue500 = 0xFF2196F3,
            Blue600 = 0xFF1E88E5,
            Blue700 = 0xFF1976D2,
            Blue800 = 0xFF1565C0,
            Blue900 = 0xFF0D47A1,

            BlueA100 = 0xFF82B1FF,
            BlueA200 = 0xFF448AFF,
            BlueA400 = 0xFF2979FF,
            BlueA700 = 0xFF2962FF,

            // Light Blue
            LightBlue50  = 0xFFE1F5FE,
            LightBlue100 = 0xFFB3E5FC,
            LightBlue200 = 0xFF81D4FA,
            LightBlue300 = 0xFF4FC3F7,
            LightBlue400 = 0xFF29B6F6,
            LightBlue500 = 0xFF03A9F4,
            LightBlue600 = 0xFF039BE5,
            LightBlue700 = 0xFF0288D1,
            LightBlue800 = 0xFF0277BD,
            LightBlue900 = 0xFF01579B,

            LightBlueA100 = 0xFF80D8FF,
            LightBlueA200 = 0xFF40C4FF,
            LightBlueA400 = 0xFF00B0FF,
            LightBlueA700 = 0xFF0091EA,

            // Cyan
            Cyan50  = 0xFFE0F7FA,
            Cyan100 = 0xFFB2EBF2,
            Cyan200 = 0xFF80DEEA,
            Cyan300 = 0xFF4DD0E1,
            Cyan400 = 0xFF26C6DA,
            Cyan500 = 0xFF00BCD4,
            Cyan600 = 0xFF00ACC1,
            Cyan700 = 0xFF0097A7,
            Cyan800 = 0xFF00838F,
            Cyan900 = 0xFF006064,

            CyanA100 = 0xFF84FFFF,
            CyanA200 = 0xFF18FFFF,
            CyanA400 = 0xFF00E5FF,
            CyanA700 = 0xFF00B8D4,

            // Teal
            Teal50  = 0xFFE0F2F1,
            Teal100 = 0xFFB2DFDB,
            Teal200 = 0xFF80CBC4,
            Teal300 = 0xFF4DB6AC,
            Teal400 = 0xFF26A69A,
            Teal500 = 0xFF009688,
            Teal600 = 0xFF00897B,
            Teal700 = 0xFF00796B,
            Teal800 = 0xFF00695C,
            Teal900 = 0xFF004D40,

            TealA100 = 0xFFA7FFEB,
            TealA200 = 0xFF64FFDA,
            TealA400 = 0xFF1DE9B6,
            TealA700 = 0xFF00BFA5,

            // Green
            Green50  = 0xFFE8F5E9,
            Green100 = 0xFFC8E6C9,
            Green200 = 0xFFA5D6A7,
            Green300 = 0xFF81C784,
            Green400 = 0xFF66BB6A,
            Green500 = 0xFF4CAF50,
            Green600 = 0xFF43A047,
            Green700 = 0xFF388E3C,
            Green800 = 0xFF2E7D32,
            Green900 = 0xFF1B5E20,

            GreenA100 = 0xFFB9F6CA,
            GreenA200 = 0xFF69F0AE,
            GreenA400 = 0xFF00E676,
            GreenA700 = 0xFF00C853,

            // Light Green
            LightGreen50  = 0xFFF1F8E9,
            LightGreen100 = 0xFFDCEDC8,
            LightGreen200 = 0xFFC5E1A5,
            LightGreen300 = 0xFFAED581,
            LightGreen400 = 0xFF9CCC65,
            LightGreen500 = 0xFF8BC34A,
            LightGreen600 = 0xFF7CB342,
            LightGreen700 = 0xFF689F38,
            LightGreen800 = 0xFF558B2F,
            LightGreen900 = 0xFF33691E,

            LightGreenA100 = 0xFFCCFF90,
            LightGreenA200 = 0xFFB2FF59,
            LightGreenA400 = 0xFF76FF03,
            LightGreenA700 = 0xFF64DD17,

            // Lime
            Lime50  = 0xFFF9FBE7,
            Lime100 = 0xFFF0F4C3,
            Lime200 = 0xFFE6EE9C,
            Lime300 = 0xFFDCE775,
            Lime400 = 0xFFD4E157,
            Lime500 = 0xFFCDDC39,
            Lime600 = 0xFFC0CA33,
            Lime700 = 0xFFAFB42B,
            Lime800 = 0xFF9E9D24,
            Lime900 = 0xFF827717,

            LimeA100 = 0xFFF4FF81,
            LimeA200 = 0xFFEEFF41,
            LimeA400 = 0xFFC6FF00,
            LimeA700 = 0xFFAEEA00,

            // Yellow
            Yellow50  = 0xFFFFFDE7,
            Yellow100 = 0xFFFFF9C4,
            Yellow200 = 0xFFFFF59D,
            Yellow300 = 0xFFFFF176,
            Yellow400 = 0xFFFFEE58,
            Yellow500 = 0xFFFFEB3B,
            Yellow600 = 0xFFFDD835,
            Yellow700 = 0xFFFBC02D,
            Yellow800 = 0xFFF9A825,
            Yellow900 = 0xFFF57F17,

            YellowA100 = 0xFFFFFF8D,
            YellowA200 = 0xFFFFFF00,
            YellowA400 = 0xFFFFEA00,
            YellowA700 = 0xFFFFD600,

            // Amber
            Amber50  = 0xFFFFF8E1,
            Amber100 = 0xFFFFECB3,
            Amber200 = 0xFFFFE082,
            Amber300 = 0xFFFFD54F,
            Amber400 = 0xFFFFCA28,
            Amber500 = 0xFFFFC107,
            Amber600 = 0xFFFFB300,
            Amber700 = 0xFFFFA000,
            Amber800 = 0xFFFF8F00,
            Amber900 = 0xFFFF6F00,

            AmberA100 = 0xFFFFE57F,
            AmberA200 = 0xFFFFD740,
            AmberA400 = 0xFFFFC400,
            AmberA700 = 0xFFFFAB00,

            // Orange
            Orange50  = 0xFFFFF3E0,
            Orange100 = 0xFFFFE0B2,
            Orange200 = 0xFFFFCC80,
            Orange300 = 0xFFFFB74D,
            Orange400 = 0xFFFFA726,
            Orange500 = 0xFFFF9800,
            Orange600 = 0xFFFB8C00,
            Orange700 = 0xFFF57C00,
            Orange800 = 0xFFEF6C00,
            Orange900 = 0xFFE65100,

            OrangeA100 = 0xFFFFD180,
            OrangeA200 = 0xFFFFAB40,
            OrangeA400 = 0xFFFF9100,
            OrangeA700 = 0xFFFF6D00,

            // Deep Orange
            DeepOrange50  = 0xFFFBE9E7,
            DeepOrange100 = 0xFFFFCCBC,
            DeepOrange200 = 0xFFFFAB91,
            DeepOrange300 = 0xFFFF8A65,
            DeepOrange400 = 0xFFFF7043,
            DeepOrange500 = 0xFFFF5722,
            DeepOrange600 = 0xFFF4511E,
            DeepOrange700 = 0xFFE64A19,
            DeepOrange800 = 0xFFD84315,
            DeepOrange900 = 0xFFBF360C,

            DeepOrangeA100 = 0xFFFF9E80,
            DeepOrangeA200 = 0xFFFF6E40,
            DeepOrangeA400 = 0xFFFF3D00,
            DeepOrangeA700 = 0xFFDD2C00,

            // Brown
            Brown50  = 0xFFEFEBE9,
            Brown100 = 0xFFD7CCC8,
            Brown200 = 0xFFBCAAA4,
            Brown300 = 0xFFA1887F,
            Brown400 = 0xFF8D6E63,
            Brown500 = 0xFF795548,
            Brown600 = 0xFF6D4C41,
            Brown700 = 0xFF5D4037,
            Brown800 = 0xFF4E342E,
            Brown900 = 0xFF3E2723,

            // Gray
            Gray50  = 0xFFFAFAFA,
            Gray100 = 0xFFF5F5F5,
            Gray200 = 0xFFEEEEEE,
            Gray300 = 0xFFE0E0E0,
            Gray400 = 0xFFBDBDBD,
            Gray500 = 0xFF9E9E9E,
            Gray600 = 0xFF757575,
            Gray700 = 0xFF616161,
            Gray800 = 0xFF424242,
            Gray900 = 0xFF212121,

            // Blue Gray
            BlueGray50  = 0xFFECEFF1,
            BlueGray100 = 0xFFCFD8DC,
            BlueGray200 = 0xFFB0BEC5,
            BlueGray300 = 0xFF90A4AE,
            BlueGray400 = 0xFF78909C,
            BlueGray500 = 0xFF607D8B,
            BlueGray600 = 0xFF546E7A,
            BlueGray700 = 0xFF455A64,
            BlueGray800 = 0xFF37474F,
            BlueGray900 = 0xFF263238,

            Black = 0xFF000000,
            White = 0xFFFFFFFF,
        }

        // See: https://material.io/design/color/the-color-system.html#tools-for-picking-colors
        // This is a reduced palette for uniformity
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
                        Color.FromUInt32((uint)MaterialColor.Red100),
                        Color.FromUInt32((uint)MaterialColor.Red200),
                        Color.FromUInt32((uint)MaterialColor.Red300),
                        Color.FromUInt32((uint)MaterialColor.Red400),
                        Color.FromUInt32((uint)MaterialColor.Red500),
                        Color.FromUInt32((uint)MaterialColor.Red600),
                        Color.FromUInt32((uint)MaterialColor.Red700),
                        Color.FromUInt32((uint)MaterialColor.Red800),
                        Color.FromUInt32((uint)MaterialColor.Red900),
                    },

                    // Pink
                    {
                        Color.FromUInt32((uint)MaterialColor.Pink50),
                        Color.FromUInt32((uint)MaterialColor.Pink100),
                        Color.FromUInt32((uint)MaterialColor.Pink200),
                        Color.FromUInt32((uint)MaterialColor.Pink300),
                        Color.FromUInt32((uint)MaterialColor.Pink400),
                        Color.FromUInt32((uint)MaterialColor.Pink500),
                        Color.FromUInt32((uint)MaterialColor.Pink600),
                        Color.FromUInt32((uint)MaterialColor.Pink700),
                        Color.FromUInt32((uint)MaterialColor.Pink800),
                        Color.FromUInt32((uint)MaterialColor.Pink900),
                    },

                    // Purple
                    {
                        Color.FromUInt32((uint)MaterialColor.Purple50),
                        Color.FromUInt32((uint)MaterialColor.Purple100),
                        Color.FromUInt32((uint)MaterialColor.Purple200),
                        Color.FromUInt32((uint)MaterialColor.Purple300),
                        Color.FromUInt32((uint)MaterialColor.Purple400),
                        Color.FromUInt32((uint)MaterialColor.Purple500),
                        Color.FromUInt32((uint)MaterialColor.Purple600),
                        Color.FromUInt32((uint)MaterialColor.Purple700),
                        Color.FromUInt32((uint)MaterialColor.Purple800),
                        Color.FromUInt32((uint)MaterialColor.Purple900),
                    },

                    // Deep Purple
                    {
                        Color.FromUInt32((uint)MaterialColor.DeepPurple50),
                        Color.FromUInt32((uint)MaterialColor.DeepPurple100),
                        Color.FromUInt32((uint)MaterialColor.DeepPurple200),
                        Color.FromUInt32((uint)MaterialColor.DeepPurple300),
                        Color.FromUInt32((uint)MaterialColor.DeepPurple400),
                        Color.FromUInt32((uint)MaterialColor.DeepPurple500),
                        Color.FromUInt32((uint)MaterialColor.DeepPurple600),
                        Color.FromUInt32((uint)MaterialColor.DeepPurple700),
                        Color.FromUInt32((uint)MaterialColor.DeepPurple800),
                        Color.FromUInt32((uint)MaterialColor.DeepPurple900),
                    },

                    // Indigo
                    {
                        Color.FromUInt32((uint)MaterialColor.Indigo50),
                        Color.FromUInt32((uint)MaterialColor.Indigo100),
                        Color.FromUInt32((uint)MaterialColor.Indigo200),
                        Color.FromUInt32((uint)MaterialColor.Indigo300),
                        Color.FromUInt32((uint)MaterialColor.Indigo400),
                        Color.FromUInt32((uint)MaterialColor.Indigo500),
                        Color.FromUInt32((uint)MaterialColor.Indigo600),
                        Color.FromUInt32((uint)MaterialColor.Indigo700),
                        Color.FromUInt32((uint)MaterialColor.Indigo800),
                        Color.FromUInt32((uint)MaterialColor.Indigo900),
                    },

                    // Blue
                    {
                        Color.FromUInt32((uint)MaterialColor.Blue50),
                        Color.FromUInt32((uint)MaterialColor.Blue100),
                        Color.FromUInt32((uint)MaterialColor.Blue200),
                        Color.FromUInt32((uint)MaterialColor.Blue300),
                        Color.FromUInt32((uint)MaterialColor.Blue400),
                        Color.FromUInt32((uint)MaterialColor.Blue500),
                        Color.FromUInt32((uint)MaterialColor.Blue600),
                        Color.FromUInt32((uint)MaterialColor.Blue700),
                        Color.FromUInt32((uint)MaterialColor.Blue800),
                        Color.FromUInt32((uint)MaterialColor.Blue900),
                    },

                    // Light Blue
                    {
                        Color.FromUInt32((uint)MaterialColor.LightBlue50),
                        Color.FromUInt32((uint)MaterialColor.LightBlue100),
                        Color.FromUInt32((uint)MaterialColor.LightBlue200),
                        Color.FromUInt32((uint)MaterialColor.LightBlue300),
                        Color.FromUInt32((uint)MaterialColor.LightBlue400),
                        Color.FromUInt32((uint)MaterialColor.LightBlue500),
                        Color.FromUInt32((uint)MaterialColor.LightBlue600),
                        Color.FromUInt32((uint)MaterialColor.LightBlue700),
                        Color.FromUInt32((uint)MaterialColor.LightBlue800),
                        Color.FromUInt32((uint)MaterialColor.LightBlue900),
                    },

                    // Cyan
                    {
                        Color.FromUInt32((uint)MaterialColor.Cyan50),
                        Color.FromUInt32((uint)MaterialColor.Cyan100),
                        Color.FromUInt32((uint)MaterialColor.Cyan200),
                        Color.FromUInt32((uint)MaterialColor.Cyan300),
                        Color.FromUInt32((uint)MaterialColor.Cyan400),
                        Color.FromUInt32((uint)MaterialColor.Cyan500),
                        Color.FromUInt32((uint)MaterialColor.Cyan600),
                        Color.FromUInt32((uint)MaterialColor.Cyan700),
                        Color.FromUInt32((uint)MaterialColor.Cyan800),
                        Color.FromUInt32((uint)MaterialColor.Cyan900),
                    },

                    // Teal
                    {
                        Color.FromUInt32((uint)MaterialColor.Teal50),
                        Color.FromUInt32((uint)MaterialColor.Teal100),
                        Color.FromUInt32((uint)MaterialColor.Teal200),
                        Color.FromUInt32((uint)MaterialColor.Teal300),
                        Color.FromUInt32((uint)MaterialColor.Teal400),
                        Color.FromUInt32((uint)MaterialColor.Teal500),
                        Color.FromUInt32((uint)MaterialColor.Teal600),
                        Color.FromUInt32((uint)MaterialColor.Teal700),
                        Color.FromUInt32((uint)MaterialColor.Teal800),
                        Color.FromUInt32((uint)MaterialColor.Teal900),
                    },

                    // Green
                    {
                        Color.FromUInt32((uint)MaterialColor.Green50),
                        Color.FromUInt32((uint)MaterialColor.Green100),
                        Color.FromUInt32((uint)MaterialColor.Green200),
                        Color.FromUInt32((uint)MaterialColor.Green300),
                        Color.FromUInt32((uint)MaterialColor.Green400),
                        Color.FromUInt32((uint)MaterialColor.Green500),
                        Color.FromUInt32((uint)MaterialColor.Green600),
                        Color.FromUInt32((uint)MaterialColor.Green700),
                        Color.FromUInt32((uint)MaterialColor.Green800),
                        Color.FromUInt32((uint)MaterialColor.Green900),
                    },

                    // Light Green
                    {
                        Color.FromUInt32((uint)MaterialColor.LightGreen50),
                        Color.FromUInt32((uint)MaterialColor.LightGreen100),
                        Color.FromUInt32((uint)MaterialColor.LightGreen200),
                        Color.FromUInt32((uint)MaterialColor.LightGreen300),
                        Color.FromUInt32((uint)MaterialColor.LightGreen400),
                        Color.FromUInt32((uint)MaterialColor.LightGreen500),
                        Color.FromUInt32((uint)MaterialColor.LightGreen600),
                        Color.FromUInt32((uint)MaterialColor.LightGreen700),
                        Color.FromUInt32((uint)MaterialColor.LightGreen800),
                        Color.FromUInt32((uint)MaterialColor.LightGreen900),
                    },

                    // Lime
                    {
                        Color.FromUInt32((uint)MaterialColor.Lime50),
                        Color.FromUInt32((uint)MaterialColor.Lime100),
                        Color.FromUInt32((uint)MaterialColor.Lime200),
                        Color.FromUInt32((uint)MaterialColor.Lime300),
                        Color.FromUInt32((uint)MaterialColor.Lime400),
                        Color.FromUInt32((uint)MaterialColor.Lime500),
                        Color.FromUInt32((uint)MaterialColor.Lime600),
                        Color.FromUInt32((uint)MaterialColor.Lime700),
                        Color.FromUInt32((uint)MaterialColor.Lime800),
                        Color.FromUInt32((uint)MaterialColor.Lime900),
                    },

                    // Yellow
                    {
                        Color.FromUInt32((uint)MaterialColor.Yellow50),
                        Color.FromUInt32((uint)MaterialColor.Yellow100),
                        Color.FromUInt32((uint)MaterialColor.Yellow200),
                        Color.FromUInt32((uint)MaterialColor.Yellow300),
                        Color.FromUInt32((uint)MaterialColor.Yellow400),
                        Color.FromUInt32((uint)MaterialColor.Yellow500),
                        Color.FromUInt32((uint)MaterialColor.Yellow600),
                        Color.FromUInt32((uint)MaterialColor.Yellow700),
                        Color.FromUInt32((uint)MaterialColor.Yellow800),
                        Color.FromUInt32((uint)MaterialColor.Yellow900),
                    },

                    // Amber
                    {
                        Color.FromUInt32((uint)MaterialColor.Amber50),
                        Color.FromUInt32((uint)MaterialColor.Amber100),
                        Color.FromUInt32((uint)MaterialColor.Amber200),
                        Color.FromUInt32((uint)MaterialColor.Amber300),
                        Color.FromUInt32((uint)MaterialColor.Amber400),
                        Color.FromUInt32((uint)MaterialColor.Amber500),
                        Color.FromUInt32((uint)MaterialColor.Amber600),
                        Color.FromUInt32((uint)MaterialColor.Amber700),
                        Color.FromUInt32((uint)MaterialColor.Amber800),
                        Color.FromUInt32((uint)MaterialColor.Amber900),
                    },

                    // Orange
                    {
                        Color.FromUInt32((uint)MaterialColor.Orange50),
                        Color.FromUInt32((uint)MaterialColor.Orange100),
                        Color.FromUInt32((uint)MaterialColor.Orange200),
                        Color.FromUInt32((uint)MaterialColor.Orange300),
                        Color.FromUInt32((uint)MaterialColor.Orange400),
                        Color.FromUInt32((uint)MaterialColor.Orange500),
                        Color.FromUInt32((uint)MaterialColor.Orange600),
                        Color.FromUInt32((uint)MaterialColor.Orange700),
                        Color.FromUInt32((uint)MaterialColor.Orange800),
                        Color.FromUInt32((uint)MaterialColor.Orange900),
                    },

                    // Deep Orange
                    {
                        Color.FromUInt32((uint)MaterialColor.DeepOrange50),
                        Color.FromUInt32((uint)MaterialColor.DeepOrange100),
                        Color.FromUInt32((uint)MaterialColor.DeepOrange200),
                        Color.FromUInt32((uint)MaterialColor.DeepOrange300),
                        Color.FromUInt32((uint)MaterialColor.DeepOrange400),
                        Color.FromUInt32((uint)MaterialColor.DeepOrange500),
                        Color.FromUInt32((uint)MaterialColor.DeepOrange600),
                        Color.FromUInt32((uint)MaterialColor.DeepOrange700),
                        Color.FromUInt32((uint)MaterialColor.DeepOrange800),
                        Color.FromUInt32((uint)MaterialColor.DeepOrange900),
                    },

                    // Brown
                    {
                        Color.FromUInt32((uint)MaterialColor.Brown50),
                        Color.FromUInt32((uint)MaterialColor.Brown100),
                        Color.FromUInt32((uint)MaterialColor.Brown200),
                        Color.FromUInt32((uint)MaterialColor.Brown300),
                        Color.FromUInt32((uint)MaterialColor.Brown400),
                        Color.FromUInt32((uint)MaterialColor.Brown500),
                        Color.FromUInt32((uint)MaterialColor.Brown600),
                        Color.FromUInt32((uint)MaterialColor.Brown700),
                        Color.FromUInt32((uint)MaterialColor.Brown800),
                        Color.FromUInt32((uint)MaterialColor.Brown900),
                    },

                    // Gray
                    {
                        Color.FromUInt32((uint)MaterialColor.Gray50),
                        Color.FromUInt32((uint)MaterialColor.Gray100),
                        Color.FromUInt32((uint)MaterialColor.Gray200),
                        Color.FromUInt32((uint)MaterialColor.Gray300),
                        Color.FromUInt32((uint)MaterialColor.Gray400),
                        Color.FromUInt32((uint)MaterialColor.Gray500),
                        Color.FromUInt32((uint)MaterialColor.Gray600),
                        Color.FromUInt32((uint)MaterialColor.Gray700),
                        Color.FromUInt32((uint)MaterialColor.Gray800),
                        Color.FromUInt32((uint)MaterialColor.Gray900),
                    },

                    // Blue Gray
                    {
                        Color.FromUInt32((uint)MaterialColor.BlueGray50),
                        Color.FromUInt32((uint)MaterialColor.BlueGray100),
                        Color.FromUInt32((uint)MaterialColor.BlueGray200),
                        Color.FromUInt32((uint)MaterialColor.BlueGray300),
                        Color.FromUInt32((uint)MaterialColor.BlueGray400),
                        Color.FromUInt32((uint)MaterialColor.BlueGray500),
                        Color.FromUInt32((uint)MaterialColor.BlueGray600),
                        Color.FromUInt32((uint)MaterialColor.BlueGray700),
                        Color.FromUInt32((uint)MaterialColor.BlueGray800),
                        Color.FromUInt32((uint)MaterialColor.BlueGray900),
                    },
                };
            }

            return;
        }

        /// <inheritdoc/>
        public int ColorCount
        {
            get => 19;
        }

        /// <inheritdoc/>
        public int ShadeCount
        {
            get => 10;
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
