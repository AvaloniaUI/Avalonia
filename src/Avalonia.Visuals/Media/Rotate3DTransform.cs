using System;
using System.Numerics;
using Avalonia.Animation.Animators;

namespace Avalonia.Media;

/// <summary>
///  Non-Affine 3D transformation for rotating an visual around a definable axis
/// </summary>
public class Rotate3DTransform : Transform
{
    /// <summary>
    /// Defines the <see cref="AngleX"/> property.
    /// </summary>
    public static readonly StyledProperty<double> AngleXProperty =
        AvaloniaProperty.Register<Rotate3DTransform, double>(nameof(AngleX));

    /// <summary>
    /// Defines the <see cref="AngleY"/> property.
    /// </summary>
    public static readonly StyledProperty<double> AngleYProperty =
        AvaloniaProperty.Register<Rotate3DTransform, double>(nameof(AngleY));

    /// <summary>
    /// Defines the <see cref="AngleZ"/> property.
    /// </summary>
    public static readonly StyledProperty<double> AngleZProperty =
        AvaloniaProperty.Register<Rotate3DTransform, double>(nameof(AngleZ));


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
        this.GetObservable(AngleXProperty).Subscribe(_ => RaiseChanged());
        this.GetObservable(AngleYProperty).Subscribe(_ => RaiseChanged());
        this.GetObservable(AngleZProperty).Subscribe(_ => RaiseChanged());
        this.GetObservable(CenterXProperty).Subscribe(_ => RaiseChanged());
        this.GetObservable(CenterYProperty).Subscribe(_ => RaiseChanged());
        this.GetObservable(CenterZProperty).Subscribe(_ => RaiseChanged());
        this.GetObservable(DepthProperty).Subscribe(_ => RaiseChanged());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rotate3DTransform"/> class.
    /// </summary>
    /// <param name="angleX">The rotation around the X-Axis</param>
    /// <param name="angleY">The rotation around the Y-Axis</param>
    /// <param name="angleZ">The rotation around the Z-Axis</param>
    /// <param name="centerX">The origin of the X-Axis</param>
    /// <param name="centerY">The origin of the Y-Axis</param>
    /// <param name="centerZ">The origin of the Z-Axis</param>
    public Rotate3DTransform(
        double angleX,
        double angleY,
        double angleZ,
        double centerX,
        double centerY,
        double centerZ) : this()
    {
        AngleX = angleX;
        AngleY = angleY;
        AngleZ = angleZ;
        CenterX = centerX;
        CenterY = centerY;
        CenterZ = centerZ;
    }

    /// <summary>
    /// Sets the rotation around the X-Axis
    /// </summary>
    public double AngleX
    {
        get => GetValue(AngleXProperty);
        set => SetValue(AngleXProperty, value);
    }

    /// <summary>
    /// Sets the rotation around the Y-Axis
    /// </summary>
    public double AngleY
    {
        get => GetValue(AngleYProperty);
        set => SetValue(AngleYProperty, value);
    }

    /// <summary>
    ///  Sets the rotation around the Z-Axis
    /// </summary>
    public double AngleZ
    {
        get => GetValue(AngleZProperty);
        set => SetValue(AngleZProperty, value);
    }

    /// <summary>
    ///  Moves the origin the X-Axis rotates around
    /// </summary>
    public double CenterX
    {
        get => GetValue(CenterXProperty);
        set => SetValue(CenterXProperty, value);
    }

    /// <summary>
    ///  Moves the origin the Y-Axis rotates around
    /// </summary>
    public double CenterY
    {
        get => GetValue(CenterYProperty);
        set => SetValue(CenterYProperty, value);
    }

    /// <summary>
    ///  Moves the origin the Z-Axis rotates around
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

            matrix44 *= Matrix4x4.CreateRotationX((float)Matrix.ToRadians(AngleX));
            matrix44 *= Matrix4x4.CreateRotationY((float)Matrix.ToRadians(AngleY));
            matrix44 *= Matrix4x4.CreateRotationZ((float)Matrix.ToRadians(AngleZ));

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
