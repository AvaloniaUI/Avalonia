using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.VisualTree;

namespace ControlCatalog.Pages;

public partial class CompositionPage : UserControl
{
    private ImplicitAnimationCollection? _implicitAnimations;

    public CompositionPage()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        this.Get<ItemsControl>("Items").Items = CreateColorItems();
    }

    private List<CompositionPageColorItem> CreateColorItems()
    {
        var list = new List<CompositionPageColorItem>();

        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 255, 185, 0)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 231, 72, 86)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 0, 120, 215)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 0, 153, 188)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 122, 117, 116)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 118, 118, 118)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 255, 141, 0)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 232, 17, 35)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 0, 99, 177)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 45, 125, 154)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 93, 90, 88)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 76, 74, 72)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 247, 99, 12)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 234, 0, 94)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 142, 140, 216)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 0, 183, 195)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 104, 118, 138)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 105, 121, 126)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 202, 80, 16)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 195, 0, 82)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 107, 105, 214)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 3, 131, 135)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 81, 92, 107)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 74, 84, 89)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 218, 59, 1)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 227, 0, 140)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 135, 100, 184)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 0, 178, 148)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 86, 124, 115)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 100, 124, 100)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 239, 105, 80)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 191, 0, 119)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 116, 77, 169)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 1, 133, 116)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 72, 104, 96)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 82, 94, 84)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 209, 52, 56)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 194, 57, 179)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 177, 70, 194)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 0, 204, 106)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 73, 130, 5)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 132, 117, 69)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 255, 67, 67)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 154, 0, 137)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 136, 23, 152)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 16, 137, 62)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 16, 124, 16)));
        list.Add(new CompositionPageColorItem(Color.FromArgb(255, 126, 115, 95)));

        return list;
    }
    
    private void EnsureImplicitAnimations()
    {
        if (_implicitAnimations == null)
        {
            var compositor = ElementComposition.GetElementVisual(this)!.Compositor;

            var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
            offsetAnimation.Target = "Offset";
            offsetAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
            offsetAnimation.Duration = TimeSpan.FromMilliseconds(400);

            var rotationAnimation = compositor.CreateScalarKeyFrameAnimation();
            rotationAnimation.Target = "RotationAngle";
            rotationAnimation.InsertKeyFrame(.5f, 0.160f);
            rotationAnimation.InsertKeyFrame(1f, 0f);
            rotationAnimation.Duration = TimeSpan.FromMilliseconds(400);

            var animationGroup = compositor.CreateAnimationGroup();
            animationGroup.Add(offsetAnimation);
            animationGroup.Add(rotationAnimation);

            _implicitAnimations = compositor.CreateImplicitAnimationCollection();
            _implicitAnimations["Offset"] = animationGroup;
        }
    }

    public static void SetEnableAnimations(Border border, bool value)
    {
        var page = border.FindAncestorOfType<CompositionPage>();
        if (page == null)
        {
            border.AttachedToVisualTree += delegate { SetEnableAnimations(border, true); };
            return;
        }

        if (ElementComposition.GetElementVisual(page) == null)
            return;

        page.EnsureImplicitAnimations();
        if (border.GetVisualParent() is Visual visualParent 
            && ElementComposition.GetElementVisual(visualParent) is CompositionVisual compositionVisual)
        {
            compositionVisual.ImplicitAnimations = page._implicitAnimations;
        }
    }
}

public class CompositionPageColorItem
{
    public Color Color { get; private set; }

    public SolidColorBrush ColorBrush
    {
        get { return new SolidColorBrush(Color); }
    }

    public String ColorHexValue
    {
        get { return Color.ToString().Substring(3).ToUpperInvariant(); }
    }

    public CompositionPageColorItem(Color color)
    {
        Color = color;
    }
}
