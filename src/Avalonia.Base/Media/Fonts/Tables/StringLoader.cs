// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/

using System.Diagnostics;
using System.Text;

namespace Avalonia.Media.Fonts.Tables
{
    [DebuggerDisplay("Offset: {Offset}, Length: {Length}, Value: {Value}")]
    internal class StringLoader
    {
        public StringLoader(ushort length, ushort offset, Encoding encoding)
        {
            Length = length;
            Offset = offset;
            Encoding = encoding;
            Value = string.Empty;
        }

        public ushort Length { get; }

        public ushort Offset { get; }

        public string Value { get; private set; }

        public Encoding Encoding { get; }

        public static StringLoader Create(BigEndianBinaryReader reader)
            => Create(reader, Encoding.BigEndianUnicode);

        public static StringLoader Create(BigEndianBinaryReader reader, Encoding encoding)
            => new StringLoader(reader.ReadUInt16(), reader.ReadUInt16(), encoding);

        public void LoadValue(BigEndianBinaryReader reader)
            => Value = reader.ReadString(Length, Encoding).Replace("\0", string.Empty);
    }
}
