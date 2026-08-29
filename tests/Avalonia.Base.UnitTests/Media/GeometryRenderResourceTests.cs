using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Media;

public class GeometryRenderResourceTests
{
    [Fact]
    public void Changing_Geometry_Property_Raises_Invalidated()
    {
        var target = new EllipseGeometry(new Rect(0, 0, 10, 10));

        RenderResourceTestHelper.AssertResourceInvalidation(
            target,
            () => target.Rect = new Rect(0, 0, 20, 20));
    }

    [Fact]
    public void Changing_Transform_Raises_Invalidated()
    {
        var target = new EllipseGeometry(new Rect(0, 0, 10, 10));

        RenderResourceTestHelper.AssertResourceInvalidation(
            target,
            () => target.Transform = new TranslateTransform(5, 5));
    }

    [Fact]
    public void Changing_Transform_Value_Raises_Invalidated()
    {
        var transform = new TranslateTransform(5, 5);
        var target = new EllipseGeometry(new Rect(0, 0, 10, 10)) { Transform = transform };

        RenderResourceTestHelper.AssertResourceInvalidation(
            target,
            () => transform.X = 10);
    }

    [Fact]
    public void Adding_Child_To_GeometryGroup_Raises_Invalidated()
    {
        var target = new GeometryGroup();

        RenderResourceTestHelper.AssertResourceInvalidation(
            target,
            () => target.Children.Add(new EllipseGeometry(new Rect(0, 0, 10, 10))));
    }

    [Fact]
    public void Changing_Child_Of_GeometryGroup_Raises_Invalidated()
    {
        var child = new EllipseGeometry(new Rect(0, 0, 10, 10));
        var target = new GeometryGroup();
        target.Children.Add(child);

        RenderResourceTestHelper.AssertResourceInvalidation(
            target,
            () => child.Rect = new Rect(0, 0, 20, 20));
    }

    [Fact]
    public void Changing_Geometry1_Of_CombinedGeometry_Raises_Invalidated()
    {
        var geometry1 = new EllipseGeometry(new Rect(0, 0, 10, 10));
        var geometry2 = new RectangleGeometry(new Rect(5, 5, 10, 10));
        var target = new CombinedGeometry(GeometryCombineMode.Union, geometry1, geometry2);

        RenderResourceTestHelper.AssertResourceInvalidation(
            target,
            () => geometry1.Rect = new Rect(0, 0, 20, 20));
    }

    [Fact]
    public void Changing_CombineMode_Of_CombinedGeometry_Raises_Invalidated()
    {
        var geometry1 = new EllipseGeometry(new Rect(0, 0, 10, 10));
        var geometry2 = new RectangleGeometry(new Rect(5, 5, 10, 10));
        var target = new CombinedGeometry(GeometryCombineMode.Union, geometry1, geometry2);

        RenderResourceTestHelper.AssertResourceInvalidation(
            target,
            () => target.GeometryCombineMode = GeometryCombineMode.Intersect);
    }
}
