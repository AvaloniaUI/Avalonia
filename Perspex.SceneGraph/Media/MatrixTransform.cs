// -----------------------------------------------------------------------
// <copyright file="MatrixTransform.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using System;

    /// <summary>
    /// Transforms an <see cref="IVisual"/> according to a <see cref="Matrix"/>.
    /// </summary>
    public class MatrixTransform : Transform
    {
        /// <summary>
        /// Defines the <see cref="Matrix"/> property.
        /// </summary>
        public static readonly PerspexProperty<Matrix> MatrixProperty =
            PerspexProperty.Register<MatrixTransform, Matrix>("Matrix", Matrix.Identity);

        /// <summary>
        /// Initializes a new instance of the <see cref="MatrixTransform"/> class.
        /// </summary>
        public MatrixTransform()
        {
            this.GetObservable(MatrixProperty).Subscribe(_ => this.RaiseChanged());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MatrixTransform"/> class.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        public MatrixTransform(Matrix matrix)
            : this()
        {
            this.Matrix = matrix;
        }

        /// <summary>
        /// Gets or sets the matrix.
        /// </summary>
        public Matrix Matrix
        {
            get { return this.GetValue(MatrixProperty); }
            set { this.SetValue(MatrixProperty, value); }
        }

        /// <summary>
        /// Gets the matrix.
        /// </summary>
        public override Matrix Value
        {
            get { return this.Matrix; }
        }
    }
}
