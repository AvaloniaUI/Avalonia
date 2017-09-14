# Avalonia for WPF Developers

Avalonia is in general very similar to WPF, but you will find differences. Here
are the most common:

## Styling

The most obvious difference from other XAML frameworks is that Avalonia uses a
[CSS-like styling system](../spec/styles.md). Styles aren't stored in a
`Resources` collection as in WPF, they are stored in a separate `Styles`
collection:

    <UserControl>
        <UserControl.Styles>
            <!-- Make TextBlocks with the h1 style class have a font size of 24 points -->
            <Style Selector="TextBlock.h1">
                <Setter Property="FontSize" Value="24"/>
            </Style>
        </UserControl.Styles>
        <TextBlock Classes="h1">Header</TextBlock>
    <UserControl>    

## DataTemplates

As styles aren't stored  in `Resources`, neither are `DataTemplates` ([in fact
there is no `Resources` collection](#resources)). Instead, `DataTemplates` are
placed in a `DataTemplates` collection on each control (and on `Application`):

    <UserControl xmlns:viewmodels="clr-namespace:MyApp.ViewModels;assembly=MyApp">
        <UserControl.DataTemplates>
            <DataTemplate DataType="viewmodels:FooViewModel">
                <Border Background="Red" CornerRadius="8">
                    <TextBox Text="{Binding Name}"/>
                </Border>
            </DataTemplate>
        </UserControl.DataTemplates>
        <!-- Assuming that DataContext.Foo is an object of type
             MyApp.ViewModels.FooViewModel then a red border with a corner
             radius of 8 containing a TextBox will be displayed here -->
        <ContentControl Content="{Binding Foo}"/>
    <UserControl>    

Data templates in Avalonia can also target interfaces and derived classes (which
cannot be done in WPF) and so the order of `DataTemplate`s can be important:
`DataTemplate`s  within the same collection are evaluated in declaration order
so you need to place them from most-specific to least-specific as you would in
code.

## HierachicalDataTemplate

WPF's `HierarchicalDataTemplate` is called `TreeDataTemplate` in Avalonia (as the
former is difficult to type!). The two are almost entirely equivalent except
that the `ItemTemplate` property is not present in Avalonia.

## UIElement, FrameworkElement and Control

WPF's `UIElement` and `FrameworkElement` are non-templated control base classes,
which roughly equate to the Avalonia `Control` class. WPF's `Control` class on
the other hand is a templated control - Avalonia's equivalent of this is
`TemplatedControl`.

So to recap:

- `UIElement`: `Control`
- `FrameworkElement`: `Control`
- `Control`: `TemplatedControl`

## DependencyProperty

The Avalonia equivalent of `DependencyProperty` is `StyledProperty`, however
Avalonia [has a richer property system than WPF](../spec/defining-properties.md),
and includes `DirectProperty` for turning standard CLR properties into Avalonia
properties. The common base class of `StyledProperty` and `DirectProperty`
is `AvaloniaProperty`.

## Grid

Column and row definitions can be specified in Avalonia using strings, avoiding
the clunky syntax in WPF:

    <Grid ColumnDefinitions="Auto,*,32" RowDefinitions="*,Auto">

A common use of `Grid` in WPF is to stack two controls on top of each other.
For this purpose in Avalonia you can just use a `Panel` which is more lightweight
than `Grid`.

We don't yet support `SharedSizeScope` in `Grid`.

## ItemsControl

In WPF, `ItemsControl` and derived classes such as `ListBox` have two separate
items properties: `Items` and `ItemsSource`. Avalonia however just has a single
one: `Items`.

## Tunnelling Events

Avalonia has tunnelling events (unlike UWP!) but they're not exposed via
separate `Preview` CLR event handlers. To subscribe to a tunnelling event you
must call `AddHandler` with `RoutingStrategies.Tunnel`:

```
target.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);

void OnPreviewKeyDown(object sender, KeyEventArgs e)
{
    // Handler code
}
```

## Class Handlers

In WPF, class handlers for events can be added by calling
[EventManager.RegisterClassHandler](https://msdn.microsoft.com/en-us/library/ms597875.aspx).
An example of registering a class handler in WPF might be:

    static MyControl()
    {
      EventManager.RegisterClassHandler(typeof(MyControl), MyEvent, HandleMyEvent));
    }

    private static void HandleMyEvent(object sender, RoutedEventArgs e)
    {
    }

The equivalent of this in Avalonia would be:

    static MyControl()
    {
        MyEvent.AddClassHandler<MyControl>(x => x.HandleMyEvent);
    }

    private void HandleMyEvent(object sender, RoutedEventArgs e)
    {
    }

Notice that in WPF you have to add the class handler as a static method, whereas
in Avalonia the class handler is not static: the notification is automatically
directed to the correct instance.

## PropertyChangedCallback

Listening to changes on DependencyProperties in WPF can be complex. When you
register a `DependencyProperty` you can supply a static `PropertyChangedCallback`
but if you want to listen to changes from elsewhere [things can get complicated
and error-prone](http://stackoverflow.com/questions/23682232).

In Avalonia, there is no `PropertyChangedCallback` at the time of registration,
instead a class listener is [added to the control's static constructor in much
the same way that event class listeners are added](../spec/working-with-properties.md#subscribing-to-a-property-on-any-object).

## RenderTransforms and RenderTransformOrigin

RenderTransformOrigins are different in WPF and Avalonia: If you apply a `RenderTransform`, keep in mind that our default value for the RenderTransformOrigin is `RelativePoint.Center`. In WPF the default value is `RelativePoint.TopLeft` (0, 0). In controls like Viewbox (currently being developed) the same code will lead to a different rendering behavior:

In WPF:
![WPF](https://files.gitter.im/AvaloniaUI/Avalonia/cDrM/image.png)

In Avalonia:
![Avalonia](https://files.gitter.im/AvaloniaUI/Avalonia/KGk7/image.png)

In AvaloniaUI, to get the same scale transform we should indicate that the RenderTransformOrigin is the TopLeft part of the Visual. 
