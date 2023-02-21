using Avalonia.Layout;
using Avalonia.Threading;

namespace Avalonia.Controls
{
    public partial class RelativePanel
    {

        static RelativePanel()
        {
            ClipToBoundsProperty.OverrideDefaultValue<RelativePanel>(true);

            AffectsParentArrange<RelativePanel>(
                AlignLeftWithPanelProperty, AlignLeftWithProperty, LeftOfProperty,
                AlignRightWithPanelProperty, AlignRightWithProperty, RightOfProperty,
                AlignTopWithPanelProperty, AlignTopWithProperty, AboveProperty,
                AlignBottomWithPanelProperty, AlignBottomWithProperty, BelowProperty,
                AlignHorizontalCenterWithPanelProperty, AlignHorizontalCenterWithProperty,
                AlignVerticalCenterWithPanelProperty, AlignVerticalCenterWithProperty);
            
            AffectsParentMeasure<RelativePanel>(
                AlignLeftWithPanelProperty, AlignLeftWithProperty, LeftOfProperty,
                AlignRightWithPanelProperty, AlignRightWithProperty, RightOfProperty,
                AlignTopWithPanelProperty, AlignTopWithProperty, AboveProperty,
                AlignBottomWithPanelProperty, AlignBottomWithProperty, BelowProperty,
                AlignHorizontalCenterWithPanelProperty, AlignHorizontalCenterWithProperty,
                AlignVerticalCenterWithPanelProperty, AlignVerticalCenterWithProperty);
        }

        /// <summary>
        /// Gets the value of the RelativePanel.Above XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.Above XAML attached property value of the specified object.
        /// (The element to position this element above.)
        /// </returns>        
        [ResolveByName]
        public static object GetAbove(AvaloniaObject obj)
        {
            return (object)obj.GetValue(AboveProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.Above XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to position this element above.)</param>
        [ResolveByName]
        public static void SetAbove(AvaloniaObject obj, object value)
        {
            obj.SetValue(AboveProperty, value);
        }


        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AboveProperty"/> XAML attached property.
        /// </summary>        

        public static readonly AttachedProperty<object> AboveProperty =
            AvaloniaProperty.RegisterAttached<Layoutable, object>("Above", typeof(RelativePanel));


        /// <summary>
        /// Gets the value of the RelativePanel.AlignBottomWithPanel XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignBottomWithPanel XAML attached property value of the specified
        ///    object. (true to align this element's bottom edge with the panel's bottom edge;
        /// otherwise, false.)
        /// </returns>
        public static bool GetAlignBottomWithPanel(AvaloniaObject obj)
        {
            return (bool)obj.GetValue(AlignBottomWithPanelProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.Above XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">
        /// The value to set. (true to align this element's bottom edge with the panel's
        /// bottom edge; otherwise, false.)
        /// </param>
        public static void SetAlignBottomWithPanel(AvaloniaObject obj, bool value)
        {
            obj.SetValue(AlignBottomWithPanelProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignBottomWithPanelProperty"/> XAML attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> AlignBottomWithPanelProperty =
            AvaloniaProperty.RegisterAttached<Layoutable, bool>("AlignBottomWithPanel", typeof(RelativePanel));

        /// <summary>
        /// Gets the value of the RelativePanel.AlignBottomWith XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignBottomWith XAML attached property value of the specified object.
        /// (The element to align this element's bottom edge with.)
        /// </returns>        
        [ResolveByName]
        public static object GetAlignBottomWith(AvaloniaObject obj)
        {
            return (object)obj.GetValue(AlignBottomWithProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.Above XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to align this element's bottom edge with.)</param>
        [ResolveByName]
        public static void SetAlignBottomWith(AvaloniaObject obj, object value)
        {
            obj.SetValue(AlignBottomWithProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignBottomWithProperty"/> XAML attached property.
        /// </summary>

        public static readonly AttachedProperty<object> AlignBottomWithProperty =
            AvaloniaProperty.RegisterAttached<Layoutable, object>("AlignBottomWith", typeof(RelativePanel));

        /// <summary>
        /// Gets the value of the RelativePanel.AlignHorizontalCenterWithPanel XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignHorizontalCenterWithPanel XAML attached property value
        /// of the specified object. (true to horizontally center this element in the panel;
        /// otherwise, false.)
        /// </returns>
        public static bool GetAlignHorizontalCenterWithPanel(AvaloniaObject obj)
        {
            return (bool)obj.GetValue(AlignHorizontalCenterWithPanelProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.Above XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">
        /// The value to set. (true to horizontally center this element in the panel; otherwise,
        /// false.)
        /// </param>
        public static void SetAlignHorizontalCenterWithPanel(AvaloniaObject obj, bool value)
        {
            obj.SetValue(AlignHorizontalCenterWithPanelProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignHorizontalCenterWithPanelProperty"/> XAML attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> AlignHorizontalCenterWithPanelProperty =
            AvaloniaProperty.RegisterAttached<Layoutable, bool>("AlignHorizontalCenterWithPanel", typeof(RelativePanel), false);

        /// <summary>
        /// Gets the value of the RelativePanel.AlignHorizontalCenterWith XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignHorizontalCenterWith XAML attached property value of the
        /// specified object. (The element to align this element's horizontal center with.)
        /// </returns>        
        [ResolveByName]
        public static object GetAlignHorizontalCenterWith(AvaloniaObject obj)
        {
            return (object)obj.GetValue(AlignHorizontalCenterWithProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.Above XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to align this element's horizontal center with.)</param>
        [ResolveByName]
        public static void SetAlignHorizontalCenterWith(AvaloniaObject obj, object value)
        {
            obj.SetValue(AlignHorizontalCenterWithProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignHorizontalCenterWithProperty"/> XAML attached property.
        /// </summary>

        public static readonly AttachedProperty<object> AlignHorizontalCenterWithProperty =
            AvaloniaProperty.RegisterAttached<Layoutable, object>("AlignHorizontalCenterWith", typeof(object), typeof(RelativePanel));

        /// <summary>
        /// Gets the value of the RelativePanel.AlignLeftWithPanel XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignLeftWithPanel XAML attached property value of the specified
        /// object. (true to align this element's left edge with the panel's left edge; otherwise,
        /// false.)
        /// </returns>
        public static bool GetAlignLeftWithPanel(AvaloniaObject obj)
        {
            return (bool)obj.GetValue(AlignLeftWithPanelProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.Above XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">
        ///  The value to set. (true to align this element's left edge with the panel's left
        ///  edge; otherwise, false.)
        /// </param>
        public static void SetAlignLeftWithPanel(AvaloniaObject obj, bool value)
        {
            obj.SetValue(AlignLeftWithPanelProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignLeftWithPanelProperty"/> XAML attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> AlignLeftWithPanelProperty =
            AvaloniaProperty.RegisterAttached<Layoutable, bool>("AlignLeftWithPanel", typeof(RelativePanel), false);


        /// <summary>
        /// Gets the value of the RelativePanel.AlignLeftWith XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignLeftWith XAML attached property value of the specified
        /// object. (The element to align this element's left edge with.)
        /// </returns>        
        [ResolveByName]
        public static object GetAlignLeftWith(AvaloniaObject obj)
        {
            return (object)obj.GetValue(AlignLeftWithProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.Above XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to align this element's left edge with.)</param>
        [ResolveByName]
        public static void SetAlignLeftWith(AvaloniaObject obj, object value)
        {
            obj.SetValue(AlignLeftWithProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignLeftWithProperty"/> XAML attached property.
        /// </summary>

        public static readonly AttachedProperty<object> AlignLeftWithProperty =
            AvaloniaProperty.RegisterAttached<RelativePanel, Layoutable, object>("AlignLeftWith");


        /// <summary>
        /// Gets the value of the RelativePanel.AlignRightWithPanel XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignRightWithPanel XAML attached property value of the specified
        /// object. (true to align this element's right edge with the panel's right edge;
        /// otherwise, false.)
        /// </returns>
        public static bool GetAlignRightWithPanel(AvaloniaObject obj)
        {
            return (bool)obj.GetValue(AlignRightWithPanelProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.Above XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">
        /// The value to set. (true to align this element's right edge with the panel's right
        /// edge; otherwise, false.)
        /// </param>
        public static void SetAlignRightWithPanel(AvaloniaObject obj, bool value)
        {
            obj.SetValue(AlignRightWithPanelProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignRightWithPanelProperty"/> XAML attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> AlignRightWithPanelProperty =
            AvaloniaProperty.RegisterAttached<RelativePanel, Layoutable, bool>("AlignRightWithPanel", false);

        /// <summary>
        /// Gets the value of the RelativePanel.AlignRightWith XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignRightWith XAML attached property value of the specified
        /// object. (The element to align this element's right edge with.)
        /// </returns>        
        [ResolveByName]
        public static object GetAlignRightWith(AvaloniaObject obj)
        {
            return (object)obj.GetValue(AlignRightWithProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.AlignRightWith XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to align this element's right edge with.)</param>
        [ResolveByName]
        public static void SetAlignRightWith(AvaloniaObject obj, object value)
        {
            obj.SetValue(AlignRightWithProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignRightWithProperty"/> XAML attached property.
        /// </summary>

        public static readonly AttachedProperty<object> AlignRightWithProperty =
            AvaloniaProperty.RegisterAttached<RelativePanel, Layoutable, object>("AlignRightWith");

        /// <summary>
        /// Gets the value of the RelativePanel.AlignTopWithPanel XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignTopWithPanel XAML attached property value of the specified
        /// object. (true to align this element's top edge with the panel's top edge; otherwise,
        /// false.)
        /// </returns>
        public static bool GetAlignTopWithPanel(AvaloniaObject obj)
        {
            return (bool)obj.GetValue(AlignTopWithPanelProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.AlignTopWithPanel XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">
        /// The value to set. (true to align this element's top edge with the panel's top
        /// edge; otherwise, false.)
        /// </param>
        public static void SetAlignTopWithPanel(AvaloniaObject obj, bool value)
        {
            obj.SetValue(AlignTopWithPanelProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignTopWithPanelProperty"/> XAML attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> AlignTopWithPanelProperty =
            AvaloniaProperty.RegisterAttached<RelativePanel, Layoutable, bool>("AlignTopWithPanel", false);

        /// <summary>
        /// Gets the value of the RelativePanel.AlignTopWith XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>The value to set. (The element to align this element's top edge with.)</returns>        
        [ResolveByName]
        public static object GetAlignTopWith(AvaloniaObject obj)
        {
            return (object)obj.GetValue(AlignTopWithProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.AlignTopWith XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to align this element's top edge with.)</param>
        [ResolveByName]
        public static void SetAlignTopWith(AvaloniaObject obj, object value)
        {
            obj.SetValue(AlignTopWithProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignTopWithProperty"/> XAML attached property.
        /// </summary>

        public static readonly AttachedProperty<object> AlignTopWithProperty =
            AvaloniaProperty.RegisterAttached<RelativePanel, Layoutable, object>("AlignTopWith");

        /// <summary>
        /// Gets the value of the RelativePanel.AlignVerticalCenterWithPanel XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignVerticalCenterWithPanel XAML attached property value of
        /// the specified object. (true to vertically center this element in the panel; otherwise,
        /// false.)
        /// </returns>
        public static bool GetAlignVerticalCenterWithPanel(AvaloniaObject obj)
        {
            return (bool)obj.GetValue(AlignVerticalCenterWithPanelProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.AlignVerticalCenterWithPanel XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">
        /// The value to set. (true to vertically center this element in the panel; otherwise,
        /// false.)
        /// </param>
        public static void SetAlignVerticalCenterWithPanel(AvaloniaObject obj, bool value)
        {
            obj.SetValue(AlignVerticalCenterWithPanelProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignVerticalCenterWithPanelProperty"/> XAML attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> AlignVerticalCenterWithPanelProperty =
            AvaloniaProperty.RegisterAttached<RelativePanel, Layoutable, bool>("AlignVerticalCenterWithPanel", false);

        /// <summary>
        /// Gets the value of the RelativePanel.AlignVerticalCenterWith XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>The value to set. (The element to align this element's vertical center with.)</returns>        
        [ResolveByName]
        public static object GetAlignVerticalCenterWith(AvaloniaObject obj)
        {
            return (object)obj.GetValue(AlignVerticalCenterWithProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.AlignVerticalCenterWith XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to align this element's horizontal center with.)</param>        
        [ResolveByName]
        public static void SetAlignVerticalCenterWith(AvaloniaObject obj, object value)
        {
            obj.SetValue(AlignVerticalCenterWithProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignVerticalCenterWithProperty"/> XAML attached property.
        /// </summary>
        public static readonly AttachedProperty<object> AlignVerticalCenterWithProperty =
            AvaloniaProperty.RegisterAttached<RelativePanel, Layoutable, object>("AlignVerticalCenterWith");

        /// <summary>
        /// Gets the value of the RelativePanel.Below XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.Below XAML attached property value of the specified object.
        /// (The element to position this element below.)                                
        /// </returns>       
        [ResolveByName]
        public static object GetBelow(AvaloniaObject obj)
        {
            return (object)obj.GetValue(BelowProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.Above XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to position this element below.)</param>
        [ResolveByName]
        public static void SetBelow(AvaloniaObject obj, object value)
        {
            obj.SetValue(BelowProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.BelowProperty"/> XAML attached property.
        /// </summary>

        public static readonly AttachedProperty<object> BelowProperty =
            AvaloniaProperty.RegisterAttached<RelativePanel, Layoutable, object>("Below");

        /// <summary>
        /// Gets the value of the RelativePanel.LeftOf XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.LeftOf XAML attached property value of the specified object.
        /// (The element to position this element to the left of.)                                 
        /// </returns>        
        [ResolveByName]
        public static object GetLeftOf(AvaloniaObject obj)
        {
            return (object)obj.GetValue(LeftOfProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.LeftOf XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to position this element to the left of.)</param>
        [ResolveByName]
        public static void SetLeftOf(AvaloniaObject obj, object value)
        {
            obj.SetValue(LeftOfProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.LeftOfProperty"/> XAML attached property.
        /// </summary>

        public static readonly AttachedProperty<object> LeftOfProperty =
            AvaloniaProperty.RegisterAttached<RelativePanel, Layoutable, object>("LeftOf");

        /// <summary>
        /// Gets the value of the RelativePanel.RightOf XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.RightOf XAML attached property value of the specified object.
        /// (The element to position this element to the right of.)                                   
        /// </returns>        
        [ResolveByName]
        public static object GetRightOf(AvaloniaObject obj)
        {
            return (object)obj.GetValue(RightOfProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.RightOf XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to position this element to the right of.)</param>
        [ResolveByName]
        public static void SetRightOf(AvaloniaObject obj, object value)
        {
            obj.SetValue(RightOfProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.RightOfProperty"/> XAML attached property.
        /// </summary>

        public static readonly AttachedProperty<object> RightOfProperty =
            AvaloniaProperty.RegisterAttached<RelativePanel, Layoutable, object>("RightOf");
    }
}
