using System;

namespace Avalonia.Media.Fonts
{
    public readonly record struct OpenTypeTag
    {
        internal static readonly OpenTypeTag None = new OpenTypeTag(0, 0, 0, 0);
        internal static readonly OpenTypeTag Max = new OpenTypeTag(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
        internal static readonly OpenTypeTag MaxSigned = new OpenTypeTag((byte)sbyte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

        private readonly uint _value;

        public OpenTypeTag(uint value)
        {
            _value = value;
        }

        public OpenTypeTag(char c1, char c2, char c3, char c4)
        {
            _value = (uint)(((byte)c1 << 24) | ((byte)c2 << 16) | ((byte)c3 << 8) | (byte)c4);
        }

        private OpenTypeTag(byte c1, byte c2, byte c3, byte c4)
        {
            _value = (uint)((c1 << 24) | (c2 << 16) | (c3 << 8) | c4);
        }

        public static OpenTypeTag Parse(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return None;

            var realTag = new char[4];

            var len = Math.Min(4, tag.Length);
            var i = 0;
            for (; i < len; i++)
                realTag[i] = tag[i];
            for (; i < 4; i++)
                realTag[i] = ' ';

            return new OpenTypeTag(realTag[0], realTag[1], realTag[2], realTag[3]);
        }

        public override string ToString()
        {
            if (_value == None)
            {
                return nameof(None);
            }
            if (_value == Max)
            {
                return nameof(Max);
            }
            if (_value == MaxSigned)
            {
                return nameof(MaxSigned);
            }

            return string.Concat(
                (char)(byte)(_value >> 24),
                (char)(byte)(_value >> 16),
                (char)(byte)(_value >> 8),
                (char)(byte)_value);
        }

        public static implicit operator uint(OpenTypeTag tag) => tag._value;

        public static implicit operator OpenTypeTag(uint tag) => new OpenTypeTag(tag);
    }
}
