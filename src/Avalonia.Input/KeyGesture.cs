// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Input
{
    /// <summary>
    /// Defines a keyboard input combination.
    /// </summary>
    public sealed class KeyGesture : IEquatable<KeyGesture>
    {
        private static readonly Dictionary<string, Key> s_keySynonyms = new Dictionary<string, Key>
        {
            { "+", Key.OemPlus }, { "-", Key.OemMinus }, { ".", Key.OemPeriod }, { ",", Key.OemComma }
        };

        [Obsolete("Use constructor taking KeyModifiers")]
        public KeyGesture(Key key, InputModifiers modifiers)
        {
            Key = key;
            KeyModifiers = (KeyModifiers)(((int)modifiers) & 0xf);
        }

        public KeyGesture(Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            Key = key;
            KeyModifiers = modifiers;
        }

        public bool Equals(KeyGesture other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Key == other.Key && KeyModifiers == other.KeyModifiers;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            return obj is KeyGesture && Equals((KeyGesture)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Key * 397) ^ (int)KeyModifiers;
            }
        }

        public static bool operator ==(KeyGesture left, KeyGesture right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(KeyGesture left, KeyGesture right)
        {
            return !Equals(left, right);
        }

        public Key Key { get; }

        [Obsolete("Use KeyModifiers")]
        public InputModifiers Modifiers => (InputModifiers)KeyModifiers;

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

                    if (isLast)
                    {
                        key = ParseKey(partSpan.ToString());
                    }
                    else
                    {
                        keyModifiers |= ParseModifier(partSpan);
                    }

                    cstart = c + 1;
                }
            }


            return new KeyGesture(key, keyModifiers);
        }

        public override string ToString()
        {
            var parts = new List<string>();

            foreach (var flag in Enum.GetValues(typeof(KeyModifiers)).Cast<KeyModifiers>())
            {
                if (KeyModifiers.HasFlag(flag) && flag != KeyModifiers.None)
                {
                    parts.Add(flag.ToString());
                }
            }

            parts.Add(Key.ToString());

            return string.Join(" + ", parts);
        }

        public bool Matches(KeyEventArgs keyEvent) => ResolveNumPadOperationKey(keyEvent.Key) == Key && keyEvent.KeyModifiers == KeyModifiers;

        // TODO: Move that to external key parser
        private static Key ParseKey(string key)
        {
            if (s_keySynonyms.TryGetValue(key.ToLower(), out Key rv))
                return rv;

            return (Key)Enum.Parse(typeof(Key), key, true);
        }

        private static KeyModifiers ParseModifier(ReadOnlySpan<char> modifier)
        {
            if (modifier.Equals("ctrl".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return KeyModifiers.Control;
            }

            if (modifier.Equals("cmd".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return KeyModifiers.Meta;
            }

            return (KeyModifiers)Enum.Parse(typeof(KeyModifiers), modifier.ToString(), true);
        }

        private Key ResolveNumPadOperationKey(Key key)
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
