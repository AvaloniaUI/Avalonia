// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives.PopupPositioning;
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
        private readonly TopLevel _parent;
        private IDisposable _presenterSubscription;
        private PopupPositionerParameters _positionerParameters;

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
        public PopupRoot(TopLevel parent)
            : this(parent, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PopupRoot"/> class.
        /// </summary>
        /// <param name="dependencyResolver">
        /// The dependency resolver to use. If null the default dependency resolver will be used.
        /// </param>
        public PopupRoot(TopLevel parent, IAvaloniaDependencyResolver dependencyResolver)
            : base(parent.PlatformImpl.CreatePopup(), dependencyResolver)
        {
            _parent = parent;
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

        void UpdatePosition()
        {
            PlatformImpl?.PopupPositioner.Update(_positionerParameters);
        }

        public void ConfigurePosition(Control target, PlacementMode placement, Point offset,
            PopupPositioningEdge anchor = PopupPositioningEdge.None,
            PopupPositioningEdge gravity = PopupPositioningEdge.None)
        {
            // We need a better way for tracking the last pointer position
            var pointer = _parent.PointToClient(_parent.PlatformImpl.MouseDevice.Position);
            
            _positionerParameters.Offset = offset;
            _positionerParameters.ConstraintAdjustment = PopupPositionerConstraintAdjustment.All;
            if (placement == PlacementMode.Pointer)
            {
                _positionerParameters.AnchorRectangle = new Rect(pointer, new Size(1, 1));
                _positionerParameters.Anchor = PopupPositioningEdge.BottomRight;
                _positionerParameters.Gravity = PopupPositioningEdge.BottomRight;
            }
            else
            {
                if (target == null)
                    throw new InvalidOperationException("Placement mode is not Pointer and PlacementTarget is null");
                var matrix = target.TransformToVisual(_parent);
                if (matrix == null)
                    throw new InvalidCastException("Target control is not in the same tree as the popup parent");

                _positionerParameters.AnchorRectangle = new Rect(default, target.Bounds.Size)
                    .TransformToAABB(matrix.Value);

                if (placement == PlacementMode.Right)
                {
                    _positionerParameters.Anchor = PopupPositioningEdge.TopRight;
                    _positionerParameters.Gravity = PopupPositioningEdge.BottomRight;
                }
                else if (placement == PlacementMode.Bottom)
                {
                    _positionerParameters.Anchor = PopupPositioningEdge.BottomLeft;
                    _positionerParameters.Gravity = PopupPositioningEdge.BottomRight;
                }
                else if (placement == PlacementMode.Left)
                {
                    _positionerParameters.Anchor = PopupPositioningEdge.TopLeft;
                    _positionerParameters.Gravity = PopupPositioningEdge.BottomLeft;
                }
                else if (placement == PlacementMode.Top)
                {
                    _positionerParameters.Anchor = PopupPositioningEdge.TopLeft;
                    _positionerParameters.Gravity = PopupPositioningEdge.TopRight;
                }
                else if (placement == PlacementMode.AnchorAndGravity)
                {
                    _positionerParameters.Anchor = anchor;
                    _positionerParameters.Gravity = gravity;
                }
                else
                    throw new InvalidOperationException("Invalid value for Popup.PlacementMode");
            }

            if (_positionerParameters.Size != default)
                UpdatePosition();
        }
        
        /// <summary>
        /// Carries out the arrange pass of the window.
        /// </summary>
        /// <param name="finalSize">The final window size.</param>
        /// <returns>The <paramref name="finalSize"/> parameter unchanged.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            using (BeginAutoSizing())
            {
                _positionerParameters.Size = finalSize;
                UpdatePosition();
            }

            return base.ArrangeOverride(PlatformImpl?.ClientSize ?? default(Size));
        }
    }
}
