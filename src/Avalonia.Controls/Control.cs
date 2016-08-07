// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.LogicalTree;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    /// <summary>
    /// Base class for Avalonia controls.
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
    public class Control : InputElement, IControl, INamed, ISetInheritanceParent, ISetLogicalParent, ISupportInitialize
    {
        /// <summary>
        /// Defines the <see cref="DataContext"/> property.
        /// </summary>
        public static readonly StyledProperty<object> DataContextProperty =
            AvaloniaProperty.Register<Control, object>(
                nameof(DataContext), 
                inherits: true,
                notifying: DataContextNotifying);

        /// <summary>
        /// Defines the <see cref="FocusAdorner"/> property.
        /// </summary>
        public static readonly StyledProperty<ITemplate<IControl>> FocusAdornerProperty =
            AvaloniaProperty.Register<Control, ITemplate<IControl>>(nameof(FocusAdorner));

        /// <summary>
        /// Defines the <see cref="Name"/> property.
        /// </summary>
        public static readonly DirectProperty<Control, string> NameProperty =
            AvaloniaProperty.RegisterDirect<Control, string>(nameof(Name), o => o.Name, (o, v) => o.Name = v);

        /// <summary>
        /// Defines the <see cref="Parent"/> property.
        /// </summary>
        public static readonly DirectProperty<Control, IControl> ParentProperty =
            AvaloniaProperty.RegisterDirect<Control, IControl>(nameof(Parent), o => o.Parent);

        /// <summary>
        /// Defines the <see cref="Tag"/> property.
        /// </summary>
        public static readonly StyledProperty<object> TagProperty =
            AvaloniaProperty.Register<Control, object>(nameof(Tag));

        /// <summary>
        /// Defines the <see cref="TemplatedParent"/> property.
        /// </summary>
        public static readonly StyledProperty<ITemplatedControl> TemplatedParentProperty =
            AvaloniaProperty.Register<Control, ITemplatedControl>(nameof(TemplatedParent), inherits: true);

        /// <summary>
        /// Defines the <see cref="ContextMenu"/> property.
        /// </summary>
        public static readonly StyledProperty<ContextMenu> ContextMenuProperty =
            AvaloniaProperty.Register<Control, ContextMenu>(nameof(ContextMenu));

        /// <summary>
        /// Event raised when an element wishes to be scrolled into view.
        /// </summary>
        public static readonly RoutedEvent<RequestBringIntoViewEventArgs> RequestBringIntoViewEvent =
            RoutedEvent.Register<Control, RequestBringIntoViewEventArgs>("RequestBringIntoView", RoutingStrategies.Bubble);

        private int _initCount;
        private string _name;
        private IControl _parent;
        private readonly Classes _classes = new Classes();
        private DataTemplates _dataTemplates;
        private IControl _focusAdorner;
        private bool _isAttachedToLogicalTree;
        private IAvaloniaList<ILogical> _logicalChildren;
        private INameScope _nameScope;
        private Styles _styles;
        private bool _styled;
        private Subject<IStyleable> _styleDetach = new Subject<IStyleable>();

        /// <summary>
        /// Initializes static members of the <see cref="Control"/> class.
        /// </summary>
        static Control()
        {
            AffectsMeasure(IsVisibleProperty);
            PseudoClass(IsEnabledCoreProperty, x => !x, ":disabled");
            PseudoClass(IsFocusedProperty, ":focus");
            PseudoClass(IsPointerOverProperty, ":pointerover");
            ////PseudoClass(ValidationStatusProperty, status => !status.IsValid, ":invalid");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Control"/> class.
        /// </summary>
        public Control()
        {
            _nameScope = this as INameScope;
        }

        /// <summary>
        /// Raised when the control is attached to a rooted logical tree.
        /// </summary>
        public event EventHandler<LogicalTreeAttachmentEventArgs> AttachedToLogicalTree;

        /// <summary>
        /// Raised when the control is detached from a rooted logical tree.
        /// </summary>
        public event EventHandler<LogicalTreeAttachmentEventArgs> DetachedFromLogicalTree;

        /// <summary>
        /// Occurs when the <see cref="DataContext"/> property changes.
        /// </summary>
        /// <remarks>
        /// This event will be raised when the <see cref="DataContext"/> property has changed and
        /// all subscribers to that change have been notified.
        /// </remarks>
        public event EventHandler DataContextChanged;

        /// <summary>
        /// Occurs when the control has finished initialization.
        /// </summary>
        /// <remarks>
        /// The Initialized event indicates that all property values on the control have been set.
        /// When loading the control from markup, it occurs when 
        /// <see cref="ISupportInitialize.EndInit"/> is called *and* the control
        /// is attached to a rooted logical tree. When the control is created by code and
        /// <see cref="ISupportInitialize"/> is not used, it is called when the control is attached
        /// to the visual tree.
        /// </remarks>
        public event EventHandler Initialized;

        /// <summary>
        /// Gets or sets the name of the control.
        /// </summary>
        /// <remarks>
        /// An element's name is used to uniquely identify a control within the control's name
        /// scope. Once the element is added to a logical tree, its name cannot be changed.
        /// </remarks>
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (value.Trim() == string.Empty)
                {
                    throw new InvalidOperationException("Cannot set Name to empty string.");
                }

                if (_styled)
                {
                    throw new InvalidOperationException("Cannot set Name : control already styled.");
                }

                _name = value;
            }
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
                    _classes.Replace(value);
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
        /// Gets a value that indicates whether the element has finished initialization.
        /// </summary>
        /// <remarks>
        /// For more information about when IsInitialized is set, see the <see cref="Initialized"/>
        /// event.
        /// </remarks>
        public bool IsInitialized { get; private set; }

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
        /// Gets or sets a context menu to the control.
        /// </summary>
        public ContextMenu ContextMenu
        {
            get { return GetValue(ContextMenuProperty); }
            set { SetValue(ContextMenuProperty, value); }
        }

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
        /// Gets a value indicating whether the element is attached to a rooted logical tree.
        /// </summary>
        bool ILogical.IsAttachedToLogicalTree => _isAttachedToLogicalTree;

        /// <summary>
        /// Gets the control's logical parent.
        /// </summary>
        ILogical ILogical.LogicalParent => Parent;

        /// <summary>
        /// Gets the control's logical children.
        /// </summary>
        IAvaloniaReadOnlyList<ILogical> ILogical.LogicalChildren => LogicalChildren;

        /// <inheritdoc/>
        IAvaloniaReadOnlyList<string> IStyleable.Classes => Classes;

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

        /// <inheritdoc/>
        IObservable<IStyleable> IStyleable.StyleDetach => _styleDetach;

        /// <inheritdoc/>
        IStyleHost IStyleHost.StylingParent => (IStyleHost)InheritanceParent;

        /// <inheritdoc/>
        public virtual void BeginInit()
        {
            ++_initCount;
        }

        /// <inheritdoc/>
        public virtual void EndInit()
        {
            if (_initCount == 0)
            {
                throw new InvalidOperationException("BeginInit was not called.");
            }

            if (--_initCount == 0 && _isAttachedToLogicalTree)
            {
                if (!_styled)
                {
                    RegisterWithNameScope();
                    ApplyStyling();
                    _styled = true;
                }

                if (!IsInitialized)
                {
                    IsInitialized = true;
                    Initialized?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <inheritdoc/>
        void ILogical.NotifyDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            this.OnDetachedFromLogicalTreeCore(e);
        }

        /// <summary>
        /// Gets the control's logical children.
        /// </summary>
        protected IAvaloniaList<ILogical> LogicalChildren
        {
            get
            {
                if (_logicalChildren == null)
                {
                    var list = new AvaloniaList<ILogical>();
                    list.ResetBehavior = ResetBehavior.Remove;
                    list.Validate = ValidateLogicalChild;
                    list.CollectionChanged += LogicalChildrenCollectionChanged;
                    _logicalChildren = list;
                }

                return _logicalChildren;
            }
        }

        /// <summary>
        /// Gets the <see cref="Classes"/> collection in a form that allows adding and removing
        /// pseudoclasses.
        /// </summary>
        protected IPseudoClasses PseudoClasses => Classes;

        /// <inheritdoc/>
        protected override void DataValidationChanged(AvaloniaProperty property, BindingNotification status)
        {
            base.DataValidationChanged(property, status);
            ////ValidationStatus.UpdateValidationStatus(status);
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

                if (_isAttachedToLogicalTree)
                {
                    var oldRoot = FindStyleRoot(old);

                    if (oldRoot == null)
                    {
                        throw new AvaloniaInternalException("Was attached to logical tree but cannot find root.");
                    }

                    var e = new LogicalTreeAttachmentEventArgs(oldRoot);
                    OnDetachedFromLogicalTreeCore(e);
                }

                if (InheritanceParent == null || parent == null)
                {
                    InheritanceParent = parent as AvaloniaObject;
                }

                _parent = (IControl)parent;

                if (_parent is IStyleRoot || _parent?.IsAttachedToLogicalTree == true)
                {
                    var newRoot = FindStyleRoot(this);

                    if (newRoot == null)
                    {
                        throw new AvaloniaInternalException("Parent is atttached to logical tree but cannot find root.");
                    }

                    var e = new LogicalTreeAttachmentEventArgs(newRoot);
                    OnAttachedToLogicalTreeCore(e);
                }

                RaisePropertyChanged(ParentProperty, old, _parent, BindingPriority.LocalValue);
            }
        }

        /// <summary>
        /// Sets the control's inheritance parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        void ISetInheritanceParent.SetParent(IAvaloniaObject parent)
        {
            InheritanceParent = parent;
        }

        /// <summary>
        /// Adds a pseudo-class to be set when a property is true.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="className">The pseudo-class.</param>
        protected static void PseudoClass(AvaloniaProperty<bool> property, string className)
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
            AvaloniaProperty<T> property,
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
                .Where(e => e.Sender is Control)
                .Subscribe(e =>
                {
                    if (selector((T)e.NewValue))
                    {
                        ((Control)e.Sender).PseudoClasses.Add(className);
                    }
                    else
                    {
                        ((Control)e.Sender).PseudoClasses.Remove(className);
                    }
                });
        }

        /// <summary>
        /// Gets the element that recieves the focus adorner.
        /// </summary>
        /// <returns>The control that recieves the focus adorner.</returns>
        protected virtual IControl GetTemplateFocusTarget()
        {
            return this;
        }

        /// <summary>
        /// Called when the control is added to a rooted logical tree.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            AttachedToLogicalTree?.Invoke(this, e);
        }

        /// <summary>
        /// Called when the control is removed from a rooted logical tree.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            DetachedFromLogicalTree?.Invoke(this, e);
        }

        /// <inheritdoc/>
        protected sealed override void OnAttachedToVisualTreeCore(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTreeCore(e);

            if (!IsInitialized)
            {
                IsInitialized = true;
                Initialized?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <inheritdoc/>
        protected sealed override void OnDetachedFromVisualTreeCore(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTreeCore(e);
        }

        /// <summary>
        /// Called before the <see cref="DataContext"/> property changes.
        /// </summary>
        protected virtual void OnDataContextChanging()
        {
        }

        /// <summary>
        /// Called after the <see cref="DataContext"/> property changes.
        /// </summary>
        protected virtual void OnDataContextChanged()
        {
            DataContextChanged?.Invoke(this, EventArgs.Empty);
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
                        var target = (Visual)GetTemplateFocusTarget();

                        if (target != null)
                        {
                            AdornerLayer.SetAdornedElement((Visual)_focusAdorner, target);
                            adornerLayer.Children.Add(_focusAdorner);
                        }
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

        /// <summary>
        /// Called when the <see cref="DataContext"/> property begins and ends being notified.
        /// </summary>
        /// <param name="o">The object on which the DataContext is changing.</param>
        /// <param name="notifying">Whether the notifcation is beginning or ending.</param>
        private static void DataContextNotifying(IAvaloniaObject o, bool notifying)
        {
            var control = o as Control;

            if (control != null)
            {
                if (notifying)
                {
                    control.OnDataContextChanging();
                }
                else
                {
                    control.OnDataContextChanged();
                }
            }
        }

        private static IStyleRoot FindStyleRoot(IStyleHost e)
        {
            while (e != null)
            {
                var root = e as IStyleRoot;

                if (root != null && root.StylingParent == null)
                {
                    return root;
                }

                e = e.StylingParent;
            }

            return null;
        }

        private void ApplyStyling()
        {
            AvaloniaLocator.Current.GetService<IStyler>()?.ApplyStyles(this);
        }

        private void RegisterWithNameScope()
        {
            if (_nameScope == null)
            {
                _nameScope = NameScope.GetNameScope(this) ?? ((Control)Parent)?._nameScope;
            }

            if (Name != null)
            {
                _nameScope?.Register(Name, this);
            }
        }

        private static void ValidateLogicalChild(ILogical c)
        {
            if (c == null)
            {
                throw new ArgumentException("Cannot add null to LogicalChildren.");
            }
        }

        private void OnAttachedToLogicalTreeCore(LogicalTreeAttachmentEventArgs e)
        {
            // This method can be called when a control is already attached to the logical tree
            // in the following scenario:
            // - ListBox gets assigned Items containing ListBoxItem
            // - ListBox makes ListBoxItem a logical child
            // - ListBox template gets applied; making its Panel get attached to logical tree
            // - That AttachedToLogicalTree signal travels down to the ListBoxItem
            if (!_isAttachedToLogicalTree)
            {
                _isAttachedToLogicalTree = true;

                if (_initCount == 0)
                {
                    RegisterWithNameScope();
                    ApplyStyling();
                    _styled = true;
                }

                OnAttachedToLogicalTree(e);
            }

            foreach (var child in LogicalChildren.OfType<Control>())
            {
                child.OnAttachedToLogicalTreeCore(e);
            }
        }

        private void OnDetachedFromLogicalTreeCore(LogicalTreeAttachmentEventArgs e)
        {
            if (_isAttachedToLogicalTree)
            {
                if (Name != null)
                {
                    _nameScope?.Unregister(Name);
                }

                _isAttachedToLogicalTree = false;
                _styleDetach.OnNext(this);
                this.TemplatedParent = null;
                OnDetachedFromLogicalTree(e);

                foreach (var child in LogicalChildren.OfType<Control>())
                {
                    child.OnDetachedFromLogicalTreeCore(e);
                }

#if DEBUG
                if (((INotifyCollectionChangedDebug)_classes).GetCollectionChangedSubscribers()?.Length > 0)
                {
                    Logger.Warning(
                        LogArea.Control,
                        this,
                        "{Type} detached from logical tree but still has class listeners",
                        this.GetType());
                }
#endif
            }
        }

        private void LogicalChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    SetLogicalParent(e.NewItems.Cast<ILogical>());
                    break;

                case NotifyCollectionChangedAction.Remove:
                    ClearLogicalParent(e.OldItems.Cast<ILogical>());
                    break;

                case NotifyCollectionChangedAction.Replace:
                    ClearLogicalParent(e.OldItems.Cast<ILogical>());
                    SetLogicalParent(e.NewItems.Cast<ILogical>());
                    break;

                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException("Reset should not be signalled on LogicalChildren collection");
            }
        }

        private void SetLogicalParent(IEnumerable<ILogical> children)
        {
            foreach (var i in children)
            {
                if (i.LogicalParent == null)
                {
                    ((ISetLogicalParent)i).SetParent(this);
                }
            }
        }

        private void ClearLogicalParent(IEnumerable<ILogical> children)
        {
            foreach (var i in children)
            {
                if (i.LogicalParent == this)
                {
                    ((ISetLogicalParent)i).SetParent(null);
                }
            }
        }
    }
}
