# Styling in Avalonia

The main difference between Avalonia and existing XAML toolkits such as WPF and
UWP is in styling. Styling in Avalonia uses a CSS-like system that aims to be
more powerful and flexible than existing XAML styling systems. For convenience
for the rest of this document we'll refer to existing XAML toolkit's styling as
"WPF styling" as that's where it originated.

## Basics

- Styles are defined on the `Control.Styles` collection (as opposed to in
`ResourceDictionaries` in WPF).
- Styles have a `Selector` and a collection of `Setter`s
- Selector works like a CSS selector.
- Setters function like WPF's setters.
- Styles are applied to a control and all its descendants, depending on whether
  the selector matches.

## Simple example

Make all `Button`s in a `StackPanel` have a blue `Background`:

```xaml
<StackPanel>
  <StackPanel.Styles>
    <Style Selector="Button">
      <Setter Property="Button.Background" Value="Blue"/>
    </Style>
  </StackPanel.Styles>
  <Button>I will have a blue background.</Button>
</StackPanel>
```

This is very similar to WPF, except `TargetType` is replaced by `Selector`.

_Note that currently (as of Alpha 2) you **always** need to specify the fully
qualified property name (i.e. `Button.Background` instead of simply
`Background`). This restriction will be lifted in future._

## Style Classes

As in CSS, controls can be given *style classes* which can be used in selectors:

```xaml
<StackPanel>
  <StackPanel.Styles>
    <Style Selector="Button.blue">
      <Setter Property="Button.Background" Value="Blue"/>
    </Style>
  </StackPanel.Styles>
  <Button Classes="blue">I will have a blue background.</Button>
  <Button>I will not.</Button>
</StackPanel>
```

Each control can be given 0 or more style classes. This is different to WPF
where only a single style can be applied to a control: in Avalonia any number
of separate styles can be applied to a control. If more than one style affects
a particular property, the style closest to the control will take precedence.

Style classes can also be manipulated in code using the `Classes` collection:

```csharp
control.Classes.Add("blue");
control.Classes.Remove("red");
```

## Pseudoclasses

Also as in CSS, controls can have pseudoclasses; these are classes that are
defined by the control itself rather than by the user. Pseudoclasses start
with a `:` character.

One example of a pseudoclass is the `:pointerover`
pseudoclass (`:hover` in CSS - we may change to that in future).

Pseudoclasses provide the functionality of `Triggers` in WPF and
`VisualStateManager` in UWP:

```xaml
<StackPanel>
  <StackPanel.Styles>
    <Style Selector="Button:pointerover">
      <Setter Property="Button.Foreground" Value="Red"/>
    </Style>
  </StackPanel.Styles>
  <Button>I will have red text when hovered.</Button>
</StackPanel>
```

Other pseudoclasses include `:focus`, `:disabled`, `:pressed` for buttons,
`:checked` for checkboxes etc.

## Named Controls

Named controls can be selected using `#` as in CSS, e.g. `Button#Name`.

## Children

As with CSS, you can select children and descendants:

- `StackPanel > Button#Foo` selects a `Button` named `"Foo"` that is the child
  of a `StackPanel`.
- `StackPanel Button.foo` selects all `Button`s with the `foo` class that are
  descendants of a `StackPanel`.

## Templates

You can select controls in the template of a lookless control by using the
`/template/` selector, so `Button /template/ Border#outline` selects `Border`
controls named `"outline"` in the template of a `Button`.
