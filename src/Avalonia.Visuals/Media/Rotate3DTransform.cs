using System;
using System.Numerics;

namespace Avalonia.Media;

/// <summary>
///  Non-Affine 3D transformation for rotating an visual around a definable axis
/// </summary>
public class Rotate3DTransform : Transform
{
    /// <summary>
    /// Defines the <see cref="RotationX"/> property.
    /// </summary>
    public static readonly StyledProperty<double> RotationXProperty =
        AvaloniaProperty.Register<Rotate3DTransform, double>(nameof(RotationX));

    /// <summary>
    /// Defines the <see cref="RotationY"/> property.
    /// </summary>
    public static readonly StyledProperty<double> RotationYProperty =
        AvaloniaProperty.Register<Rotate3DTransform, double>(nameof(RotationY));

    /// <summary>
    /// Defines the <see cref="RotationZ"/> property.
    /// </summary>
    public static readonly StyledProperty<double> RotationZProperty =
        AvaloniaProperty.Register<Rotate3DTransform, double>(nameof(RotationZ));


    /// <summary>
    /// Defines the <see cref="CenterX"/> property.
    /// </summary>
    public static readonly StyledProperty<double> CenterXProperty =
        AvaloniaProperty.Register<Rotate3DTransform, double>(nameof(CenterX));


    /// <summary>
    /// Defines the <see cref="CenterY"/> property.
    /// </summary>
    public static readonly StyledProperty<double> CenterYProperty =
        AvaloniaProperty.Register<Rotate3DTransform, double>(nameof(CenterY));


    /// <summary>
    /// Defines the <see cref="CenterZ"/> property.
    /// </summary>
    public static readonly StyledProperty<double> CenterZProperty =
        AvaloniaProperty.Register<Rotate3DTransform, double>(nameof(CenterZ));

    /// <summary>
    /// Defines the <see cref="Depth"/> property.
    /// </summary>
    public static readonly StyledProperty<double> DepthProperty =
        AvaloniaProperty.Register<Rotate3DTransform, double>(nameof(Depth));

    /// <summary>
    /// Initializes a new instance of the <see cref="Rotate3DTransform"/> class.
    /// </summary>
    public Rotate3DTransform()
    {
        this.GetObservable(RotationXProperty).Subscribe(_ => RaiseChanged());
        this.GetObservable(RotationYProperty).Subscribe(_ => RaiseChanged());
        this.GetObservable(RotationZProperty).Subscribe(_ => RaiseChanged());
        this.GetObservable(CenterXProperty).Subscribe(_ => RaiseChanged());
        this.GetObservable(CenterYProperty).Subscribe(_ => RaiseChanged());
        this.GetObservable(CenterZProperty).Subscribe(_ => RaiseChanged());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rotate3DTransform"/> class.
    /// </summary>
    /// <param name="rotationX">The rotation around the X-Axis</param>
    /// <param name="rotationY">The rotation around the Y-Axis</param>
    /// <param name="rotationZ">The rotation around the Z-Axis</param>
    /// <param name="centerX">The origin of the X-Axis</param>
    /// <param name="centerY">The origin of the Y-Axis</param>
    /// <param name="centerZ">The origin of the Z-Axis</param>
    public Rotate3DTransform(
        double rotationX,
        double rotationY,
        double rotationZ,
        double centerX,
        double centerY,
        double centerZ) : this()
    {
        RotationX = rotationX;
        RotationY = rotationY;
        RotationZ = rotationZ;
        CenterX = centerX;
        CenterY = centerY;
        CenterZ = centerZ;
    }

    /// <summary>
    /// Sets the rotation around the X-Axis
    /// </summary>
    public double RotationX
    {
        get => GetValue(RotationXProperty);
        set => SetValue(RotationXProperty, value);
    }

    /// <summary>
    /// Sets the rotation around the Y-Axis
    /// </summary>
    public double RotationY
    {
        get => GetValue(RotationYProperty);
        set => SetValue(RotationYProperty, value);
    }

    /// <summary>
    ///  Sets the rotation around the Z-Axis
    /// </summary>
    public double RotationZ
    {
        get => GetValue(RotationZProperty);
        set => SetValue(RotationZProperty, value);
    }

    /// <summary>
    ///  Moves the origin of the X-Axis
    /// </summary>
    public double CenterX
    {
        get => GetValue(CenterXProperty);
        set => SetValue(CenterXProperty, value);
    }

    /// <summary>
    ///  Moves the origin of the Y-Axis
    /// </summary>
    public double CenterY
    {
        get => GetValue(CenterYProperty);
        set => SetValue(CenterYProperty, value);
    }

    /// <summary>
    ///  Moves the origin of the Z-Axis
    /// </summary>
    public double CenterZ
    {
        get => GetValue(CenterZProperty);
        set => SetValue(CenterZProperty, value);
    }

    /// <summary>
    ///  Affects the depth of the rotation effect
    /// </summary>
    public double Depth
    {
        get => GetValue(DepthProperty);
        set => SetValue(DepthProperty, value);
    }

    /// <summary>
    /// Gets the transform's <see cref="Matrix"/>.
    /// </summary>
    public override Matrix Value
    {
        get
        {
            var matrix44 = Matrix4x4.Identity;

            matrix44 *= Matrix4x4.CreateTranslation(-(float)CenterX, -(float)CenterY, -(float)CenterZ);

            matrix44 *= Matrix4x4.CreateRotationX((float)Matrix.ToRadians(RotationX));
            matrix44 *= Matrix4x4.CreateRotationY((float)Matrix.ToRadians(RotationY));
            matrix44 *= Matrix4x4.CreateRotationZ((float)Matrix.ToRadians(RotationZ));

            matrix44 *= Matrix4x4.CreateTranslation((float)CenterX, (float)CenterY, (float)CenterZ);

            if (Depth != 0)
            {
                var perspectiveMatrix = Matrix4x4.Identity;
                perspectiveMatrix.M34 = -1 / (float)Depth;
                matrix44 *= perspectiveMatrix;
            }

            var matrix = new Matrix(
                matrix44.M11,
                matrix44.M12,
                matrix44.M14,
                matrix44.M21,
                matrix44.M22,
                matrix44.M24,
                matrix44.M41,
                matrix44.M42,
                matrix44.M44);

            return matrix;
        }
    }
}
