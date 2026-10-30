using System.Numerics;

namespace Avalonia.Animation
{
    /// <summary>
    /// Static class for attached properties representing animatable composition targets.
    /// </summary>
    public class Composition : AvaloniaObject
    {
        /// <summary>
        /// Defines the Visible attached property.
        /// </summary>
        public readonly static AttachedProperty<bool> VisibleProperty = AvaloniaProperty.RegisterAttached<Composition, Visual, bool>("Visible", true);

        /// <summary>
        /// Defines the Opacity attached property.
        /// </summary>
        public readonly static AttachedProperty<float> OpacityProperty = AvaloniaProperty.RegisterAttached<Composition, Visual, float>("Opacity", 1f);

        /// <summary>
        /// Defines the ClipToBounds attached property.
        /// </summary>
        public readonly static AttachedProperty<bool> ClipToBoundsProperty = AvaloniaProperty.RegisterAttached<Composition, Visual, bool>("ClipToBounds", true);

        /// <summary>
        /// Defines the Offset attached property.
        /// </summary>
        public readonly static AttachedProperty<Vector3D> OffsetProperty = AvaloniaProperty.RegisterAttached<Composition, Visual, Vector3D>("Offset");

        /// <summary>
        /// Defines the Translation attached property.
        /// </summary>
        public readonly static AttachedProperty<Vector3D> TranslationProperty = AvaloniaProperty.RegisterAttached<Composition, Visual, Vector3D>("Translation");

        /// <summary>
        /// Defines the Size attached property.
        /// </summary>
        public readonly static AttachedProperty<Vector> SizeProperty = AvaloniaProperty.RegisterAttached<Composition, Visual, Vector>("Size");

        /// <summary>
        /// Defines the AnchorPoint attached property.
        /// </summary>
        public readonly static AttachedProperty<Vector> AnchorPointProperty = AvaloniaProperty.RegisterAttached<Composition, Visual, Vector>("AnchorPoint");

        /// <summary>
        /// Defines the CenterPoint attached property.
        /// </summary>
        public readonly static AttachedProperty<Vector3D> CenterPointProperty = AvaloniaProperty.RegisterAttached<Composition, Visual, Vector3D>("CenterPoint");

        /// <summary>
        /// Defines the RotationAngle attached property.
        /// </summary>
        public readonly static AttachedProperty<float> RotationAngleProperty = AvaloniaProperty.RegisterAttached<Composition, Visual, float>("RotationAngle");

        /// <summary>
        /// Defines the Orientation attached property.
        /// </summary>
        public readonly static AttachedProperty<Quaternion> OrientationProperty = AvaloniaProperty.RegisterAttached<Composition, Visual, Quaternion>("Orientation", Quaternion.Identity);

        /// <summary>
        /// Defines the Scale attached property.
        /// </summary>
        public readonly static AttachedProperty<Vector3D> ScaleProperty = AvaloniaProperty.RegisterAttached<Composition, Visual, Vector3D>("Scale", new Vector3D(1, 1, 1));

        /// <summary>
        /// Gets the opacity of the visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The opacity of the visual.</returns>
        public static float GetOpacity(Visual visual) => visual.GetValue(OpacityProperty);

        /// <summary>
        /// Sets the opacity of the visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="value">The opacity of the visual.</param>
        public static void SetOpacity(Visual visual, float value) => visual.SetValue(OpacityProperty, value);

        /// <summary>
        /// Gets a value indicating whether the visual is visible.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns><see langword="true"/> if the visual is visible; otherwise, <see langword="false"/>.</returns>
        public static bool GetVisible(Visual visual) => visual.GetValue(VisibleProperty);

        /// <summary>
        /// Sets a value indicating whether the visual is visible.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="value"><see langword="true"/> to make the visual visible; otherwise, <see langword="false"/>.</param>
        public static void SetVisible(Visual visual, bool value) => visual.SetValue(VisibleProperty, value);

        /// <summary>
        /// Gets a value indicating whether the visual clips its content to its bounds.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns><see langword="true"/> if the visual clips its content; otherwise, <see langword="false"/>.</returns>
        public static bool GetClipToBounds(Visual visual) => visual.GetValue(ClipToBoundsProperty);

        /// <summary>
        /// Sets a value indicating whether the visual clips its content to its bounds.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="value"><see langword="true"/> to clip the visual's content; otherwise, <see langword="false"/>.</param>
        public static void SetClipToBounds(Visual visual, bool value) => visual.SetValue(ClipToBoundsProperty, value);

        /// <summary>
        /// Gets the offset of the visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The offset of the visual.</returns>
        public static Vector3D GetOffset(Visual visual) => visual.GetValue(OffsetProperty);

        /// <summary>
        /// Sets the offset of the visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="value">The offset of the visual.</param>
        public static void SetOffset(Visual visual, Vector3D value) => visual.SetValue(OffsetProperty, value);

        /// <summary>
        /// Gets the translation of the visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The translation of the visual.</returns>
        public static Vector3D GetTranslation(Visual visual) => visual.GetValue(TranslationProperty);

        /// <summary>
        /// Sets the translation of the visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="value">The translation of the visual.</param>
        public static void SetTranslation(Visual visual, Vector3D value) => visual.SetValue(TranslationProperty, value);

        /// <summary>
        /// Gets the size of the visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The size of the visual.</returns>
        public static Vector GetSize(Visual visual) => visual.GetValue(SizeProperty);

        /// <summary>
        /// Sets the size of the visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="value">The size of the visual.</param>
        public static void SetSize(Visual visual, Vector value) => visual.SetValue(SizeProperty, value);

        /// <summary>
        /// Gets the anchor point of the visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The anchor point of the visual.</returns>
        public static Vector GetAnchorPoint(Visual visual) => visual.GetValue(AnchorPointProperty);

        /// <summary>
        /// Sets the anchor point of the visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="value">The anchor point of the visual.</param>
        public static void SetAnchorPoint(Visual visual, Vector value) => visual.SetValue(AnchorPointProperty, value);

        /// <summary>
        /// Gets the center point of the visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The center point of the visual.</returns>
        public static Vector3D GetCenterPoint(Visual visual) => visual.GetValue(CenterPointProperty);

        /// <summary>
        /// Sets the center point of the visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="value">The center point of the visual.</param>
        public static void SetCenterPoint(Visual visual, Vector3D value) => visual.SetValue(CenterPointProperty, value);

        /// <summary>
        /// Gets the rotation angle of the visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The rotation angle of the visual.</returns>
        public static float GetRotationAngle(Visual visual) => visual.GetValue(RotationAngleProperty);
        /// <summary>
        /// Sets the rotation angle of the visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="value">The rotation angle of the visual.</param>
        public static void SetRotationAngle(Visual visual, float value) => visual.SetValue(RotationAngleProperty, value);

        /// <summary>
        /// Gets the orientation of the visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The orientation of the visual.</returns>
        public static Quaternion GetOrientation(Visual visual) => visual.GetValue(OrientationProperty);
        /// <summary>
        /// Sets the orientation of the visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="value">The orientation of the visual.</param>
        public static void SetOrientation(Visual visual, Quaternion value) => visual.SetValue(OrientationProperty, value);

        /// <summary>
        /// Gets the scale of the visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The scale of the visual.</returns>
        public static Vector3D GetScale(Visual visual) => visual.GetValue(ScaleProperty);
        /// <summary>
        /// Sets the scale of the visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="value">The scale of the visual.</param>
        public static void SetScale(Visual visual, Vector3D value) => visual.SetValue(ScaleProperty, value);
    }
}
