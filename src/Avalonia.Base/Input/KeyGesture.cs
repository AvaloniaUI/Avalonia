using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Avalonia.Input.Platform;
using Avalonia.Utilities;

namespace Avalonia.Input
{
    /// <summary>
    /// Defines a keyboard input combination.
    /// </summary>
    public sealed class KeyGesture : IEquatable<KeyGesture>, IFormattable
    {
        private static readonly Dictionary<string, Key> s_keySynonyms = new Dictionary<string, Key>
        {
            { "+", Key.OemPlus }, { "-", Key.OemMinus }, { ".", Key.OemPeriod }, { ",", Key.OemComma }
        };

        public KeyGesture(Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            Key = key;
            KeyModifiers = modifiers;
        }

        public bool Equals(KeyGesture? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Key == other.Key && KeyModifiers == other.KeyModifiers;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            return obj is KeyGesture gesture && Equals(gesture);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Key * 397) ^ (int)KeyModifiers;
            }
        }

        public static bool operator ==(KeyGesture? left, KeyGesture? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(KeyGesture? left, KeyGesture? right)
        {
            return !Equals(left, right);
        }

        public Key Key { get; }
        
        public KeyModifiers KeyModifiers { get; }

        public static KeyGesture Parse(string gesture)
        {
            // string.Split can't be used here because "Ctrl++" is a perfectly valid key gesture

            var key = Key.None;
            var keyModifiers = KeyModifiers.None;

            var cstart = 0;

            for (var c = 0; c <= gesture.Length; c++)
            {
                var ch = c == gesture.Length ? '\0' : gesture[c];
                bool isLast = c == gesture.Length;

                if (isLast || (ch == '+' && cstart != c))
                {
                    var partSpan = gesture.AsSpan(cstart, c - cstart).Trim();

                    if (!TryParseKey(partSpan.ToString(), out key))
                    {
                        keyModifiers |= ParseModifier(partSpan);
                    }
                    cstart = c + 1;
                }
            }


            return new KeyGesture(key, keyModifiers);
        }

        public override string ToString() => ToString(null, null);

        /// <summary>
        /// Returns the current KeyGesture as a string formatted according to the format string and appropriate IFormatProvider
        /// </summary>
        /// <param name="format">The format to use. 
        /// <list type="bullet">
        /// <item><term>null or "" or "g"</term><description>The Invariant format, uses Enum.ToString() to format Keys.</description></item>
        /// <item><term>"p"</term><description>Use platform specific formatting as registerd.</description></item>
        /// </list></param>
        /// <param name="formatProvider">The IFormatProvider to use.  If null, uses the appropriate provider registered in the Avalonia Locator, or Invariant.</param>
        /// <returns>The formatted string.</returns>
        /// <exception cref="FormatException">Thrown if the format string is not null, "", "g", or "p"</exception>
        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            var formatInfo = format switch
            {
                null or "" or "g" => KeyGestureFormatInfo.Invariant,
                "p" => KeyGestureFormatInfo.GetInstance(formatProvider),
                _ => throw new FormatException("Unknown format specifier")
            };

            var s = StringBuilderCache.Acquire();

            static void Plus(StringBuilder s)
            {
                if (s.Length > 0)
                {
                    s.Append("+");
                }
            }

            if (KeyModifiers.HasAllFlags(KeyModifiers.Control))
            {
                s.Append(formatInfo.Ctrl);
            }

            if (KeyModifiers.HasAllFlags(KeyModifiers.Shift))
            {
                Plus(s);
                s.Append(formatInfo.Shift);
            }

            if (KeyModifiers.HasAllFlags(KeyModifiers.Alt))
            {
                Plus(s);
                s.Append(formatInfo.Alt);
            }

            if (KeyModifiers.HasAllFlags(KeyModifiers.Meta))
            {
                Plus(s);
                s.Append(formatInfo.Meta);
            }

            if ((Key != Key.None) || (KeyModifiers == KeyModifiers.None))
            {
                Plus(s);
                s.Append(formatInfo.FormatKey(Key));
            }

            return StringBuilderCache.GetStringAndRelease(s);
        }

        public bool Matches(KeyEventArgs? keyEvent) =>
            keyEvent != null &&
            keyEvent.KeyModifiers == KeyModifiers &&
            ResolveNumPadOperationKey(keyEvent.Key) == ResolveNumPadOperationKey(Key);

        // TODO: Move that to external key parser
        private static bool TryParseKey(string keyStr, out Key key)
        {
            key = Key.None;
            if (s_keySynonyms.TryGetValue(keyStr.ToLower(CultureInfo.InvariantCulture), out key))
                return true;

            if (EnumHelper.TryParse(keyStr, true, out key))
                return true;

            return false;
        }

        private static KeyModifiers ParseModifier(ReadOnlySpan<char> modifier)
        {
            if (modifier.Equals("ctrl".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return KeyModifiers.Control;
            }

            if (modifier.Equals("cmd".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
                modifier.Equals("win".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
                modifier.Equals("⌘".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return KeyModifiers.Meta;
            }

            return EnumHelper.Parse<KeyModifiers>(modifier.ToString(), true);
        }

        private static Key ResolveNumPadOperationKey(Key key)
        {
            switch (key)
            {
                case Key.Add:
                    return Key.OemPlus;
                case Key.Subtract:
                    return Key.OemMinus;
                case Key.Decimal:
                    return Key.OemPeriod;
                default:
                    return key;
            }
        }
    }
}
