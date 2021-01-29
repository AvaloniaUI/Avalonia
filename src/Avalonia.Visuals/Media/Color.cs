using System;
using System.Globalization;
using System.Runtime.InteropServices;
#if !BUILDTASK
using Avalonia.Animation.Animators;
#endif

namespace Avalonia.Media
{
    /// <summary>
    /// An ARGB color.
    /// </summary>
#if !BUILDTASK
    public
#endif
    readonly struct Color : IEquatable<Color>
    {
        static Color()
        {
#if !BUILDTASK
            Animation.Animation.RegisterAnimator<ColorAnimator>(prop => typeof(Color).IsAssignableFrom(prop.PropertyType));
#endif
        }

        private const string DefaultAccentColor = "#FF0078D7";

        /// <summary>
        /// Gets the Alpha component of the color.
        /// </summary>
        public byte A { get; }

        /// <summary>
        /// Gets the Red component of the color.
        /// </summary>
        public byte R { get; }

        /// <summary>
        /// Gets the Green component of the color.
        /// </summary>
        public byte G { get; }

        /// <summary>
        /// Gets the Blue component of the color.
        /// </summary>
        public byte B { get; }

        public Color(byte a, byte r, byte g, byte b)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }

        /// <summary>
        /// Creates a <see cref="Color"/> from alpha, red, green and blue components.
        /// </summary>
        /// <param name="a">The alpha component.</param>
        /// <param name="r">The red component.</param>
        /// <param name="g">The green component.</param>
        /// <param name="b">The blue component.</param>
        /// <returns>The color.</returns>
        public static Color FromArgb(byte a, byte r, byte g, byte b)
        {
            return new Color(a, r, g, b);
        }

        /// <summary>
        /// Creates a <see cref="Color"/> from red, green and blue components.
        /// </summary>
        /// <param name="r">The red component.</param>
        /// <param name="g">The green component.</param>
        /// <param name="b">The blue component.</param>
        /// <returns>The color.</returns>
        public static Color FromRgb(byte r, byte g, byte b)
        {
            return new Color(0xff, r, g, b);
        }

        /// <summary>
        /// Creates a <see cref="Color"/> from an integer.
        /// </summary>
        /// <param name="value">The integer value.</param>
        /// <returns>The color.</returns>
        public static Color FromUInt32(uint value)
        {
            return new Color(
                (byte)((value >> 24) & 0xff),
                (byte)((value >> 16) & 0xff),
                (byte)((value >> 8) & 0xff),
                (byte)(value & 0xff)
            );
        }

        /// <summary>
        /// Parses a color string.
        /// </summary>
        /// <param name="s">The color string.</param>
        /// <returns>The <see cref="Color"/>.</returns>
        public static Color Parse(string s)
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            if (TryParse(s, out Color color))
            {
                return color;
            }

            throw new FormatException($"Invalid color string: '{s}'.");
        }

        /// <summary>
        /// Parses a color string.
        /// </summary>
        /// <param name="s">The color string.</param>
        /// <returns>The <see cref="Color"/>.</returns>
        public static Color Parse(ReadOnlySpan<char> s)
        {
            if (TryParse(s, out Color color))
            {
                return color;
            }

            throw new FormatException($"Invalid color string: '{s.ToString()}'.");
        }

        /// <summary>
        /// Parses a color string.
        /// </summary>
        /// <param name="s">The color string.</param>
        /// <param name="color">The parsed color</param>
        /// <returns>The status of the operation.</returns>
        public static bool TryParse(string s, out Color color)
        {
            color = default;

            if (s is null)
            {
                return false;
            }

            if (s.Length == 0)
            {
                return false;
            }

            if (s[0] == '#' && TryParseInternal(s.AsSpan(), out color))
            {
                return true;
            }

            var knownColor = KnownColors.GetKnownColor(s);

            if (knownColor != KnownColor.None)
            {
                color = knownColor.ToColor();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Parses a color string.
        /// </summary>
        /// <param name="s">The color string.</param>
        /// <param name="color">The parsed color</param>
        /// <returns>The status of the operation.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out Color color)
        {
            if (s.Length == 0)
            {
                color = default;

                return false;
            }

            if (s[0] == '#')
            {
                return TryParseInternal(s, out color);
            }

            var knownColor = KnownColors.GetKnownColor(s.ToString());

            if (knownColor != KnownColor.None)
            {
                color = knownColor.ToColor();

                return true;
            }

            color = default;

            return false;
        }

        private static bool TryParseInternal(ReadOnlySpan<char> s, out Color color)
        {
            static bool TryParseCore(ReadOnlySpan<char> input, ref Color color)
            {
                var alphaComponent = 0u;

                if (input.Length == 6)
                {
                    alphaComponent = 0xff000000;
                }
                else if (input.Length != 8)
                {
                    return false;
                }

                // TODO: (netstandard 2.1) Can use allocation free parsing.
                if (!uint.TryParse(input.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                    out var parsed))
                {
                    return false;
                }

                color = FromUInt32(parsed | alphaComponent);

                return true;
            }

            color = default;

            ReadOnlySpan<char> input = s.Slice(1);

            // Handle shorthand cases like #FFF (RGB) or #FFFF (ARGB).
            if (input.Length == 3 || input.Length == 4)
            {
                var extendedLength = 2 * input.Length;
                
#if !BUILDTASK
                Span<char> extended = stackalloc char[extendedLength];
#else
                char[] extended = new char[extendedLength];
#endif

                for (int i = 0; i < input.Length; i++)
                {
                    extended[2 * i + 0] = input[i];
                    extended[2 * i + 1] = input[i];
                }

                return TryParseCore(extended, ref color);
            }

            return TryParseCore(input, ref color);
        }

        /// <summary>
        /// Returns the string representation of the color.
        /// </summary>
        /// <returns>
        /// The string representation of the color.
        /// </returns>
        public override string ToString()
        {
            uint rgb = ToUint32();
            return KnownColors.GetKnownColorName(rgb) ?? $"#{rgb:x8}";
        }

        /// <summary>
        /// Returns the integer representation of the color.
        /// </summary>
        /// <returns>
        /// The integer representation of the color.
        /// </returns>
        public uint ToUint32()
        {
            return ((uint)A << 24) | ((uint)R << 16) | ((uint)G << 8) | (uint)B;
        }

        /// <summary>
        /// Check if two colors are equal.
        /// </summary>
        public bool Equals(Color other)
        {
            return A == other.A && R == other.R && G == other.G && B == other.B;
        }

        public override bool Equals(object obj)
        {
            return obj is Color other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = A.GetHashCode();
                hashCode = (hashCode * 397) ^ R.GetHashCode();
                hashCode = (hashCode * 397) ^ G.GetHashCode();
                hashCode = (hashCode * 397) ^ B.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(Color left, Color right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Color left, Color right)
        {
            return !left.Equals(right);
        }
#if !BUILDTASK
        /// <summary>
        /// Get the System Accent Color, if the Operating System is not supported, returns #FF0078D7
        /// </summary>
        /// <returns>The accent color</returns>
        public static Color GetSystemAccentColor()
        {
            var isWindows10_8 = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) & 
                            !(Environment.OSVersion == new OperatingSystem(PlatformID.Win32NT, new Version(10,0)) ||
                             Environment.OSVersion == new OperatingSystem(PlatformID.Win32NT, new Version(6,3))); //check if the OS is Windows 10 or Windows 8
            var defaultValue = Color.DefaultAccentColor;// default Avalonia Accent Color

            //TODO: Add Support for MacOS X Accent color and Linux Desktop Envs with this feature
            if(isWindows10_8)
            {
                return GetAccentColorWin("ImmersiveSystemAccent");
            }
            else
            {
                return Color.Parse(defaultValue);
            }
        }


        private static Color GetAccentColorWin(string name)
        {
            var colorSet = Avalonia.Win32.Interop.UnmanagedMethods.GetImmersiveUserColorSetPreference(false, false);
            var colorType = Avalonia.Win32.Interop.UnmanagedMethods.GetImmersiveColorTypeFromName(name);
            var rawColor = Avalonia.Win32.Interop.UnmanagedMethods.GetImmersiveColorFromColorSetEx(colorSet, colorType, false, 0);

            var bytes = BitConverter.GetBytes(rawColor);
            return Color.FromArgb(bytes[3], bytes[0], bytes[1], bytes[2]);
        }
#endif

        /// <summary>
        /// Change the luminosity of a color.
        /// </summary>
        /// <param name="color">The color to change its luminosity</param>
        /// <param name="newluminosityFactor">The new Luminosity for the Color</param>
        /// <returns>A new Color with the Luminosity Factor applied.</returns>
        public static Color ChangeColorLuminosity(Color color, double newluminosityFactor)
        {
            var red = (double)color.R;
            var green = (double)color.G;
            var blue = (double)color.B;

            if (newluminosityFactor < 0)//applies darkness
            {
                newluminosityFactor = 1 + newluminosityFactor;
                red *= newluminosityFactor;
                green *= newluminosityFactor;
                blue *= newluminosityFactor;
            }
            else if(newluminosityFactor >= 0) //applies lightness
            {
                red = (255 - red) * newluminosityFactor + red;
                green = (255 - green) * newluminosityFactor + green;
                blue = (255 - blue) * newluminosityFactor + blue;
            }
            else
            {
                throw new ArgumentOutOfRangeException("The Luminosity Factor must be a finite number.");
            }

            return new Color(color.A, (byte)red, (byte)green, (byte)blue);
        }
    }
}
