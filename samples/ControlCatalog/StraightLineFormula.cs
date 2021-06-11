using Avalonia;

namespace ControlCatalog
{
	public class StraightLineFormula
	{
		public StraightLineFormula()
		{
			M = 1;
			C = 0;
			Y = 1;
		}

		public StraightLineFormula(double m, double c, double y)
		{
			M = m;
			C = c;
			Y = y;
		}

		/// <summary>
		/// Used for intersects
		/// </summary>
		private double Y { get; set; }

		/// <summary>
		/// The gradient
		/// </summary>
		public double M { get; set; }

		/// <summary>
		/// The Y intercept
		/// </summary>
		public double C { get; set; }

		public static StraightLineFormula operator *(StraightLineFormula f, double multiplier)
		{
			return new StraightLineFormula((multiplier * f.M), (multiplier * f.C), (multiplier * f.Y));
		}

		public static StraightLineFormula operator -(StraightLineFormula f, StraightLineFormula g)
		{
			return new StraightLineFormula((f.M - g.M), (f.C - g.C), (f.Y - g.Y));
		}

		public static StraightLineFormula operator /(StraightLineFormula f, double divisor)
		{
			return new StraightLineFormula((f.M / divisor), (f.C / divisor), (f.Y / divisor));
		}

		public static Point IntersectionBetween(StraightLineFormula f, StraightLineFormula g)
		{
			return new Point((int)XIntersectionBetween(f, g), (int)YIntersectionBetween(f, g));
		}

		public static double YIntersectionBetween(StraightLineFormula f, StraightLineFormula g)
		{
			StraightLineFormula tempA = (f * g.M) - (g * f.M);
			tempA = tempA / tempA.Y;
			return tempA.C;
		}

		public static double XIntersectionBetween(StraightLineFormula f, StraightLineFormula g)
		{
			double y = YIntersectionBetween(f, g);
			return (y - f.C) / f.M;
		}

		public void CalculateFrom(double x1, double x2, double y1, double y2)
		{
			CalculateFrom(x1, x2, y1, y2, x1);
		}

		public void CalculateFrom(double x1, double x2, double y1, double y2, double x)
		{
			CalculateMFrom(x1, x2, y1, y2);

			C = y1 - (M * x);
		}

		public void CalculateMFrom(double x1, double x2, double y1, double y2)
		{
			if ((y2 - y1) != 0 && (x2 - x1) != 0)
			{
				M = (y2 - y1) / (x2 - x1);
			}
		}

		public double GetYforX(double x)
		{
			return ((M * x) + C);
		}

		public double GetXforY(double y)
		{
			return (y - C) / M;
		}
	}
}
