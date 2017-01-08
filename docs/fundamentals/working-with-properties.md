# Working with Properties

Avalonia controls expose their properties as standard CLR properties, so for
reading and writing values there's no surprises:

```c#
    // Create a TextBlock and set its Text property.
    var textBlock = new TextBlock();
    textBlock.Text = "Hello World!";
```


However there's a lot more you can do with properties such as subscribing to
changes on the property, and binding.

# Subscribing to Changes to a Property

You can subscribe to changes on a property by calling the `GetObservable`
method. This returns an `IObservable<T>` which can be used to listen for changes
to the property:

```c#
    var textBlock = new TextBlock();
    var text = textBlock.GetObservable(TextBlock.TextProperty);
```

Each property that can be subscribed to has a static readonly field called
`[PropertyName]Property` which is passed to `GetObservable` in order to
subscribe to the property's changes.

`IObservable` (part of Reactive Extensions, or rx for short) is out of scope
for this guide, but here's an example which uses the returned observable to
print a message with the changing property values to the console:

```c#
    var textBlock = new TextBlock();
    var text = textBlock.GetObservable(TextBlock.TextProperty);
    text.Subscribe(value => Console.WriteLine(value + " Changed"));
```

When the returned observable is subscribed, it will return the current value
of the property immediately and then push a new value each time the property
changes. If you don't want the current value, you can use the rx `Skip`
operator:

```c#
    var text = textBlock.GetObservable(TextBlock.TextProperty).Skip(1);
```

# Binding a property

Observables don't just go one way! You can also use them to bind properties.
For example here we create two `TextBlock`s and bind the second's `Text`
property to the first:

```c#
  var textBlock1 = new TextBlock();
  var textBlock2 = new TextBlock();

  // Get an observable for the first text block's Text property.
  var source = textBlock1.GetObservable(TextBlock.TextProperty);

  // And bind it to the second.
  textBlock2.Bind(TextBlock.TextProperty, source);

  // Changes to the first TextBlock's Text property will now be propagated
  // to the second.
  textBlock1.Text = "Goodbye Cruel World";

  // Prints "Goodbye Cruel World"
  Console.WriteLine(textBlock2.Text);
```

To read more about creating bindings from code, see [Binding from Code](binding-from-code.md).

# Subscribing to a Property on Any Object

The `GetObservable` method returns an observable that tracks changes to a
property on a single instance. However, if you're writing a control you may
want to implement an `OnPropertyChanged` method. In WPF this is done by passing
a static `PropertyChangedCallback` to the `DependencyProperty` registration
method, but in Avalonia it's slightly different (and hopefully easier!)

The field which defines the property is derived from `AvaloniaProperty` and this
has a `Changed` observable which is fired every time the property changes on
*any* object. In addition there is an `AddClassHandler` extension method which
can automatically route the event to a method on your control.

For example if you want to listen to changes to your control's `Foo` property
you'd do it like this:

```c#
    static MyControl()
    {
        FooProperty.Changed.AddClassHandler<MyControl>(x => x.FooChanged);
    }

    private void FooChanged(AvaloniaPropertyChangedEventArgs e)
    {
        // The 'e' parameter describes what's changed.
    }
```
