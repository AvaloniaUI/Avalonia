namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="Matrix"/> properties.
    /// </summary>
    public class MatrixAnimator : Animator<Matrix>
    {
        /// <inheritdocs/>
        public override Matrix Interpolate(double progress, Matrix oldValue, Matrix newValue)
        {
            return new Matrix(
                ((newValue.M11 - oldValue.M11) * progress) + oldValue.M11,
                ((newValue.M12 - oldValue.M12) * progress) + oldValue.M12,
                ((newValue.M21 - oldValue.M21) * progress) + oldValue.M21,
                ((newValue.M22 - oldValue.M22) * progress) + oldValue.M22,
                ((newValue.M31 - oldValue.M31) * progress) + oldValue.M31,
                ((newValue.M32 - oldValue.M32) * progress) + oldValue.M32);
        }
    }
}
