using System;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Reactive;

namespace Avalonia.Controls
{
    /// <summary>
    /// An item in  a <see cref="PivotHeader"/>/>.
    /// </summary>
    [PseudoClasses(":pressed", ":selected")]
    public class PivotItem : HeaderedContentControl
    {
        private IDisposable? _boundsObservable;

        /// <summary>
        /// Initializes static members of the <see cref="PivotItem"/> class.
        /// </summary>
        static PivotItem()
        {
            PressedMixin.Attach<PivotItem>();
            FocusableProperty.OverrideDefaultValue(typeof(PivotItem), true);
            DataContextProperty.Changed.AddClassHandler<PivotItem>((x, e) => x.UpdateHeader(e));
            AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<PivotItem>(AutomationControlType.PivotItem);
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new ListItemAutomationPeer(this);

        private void UpdateHeader(AvaloniaPropertyChangedEventArgs obj)
        {
            if (Header == null)
            {
                if (obj.NewValue is IHeadered headered)
                {
                    if (Header != headered.Header)
                    {
                        Header = headered.Header;
                    }
                }
                else
                {
                    if (!(obj.NewValue is Control))
                    {
                        Header = obj.NewValue;
                    }
                }
            }
            else
            {
                if (Header == obj.OldValue)
                {
                    Header = obj.NewValue;
                }
            }          
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);

            if(Parent is Pivot pivot)
            {
                _boundsObservable = pivot.GetObservable(BoundsProperty).Subscribe(ParentBoundsChanged);
            }
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);

            _boundsObservable?.Dispose();
            _boundsObservable = null;
        }

        private void ParentBoundsChanged(Rect bounds)
        {
            InvalidateMeasure();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if(Parent is Pivot pivot && pivot.BorderPart != null)
            {
                var size = pivot.BorderPart.Bounds.Size;

                base.MeasureOverride(size);

                return size;
            }
            return base.MeasureOverride(availableSize);
        }
    }
}
