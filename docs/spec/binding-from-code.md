# Binding from Code

Avalonia binding from code works somewhat differently to WPF/UWP. At the low level, Avalonia's
binding system is based on Reactive Extensions' `IObservable` which is then built upon by XAML
bindings (which can also be instantiated in code).

## Binding to an observable

You can bind a property to an observable using the `AvaloniaObject.Bind` method:

```csharp
// We use an Rx Subject here so we can push new values using OnNext
var source = new Subject<string>();
var textBlock = new TextBlock();

// Bind TextBlock.Text to source
textBlock.Bind(TextBlock.TextProperty, source);

// Set textBlock.Text to "hello"
source.OnNext("hello");
// Set textBlock.Text to "world!"
source.OnNext("world!");
```

## Binding priorities

You can also pass a priority to a binding. *Note: Priorities only apply to styled properties: they*
*are ignored for direct properties.*

The priority is passed using the `BindingPriority` enum, which looks like this:

```csharp
/// <summary>
/// The priority of a binding.
/// </summary>
public enum BindingPriority
{
    /// <summary>
    /// A value that comes from an animation.
    /// </summary>
    Animation = -1,

    /// <summary>
    /// A local value: this is the default.
    /// </summary>
    LocalValue = 0,

    /// <summary>
    /// A triggered style binding.
    /// </summary>
    /// <remarks>
    /// A style trigger is a selector such as .class which overrides a
    /// <see cref="TemplatedParent"/> binding. In this way, a basic control can have
    /// for example a Background from the templated parent which changes when the
    /// control has the :pointerover class.
    /// </remarks>
    StyleTrigger,

    /// <summary>
    /// A binding to a property on the templated parent.
    /// </summary>
    TemplatedParent,

    /// <summary>
    /// A style binding.
    /// </summary>
    Style,

    /// <summary>
    /// The binding is uninitialized.
    /// </summary>
    Unset = int.MaxValue,
}
```

Bindings with a priority with a smaller number take precedence over bindings with a higher value
priority, and bindings added more recently take precedence over other bindings with the same
priority. Whenever the binding produces `AvaloniaProperty.UnsetValue` then the next binding in the
priority order is selected.

## Setting a binding in an object initializer

It is often useful to set up bindings in object initializers. You can do this using the indexer:

```csharp
var source = new Subject<string>();
var textBlock = new TextBlock
{
    Foreground = Brushes.Red,
    MaxWidth = 200,
    [!TextBlock.TextProperty] = source.ToBinding(),
};
```

Using this method you can also easily bind a property on one control to a property on another:

```csharp
var textBlock1 = new TextBlock();
var textBlock2 = new TextBlock
{
    Foreground = Brushes.Red,
    MaxWidth = 200,
    [!TextBlock.TextProperty] = textBlock1[!TextBlock.TextProperty],
};
```

Of course the indexer can be used outside object initializers too:

```csharp
textBlock2[!TextBlock.TextProperty] = textBlock1[!TextBlock.TextProperty];
```

# Transforming binding values

Because we're working with observables, we can easily transform the values we're binding!

```csharp
var source = new Subject<string>();
var textBlock = new TextBlock
{
    Foreground = Brushes.Red,
    MaxWidth = 200,
    [!TextBlock.TextProperty] = source.Select(x => "Hello " + x).ToBinding(),
};
```

# Using XAML bindings from code

Sometimes when you want the additional features that XAML bindings provide, it's easier to use XAML bindings from code. For example, using only observables you could bind to a property on `DataContext` like this:

```csharp
var textBlock = new TextBlock();
var viewModelProperty = textBlock.GetObservable(TextBlock.DataContext)
    .OfType<MyViewModel>()
    .Select(x => x?.Name);
textBlock.Bind(TextBlock, viewModelProperty);
```

However, it might be preferable to use a XAML binding in this case:

```csharp
var textBlock = new TextBlock
{
    [!TextBlock.TextProperty] = new Binding("Name")
};
```

By using XAML binding objects, you get access to binding to named controls and [all the other features that XAML bindings bring](binding-from.xaml.md):

```csharp
var textBlock = new TextBlock
{
    [!TextBlock.TextProperty] = new Binding("Text") { ElementName = "other" }
};
```

