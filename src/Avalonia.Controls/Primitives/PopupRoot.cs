// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.VisualTree;
using JetBrains.Annotations;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// The root window of a <see cref="Popup"/>.
    /// </summary>
    public class PopupRoot : WindowBase, IInteractive, IHostedVisualTreeRoot, IDisposable, IStyleHost
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
        public PopupRoot(IAvaloniaDependencyResolver dependencyResolver)
            : base(PlatformManager.CreatePopup(), dependencyResolver)
        {
        }

        /// <summary>
        /// Gets the platform-specific window implementation.
        /// </summary>
        [CanBeNull]
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
        /// Gets the styling parent of the popup root.
        /// </summary>
        IStyleHost IStyleHost.StylingParent => Parent;

        /// <inheritdoc/>
        public void Dispose() => PlatformImpl?.Dispose();

        /// <summary>
        /// Moves the Popups position so that it doesnt overlap screen edges.
        /// This method can be called immediately after Show has been called.
        /// </summary>
        public void SnapInsideScreenEdges()
        {
            var screen = (VisualRoot as WindowBase)?.Screens?.ScreenFromPoint(Position);

            if (screen != null)
            {
                var scaling = VisualRoot.RenderScaling;
                var bounds = PixelRect.FromRect(Bounds, scaling);
                var screenX = Position.X + bounds.Width - screen.Bounds.X;
                var screenY = Position.Y + bounds.Height - screen.Bounds.Y;

                if (screenX > screen.Bounds.Width)
                {
                    Position = Position.WithX(Position.X - (screenX - screen.Bounds.Width));
                }

                if (screenY > screen.Bounds.Height)
                {
                    Position = Position.WithY(Position.Y - (screenY - screen.Bounds.Height));
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);

            if (Parent?.TemplatedParent != null)
            {
                if (_presenterSubscription != null)
                {
                    _presenterSubscription.Dispose();
                    _presenterSubscription = null;
                }

                Presenter?.ApplyTemplate();
                Presenter?.GetObservable(ContentPresenter.ChildProperty)
                    .Subscribe(SetTemplatedParentAndApplyChildTemplates);
            }
        }

        private void SetTemplatedParentAndApplyChildTemplates(IControl control)
        {
            if (control != null)
            {
                var templatedParent = Parent.TemplatedParent;

                if (control.TemplatedParent == null)
                {
                    control.SetValue(TemplatedParentProperty, templatedParent);
                }

                control.ApplyTemplate();

                if (!(control is IPresenter) && control.TemplatedParent == templatedParent)
                {
                    foreach (IControl child in control.GetVisualChildren())
                    {
                        SetTemplatedParentAndApplyChildTemplates(child);
                    }
                }
            }
        }
    }
}
