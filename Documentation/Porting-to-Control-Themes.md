# Porting to Control Themes

Avalonia 11.0 has introduced a new concept called Control Themes. In previous versions, control themes were defined by styles, but there was a problem: it was impossible to override the theme of an indivdual control because the global theme would still be applied. Because of this when you wanted to fully re-theme a control you needed to reset every value set in the globally-applied theme.

Control themes provide a solution for this. Starting from 11.0, control themes are used for both the Avalonia Fluent and Simple themes, and we recommend that you port any 3rd party themes too. Only a single control theme can apply to any individual control, meaning that styles from the global theme won't "leak" into other themes.

> You're not forced to use control themes: existing style-based themes will continue to work as before

This guide will assume that you had each style for an individual control in a separate `.axaml` file with a root element of `<Styles>`. For a more detailed look into how this was done for the Avalonia Fluent theme, see the [pull request](https://github.com/AvaloniaUI/Avalonia/pull/8479).

We're going to use `[ControlType]` to refer to the type of control that the styles were targeting.

## Move the Styles Into a Resource Dictionary

The first step of porting from styles to control themes is to move the style into a resource dictionary. To do this we'll need to change the root element to be a `<ResourceDictionary>`

For example:

```xml
<Styles xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
```

Becomes:

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
```

## Move any resources from `<Styles.Resources>`

Because the root element is now a `ResourceDictionary`, any resources previously defined in `<Styles.Resources>` can be moved to be child of the root element.

```xml
<Styles.Resources>
  <Thickness x:Key="ListBoxItemPadding">12,9,12,12</Thickness>
</Styles.Resources>
```

Becomes:

```xml
<Thickness x:Key="ListBoxItemPadding">12,9,12,12</Thickness>
```

## Change the Base `<Style>` Element to `<ControlTheme>`

First find the base `Style` element within the file. This will usually be of the form `<Style Selector="[ControlType]">`.

Change this element to a `<ControlTheme>`, with an `x:Key` and `TargetType` which specify the `[ControlType]`. For example:

```xml
<Style Selector="ListBoxItem">
```

Becomes

```xml
<ControlTheme x:Key="{x:Type ListBoxItem}" TargetType="ListBoxItem">
```

## Move the Trigger Styles into the ControlTheme

Most control themes will usually have additional styles that apply on some trigger; for example when the button is pressed. When using `<Styles>`, these were usually declared after the base style but with control themes these styles need to be moved into the `<ControlTheme>` itself.

In addition, the first part of the selector which should be `[ControlType]` must be changed to `^`, the parent selector.

For example:

```xml
<Styles xmlns="https://github.com/avaloniaui">
  <Style Selector="ListBoxItem">
    ...
  </Style>

  <Style Selector="ListBoxItem:pressed /template/ ContentPresenter#PART_ContentPresenter">
    ...
  </Style>
</Styles>
```

Becomes:

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui">
  <ControlTheme x:Key="{x:Type ListBoxItem}" TargetType="ListBoxItem">
    ...
    <Style Selector="^:pressed /template/ ContentPresenter#PART_ContentPresenter">
      ...
    </Style>
  </ControlTheme>
</Styles>
```

## Template Priority

Before Avalonia 11, values set in a XAML template were assigned with `LocalValue` priority, meaning that they could not be overridden by a style. This is no longer the case, and values set in a XAML template will be set with `Template` priority, which can be overridden by styles with triggers. This may surface bugs in your styles where previously setters had no effect.

## Styling descendents

Prevously, a `<Style>` could "reach outside" the control being themed to also affect descendents of the control. This is no longer possible with control themes as their very reason for existence is that they can't "leak through" to other controls.

If you still need to do this, you can still use a `<Style>` which lives outside of a `<ControlTheme>` but there are usually better options:

- Check whether the property you want to set on the child control is an inherited property. If it is, you can simply set it on the presenter and it will be inherited
- One common usage of this pattern was to set the color of a `<Shape>` such as a `<Path>`. Instead use a `<PathIcon>` which uses the inherited `Foreground` property as its `Fill`

