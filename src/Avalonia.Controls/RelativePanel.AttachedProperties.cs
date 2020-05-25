using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Controls
{
    public partial class RelativePanel
    {
        private static void OnAlignPropertiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var elm = d as FrameworkElement;
            if (elm.Parent is FrameworkElement)
                ((FrameworkElement)elm.Parent).InvalidateArrange();
        }

        /// <summary>
        /// Gets the value of the RelativePanel.Above XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.Above XAML attached property value of the specified object.
        /// (The element to position this element above.)
        /// </returns>
        [TypeConverter(typeof(NameReferenceConverter))]
        public static object GetAbove(DependencyObject obj)
        {
            return (object)obj.GetValue(AboveProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.Above XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to position this element above.)</param>
        public static void SetAbove(DependencyObject obj, object value)
        {
            obj.SetValue(AboveProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AboveProperty"/> XAML attached property.
        /// </summary>
        public static readonly DependencyProperty AboveProperty =
            DependencyProperty.RegisterAttached("Above", typeof(object), typeof(RelativePanel), new PropertyMetadata(null, OnAlignPropertiesChanged));


        /// <summary>
        /// Gets the value of the RelativePanel.AlignBottomWithPanel XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignBottomWithPanel XAML attached property value of the specified
        ///    object. (true to align this element's bottom edge with the panel's bottom edge;
        /// otherwise, false.)
        /// </returns>
        public static bool GetAlignBottomWithPanel(DependencyObject obj)
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
        public static void SetAlignBottomWithPanel(DependencyObject obj, bool value)
        {
            obj.SetValue(AlignBottomWithPanelProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignBottomWithPanelProperty"/> XAML attached property.
        /// </summary>
        public static readonly DependencyProperty AlignBottomWithPanelProperty =
            DependencyProperty.RegisterAttached("AlignBottomWithPanel", typeof(bool), typeof(RelativePanel), new PropertyMetadata(false, OnAlignPropertiesChanged));

        /// <summary>
        /// Gets the value of the RelativePanel.AlignBottomWith XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignBottomWith XAML attached property value of the specified object.
        /// (The element to align this element's bottom edge with.)
        /// </returns>
        [TypeConverter(typeof(NameReferenceConverter))]
        public static object GetAlignBottomWith(DependencyObject obj)
        {
            return (object)obj.GetValue(AlignBottomWithProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.Above XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to align this element's bottom edge with.)</param>
        public static void SetAlignBottomWith(DependencyObject obj, object value)
        {
            obj.SetValue(AlignBottomWithProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignBottomWithProperty"/> XAML attached property.
        /// </summary>
        public static readonly DependencyProperty AlignBottomWithProperty =
            DependencyProperty.RegisterAttached("AlignBottomWith", typeof(object), typeof(RelativePanel), new PropertyMetadata(null, OnAlignPropertiesChanged));

        /// <summary>
        /// Gets the value of the RelativePanel.AlignHorizontalCenterWithPanel XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignHorizontalCenterWithPanel XAML attached property value
        /// of the specified object. (true to horizontally center this element in the panel;
        /// otherwise, false.)
        /// </returns>
        public static bool GetAlignHorizontalCenterWithPanel(DependencyObject obj)
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
        public static void SetAlignHorizontalCenterWithPanel(DependencyObject obj, bool value)
        {
            obj.SetValue(AlignHorizontalCenterWithPanelProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignHorizontalCenterWithPanelProperty"/> XAML attached property.
        /// </summary>
        public static readonly DependencyProperty AlignHorizontalCenterWithPanelProperty =
            DependencyProperty.RegisterAttached("AlignHorizontalCenterWithPanel", typeof(bool), typeof(RelativePanel), new PropertyMetadata(false, OnAlignPropertiesChanged));

        /// <summary>
        /// Gets the value of the RelativePanel.AlignHorizontalCenterWith XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignHorizontalCenterWith XAML attached property value of the
        /// specified object. (The element to align this element's horizontal center with.)
        /// </returns>
        [TypeConverter(typeof(NameReferenceConverter))]
        public static object GetAlignHorizontalCenterWith(DependencyObject obj)
        {
            return (object)obj.GetValue(AlignHorizontalCenterWithProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.Above XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to align this element's horizontal center with.)</param>
        public static void SetAlignHorizontalCenterWith(DependencyObject obj, object value)
        {
            obj.SetValue(AlignHorizontalCenterWithProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignHorizontalCenterWithProperty"/> XAML attached property.
        /// </summary>
        public static readonly DependencyProperty AlignHorizontalCenterWithProperty =
            DependencyProperty.RegisterAttached("AlignHorizontalCenterWith", typeof(object), typeof(RelativePanel), new PropertyMetadata(null, OnAlignPropertiesChanged));

        /// <summary>
        /// Gets the value of the RelativePanel.AlignLeftWithPanel XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignLeftWithPanel XAML attached property value of the specified
        /// object. (true to align this element's left edge with the panel's left edge; otherwise,
        /// false.)
        /// </returns>
        public static bool GetAlignLeftWithPanel(DependencyObject obj)
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
        public static void SetAlignLeftWithPanel(DependencyObject obj, bool value)
        {
            obj.SetValue(AlignLeftWithPanelProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignLeftWithPanelProperty"/> XAML attached property.
        /// </summary>
        public static readonly DependencyProperty AlignLeftWithPanelProperty =
            DependencyProperty.RegisterAttached("AlignLeftWithPanel", typeof(bool), typeof(RelativePanel), new PropertyMetadata(false, OnAlignPropertiesChanged));


        /// <summary>
        /// Gets the value of the RelativePanel.AlignLeftWith XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignLeftWith XAML attached property value of the specified
        /// object. (The element to align this element's left edge with.)
        /// </returns>
        [TypeConverter(typeof(NameReferenceConverter))]
        public static object GetAlignLeftWith(DependencyObject obj)
        {
            return (object)obj.GetValue(AlignLeftWithProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.Above XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to align this element's left edge with.)</param>
        public static void SetAlignLeftWith(DependencyObject obj, object value)
        {
            obj.SetValue(AlignLeftWithProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignLeftWithProperty"/> XAML attached property.
        /// </summary>
        public static readonly DependencyProperty AlignLeftWithProperty =
            DependencyProperty.RegisterAttached("AlignLeftWith", typeof(object), typeof(RelativePanel), new PropertyMetadata(null, OnAlignPropertiesChanged));


        /// <summary>
        /// Gets the value of the RelativePanel.AlignRightWithPanel XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignRightWithPanel XAML attached property value of the specified
        /// object. (true to align this element's right edge with the panel's right edge;
        /// otherwise, false.)
        /// </returns>
        public static bool GetAlignRightWithPanel(DependencyObject obj)
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
        public static void SetAlignRightWithPanel(DependencyObject obj, bool value)
        {
            obj.SetValue(AlignRightWithPanelProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignRightWithPanelProperty"/> XAML attached property.
        /// </summary>
        public static readonly DependencyProperty AlignRightWithPanelProperty =
            DependencyProperty.RegisterAttached("AlignRightWithPanel", typeof(bool), typeof(RelativePanel), new PropertyMetadata(false, OnAlignPropertiesChanged));

        /// <summary>
        /// Gets the value of the RelativePanel.AlignRightWith XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignRightWith XAML attached property value of the specified
        /// object. (The element to align this element's right edge with.)
        /// </returns>
        [TypeConverter(typeof(NameReferenceConverter))]
        public static object GetAlignRightWith(DependencyObject obj)
        {
            return (object)obj.GetValue(AlignRightWithProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.AlignRightWith XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to align this element's right edge with.)</param>
        public static void SetAlignRightWith(DependencyObject obj, object value)
        {
            obj.SetValue(AlignRightWithProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignRightWithProperty"/> XAML attached property.
        /// </summary>
        public static readonly DependencyProperty AlignRightWithProperty =
            DependencyProperty.RegisterAttached("AlignRightWith", typeof(object), typeof(RelativePanel), new PropertyMetadata(null, OnAlignPropertiesChanged));

        /// <summary>
        /// Gets the value of the RelativePanel.AlignTopWithPanel XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignTopWithPanel XAML attached property value of the specified
        /// object. (true to align this element's top edge with the panel's top edge; otherwise,
        /// false.)
        /// </returns>
        public static bool GetAlignTopWithPanel(DependencyObject obj)
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
        public static void SetAlignTopWithPanel(DependencyObject obj, bool value)
        {
            obj.SetValue(AlignTopWithPanelProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignTopWithPanelProperty"/> XAML attached property.
        /// </summary>
        public static readonly DependencyProperty AlignTopWithPanelProperty =
            DependencyProperty.RegisterAttached("AlignTopWithPanel", typeof(bool), typeof(RelativePanel), new PropertyMetadata(false, OnAlignPropertiesChanged));

        /// <summary>
        /// Gets the value of the RelativePanel.AlignTopWith XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>The value to set. (The element to align this element's top edge with.)</returns>
        [TypeConverter(typeof(NameReferenceConverter))]
        public static object GetAlignTopWith(DependencyObject obj)
        {
            return (object)obj.GetValue(AlignTopWithProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.AlignTopWith XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to align this element's top edge with.)</param>
        public static void SetAlignTopWith(DependencyObject obj, object value)
        {
            obj.SetValue(AlignTopWithProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignTopWithProperty"/> XAML attached property.
        /// </summary>
        public static readonly DependencyProperty AlignTopWithProperty =
            DependencyProperty.RegisterAttached("AlignTopWith", typeof(object), typeof(RelativePanel), new PropertyMetadata(null, OnAlignPropertiesChanged));

        /// <summary>
        /// Gets the value of the RelativePanel.AlignVerticalCenterWithPanel XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignVerticalCenterWithPanel XAML attached property value of
        /// the specified object. (true to vertically center this element in the panel; otherwise,
        /// false.)
        /// </returns>
        public static bool GetAlignVerticalCenterWithPanel(DependencyObject obj)
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
        public static void SetAlignVerticalCenterWithPanel(DependencyObject obj, bool value)
        {
            obj.SetValue(AlignVerticalCenterWithPanelProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignVerticalCenterWithPanelProperty"/> XAML attached property.
        /// </summary>
        public static readonly DependencyProperty AlignVerticalCenterWithPanelProperty =
            DependencyProperty.RegisterAttached("AlignVerticalCenterWithPanel", typeof(bool), typeof(RelativePanel), new PropertyMetadata(false, OnAlignPropertiesChanged));

        /// <summary>
        /// Gets the value of the RelativePanel.AlignVerticalCenterWith XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>The value to set. (The element to align this element's vertical center with.)</returns>
        [TypeConverter(typeof(NameReferenceConverter))]
        public static object GetAlignVerticalCenterWith(DependencyObject obj)
        {
            return (object)obj.GetValue(AlignVerticalCenterWithProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.AlignVerticalCenterWith XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to align this element's horizontal center with.)</param>
        public static void SetAlignVerticalCenterWith(DependencyObject obj, object value)
        {
            obj.SetValue(AlignVerticalCenterWithProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignVerticalCenterWithProperty"/> XAML attached property.
        /// </summary>
        public static readonly DependencyProperty AlignVerticalCenterWithProperty =
            DependencyProperty.RegisterAttached("AlignVerticalCenterWith", typeof(object), typeof(RelativePanel), new PropertyMetadata(null, OnAlignPropertiesChanged));

        /// <summary>
        /// Gets the value of the RelativePanel.Below XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.Below XAML attached property value of the specified object.
        /// (The element to position this element below.)                                
        /// </returns>
        [TypeConverter(typeof(NameReferenceConverter))]
        public static object GetBelow(DependencyObject obj)
        {
            return (object)obj.GetValue(BelowProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.Above XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to position this element below.)</param>
        public static void SetBelow(DependencyObject obj, object value)
        {
            obj.SetValue(BelowProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.BelowProperty"/> XAML attached property.
        /// </summary>
        public static readonly DependencyProperty BelowProperty =
            DependencyProperty.RegisterAttached("Below", typeof(object), typeof(RelativePanel), new PropertyMetadata(null, OnAlignPropertiesChanged));

        /// <summary>
        /// Gets the value of the RelativePanel.LeftOf XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.LeftOf XAML attached property value of the specified object.
        /// (The element to position this element to the left of.)                                 
        /// </returns>
        [TypeConverter(typeof(NameReferenceConverter))]
        public static object GetLeftOf(DependencyObject obj)
        {
            return (object)obj.GetValue(LeftOfProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.LeftOf XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to position this element to the left of.)</param>
        public static void SetLeftOf(DependencyObject obj, object value)
        {
            obj.SetValue(LeftOfProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.LeftOfProperty"/> XAML attached property.
        /// </summary>
        public static readonly DependencyProperty LeftOfProperty =
            DependencyProperty.RegisterAttached("LeftOf", typeof(object), typeof(RelativePanel), new PropertyMetadata(null, OnAlignPropertiesChanged));

        /// <summary>
        /// Gets the value of the RelativePanel.RightOf XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.RightOf XAML attached property value of the specified object.
        /// (The element to position this element to the right of.)                                   
        /// </returns>
        [TypeConverter(typeof(NameReferenceConverter))]
        public static object GetRightOf(DependencyObject obj)
        {
            return (object)obj.GetValue(RightOfProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.RightOf XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to position this element to the right of.)</param>
        public static void SetRightOf(DependencyObject obj, object value)
        {
            obj.SetValue(RightOfProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.RightOfProperty"/> XAML attached property.
        /// </summary>
        public static readonly DependencyProperty RightOfProperty =
            DependencyProperty.RegisterAttached("RightOf", typeof(object), typeof(RelativePanel), new PropertyMetadata(null, OnAlignPropertiesChanged));
    }
}
