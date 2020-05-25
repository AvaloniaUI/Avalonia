using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Layout;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines an area within which you can position and align child objects in relation
    /// to each other or the parent panel.
    /// </summary>
    /// <remarks>
    /// <para><b>Default position</b></para>
    ///    <para>By default, any unconstrained element declared as a child of the RelativePanel is given the entire
    ///    available space and positioned at the(0, 0) coordinates(upper left corner) of the panel.So, if you
    /// are positioning a second element relative to an unconstrained element, keep in mind that the second
    /// element might get pushed out of the panel.
    /// </para>
    ///<para><b>Conflicting relationships</b></para>
    ///    <para>
    ///    If you set multiple relationships that target the same edge of an element, you might have conflicting
    /// relationships in your layout as a result.When this happens, the relationships are applied in the
    ///    following order of priority:
    ///      •   Panel alignment relationships (AlignTopWithPanel, AlignLeftWithPanel, …) are applied first.
    ///      •   Sibling alignment relationships(AlignTopWith, AlignLeftWith, …) are applied second.
    ///      •   Sibling positional relationships(Above, Below, RightOf, LeftOf) are applied last.
    /// </para>
    /// <para>
    /// The panel-center alignment properties(AlignVerticalCenterWith, AlignHorizontalCenterWithPanel, ...) are
    /// typically used independently of other constraints and are applied if there is no conflict.
    ///</para>
    /// <para>
    /// The HorizontalAlignment and VerticalAlignment properties on UI elements are applied after relationship
    /// properties are evaluated and applied. These properties control the placement of the element within the
    /// available size for the element, if the desired size is smaller than the available size.
    /// </para>
    /// </remarks>
    public partial class RelativePanel : Panel
    {
        // Dependency property for storing intermediate arrange state on the children
        private static readonly StyledProperty<double[]> ArrangeStateProperty =
            AvaloniaProperty.Register<RelativePanel, double[]>("ArrangeState");

        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for
        /// child elements and determines a size for the System.Windows.FrameworkElement-derived
        /// class.</summary>
        /// <param name="availableSize">
        /// The available size that this element can give to child elements. Infinity can
        /// be specified as a value to indicate that the element will size to whatever content
        /// is available.
        /// </param>
        /// <returns>
        /// The size that this element determines it needs during layout, based on its calculations
        /// of child element sizes.
        /// </returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (var child in Children.OfType<Layoutable>())
            {
                child.Measure(availableSize);
            }
            foreach (var item in CalculateLocations(availableSize))
            {
                if (item.Item2.Size.Width < item.Item1.DesiredSize.Width || item.Item2.Size.Height < item.Item1.DesiredSize.Height)
                    item.Item1.Measure(item.Item2.Size);
            }
            return base.MeasureOverride(availableSize);
        }

        /// <summary>
        ///  When overridden in a derived class, positions child elements and determines a
        ///  size for a System.Windows.FrameworkElement derived class.
        /// </summary>
        /// <param name="finalSize">
        /// The final area within the parent that this element should use to arrange itself
        /// and its children.
        /// </param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (var item in CalculateLocations(finalSize))
                item.Item1.Arrange(item.Item2);
            return base.ArrangeOverride(finalSize);
        }

        private IEnumerable<Tuple<ILayoutable, Rect>> CalculateLocations(Size finalSize)
        {
            //List of margins for each element between the element and panel (left, top, right, bottom)
            List<double[]> arranges = new List<double[]>(Children.Count);
            //First pass aligns all sides that aren't constrained by other elements
            int arrangedCount = 0;
            foreach (var child in Children.OfType<Layoutable>())
            {
                //NaN means the arrange value is not constrained yet for that side
                double[] rect = new[] { double.NaN, double.NaN, double.NaN, double.NaN };
                arranges.Add(rect);
                child.SetValue(ArrangeStateProperty, rect);

                //Align with panels always wins, so do these first, or if no constraints are set at all set margin to 0

                //Left side
                if (GetAlignLeftWithPanel(child))
                {
                    rect[0] = 0;
                }
                else if (
                    child.GetValue(AlignLeftWithProperty) == null &&
                    child.GetValue(RightOfProperty) == null &&
                    child.GetValue(AlignHorizontalCenterWithProperty) == null &&
                    !GetAlignHorizontalCenterWithPanel(child))
                {
                    if (GetAlignRightWithPanel(child))
                        rect[0] = finalSize.Width - child.DesiredSize.Width;
                    else if (child.GetValue(AlignRightWithProperty) == null && child.GetValue(AlignHorizontalCenterWithProperty) == null && child.GetValue(LeftOfProperty) == null)
                        rect[0] = 0; //default fallback to 0
                }

                //Top side
                if (GetAlignTopWithPanel(child))
                {
                    rect[1] = 0;
                }
                else if (
                    child.GetValue(AlignTopWithProperty) == null &&
                    child.GetValue(BelowProperty) == null &&
                    child.GetValue(AlignVerticalCenterWithProperty) == null &&
                    !GetAlignVerticalCenterWithPanel(child))
                {
                    if (GetAlignBottomWithPanel(child))
                        rect[1] = finalSize.Height - child.DesiredSize.Height;
                    else if (child.GetValue(AlignBottomWithProperty) == null && child.GetValue(AlignVerticalCenterWithProperty) == null && child.GetValue(AboveProperty) == null)
                        rect[1] = 0; //default fallback to 0
                }

                //Right side
                if (GetAlignRightWithPanel(child))
                {
                    rect[2] = 0;
                }
                else if (!double.IsNaN(rect[0]) &&
                 child.GetValue(AlignRightWithProperty) == null &&
                 child.GetValue(LeftOfProperty) == null &&
                 child.GetValue(AlignHorizontalCenterWithProperty) == null &&
                 !GetAlignHorizontalCenterWithPanel(child))
                {
                    rect[2] = finalSize.Width - rect[0] - child.DesiredSize.Width;
                }

                //Bottom side
                if (GetAlignBottomWithPanel(child))
                    rect[3] = 0;
                else if (!double.IsNaN(rect[1]) &&
                    (child.GetValue(AlignBottomWithProperty) == null &&
                    child.GetValue(AboveProperty) == null) &&
                    child.GetValue(AlignVerticalCenterWithProperty) == null &&
                    !GetAlignVerticalCenterWithPanel(child))
                {
                    rect[3] = finalSize.Height - rect[1] - child.DesiredSize.Height;
                }

                if (!double.IsNaN(rect[0]) && !double.IsNaN(rect[1]) &&
                    !double.IsNaN(rect[2]) && !double.IsNaN(rect[3]))
                    arrangedCount++;
            }
            int i = 0;
            //Run iterative layout passes
            while (arrangedCount < Children.Count)
            {
                bool valueChanged = false;
                i = 0;
                foreach (var child in Children.OfType<Layoutable>())
                {
                    double[] rect = arranges[i++];

                    if (!double.IsNaN(rect[0]) && !double.IsNaN(rect[1]) &&
                        !double.IsNaN(rect[2]) && !double.IsNaN(rect[3]))
                        continue; //Control is fully arranged

                    //Calculate left side
                    if (double.IsNaN(rect[0]))
                    {
                        var alignLeftWith = GetDependencyElement(RelativePanel.AlignLeftWithProperty, child);
                        if (alignLeftWith != null)
                        {
                            double[] r = (double[])alignLeftWith.GetValue(ArrangeStateProperty);
                            if (!double.IsNaN(r[0]))
                            {
                                rect[0] = r[0];
                                valueChanged = true;
                            }
                        }
                        else
                        {
                            var rightOf = GetDependencyElement(RelativePanel.RightOfProperty, child);
                            if (rightOf != null)
                            {
                                double[] r = (double[])rightOf.GetValue(ArrangeStateProperty);
                                if (!double.IsNaN(r[2]))
                                {
                                    rect[0] = finalSize.Width - r[2];
                                    valueChanged = true;
                                }
                            }
                            else if (!double.IsNaN(rect[2]))
                            {
                                rect[0] = finalSize.Width - rect[2] - child.DesiredSize.Width;
                                valueChanged = true;
                            }
                        }
                    }
                    //Calculate top side
                    if (double.IsNaN(rect[1]))
                    {
                        var alignTopWith = GetDependencyElement(RelativePanel.AlignTopWithProperty, child);
                        if (alignTopWith != null)
                        {
                            double[] r = (double[])alignTopWith.GetValue(ArrangeStateProperty);
                            if (!double.IsNaN(r[1]))
                            {
                                rect[1] = r[1];
                                valueChanged = true;
                            }
                        }
                        else
                        {
                            var below = GetDependencyElement(RelativePanel.BelowProperty, child);
                            if (below != null)
                            {
                                double[] r = (double[])below.GetValue(ArrangeStateProperty);
                                if (!double.IsNaN(r[3]))
                                {
                                    rect[1] = finalSize.Height - r[3];
                                    valueChanged = true;
                                }
                            }
                            else if (!double.IsNaN(rect[3]))
                            {
                                rect[1] = finalSize.Height - rect[3] - child.DesiredSize.Height;
                                valueChanged = true;
                            }
                        }
                    }
                    //Calculate right side
                    if (double.IsNaN(rect[2]))
                    {
                        var alignRightWith = GetDependencyElement(RelativePanel.AlignRightWithProperty, child);
                        if (alignRightWith != null)
                        {
                            double[] r = (double[])alignRightWith.GetValue(ArrangeStateProperty);
                            if (!double.IsNaN(r[2]))
                            {
                                rect[2] = r[2];
                                if (double.IsNaN(rect[0]))
                                {
                                    if (child.GetValue(RelativePanel.AlignLeftWithProperty) == null)
                                    {
                                        rect[0] = rect[2] + child.DesiredSize.Width;
                                        valueChanged = true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            var leftOf = GetDependencyElement(RelativePanel.LeftOfProperty, child);
                            if (leftOf != null)
                            {
                                double[] r = (double[])leftOf.GetValue(ArrangeStateProperty);
                                if (!double.IsNaN(r[0]))
                                {
                                    rect[2] = finalSize.Width - r[0];
                                    valueChanged = true;
                                }
                            }
                            else if (!double.IsNaN(rect[0]))
                            {
                                rect[2] = finalSize.Width - rect[0] - child.DesiredSize.Width;
                                valueChanged = true;
                            }
                        }
                    }
                    //Calculate bottom side
                    if (double.IsNaN(rect[3]))
                    {
                        var alignBottomWith = GetDependencyElement(RelativePanel.AlignBottomWithProperty, child);
                        if (alignBottomWith != null)
                        {
                            double[] r = (double[])alignBottomWith.GetValue(ArrangeStateProperty);
                            if (!double.IsNaN(r[3]))
                            {
                                rect[3] = r[3];
                                valueChanged = true;
                                if (double.IsNaN(rect[1]))
                                {
                                    if (child.GetValue(RelativePanel.AlignTopWithProperty) == null)
                                    {
                                        rect[1] = finalSize.Height - rect[3] - child.DesiredSize.Height;
                                    }
                                }
                            }
                        }
                        else
                        {
                            var above = GetDependencyElement(RelativePanel.AboveProperty, child);
                            if (above != null)
                            {
                                double[] r = (double[])above.GetValue(ArrangeStateProperty);
                                if (!double.IsNaN(r[1]))
                                {
                                    rect[3] = finalSize.Height - r[1];
                                    valueChanged = true;
                                }
                            }
                            else if (!double.IsNaN(rect[1]))
                            {
                                rect[3] = finalSize.Height - rect[1] - child.DesiredSize.Height;
                                valueChanged = true;
                            }
                        }
                    }
                    //Calculate horizontal alignment
                    if (double.IsNaN(rect[0]) && double.IsNaN(rect[2]))
                    {
                        var alignHorizontalCenterWith = GetDependencyElement(RelativePanel.AlignHorizontalCenterWithProperty, child);
                        if (alignHorizontalCenterWith != null)
                        {
                            double[] r = (double[])alignHorizontalCenterWith.GetValue(ArrangeStateProperty);
                            if (!double.IsNaN(r[0]) && !double.IsNaN(r[2]))
                            {
                                rect[0] = r[0] + (finalSize.Width - r[2] - r[0]) * .5 - child.DesiredSize.Width * .5;
                                rect[2] = finalSize.Width - rect[0] - child.DesiredSize.Width;
                                valueChanged = true;
                            }
                        }
                        else
                        {
                            if (GetAlignHorizontalCenterWithPanel(child))
                            {
                                var roomToSpare = finalSize.Width - child.DesiredSize.Width;
                                rect[0] = roomToSpare * .5;
                                rect[2] = roomToSpare * .5;
                                valueChanged = true;
                            }
                        }
                    }

                    //Calculate vertical alignment
                    if (double.IsNaN(rect[1]) && double.IsNaN(rect[3]))
                    {
                        var alignVerticalCenterWith = GetDependencyElement(RelativePanel.AlignVerticalCenterWithProperty, child);
                        if (alignVerticalCenterWith != null)
                        {
                            double[] r = (double[])alignVerticalCenterWith.GetValue(ArrangeStateProperty);
                            if (!double.IsNaN(r[1]) && !double.IsNaN(r[3]))
                            {
                                rect[1] = r[1] + (finalSize.Height - r[3] - r[1]) * .5 - child.DesiredSize.Height * .5;
                                rect[3] = finalSize.Height - rect[1] - child.DesiredSize.Height;
                                valueChanged = true;
                            }
                        }
                        else
                        {
                            if (GetAlignVerticalCenterWithPanel(child))
                            {
                                var roomToSpare = finalSize.Height - child.DesiredSize.Height;
                                rect[1] = roomToSpare * .5;
                                rect[3] = roomToSpare * .5;
                                valueChanged = true;
                            }
                        }
                    }


                    //if panel is now fully arranged, increase the counter
                    if (!double.IsNaN(rect[0]) && !double.IsNaN(rect[1]) &&
                        !double.IsNaN(rect[2]) && !double.IsNaN(rect[3]))
                        arrangedCount++;
                }
                if (!valueChanged)
                {
                    //If a layout pass didn't increase number of arranged elements,
                    //there must be a circular dependency
                    throw new ArgumentException("RelativePanel error: Circular dependency detected. Layout could not complete");
                }
            }

            i = 0;
            //Arrange iterations complete - Apply the results to the child elements
            foreach (var child in Children.OfType<ILayoutable>())
            {
                double[] rect = arranges[i++];
                //Measure child again with the new calculated available size
                //this helps for instance textblocks to reflow the text wrapping
                //We should probably have done this during the measure step but it would cause a more expensive
                //measure+arrange layout cycle
                //if(child is TextBlock)
                //    child.Measure(new Size(Math.Max(0, finalSize.Width - rect[2] - rect[0]), Math.Max(0, finalSize.Height - rect[3] - rect[1])));

                //if(child is TextBlock tb)
                //{
                //    tb.ArrangeOverride(new Rect(rect[0], rect[1], Math.Max(0, finalSize.Width - rect[2] - rect[0]), Math.Max(0, finalSize.Height - rect[3] - rect[1])));
                //}
                //else 
                yield return new Tuple<ILayoutable, Rect>(child, new Rect(rect[0], rect[1], Math.Max(0, finalSize.Width - rect[2] - rect[0]), Math.Max(0, finalSize.Height - rect[3] - rect[1])));
            }
        }

        //Gets the element that's referred to in the alignment attached properties
        private Layoutable GetDependencyElement(AvaloniaProperty property, AvaloniaObject child)
        {
            var dependency = child.GetValue(property);
            if (dependency == null)
                return null;
            if (dependency is Layoutable)
            {
                if (Children.Contains((ILayoutable)dependency))
                    return (Layoutable)dependency;
                throw new ArgumentException(string.Format("RelativePanel error: Element does not exist in the current context", property.Name));
            }

            throw new ArgumentException("RelativePanel error: Value must be of type ILayoutable");
        }
    }
}
