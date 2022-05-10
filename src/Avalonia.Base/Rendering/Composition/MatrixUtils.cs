using System.Numerics;

namespace Avalonia.Rendering.Composition
{
    static class MatrixUtils
    {
        public static Matrix4x4 ComputeTransform(Vector2 size, Vector2 anchorPoint, Vector3 centerPoint,
            Matrix4x4 transformMatrix, Vector3 scale, float rotationAngle, Quaternion orientation, Vector3 offset)
        {
            // The math here follows the *observed* UWP behavior since there are no docs on how it's supposed to work
            
            var anchor = size * anchorPoint;
            var  mat = Matrix4x4.CreateTranslation(-anchor.X, -anchor.Y, 0);

            var center = new Vector3(centerPoint.X, centerPoint.Y, centerPoint.Z);

            if (!transformMatrix.IsIdentity)
                mat = transformMatrix * mat;


            if (scale != new Vector3(1, 1, 1))
                mat *= Matrix4x4.CreateScale(scale, center);

            //TODO: RotationAxis support
            if (rotationAngle != 0)
                mat *= Matrix4x4.CreateRotationZ(rotationAngle, center);

            if (orientation != Quaternion.Identity)
            {
                if (centerPoint != default)
                {
                    mat *= Matrix4x4.CreateTranslation(-center)
                           * Matrix4x4.CreateFromQuaternion(orientation)
                           * Matrix4x4.CreateTranslation(center);
                }
                else
                    mat *= Matrix4x4.CreateFromQuaternion(orientation);
            }

            if (offset != default)
                mat *= Matrix4x4.CreateTranslation(offset);

            return mat;
        }

        public static Matrix4x4 ToMatrix4x4(Matrix matrix) =>
            new Matrix4x4(
                (float)matrix.M11, (float)matrix.M12, 0, (float)matrix.M13,
                (float)matrix.M21, (float)matrix.M22, 0, (float)matrix.M23,
                0, 0, 1, 0,
                (float)matrix.M31, (float)matrix.M32, 0, (float)matrix.M33
            );
        
        public static Matrix ToMatrix(Matrix4x4 matrix44) =>
            new Matrix(
                matrix44.M11,
                matrix44.M12,
                matrix44.M14,
                matrix44.M21,
                matrix44.M22,
                matrix44.M24,
                matrix44.M41,
                matrix44.M42,
                matrix44.M44);
    }
}