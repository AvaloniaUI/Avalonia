// Copyright (c) 2009-2010 Math.NET Taken from http://github.com/mathnet/mathnet-numerics and modified for Wasabi Wallet

namespace MathNet
{
	/// <summary>
	/// Left and right boundary conditions.
	/// </summary>
	public enum SplineBoundaryCondition
	{
		/// <summary>
		/// Natural Boundary (Zero second derivative).
		/// </summary>
		Natural = 0,

		/// <summary>
		/// Parabolically Terminated boundary.
		/// </summary>
		ParabolicallyTerminated,

		/// <summary>
		/// Fixed first derivative at the boundary.
		/// </summary>
		FirstDerivative,

		/// <summary>
		/// Fixed second derivative at the boundary.
		/// </summary>
		SecondDerivative
	}
}
