using System;

using Avalonia.Layout;

namespace Avalonia.Controls
{
    public static class Flex
    {
        /// <summary>
        /// Defines an attached property to control the cross-axis alignment of a specific child in a flex layout.
        /// </summary>
        public static readonly AttachedProperty<AlignItems?> AlignSelfProperty =
            AvaloniaProperty.RegisterAttached<Layoutable, AlignItems?>("AlignSelf", typeof(Flex));

        /// <summary>
        /// Defines an attached property to control the order of a specific child in a flex layout.
        /// </summary>
        public static readonly AttachedProperty<int> OrderProperty =
            AvaloniaProperty.RegisterAttached<Layoutable, int>("Order", typeof(Flex));

        /// <summary>
        /// Defines an attached property to control the initial main-axis size of a specific child in a flex layout.
        /// </summary>
        public static readonly AttachedProperty<FlexBasis> BasisProperty =
            AvaloniaProperty.RegisterAttached<Layoutable, FlexBasis>("Basis", typeof(Flex), FlexBasis.Auto);

        /// <summary>
        /// Defines an attached property to control the factor by which a specific child can shrink
        /// along the main-axis in a flex layout.
        /// </summary>
        public static readonly AttachedProperty<double> ShrinkProperty =
            AvaloniaProperty.RegisterAttached<Layoutable, double>("Shrink", typeof(Flex), 1.0, validate: v => v >= 0.0);

        /// <summary>
        /// Defines an attached property to control the factor by which a specific child can grow
        /// along the main-axis in a flex layout.
        /// </summary>
        public static readonly AttachedProperty<double> GrowProperty =
            AvaloniaProperty.RegisterAttached<Layoutable, double>("Grow", typeof(Flex), 0.0, validate: v => v >= 0.0);

        internal static readonly AttachedProperty<double> BaseLengthProperty =
            AvaloniaProperty.RegisterAttached<Layoutable, double>("BaseLength", typeof(Flex), 0.0);

        internal static readonly AttachedProperty<double> CurrentLengthProperty =
            AvaloniaProperty.RegisterAttached<Layoutable, double>("CurrentLength", typeof(Flex), 0.0);

        /// <summary>
        /// Gets the cross-axis alignment override for a child item in a <see cref="FlexPanel"/>
        /// </summary>
        /// <remarks>
        /// This property is used to override the <see cref="FlexPanel.AlignItems"/> property for a specific child.
        /// When omitted, <see cref="FlexPanel.AlignItems"/> in not overridden.
        /// Equivalent to CSS align-self property.
        /// </remarks>
        public static AlignItems? GetAlignSelf(Layoutable layoutable)
        {
            if (layoutable is null)
            {
                throw new ArgumentNullException(nameof(layoutable));
            }

            return layoutable.GetValue(AlignSelfProperty);
        }

        /// <summary>
        /// Sets the cross-axis alignment override for a child item in a <see cref="FlexPanel"/>
        /// </summary>
        /// <remarks>
        /// This property is used to override the <see cref="FlexPanel.AlignItems"/> property for a specific child.
        /// When omitted, <see cref="FlexPanel.AlignItems"/> in not overridden.
        /// Equivalent to CSS align-self property.
        /// </remarks>
        public static void SetAlignSelf(Layoutable layoutable, AlignItems? value)
        {
            if (layoutable is null)
            {
                throw new ArgumentNullException(nameof(layoutable));
            }

            layoutable.SetValue(AlignSelfProperty, value);
        }

        /// <summary>
        /// Retrieves the order in which a child item appears within the <see cref="FlexPanel"/>.
        /// </summary>
        /// <remarks>
        /// A lower order value means the item will be positioned earlier within the container.
        /// Items with the same order value are laid out in their source document order.
        /// When omitted, it is set to 0.
        /// Equivalent to CSS order property.
        /// </remarks>
        public static int GetOrder(Layoutable layoutable)
        {
            if (layoutable is null)
            {
                throw new ArgumentNullException(nameof(layoutable));
            }

            return layoutable.GetValue(OrderProperty);
        }

        /// <summary>
        /// Sets the order in which a child item appears within the <see cref="FlexPanel"/>.
        /// </summary>
        /// <remarks>
        /// A lower order value means the item will be positioned earlier within the container.
        /// Items with the same order value are laid out in their source document order.
        /// When omitted, it is set to 0.
        /// Equivalent to CSS order property.
        /// </remarks>
        public static void SetOrder(Layoutable layoutable, int value)
        {
            if (layoutable is null)
            {
                throw new ArgumentNullException(nameof(layoutable));
            }

            layoutable.SetValue(OrderProperty, value);
        }

        /// <summary>
        /// Gets the initial size along the main-axis of an item in a <see cref="FlexPanel"/>,
        /// before free space is distributed according to the flex factors.
        /// </summary>
        /// <remarks>
        /// Either automatic size, a fixed length, or a percentage of the container's size.
        /// When omitted, it is set to <see cref="FlexBasis.Auto"/>.
        /// Equivalent to CSS flex-basis property.
        /// </remarks>
        public static FlexBasis GetBasis(Layoutable layoutable)
        {
            if (layoutable is null)
            {
                throw new ArgumentNullException(nameof(layoutable));
            }

            return layoutable.GetValue(BasisProperty);
        }

        /// <summary>
        /// Sets the initial size along the main-axis of an item in a <see cref="FlexPanel"/>,
        /// before free space is distributed according to the flex factors.
        /// </summary>
        /// <remarks>
        /// Either automatic size, a fixed length, or a percentage of the container's size.
        /// When omitted, it is set to <see cref="FlexBasis.Auto"/>.
        /// Equivalent to CSS flex-basis property.
        /// </remarks>
        public static void SetBasis(Layoutable layoutable, FlexBasis value)
        {
            if (layoutable is null)
            {
                throw new ArgumentNullException(nameof(layoutable));
            }

            layoutable.SetValue(BasisProperty, value);
        }

        /// <summary>
        /// Gets the factor by which an item can shrink along the main-axis,
        /// relative to other items in a <see cref="FlexPanel"/>.
        /// </summary>
        /// <remarks>
        /// When omitted, it is set to 1.
        /// Equivalent to CSS flex-shrink property.
        /// </remarks>
        public static double GetShrink(Layoutable layoutable)
        {
            if (layoutable is null)
            {
                throw new ArgumentNullException(nameof(layoutable));
            }

            return layoutable.GetValue(ShrinkProperty);
        }

        /// <summary>
        /// Sets the factor by which an item can shrink along the main-axis,
        /// relative to other items in a <see cref="FlexPanel"/>.
        /// </summary>
        /// <remarks>
        /// When omitted, it is set to 1.
        /// Equivalent to CSS flex-shrink property.
        /// </remarks>
        public static void SetShrink(Layoutable layoutable, double value)
        {
            if (layoutable is null)
            {
                throw new ArgumentNullException(nameof(layoutable));
            }

            layoutable.SetValue(ShrinkProperty, value);
        }

        /// <summary>
        /// Gets the factor by which an item can grow along the main-axis,
        /// relative to other items in a <see cref="FlexPanel"/>.
        /// </summary>
        /// <remarks>
        /// When omitted, it is set to 0.
        /// Equivalent to CSS flex-grow property.
        /// </remarks>
        public static double GetGrow(Layoutable layoutable)
        {
            if (layoutable is null)
            {
                throw new ArgumentNullException(nameof(layoutable));
            }

            return layoutable.GetValue(GrowProperty);
        }

        /// <summary>
        /// Sets the factor by which an item can grow along the main-axis,
        /// relative to other items in a <see cref="FlexPanel"/>.
        /// </summary>
        /// <remarks>
        /// When omitted, it is set to 0.
        /// Equivalent to CSS flex-grow property.
        /// </remarks>
        public static void SetGrow(Layoutable layoutable, double value)
        {
            if (layoutable is null)
            {
                throw new ArgumentNullException(nameof(layoutable));
            }

            layoutable.SetValue(GrowProperty, value);
        }

        internal static double GetBaseLength(Layoutable layoutable)
        {
            if (layoutable is null)
            {
                throw new ArgumentNullException(nameof(layoutable));
            }

            return layoutable.GetValue(BaseLengthProperty);
        }

        internal static void SetBaseLength(Layoutable layoutable, double value)
        {
            if (layoutable is null)
            {
                throw new ArgumentNullException(nameof(layoutable));
            }

            layoutable.SetValue(BaseLengthProperty, value);
        }

        internal static double GetCurrentLength(Layoutable layoutable)
        {
            if (layoutable is null)
            {
                throw new ArgumentNullException(nameof(layoutable));
            }

            return layoutable.GetValue(CurrentLengthProperty);
        }

        internal static void SetCurrentLength(Layoutable layoutable, double value)
        {
            if (layoutable is null)
            {
                throw new ArgumentNullException(nameof(layoutable));
            }

            layoutable.SetValue(CurrentLengthProperty, value);
        }
    }
}
