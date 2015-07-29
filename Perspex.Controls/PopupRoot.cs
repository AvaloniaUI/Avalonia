// -----------------------------------------------------------------------
// <copyright file="PopupRoot.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using Collections;
    using Perspex.Interactivity;
    using Perspex.Media;
    using Perspex.Platform;
    using Perspex.VisualTree;
    using Splat;

    /// <summary>
    /// The root window of a <see cref="Popup"/>.
    /// </summary>
    public class PopupRoot : TopLevel, IInteractive, IHostedVisualTreeRoot
    {
        /// <summary>
        /// Initializes static members of the <see cref="PopupRoot"/> class.
        /// </summary>
        static PopupRoot()
        {
            BackgroundProperty.OverrideDefaultValue(typeof(PopupRoot), Brushes.White);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PopupRoot"/> class.
        /// </summary>
        public PopupRoot()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PopupRoot"/> class.
        /// </summary>
        /// <param name="dependencyResolver">
        /// The dependency resolver to use. If null the default dependency resolver will be used.
        /// </param>
        public PopupRoot(IDependencyResolver dependencyResolver)
            : base(Locator.Current.GetService<IPopupImpl>(), dependencyResolver)
        {
            this.GetObservable(ParentProperty).Subscribe(x => this.InheritanceParent = (PerspexObject)x);
        }

        /// <summary>
        /// Gets the platform-specific window implementation.
        /// </summary>
        public new IPopupImpl PlatformImpl
        {
            get { return (IPopupImpl)base.PlatformImpl; }
        }

        /// <summary>
        /// Gets the parent control in the event route.
        /// </summary>
        /// <remarks>
        /// Popup events are passed to their parent window. This facilitates this.
        /// </remarks>
        IInteractive IInteractive.InteractiveParent
        {
            get { return this.Parent; }
        }

        /// <summary>
        /// Gets the control that is hosting the popup root.
        /// </summary>
        IVisual IHostedVisualTreeRoot.Host
        {
            get { return this.Parent; }
        }

        /// <summary>
        /// Sets the position of the popup in screen coordinates.
        /// </summary>
        /// <param name="p">The position.</param>
        public void SetPosition(Point p)
        {
            this.PlatformImpl.SetPosition(p);
        }

        /// <summary>
        /// Hides the popup.
        /// </summary>
        public void Hide()
        {
            this.PlatformImpl.Hide();
            this.IsVisible = false;
        }

        /// <summary>
        /// Shows the popup.
        /// </summary>
        public void Show()
        {
            this.PlatformImpl.Show();
            this.LayoutManager.ExecuteLayoutPass();
            this.IsVisible = true;
        }
    }
}
