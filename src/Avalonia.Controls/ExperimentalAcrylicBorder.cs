using Avalonia.Controls.Utils;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using System;
using Avalonia.Reactive;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    public class ExperimentalAcrylicBorder : Decorator
    {
        public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
            Border.CornerRadiusProperty.AddOwner<ExperimentalAcrylicBorder>();

        public static readonly StyledProperty<ExperimentalAcrylicMaterial> MaterialProperty =
            AvaloniaProperty.Register<ExperimentalAcrylicBorder, ExperimentalAcrylicMaterial>(nameof(Material));

        private IDisposable? _subscription;
        private IDisposable? _materialSubscription;

        static ExperimentalAcrylicBorder()
        {
            AffectsRender<ExperimentalAcrylicBorder>(
                MaterialProperty,
                CornerRadiusProperty);
        }


        /// <summary>
        /// Gets or sets the radius of the border rounded corners.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public ExperimentalAcrylicMaterial Material
        {
            get => GetValue(MaterialProperty);
            set => SetValue(MaterialProperty, value);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var tl = (TopLevel)e.Root;

            _subscription = tl.GetObservable(TopLevel.ActualTransparencyLevelProperty)
                .Subscribe(x =>
                {
                    if (tl.PlatformImpl is null)
                        return;
                    if (x == WindowTransparencyLevel.Transparent || x == WindowTransparencyLevel.None)
                        Material.PlatformTransparencyCompensationLevel = tl.PlatformImpl.AcrylicCompensationLevels.TransparentLevel;
                    else if (x == WindowTransparencyLevel.Blur)
                        Material.PlatformTransparencyCompensationLevel = tl.PlatformImpl.AcrylicCompensationLevels.BlurLevel;
                    else if (x == WindowTransparencyLevel.AcrylicBlur)
                        Material.PlatformTransparencyCompensationLevel = tl.PlatformImpl.AcrylicCompensationLevels.AcrylicBlurLevel;
                });
            UpdateMaterialSubscription();
        }

        void UpdateMaterialSubscription()
        {
            _materialSubscription?.Dispose();
            _materialSubscription = null;
            if (CompositionVisual == null)
                return;
            if (Material == null!)
                return;
            _materialSubscription = Observable.FromEventPattern<AvaloniaPropertyChangedEventArgs>(
                    h => Material.PropertyChanged += h,
                    h => Material.PropertyChanged -= h)
                .Subscribe(_ => UpdateMaterialSubscription());
            SyncMaterial(CompositionVisual);
        }
        
        private void SyncMaterial(CompositionVisual? visual)
        {
            if (visual is CompositionExperimentalAcrylicVisual v)
            {
                v.CornerRadius = CornerRadius;
                v.Material = (ImmutableExperimentalAcrylicMaterial)Material.ToImmutable();
            }
        }
        
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if(change.Property == MaterialProperty)
                UpdateMaterialSubscription();
            if(change.Property == CornerRadiusProperty)
                SyncMaterial(CompositionVisual);
            base.OnPropertyChanged(change);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            UpdateMaterialSubscription();
            _subscription?.Dispose();
        }

        private protected override CompositionDrawListVisual CreateCompositionVisual(Compositor compositor)
        {
            var v = new CompositionExperimentalAcrylicVisual(compositor, this);
            SyncMaterial(v);

            return v;
        }

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size.</param>
        /// <returns>The desired size of the control.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            return LayoutHelper.MeasureChild(Child, availableSize, Padding);
        }

        /// <summary>
        /// Arranges the control's child.
        /// </summary>
        /// <param name="finalSize">The size allocated to the control.</param>
        /// <returns>The space taken.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            return LayoutHelper.ArrangeChild(Child, finalSize, Padding);
        }
    }
}
