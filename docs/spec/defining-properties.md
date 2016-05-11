# Defining Properties

If you are creating a control, you will want to define properties on your
control. The process in Avalonia is broadly similar to other XAML languages
with a few differences - the main one being that Perpsex's equivalent of
`DependencyProperty` is called `StyledProperty`.

## Registering Styled Properties

A styled property is analogous to a `DependencyProperty` in other XAML
frameworks.

You register a styled property by calling `AvaloniaProperty.Register` and
storing the result in a `static readonly` field. You then create a standard C#
property to access it.

Here's how the `Border` control defines its `Background` property:

```c#
    public static readonly StyledProperty<Brush> BackgroundProperty =
        AvaloniaProperty.Register<Border, Brush>(nameof(Background));

    public Brush Background
    {
        get { return GetValue(BackgroundProperty); }
        set { SetValue(BackgroundProperty, value); }
    }
```

The `AvaloniaProperty.Register` method also accepts a number of other parameters:

- `defaultValue`: This gives the property a default value. Be sure to only pass
value types and immutable types here as passing a reference type will cause the
same object to be used on all instances on which the property is registered.
- `inherits`: Specified that the property's default value should come from
the parent control.
- `defaultBindingMode`: The default binding mode for the property. Can be set to
`OneWay`, `TwoWay`, `OneTime` or `OneWayToSource`.
- `validate`: A validation/coercion function of type
`Func<TOwner, TValue, TValue>`. The function accepts the instance of the class
on which the property is being set and the value and returns the coerced value
or throws an exception for an invalid value.

## Using a StyledProperty on Another Class

Sometimes the property you want to add to your control already exists on another
control, `Background` being a good example. To register a property defined on
another control, you call `StyledProperty.AddOwner`:


```c#
    public static readonly StyledProperty<Brush> BackgroundProperty =
        Border.BackgroundProperty.AddOwner<Panel>();

    public Brush Background
    {
        get { return GetValue(BackgroundProperty); }
        set { SetValue(BackgroundProperty, value); }
    }
```

*Note: Unlike WPF, a property must be registered on a class otherwise it cannot
be set on an object of that class.*

## Attached Properties

Attached properties are defined almost identically to styled properties except
that they are registered using the `RegisterAttached` method and their accessors
are defined as static methods.

Here's how `Grid` defines its `Grid.Column` attached property:

```c#
    public static readonly AttachedProperty<int> ColumnProperty =
        AvaloniaProperty.RegisterAttached<Grid, Control, int>("Column");

    public static int GetColumn(Control element)
    {
        return element.GetValue(ColumnProperty);
    }

    public static void SetColumn(Control element, int value)
    {
        element.SetValue(ColumnProperty, value);
    }
```

## Readonly AvaloniaProperties

To create a readonly property you use the `AvaloniaProperty.RegisterDirect`
method. Here is how `Visual` registers the readonly `Bounds` property:

```c#
    public static readonly DirectProperty<Visual, Rect> BoundsProperty =
        AvaloniaProperty.RegisterDirect<Visual, Rect>(
            nameof(Bounds),
            o => o.Bounds);

    private Rect _bounds;

    public Rect Bounds
    {
        get { return _bounds; }
        private set { SetAndRaise(BoundsProperty, ref _bounds, value); }
    }
```

As can be seen, readonly properties are stored as a field on the object. When
registering the property, a getter is passed which is used to access the
property value through `GetValue` and then `SetAndRaise` is used to notify
listeners to changes to the property.

## Direct AvaloniaProperties

As its name suggests, `RegisterDirect` isn't just used for registering readonly
properties. You can also pass a *setter* to `RegisterDirect` to expose a
standard C# property as a Avalonia property.

A `StyledProperty` which is registered using `AvaloniaProperty.Register`
maintains a prioritized list of values and bindings that allow styles to work.
However, this is overkill for many properties, such as `ItemsControl.Items` -
this will never be styled and the overhead involved with styled properties is
unnecessary.

Here is how `ItemsControl.Items` is registered:

```c#
    public static readonly DirectProperty<ItemsControl, IEnumerable> ItemsProperty =
        AvaloniaProperty.RegisterDirect<ItemsControl, IEnumerable>(
            nameof(Items),
            o => o.Items,
            (o, v) => o.Items = v);

    private IEnumerable _items = new AvaloniaList<object>();

    public IEnumerable Items
    {
        get { return _items; }
        set { SetAndRaise(ItemsProperty, ref _items, value); }
    }
```


Direct properties are a lightweight version of styled properties that support
the following:

- AvaloniaObject.GetValue
- AvaloniaObject.SetValue for non-readonly properties
- PropertyChanged
- Binding (only with LocalValue priority)
- GetObservable
- AddOwner
- Metadata

They don't support the following:

- Validation/Coercion (although this could be done in the property setter)
- Overriding default values.
- Inherited values

## Using a DirectProperty on Another Class

In the same way that you can call `AddOwner` on a styled property, you can also
add an owner to a direct property. Because direct properties reference fields
on the control, you must also add a field for the property:

```c#
    public static readonly DirectProperty<MyControl, IEnumerable> ItemsProperty =
        ItemsControl.ItemsProperty.AddOwner<MyControl>(
            o => o.Items,
            (o, v) => o.Items = v);

    private IEnumerable _items = new AvaloniaList<object>();

    public IEnumerable Items
    {
        get { return _items; }
        set { SetAndRaise(ItemsProperty, ref _items, value); }
    }
```

## When to use a Direct vs a Styled Property

Direct properties have advantages and disadvantages:

Pros:
- No additional object is allocated per-instance for the property
- Property getter is a standard C# property getter
- Property setter is is a standard C# property setter that raises an event.

Cons:
- Cannot inherit value from parent control
- Cannot take advantage of Avalonia's styling system
- Property value is a field and as such is allocated whether the property is
set on the object or not

So use direct properties when you have the following requirements:
- Property will not need to be styled
- Property will usually or always have a value
