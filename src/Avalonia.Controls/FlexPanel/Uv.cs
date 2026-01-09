namespace Avalonia.Controls
{
    internal struct Uv
    {
        public Uv(double u, double v)
        {
            U = u;
            V = v;
        }

        public double U { get; }

        public double V { get; }

        public static Uv FromSize(double width, double height, bool swap) =>
            new Uv(swap ? height : width, swap ? width : height);

        public static Uv FromSize(Size size, bool swap) =>
            FromSize(size.Width, size.Height, swap);

        public static Point ToPoint(Uv uv, bool swap) =>
            new Point(swap ? uv.V : uv.U, swap ? uv.U : uv.V);

        public static Size ToSize(Uv uv, bool swap) =>
            new Size(swap ? uv.V : uv.U, swap ? uv.U : uv.V);

        public Uv WithU(double u) =>
            new Uv(u, V);

        public Uv WithV(double v) =>
            new Uv(U, v);

        public override string ToString() =>
            $"U: {U}, V: {V}";
    }
}
