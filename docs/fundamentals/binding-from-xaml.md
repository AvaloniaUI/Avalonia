# Binding from XAML

Binding from XAML works on the whole the same as in other XAML frameworks: you use the `{Binding}`
markup extension. Avalonia does have some extra syntacic niceties however. Here's an overview of
what you can currently do in Avalonia:

## Binding to a property on the DataContext

By default a binding binds to a property on the `DataContext`, e.g.:

```xml
<!-- Binds to the tb.DataContext.Name property -->
<TextBlock Name="tb" Text="{Binding Name}"/>
<!-- Which is the same as ('Path' is optional) -->
<TextBlock Name="tb" Text="{Binding Path=Name}"/>
```

An empty binding binds to DataContext itself

```xml
<!-- Binds to the tb.DataContext property -->
<TextBlock Name="tb" Text="{Binding}"/>
<!-- Which is the same as -->
<TextBlock Name="tb" Text="{Binding .}"/>
```

This usage is identical to WPF/UWP etc.

## Two way bindings and more

You can also specify a binding `Mode`:

```xml
<!-- Bind two-way to the property (although this is actually the default binding mode for
     TextBox.Text) so strictly speaking it's unnecessary here) -->
<TextBox Name="tb" Text="{Binding Name, Mode=TwoWay}"/>
```

This usage is identical to WPF/UWP etc.

## Binding to a property on the templated parent

When you're creating a control template and you want to bind to the templated parent you can use:

```xml
<TextBlock Name="tb" Text="{TemplateBinding Caption}"/>
<!-- Which is short for -->
<TextBlock Name="tb" Text="{Binding Caption, RelativeSource={RelativeSource TemplatedParent}}"/>
```

This usage is identical to WPF/UWP etc.

## Binding to a named control

If you want to bind to a property on another (named) control, you can use `ElementName` as in
WPF/UWP:

```xml
<!-- Binds to the Tag property of a control named "other" -->
<TextBlock Text="{Binding Tag, ElementName=other}"/>
```

However Avalonia also introduces a shorthand syntax for this:

```xml
<TextBlock Text="{Binding #other.Tag}"/>
```

## Negating bindings

You can also negate the value of a binding using the `!` operator:

```xml
<TextBox IsEnabled="{Binding !HasErrors}"/>
```

Here, the `TextBox` will only be enabled when the view model signals that it has no errors. Behind
the scenes, Avalonia tries to convert the incoming value to a boolean, and if it can be converted
it negates the value. If the incoming value cannot be converted to a boolean then no value will be
pushed to the binding target.

This syntax is specific to Avalonia.

## Binding to tasks and observables

You can subscribe to the result of a task or an observable by using the `^` stream binding operator.

```xml
<!-- If DataContext.Name is an IObservable<string> then this will bind to the length of each
     string produced by the observable as each value is produced -->
<TextBlock Text="{Binding Name^.Length}"/>
```

This syntax is specific to Avalonia.

*Note: the stream operator is actually extensible, see
[here](https://github.com/AvaloniaUI/Avalonia/blob/master/src/Markup/Avalonia.Markup/Data/Plugins/IStreamPlugin.cs)
for the interface to implement and [here](https://github.com/AvaloniaUI/Avalonia/blob/master/src/Markup/Avalonia.Markup/Data/ExpressionObserver.cs#L47)
for the registration.*
