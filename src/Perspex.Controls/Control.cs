// -----------------------------------------------------------------------
// <copyright file="Control.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using Perspex.Collections;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Templates;
    using Perspex.Input;
    using Perspex.Interactivity;
    using Perspex.LogicalTree;
    using Perspex.Rendering;
    using Perspex.Styling;
    using Splat;

    /// <summary>
    /// Base class for Perspex controls.
    /// </summary>
    /// <remarks>
    /// The control class extends <see cref="InputElement"/> and adds the following features:
    ///
    /// - A <see cref="Name"/>.
    /// - An inherited <see cref="DataContext"/>.
    /// - A <see cref="Tag"/> property to allow user-defined data to be attached to the control.
    /// - A collection of class strings for custom styling.
    /// - Implements <see cref="IStyleable"/> to allow styling to work on the control.
    /// - Implements <see cref="ILogical"/> to form part of a logical tree.
    /// </remarks>
    public class Control : InputElement, IControl, ISetLogicalParent
    {
        /// <summary>
        /// Defines the <see cref="DataContext"/> property.
        /// </summary>
        public static readonly PerspexProperty<object> DataContextProperty =
            PerspexProperty.Register<Control, object>(nameof(DataContext), inherits: true);

        /// <summary>
        /// Defines the <see cref="FocusAdorner"/> property.
        /// </summary>
        public static readonly PerspexProperty<ITemplate<IControl>> FocusAdornerProperty =
            PerspexProperty.Register<Control, ITemplate<IControl>>(nameof(FocusAdorner));

        /// <summary>
        /// Defines the <see cref="Parent"/> property.
        /// </summary>
        public static readonly PerspexProperty<IControl> ParentProperty =
            PerspexProperty.Register<Control, IControl>(nameof(Parent));

        /// <summary>
        /// Defines the <see cref="Tag"/> property.
        /// </summary>
        public static readonly PerspexProperty<object> TagProperty =
            PerspexProperty.Register<Control, object>(nameof(Tag));

        /// <summary>
        /// Defines the <see cref="TemplatedParent"/> property.
        /// </summary>
        public static readonly PerspexProperty<ITemplatedControl> TemplatedParentProperty =
            PerspexProperty.Register<Control, ITemplatedControl>(nameof(TemplatedParent));

        /// <summary>
        /// Event raised when an element wishes to be scrolled into view.
        /// </summary>
        public static readonly RoutedEvent<RequestBringIntoViewEventArgs> RequestBringIntoViewEvent =
            RoutedEvent.Register<Control, RequestBringIntoViewEventArgs>("RequestBringIntoView", RoutingStrategies.Bubble);

        private Classes classes = new Classes();

        private DataTemplates dataTemplates;

        private IControl focusAdorner;

        private string id;

        private IPerspexList<ILogical> logicalChildren;

        private Styles styles;

        /// <summary>
        /// Initializes static members of the <see cref="Control"/> class.
        /// </summary>
        static Control()
        {
            Control.AffectsMeasure(Control.IsVisibleProperty);
            PseudoClass(InputElement.IsEnabledCoreProperty, x => !x, ":disabled");
            PseudoClass(InputElement.IsFocusedProperty, ":focus");
            PseudoClass(InputElement.IsPointerOverProperty, ":pointerover");
        }

        /// <summary>
        /// Gets or sets the control's classes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Classes can be used to apply user-defined styling to controls, or to allow controls
        /// that share a common purpose to be easily selected.
        /// </para>
        /// <para>
        /// Even though this property can be set, the setter is only intended for use in object
        /// initializers. Assigning to this property does not change the underlying collection,
        /// it simply clears the existing collection and addds the contents of the assigned
        /// collection.
        /// </para>
        /// </remarks>
        public Classes Classes
        {
            get
            {
                return this.classes;
            }

            set
            {
                if (this.classes != value)
                {
                    this.classes.Clear();
                    this.classes.Add(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the control's data context.
        /// </summary>
        /// <remarks>
        /// The data context is an inherited property that specifies the default object that will
        /// be used for data binding.
        /// </remarks>
        public object DataContext
        {
            get { return this.GetValue(DataContextProperty); }
            set { this.SetValue(DataContextProperty, value); }
        }

        /// <summary>
        /// Gets or sets the control's focus adorner.
        /// </summary>
        public ITemplate<IControl> FocusAdorner
        {
            get { return this.GetValue(FocusAdornerProperty); }
            set { this.SetValue(FocusAdornerProperty, value); }
        }

        /// <summary>
        /// Gets or sets the data templates for the control.
        /// </summary>
        /// <remarks>
        /// Each control may define data templates which are applied to the control itself and its
        /// children.
        /// </remarks>
        public DataTemplates DataTemplates
        {
            get
            {
                if (this.dataTemplates == null)
                {
                    this.dataTemplates = new DataTemplates();
                }

                return this.dataTemplates;
            }

            set
            {
                this.dataTemplates = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the control.
        /// </summary>
        /// <remarks>
        /// A control's name is used to uniquely identify a control within the control's name
        /// scope. Once a control is added to a visual tree, its name cannot be changed.
        /// </remarks>
        public string Name
        {
            get
            {
                return this.id;
            }

            set
            {
                if (this.id != null)
                {
                    throw new InvalidOperationException("ID already set.");
                }

                if (((IVisual)this).VisualParent != null)
                {
                    throw new InvalidOperationException("Cannot set ID : control already added to tree.");
                }

                this.id = value;
            }
        }

        /// <summary>
        /// Gets or sets the styles for the control.
        /// </summary>
        /// <remarks>
        /// Styles for the entire application are added to the Application.Styles collection, but
        /// each control may in addition define its own styles which are applied to the control
        /// itself and its children.
        /// </remarks>
        public Styles Styles
        {
            get
            {
                if (this.styles == null)
                {
                    this.styles = new Styles();
                }

                return this.styles;
            }

            set
            {
                this.styles = value;
            }
        }

        /// <summary>
        /// Gets the control's logical parent.
        /// </summary>
        public IControl Parent
        {
            get { return this.GetValue(ParentProperty); }
        }

        /// <summary>
        /// Gets or sets a user-defined object attached to the control.
        /// </summary>
        public object Tag
        {
            get { return this.GetValue(TagProperty); }
            set { this.SetValue(TagProperty, value); }
        }

        /// <summary>
        /// Gets the control whose lookless template this control is part of.
        /// </summary>
        public ITemplatedControl TemplatedParent
        {
            get { return this.GetValue(TemplatedParentProperty); }
            internal set { this.SetValue(TemplatedParentProperty, value); }
        }

        /// <summary>
        /// Gets the control's logical parent.
        /// </summary>
        ILogical ILogical.LogicalParent
        {
            get { return this.Parent; }
        }

        /// <summary>
        /// Gets the control's logical children.
        /// </summary>
        IPerspexReadOnlyList<ILogical> ILogical.LogicalChildren
        {
            get { return this.LogicalChildren; }
        }

        /// <summary>
        /// Gets the type by which the control is styled.
        /// </summary>
        /// <remarks>
        /// Usually controls are styled by their own type, but there are instances where you want
        /// a control to be styled by its base type, e.g. creating SpecialButton that
        /// derives from Button and adds extra functionality but is still styled as a regular
        /// Button.
        /// </remarks>
        Type IStyleable.StyleKey
        {
            get { return this.GetType(); }
        }

        /// <summary>
        /// Gets the control's logical children.
        /// </summary>
        protected IPerspexList<ILogical> LogicalChildren
        {
            get
            {
                if (this.logicalChildren == null)
                {
                    this.logicalChildren = new PerspexList<ILogical>();
                }

                return this.logicalChildren;
            }
        }

        /// <summary>
        /// Sets the control's logical parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        void ISetLogicalParent.SetParent(ILogical parent)
        {
            var old = this.Parent;

            if (old != null && parent != null)
            {
                throw new InvalidOperationException("The Control already has a parent.");
            }

            this.SetValue(ParentProperty, parent);
        }

        /// <summary>
        /// Adds a pseudo-class to be set when a property is true.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="className">The pseudo-class.</param>
        protected static void PseudoClass(PerspexProperty<bool> property, string className)
        {
            PseudoClass(property, x => x, className);
        }

        /// <summary>
        /// Adds a pseudo-class to be set when a property equals a certain value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="selector">Returns a boolean value based on the property value.</param>
        /// <param name="className">The pseudo-class.</param>
        protected static void PseudoClass<T>(
            PerspexProperty<T> property,
            Func<T, bool> selector,
            string className)
        {
            Contract.Requires<ArgumentNullException>(property != null);
            Contract.Requires<ArgumentNullException>(selector != null);
            Contract.Requires<ArgumentNullException>(className != null);
            Contract.Requires<ArgumentNullException>(property != null);

            if (string.IsNullOrWhiteSpace(className))
            {
                throw new ArgumentException("Cannot supply an empty className.");
            }

            Observable.Merge(property.Changed, property.Initialized)
                .Subscribe(e =>
                {
                    if (selector((T)e.NewValue))
                    {
                        ((Control)e.Sender).Classes.Add(className);
                    }
                    else
                    {
                        ((Control)e.Sender).Classes.Remove(className);
                    }
                });
        }

        /// <inheritdoc/>
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);

            if (this.IsFocused &&
                (e.NavigationMethod == NavigationMethod.Tab ||
                 e.NavigationMethod == NavigationMethod.Directional))
            {
                var adornerLayer = AdornerLayer.GetAdornerLayer(this);

                if (adornerLayer != null)
                {
                    if (this.focusAdorner == null)
                    {
                        var template = this.GetValue(FocusAdornerProperty);

                        if (template != null)
                        {
                            this.focusAdorner = template.Build();
                        }
                    }

                    if (this.focusAdorner != null)
                    {
                        AdornerLayer.SetAdornedElement((Visual)this.focusAdorner, this);
                        adornerLayer.Children.Add(this.focusAdorner);
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            if (this.focusAdorner != null)
            {
                var adornerLayer = this.focusAdorner.Parent as Panel;
                adornerLayer.Children.Remove(this.focusAdorner);
                this.focusAdorner = null;
            }
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(IRenderRoot root)
        {
            base.OnAttachedToVisualTree(root);

            IStyler styler = Locator.Current.GetService<IStyler>();
            styler.ApplyStyles(this);
        }

        /// <summary>
        /// Makes the control use a different control's logical children as its own.
        /// </summary>
        /// <param name="collection">The logical children to use.</param>
        protected void RedirectLogicalChildren(IPerspexList<ILogical> collection)
        {
            this.logicalChildren = collection;
        }
    }
}
