using System.Numerics;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition
{
    static class MatrixUtils
    {
        public static CompositionMatrix ComputeTransform(Vector size, Vector anchorPoint, Vector3D centerPoint,
            CompositionMatrix transformMatrix, Vector3D scale, float rotationAngle, Quaternion orientation, Vector3D offset)
        {
            // The math here follows the *observed* UWP behavior since there are no docs on how it's supposed to work

            var anchor = Vector.Multiply(size, anchorPoint);
            var  mat = CompositionMatrix.CreateTranslation(-anchor.X, -anchor.Y);

            var center = new Vector3D(centerPoint.X, centerPoint.Y, centerPoint.Z);

            if (!transformMatrix.GuaranteedIdentity)
                mat = transformMatrix * mat;


            if (scale != new Vector3D(1, 1, 1))
                mat *= CompositionMatrix.CreateScale(scale, center);

            //TODO: RotationAxis support
            if (rotationAngle != 0)
                mat *= ToMatrix(Matrix4x4.CreateRotationZ(rotationAngle, center.ToVector3()));

            if (orientation != Quaternion.Identity)
            {
                if (centerPoint != default)
                {
                    mat *= ToMatrix(Matrix4x4.CreateTranslation(-center.ToVector3())
                                    * Matrix4x4.CreateFromQuaternion(orientation)
                                    * Matrix4x4.CreateTranslation(center.ToVector3()));
                }
                else
                    mat *= ToMatrix(Matrix4x4.CreateFromQuaternion(orientation));
            }

            if (offset != default)
            {
                mat *= CompositionMatrix.CreateTranslation(offset.X, offset.Y);
            }

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
