using System;

namespace Avalonia.Benchmarks
{
    internal struct Struct1 : IEquatable<Struct1>
    {
        public int Int1;

        public Struct1(int i)
        {
            Int1 = i;
        }

        public override bool Equals(object? obj)
        {
            return obj is Struct1 @struct && Equals(@struct);
        }

        public bool Equals(Struct1 other)
        {
            return Int1 == other.Int1;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Int1);
        }
    }

    internal struct Struct2 : IEquatable<Struct2>
    {
        public int Int1;
        public int Int2;

        public Struct2(int i)
        {
            Int1 = Int2 = i;
        }

        public override bool Equals(object? obj)
        {
            return obj is Struct2 @struct && Equals(@struct);
        }

        public bool Equals(Struct2 other)
        {
            return Int1 == other.Int1 &&
                   Int2 == other.Int2;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Int1, Int2);
        }
    }

    internal struct Struct3 : IEquatable<Struct3>
    {
        public int Int1;
        public int Int2;
        public int Int3;

        public Struct3(int i)
        {
            Int1 = Int2 = Int3 = i;
        }

        public override bool Equals(object? obj)
        {
            return obj is Struct3 @struct && Equals(@struct);
        }

        public bool Equals(Struct3 other)
        {
            return Int1 == other.Int1 &&
                   Int2 == other.Int2 &&
                   Int3 == other.Int3;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Int1, Int2, Int3);
        }
    }

    internal struct Struct4 : IEquatable<Struct4>
    {
        public int Int1;
        public int Int2;
        public int Int3;
        public int Int4;

        public Struct4(int i)
        {
            Int1 = Int2 = Int3 = Int4 = i;
        }

        public override bool Equals(object? obj)
        {
            return obj is Struct4 @struct && Equals(@struct);
        }

        public bool Equals(Struct4 other)
        {
            return Int1 == other.Int1 &&
                   Int2 == other.Int2 &&
                   Int3 == other.Int3 &&
                   Int4 == other.Int4;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Int1, Int2, Int3, Int4);
        }
    }

    internal struct Struct5 : IEquatable<Struct5>
    {
        public int Int1;
        public int Int2;
        public int Int3;
        public int Int4;
        public int Int5;

        public Struct5(int i)
        {
            Int1 = Int2 = Int3 = Int4 = Int5 = i;
        }

        public override bool Equals(object? obj)
        {
            return obj is Struct5 @struct && Equals(@struct);
        }

        public bool Equals(Struct5 other)
        {
            return Int1 == other.Int1 &&
                   Int2 == other.Int2 &&
                   Int3 == other.Int3 &&
                   Int4 == other.Int4 &&
                   Int5 == other.Int5;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Int1, Int2, Int3, Int4, Int5);
        }
    }

    internal struct Struct6 : IEquatable<Struct6>
    {
        public int Int1;
        public int Int2;
        public int Int3;
        public int Int4;
        public int Int5;
        public int Int6;

        public Struct6(int i)
        {
            Int1 = Int2 = Int3 = Int4 = Int5 = Int6 = i;
        }

        public override bool Equals(object? obj)
        {
            return obj is Struct6 @struct && Equals(@struct);
        }

        public bool Equals(Struct6 other)
        {
            return Int1 == other.Int1 &&
                   Int2 == other.Int2 &&
                   Int3 == other.Int3 &&
                   Int4 == other.Int4 &&
                   Int5 == other.Int5 &&
                   Int6 == other.Int6;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Int1, Int2, Int3, Int4, Int5, Int6);
        }
    }

    internal struct Struct7 : IEquatable<Struct7>
    {
        public int Int1;
        public int Int2;
        public int Int3;
        public int Int4;
        public int Int5;
        public int Int6;
        public int Int7;

        public Struct7(int i)
        {
            Int1 = Int2 = Int3 = Int4 = Int5 = Int6 = Int7 = i;
        }

        public override bool Equals(object? obj)
        {
            return obj is Struct7 @struct && Equals(@struct);
        }

        public bool Equals(Struct7 other)
        {
            return Int1 == other.Int1 &&
                   Int2 == other.Int2 &&
                   Int3 == other.Int3 &&
                   Int4 == other.Int4 &&
                   Int5 == other.Int5 &&
                   Int6 == other.Int6 &&
                   Int7 == other.Int7;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Int1, Int2, Int3, Int4, Int5, Int6, Int7);
        }
    }

    internal struct Struct8 : IEquatable<Struct8>
    {
        public int Int1;
        public int Int2;
        public int Int3;
        public int Int4;
        public int Int5;
        public int Int6;
        public int Int7;
        public int Int8;

        public Struct8(int i)
        {
            Int1 = Int2 = Int3 = Int4 = Int5 = Int6 = Int7 = Int8 = i;
        }

        public override bool Equals(object? obj)
        {
            return obj is Struct8 @struct && Equals(@struct);
        }

        public bool Equals(Struct8 other)
        {
            return Int1 == other.Int1 &&
                   Int2 == other.Int2 &&
                   Int3 == other.Int3 &&
                   Int4 == other.Int4 &&
                   Int5 == other.Int5 &&
                   Int6 == other.Int6 &&
                   Int7 == other.Int7 &&
                   Int8 == other.Int8;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Int1, Int2, Int3, Int4, Int5, Int6, Int7, Int8);
        }
    }
}
