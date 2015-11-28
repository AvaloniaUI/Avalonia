// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Collections;
using Perspex.Controls.Platform;
using Perspex.Controls.Presenters;
using Perspex.Interactivity;
using Perspex.Media;
using Perspex.Platform;
using Perspex.VisualTree;

namespace Perspex.Controls.Primitives
{
    /// <summary>
    /// The root window of a <see cref="Popup"/>.
    /// </summary>
    public class PopupRoot : TopLevel, IInteractive, IHostedVisualTreeRoot
    {
        private IDisposable _presenterSubscription;

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
        public PopupRoot(IPerspexDependencyResolver dependencyResolver)
            : base(PlatformManager.CreatePopup(), dependencyResolver)
        {
            GetObservable(ParentProperty).Subscribe(x => InheritanceParent = (PerspexObject)x);
        }

        /// <summary>
        /// Gets the platform-specific window implementation.
        /// </summary>
        public new IPopupImpl PlatformImpl => (IPopupImpl)base.PlatformImpl;

        /// <summary>
        /// Gets the parent control in the event route.
        /// </summary>
        /// <remarks>
        /// Popup events are passed to their parent window. This facilitates this.
        /// </remarks>
        IInteractive IInteractive.InteractiveParent => Parent;

        /// <summary>
        /// Gets the control that is hosting the popup root.
        /// </summary>
        IVisual IHostedVisualTreeRoot.Host => Parent;

        /// <summary>
        /// Sets the position of the popup in screen coordinates.
        /// </summary>
        /// <param name="p">The position.</param>
        public void SetPosition(Point p)
        {
            PlatformImpl.SetPosition(p);
        }

        /// <summary>
        /// Hides the popup.
        /// </summary>
        public void Hide()
        {
            PlatformImpl.Hide();
            IsVisible = false;
        }

        /// <summary>
        /// Shows the popup.
        /// </summary>
        public void Show()
        {
            PlatformImpl.Show();
            LayoutManager?.ExecuteLayoutPass();
            IsVisible = true;
        }

        /// <inheritdoc/>
        protected override void OnTemplateApplied(INameScope nameScope)
        {
            base.OnTemplateApplied(nameScope);

            if (Parent.TemplatedParent != null)
            {
                if (_presenterSubscription != null)
                {
                    _presenterSubscription.Dispose();
                    _presenterSubscription = null;
                }

                Presenter?.GetObservable(ContentPresenter.ChildProperty)
                    .Subscribe(SetTemplatedParentAndApplyChildTemplates);
            }
        }

        private void SetTemplatedParentAndApplyChildTemplates(IControl control)
        {
            var templatedParent = Parent.TemplatedParent;

            if (control.TemplatedParent == null)
            {
                control.SetValue(TemplatedParentProperty, templatedParent);
            }

            control.ApplyTemplate();

            if (!(control is IPresenter && control.TemplatedParent == templatedParent))
            {
                foreach (IControl child in control.GetVisualChildren())
                {
                    SetTemplatedParentAndApplyChildTemplates(child);
                }
            }
        }
    }
}
