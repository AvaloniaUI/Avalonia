using System;

namespace Avalonia.Benchmarks
{
    internal record struct Struct1
    {
        public Struct1(int value)
        {
            Int1 = value;
        }

        public int Int1;
    }

    internal record struct Struct2
    {
        public Struct2(int value)
        {
            Int1 = Int2 = value;
        }

        public int Int1;
        public int Int2;
    }

    internal record struct Struct3
    {
        public Struct3(int value)
        {
            Int1 = Int2 = Int3 = value;
        }

        public int Int1;
        public int Int2;
        public int Int3;
    }

    internal record struct Struct4
    {
        public Struct4(int value)
        {
            Int1 = Int2 = Int3 = Int4 = value;
        }

        public int Int1;
        public int Int2;
        public int Int3;
        public int Int4;
    }

    internal record struct Struct5
    {
        public Struct5(int value)
        {
            Int1 = Int2 = Int3 = Int4 = Int5 = value;
        }

        public int Int1;
        public int Int2;
        public int Int3;
        public int Int4;
        public int Int5;
    }

    internal record struct Struct6
    {
        public Struct6(int value)
        {
            Int1 = Int2 = Int3 = Int4 = Int5 = Int6 = value;
        }

        public int Int1;
        public int Int2;
        public int Int3;
        public int Int4;
        public int Int5;
        public int Int6;
    }

    internal record struct Struct7
    {
        public Struct7(int value)
        {
            Int1 = Int2 = Int3 = Int4 = Int5 = Int6 = Int7 = value;
        }

        public int Int1;
        public int Int2;
        public int Int3;
        public int Int4;
        public int Int5;
        public int Int6;
        public int Int7;
    }

    internal record struct Struct8
    {
        public Struct8(int value)
        {
            Int1 = Int2 = Int3 = Int4 = Int5 = Int6 = Int7 = Int8 = value;
        }

        public int Int1;
        public int Int2;
        public int Int3;
        public int Int4;
        public int Int5;
        public int Int6;
        public int Int7;
        public int Int8;
    }
}
