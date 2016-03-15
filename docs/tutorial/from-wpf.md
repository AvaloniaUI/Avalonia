# Perspex for WPF Developers

Perspex is in general very similar to WPF, but you will find differences. Here
are the most common:

## Styling

The most obvious difference from other XAML frameworks is that Perspex uses a
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
there is no `Resources` collection](#Resources)). Instead, `DataTemplates` are
placed in a `DataTemplates` collection on each control (and on `Application`):

    <UserControl xmlns:viewmodels="clr-namespace:MyApp.ViewModels;assembly=MyApp">
        <UserControl.DataTemplates>
            <DataTemplate DataType="viewmodels:FooViewModel">
                <Border Background="Red" CornerRadius="8">
                    <TextBox Text="{Binding Name}"/>
                </Border>
            </DataTemplate>
        </UserControl.Styles>
        <!-- Assuming that DataContext.Foo is an object of type
             MyApp.ViewModels.FooViewModel then a red border with a corner
             radius of 8 containing a TextBox will be displayed here -->
        <ContentControl Content="{Binding Foo}"/>
    <UserControl>    

## UIElement, FrameworkElement and Control

WPF's `UIElement` and `FrameworkElement` are non-templated control base classes,
which roughly equate to the Perspex `Control` class. WPF's `Control` class on
the other hand is a templated control - Perspex's equivalent of this is
`TemplatedControl`.

So to recap:

- `UIElement`: `Control`
- `FrameworkElement`: `Control`
- `Control`: `TemplatedControl`

## DependencyProperty

The Perspex equivalent of `DependencyProperty` is `StyledProperty`, however
Perspex [has a richer property system than WPF](../spec/defining-properties.md),
and includes `DirectProperty` for turning standard CLR properties into Perspex
properties. The common base class of `StyledProperty` and `DirectProperty`
is `PerspexProperty`.

# Resources

There is no `Resources` collection on controls in Perspex, however `Style`s
do have a `Resources` collection for style-related resources. These can be
referred to using the `{StyleResource}` markup extension both inside and outside
styles.

For non-style-related resources, we suggest defining them in code and referring
to them in markup using the `{Static}` markup extension. There are [various
reasons](http://www.codemag.com/article/1501091) for this, but briefly:

- Resources have to be parsed
- The tree has to be traversed to find them
- XAML doesn't handle immutable objects
- XAML syntax can be long-winded compared to C#

## Grid

Perspex has a `Grid` panel just like WPF, however a common use of `Grid` in WPF
is to stack two controls on top of each other. For this purpose in Perspex you
can just use a `Panel` which is more lightweight than `Grid`.

We don't yet support `SharedSizeScope` in `Grid`.

## Tunnelling Events

Perspex has tunnelling events (unlike UWP!) but they're not exposed via
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

The equivalent of this in Perspex would be:

    static MyControl()
    {
        MyEvent.AddClassHandler<MyControl>(x => x.HandleMyEvent);
    }

    private void HandleMyEvent(object sender, RoutedEventArgs e)
    {
    }

Notice that in WPF you have to add the class handler as a static method, whereas
in Perspex the class handler is not static: the notification is automatically
directed to the correct instance.

## PropertyChangedCallback

Listening to changes on DependencyProperties in WPF can be complex. When you
register a `DependencyProperty` you can supply a static `PropertyChangedCallback`
but if you want to listen to changes from elsewhere [things can get complicated
and error-prone](http://stackoverflow.com/questions/23682232).

In Perspex, there is no `PropertyChangedCallback` at the time of registration,
instead a class listener is [added to the control's static constructor in much
the same way that event class listeners are added](../spec/working-with-properties.md#Subscribing%20%to%20%a%20%Property%20%on%20%Any%20%Object).
