using System;
using System.Numerics;

namespace Avalonia.Media;

public class Transform3D : Transform
{
            /// <summary>
            /// Defines the <see cref="RotationX"/> property.
            /// </summary>
            public static readonly StyledProperty<double> s_rotationXProperty =
                        AvaloniaProperty.Register<Transform3D, double>(nameof(RotationX));
    
            /// <summary>
            /// Defines the <see cref="RotationY"/> property.
            /// </summary>
            public static readonly StyledProperty<double> s_rotationYProperty =
                        AvaloniaProperty.Register<Transform3D, double>(nameof(RotationY));
    
            /// <summary>
            /// Defines the <see cref="RotationZ"/> property.
            /// </summary>
            public static readonly StyledProperty<double> s_rotationZProperty =
                AvaloniaProperty.Register<Transform3D, double>(nameof(RotationZ));
            
            /// <summary>
            /// Defines the <see cref="Depth"/> property.
            /// </summary>
            public static readonly StyledProperty<double> s_depthProperty =
                AvaloniaProperty.Register<Transform3D, double>(nameof(Depth));
            
            /// <summary>
            /// Defines the <see cref="CenterX"/> property.
            /// </summary>
            public static readonly StyledProperty<double> s_centerXProperty =
                AvaloniaProperty.Register<Transform3D, double>(nameof(CenterX));
            
            /// <summary>
            /// Defines the <see cref="CenterY"/> property.
            /// </summary>
            public static readonly StyledProperty<double> s_centerYProperty =
                AvaloniaProperty.Register<Transform3D, double>(nameof(CenterY));
            
            /// <summary>
            /// Defines the <see cref="CenterY"/> property.
            /// </summary>
            public static readonly StyledProperty<double> s_xProperty =
                AvaloniaProperty.Register<Transform3D, double>(nameof(X));

            
            /// <summary>
            /// Defines the <see cref="CenterY"/> property.
            /// </summary>
            public static readonly StyledProperty<double> s_yProperty =
                AvaloniaProperty.Register<Transform3D, double>(nameof(Y));

            
            /// <summary>
            /// Defines the <see cref="CenterY"/> property.
            /// </summary>
            public static readonly StyledProperty<double> s_zProperty =
                AvaloniaProperty.Register<Transform3D, double>(nameof(Z));


            /// <summary>
            /// Initializes a new instance of the <see cref="Transform3D"/> class.
            /// </summary>
            public Transform3D()
            {
                this.GetObservable(s_rotationXProperty).Subscribe(_ => RaiseChanged());
                this.GetObservable(s_rotationYProperty).Subscribe(_ => RaiseChanged());
                this.GetObservable(s_rotationZProperty).Subscribe(_ => RaiseChanged());
                this.GetObservable(s_depthProperty).Subscribe(_ => RaiseChanged());
                this.GetObservable(s_centerXProperty).Subscribe(_ => RaiseChanged());
                this.GetObservable(s_centerYProperty).Subscribe(_ => RaiseChanged());
                this.GetObservable(s_xProperty).Subscribe(_ => RaiseChanged());
                this.GetObservable(s_yProperty).Subscribe(_ => RaiseChanged());
                this.GetObservable(s_zProperty).Subscribe(_ => RaiseChanged());
            }
    
            /// <summary>
            /// Initializes a new instance of the <see cref="Transform3D"/> class.
            /// </summary>
            /// <param name="rotationX">The skew angle of X-axis, in degrees.</param>
            /// <param name="rotationY">The skew angle of Y-axis, in degrees.</param>
            /// <param name="rotationZ"></param>
            public Transform3D(
                double rotationX, 
                double rotationY, 
                double rotationZ,
                double depth,
                double centerX,
                double centerY) : this()
            {
                RotationX = rotationX;
                RotationY = rotationY;
                RotationZ = rotationZ;
                Depth = depth;
                CenterX = centerX;
                CenterY = centerY;
            }
    
            /// <summary>
            /// Gets or sets the X property.
            /// </summary>
            public double RotationX
            {
                get => GetValue(s_rotationXProperty);
                set => SetValue(s_rotationXProperty, value);
            }
    
            /// <summary>
            /// Gets or sets the Y property.
            /// </summary>
            public double RotationY
            {
                get => GetValue(s_rotationYProperty);
                set => SetValue(s_rotationYProperty, value);
            }

            public double RotationZ
            {
                get => GetValue(s_rotationZProperty);
                set => SetValue(s_rotationZProperty, value);
            }

            public double Depth
            {
                get => GetValue(s_depthProperty);
                set => SetValue(s_depthProperty, value);
            }

            public double CenterX
            {
                get => GetValue(s_centerXProperty);
                set => SetValue(s_centerXProperty, value);
            }

            public double CenterY
            {
                get => GetValue(s_centerYProperty);
                set => SetValue(s_centerYProperty, value);
            }

            public double X
            {
                get => GetValue(s_xProperty);
                set => SetValue(s_xProperty, value);
            }

            public double Y
            {
                get => GetValue(s_yProperty);
                set => SetValue(s_yProperty, value);
            }

            public double Z
            {
                get => GetValue(s_zProperty);
                set => SetValue(s_zProperty, value);
            }

            /// <summary>
            /// Gets the transform's <see cref="Matrix"/>.
            /// </summary>
            public override Matrix Value
            {
                get
                {
                    var matrix44 = Matrix4x4.Identity;
                    
                    matrix44 *= Matrix4x4.CreateTranslation((float)X, (float)Y, (float)Z);
                    
                    matrix44 *= Matrix4x4.CreateRotationX((float)Matrix.ToRadians(RotationX));
                    matrix44 *= Matrix4x4.CreateRotationY((float)Matrix.ToRadians(RotationY));
                    matrix44 *= Matrix4x4.CreateRotationZ((float)Matrix.ToRadians(RotationZ));

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
