// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using Perspex.Collections;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Input;
using Perspex.Interactivity;
using Perspex.Rendering;
using Perspex.Styling;

namespace Perspex.Controls
{
    /// <summary>
    /// Base class for Perspex controls.
    /// </summary>
    /// <remarks>
    /// The control class extends <see cref="InputElement"/> and adds the following features:
    ///
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
            PerspexProperty.Register<Control, object>(
                nameof(DataContext), 
                inherits: true,
                notifying: DataContextNotifying);

        /// <summary>
        /// Defines the <see cref="FocusAdorner"/> property.
        /// </summary>
        public static readonly PerspexProperty<ITemplate<IControl>> FocusAdornerProperty =
            PerspexProperty.Register<Control, ITemplate<IControl>>(nameof(FocusAdorner));

        /// <summary>
        /// Defines the <see cref="Parent"/> property.
        /// </summary>
        public static readonly PerspexProperty<IControl> ParentProperty =
            PerspexProperty.RegisterDirect<Control, IControl>(nameof(Parent), o => o.Parent);

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

        private IControl _parent;
        private readonly Classes _classes = new Classes();
        private DataTemplates _dataTemplates;
        private IControl _focusAdorner;
        private IPerspexList<ILogical> _logicalChildren;
        private INameScope _nameScope;
        private Styles _styles;

        /// <summary>
        /// Initializes static members of the <see cref="Control"/> class.
        /// </summary>
        static Control()
        {
            AffectsMeasure(IsVisibleProperty);
            PseudoClass(IsEnabledCoreProperty, x => !x, ":disabled");
            PseudoClass(IsFocusedProperty, ":focus");
            PseudoClass(IsPointerOverProperty, ":pointerover");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Control"/> class.
        /// </summary>
        public Control()
        {
            _nameScope = this as INameScope;
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
                return _classes;
            }

            set
            {
                if (_classes != value)
                {
                    _classes.Clear();
                    _classes.Add(value);
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
            get { return GetValue(DataContextProperty); }
            set { SetValue(DataContextProperty, value); }
        }

        /// <summary>
        /// Gets or sets the control's focus adorner.
        /// </summary>
        public ITemplate<IControl> FocusAdorner
        {
            get { return GetValue(FocusAdornerProperty); }
            set { SetValue(FocusAdornerProperty, value); }
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
            get { return _dataTemplates ?? (_dataTemplates = new DataTemplates()); }
            set { _dataTemplates = value; }
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
            get { return _styles ?? (_styles = new Styles()); }
            set { _styles = value; }
        }

        /// <summary>
        /// Gets the control's logical parent.
        /// </summary>
        public IControl Parent => _parent;

        /// <summary>
        /// Gets or sets a user-defined object attached to the control.
        /// </summary>
        public object Tag
        {
            get { return GetValue(TagProperty); }
            set { SetValue(TagProperty, value); }
        }

        /// <summary>
        /// Gets the control whose lookless template this control is part of.
        /// </summary>
        public ITemplatedControl TemplatedParent
        {
            get { return GetValue(TemplatedParentProperty); }
            internal set { SetValue(TemplatedParentProperty, value); }
        }

        /// <summary>
        /// Gets the control's logical parent.
        /// </summary>
        ILogical ILogical.LogicalParent => Parent;

        /// <summary>
        /// Gets the control's logical children.
        /// </summary>
        IPerspexReadOnlyList<ILogical> ILogical.LogicalChildren => LogicalChildren;

        /// <summary>
        /// Gets the type by which the control is styled.
        /// </summary>
        /// <remarks>
        /// Usually controls are styled by their own type, but there are instances where you want
        /// a control to be styled by its base type, e.g. creating SpecialButton that
        /// derives from Button and adds extra functionality but is still styled as a regular
        /// Button.
        /// </remarks>
        Type IStyleable.StyleKey => GetType();

        /// <summary>
        /// Gets the parent style host element.
        /// </summary>
        IStyleHost IStyleHost.StylingParent => (IStyleHost)InheritanceParent;

        /// <summary>
        /// Gets a value which indicates whether a change to the <see cref="DataContext"/> is in 
        /// the process of being notified.
        /// </summary>
        protected bool IsDataContextChanging
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the control's logical children.
        /// </summary>
        protected IPerspexList<ILogical> LogicalChildren
        {
            get
            {
                if (_logicalChildren == null)
                {
                    var list = new PerspexList<ILogical>();
                    list.ResetBehavior = ResetBehavior.Remove;
                    _logicalChildren = list;
                }

                return _logicalChildren;
            }
        }

        /// <summary>
        /// Sets the control's logical parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        void ISetLogicalParent.SetParent(ILogical parent)
        {
            var old = Parent;

            if (parent != old)
            {
                if (old != null && parent != null)
                {
                    throw new InvalidOperationException("The Control already has a parent.");
                }

                SetAndRaise(ParentProperty, ref _parent, (IControl)parent);
            }
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

            property.Changed.Merge(property.Initialized)
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

            if (IsFocused &&
                (e.NavigationMethod == NavigationMethod.Tab ||
                 e.NavigationMethod == NavigationMethod.Directional))
            {
                var adornerLayer = AdornerLayer.GetAdornerLayer(this);

                if (adornerLayer != null)
                {
                    if (_focusAdorner == null)
                    {
                        var template = GetValue(FocusAdornerProperty);

                        if (template != null)
                        {
                            _focusAdorner = template.Build();
                        }
                    }

                    if (_focusAdorner != null)
                    {
                        AdornerLayer.SetAdornedElement((Visual)_focusAdorner, this);
                        adornerLayer.Children.Add(_focusAdorner);
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            if (_focusAdorner != null)
            {
                var adornerLayer = _focusAdorner.Parent as Panel;
                adornerLayer.Children.Remove(_focusAdorner);
                _focusAdorner = null;
            }
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (_nameScope == null)
            {
                _nameScope = NameScope.GetNameScope(this) ?? ((Control)Parent)?._nameScope;
            }

            if (Name != null)
            {
                _nameScope?.Register(Name, this);
            }

            PerspexLocator.Current.GetService<IStyler>()?.ApplyStyles(this);
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            if (Name != null)
            {
                _nameScope?.Unregister(Name);
            }
        }

        /// <summary>
        /// Called when the <see cref="DataContext"/> is changed and all subscribers to that change
        /// have been notified.
        /// </summary>
        protected virtual void OnDataContextFinishedChanging()
        {
        }

        /// <summary>
        /// Makes the control use a different control's logical children as its own.
        /// </summary>
        /// <param name="collection">The logical children to use.</param>
        protected void RedirectLogicalChildren(IPerspexList<ILogical> collection)
        {
            _logicalChildren = collection;
        }

        /// <summary>
        /// Called when the <see cref="DataContext"/> property begins and ends being notified.
        /// </summary>
        /// <param name="o">The object on which the DataContext is changing.</param>
        /// <param name="notifying">Whether the notifcation is beginning or ending.</param>
        private static void DataContextNotifying(PerspexObject o, bool notifying)
        {
            var control = o as Control;

            if (control != null)
            {
                control.IsDataContextChanging = notifying;
            }
        }
    }
}
