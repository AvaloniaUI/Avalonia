Breaking changes are sometimes unavoidable. We are trying to deprecate API before removing it, but it's not always possible due to either high performance impact of the old design or due to major changes in the corresponding subsystem. The list of API areas where changes are planned and most likely unavoidable can be found [here](https://github.com/AvaloniaUI/Avalonia/issues/3538).

# 0.10 → 11.0

For more breaking changes in 11.0 release visit [pull request list](https://github.com/AvaloniaUI/Avalonia/pulls?q=is%3Apr+label%3Abreaking-change+closed%3A%3E2020-06-01+).

## Interfaces removed

Many redundant interfaces were removed (see https://github.com/AvaloniaUI/Avalonia/pull/9553). The concrete classes should be used instead:

- `IAvaloniaObject` -> `AvaloniaObject`
- `IControl` -> `Control`
- `IInteractive` -> `Interactive`
- `ILayoutable` -> `Layoutable`
- `IPanel` -> `Panel`
- `IStyledElement` -> `StyledElement`
- `ITemplatedControl` -> `TemplatedControl`
- `IVisual` -> `Visual`

In a few cases, the interfaces were used to hide less commonly needed functionality, e.g. `IVisual.VisualRoot`: the extension methods should be used, e.g. `control.GetVisualRoot()`.

## Avalonia package

`Avalonia.Animation`, `Avalonia.Input`, `Avalonia.Interactivity`, `Avalonia.Layout`, `Avalonia.Visuals` and `Avalonia.Styling` assemblies were merged into a `Avalonia.Base` assembly. While API surface wasn't changed, it's still a binary breaking change, and third-party packages needs to be recompiled to work with a new version.

## Platform implementation members were hidden from the ref-assemblies

Now it's not possible to access platform interfaces (including IWindowImpl, window.PlatformImpl, IWindowingPlatform...).
Replacement for some missing members:
- `Window.PlatformImpl.Handle` can be replaced with `Window.TryGetPlatformHandle`.
- `Window.PlatformImpl.Resized` can be replaced with `Window.Resized` or `Window.SizeChanged`.
- `Window.PlatformImpl.RenderScaling` can be replaced with `((IRenderRoot)Window).RenderScaling`.
- If you are missing more API members, please raise an issue so we can discuss possibility of adding new public and stable APIs.

Note, if you are developing a custom Avalonia platform backend, you need to reference non-ref assemblies from the NuGet package directly. It should be kept in mind, that API stability is not guaranteed for these members.

## Avalonia.Themes.Fluent and Avalonia.Themes.Simple themes

Both `Avalonia.Themes.Fluent` and `Avalonia.Themes.Simple` (formally, `Default`) are not a part of the main `Avalonia` NuGet package anymore. You need to add a PackageReference to include either of these packages or both. For more details, see [#5593](https://github.com/AvaloniaUI/Avalonia/issues/5593)

"Mode" property with Dark/Light values was also removed from the FluentTheme/SimpleTheme.
Instead, use Application.RequestedThemeVariant. You also can set the same property on Window or ThemeVariantScope.

Old Default theme was renamed to SimpleTheme. Now, to include Simple theme, you need to use shorten syntax:
```
<App.Styles>
    <SimpleTheme />
</App.Styles>
```
StyleInclude with old relative path won't work anymore.

## Avalonia core no longer depends on `System.Reactive`

If you wish to use any features of `System.Reactive`, for example to subscribe to observables using the lambda syntax `observable.Subscribe(x => { })` then you must add a dependency on `Avalonia.ReactiveUI` or `System.Reactive` to your application.

Similarly all extension methods that exposed an `ISubject<T>` had to be removed from the core API as that type is defined in `System.Reactive`. Add a reference to `Avalonia.ReactiveUI` to get these extension methods back.

## ItemsControl changes (including all ItemsControl inherited controls, like ListBox or Menus)

`ItemsControl.VirtualizationMode` was removed. You can control whether virtualization is enabled for a control by changing the panel, e.g.:

To disable virtualization:

```
<ItemsControl.ItemsPanel>
  <ItemsPanelTemplate>
    <StackPanel/>
  </ItemsPanelTemplate>
</ItemsControl.ItemsPanel>
```

To enable virtualization:

```
<ItemsControl.ItemsPanel>
  <ItemsPanelTemplate>
    <VirtualizingStackPanel/>
  </ItemsPanelTemplate>
</ItemsControl.ItemsPanel>
```

`ItemsControl.Items` property is now readonly and works exactly like in WPF/UWP - a collection of item containers.
To bind a collection to the items control, please use `ItemsControl.ItemsSource`. For more details, see [#10590](https://github.com/AvaloniaUI/Avalonia/pull/10590) [#10831](https://github.com/AvaloniaUI/Avalonia/pull/10831) [#10827](https://github.com/AvaloniaUI/Avalonia/pull/10827) [#11008](https://github.com/AvaloniaUI/Avalonia/pull/11008).

## ItemContainerGenerator changes

The `Containers` property was removed: use [`ItemsControl.GetRealizedContainers`](https://github.com/AvaloniaUI/Avalonia/blob/ae1fcfed51546b0bd38d67ee9619d3f4b739131e/src/Avalonia.Controls/ItemsControl.cs#L328)

## ItemsRepeater was moved to its own separated package

If you use ItemsRepeater in your project, please include https://www.nuget.org/packages/Avalonia.Controls.ItemsRepeater package

## Virtual generic methods were removed from API

Methods like OnPropertyChanged do not have generic parameter anymore. See [#7980](https://github.com/AvaloniaUI/Avalonia/pull/7980).

In additon, generic methods were removed from `IAvaloniaObject`, so if you have an instance of this interface or one of its derived interfaces (e.g. `IControl`) then you should cast to a concrete type in order to access the typed `GetValue<T>`, `SetValue<T>` etc. methods or use the extension methods in the `Avalonia` namespace.

## Avalonia.ReactiveUI.Events

`Avalonia.ReactiveUI.Events` will no longer be supported. Use [Pharmacist.MSBuild](https://www.nuget.org/packages/Pharmacist.MSBuild/) instead. [How to use it](https://github.com/reactiveui/Pharmacist#msbuild)? For more details, see [#5423](https://github.com/AvaloniaUI/Avalonia/pull/5423)

## XamlNameReferenceGenerator is part of the main package now

Because of that, there are two breaking changes:
1. If you had XamlNameReferenceGenerator used in the project before, it will conflict with build in generator now. Simply delete reference to the XamlNameReferenceGenerator package to avoid conflicts.
2. If you didn't use XamlNameReferenceGenerator before, now it is used automatically, which might cause compile errors (missing `partial` keyword, for instance). If you want to disable this generator, add `<AvaloniaNameGeneratorIsEnabled>false</AvaloniaNameGeneratorIsEnabled>` to your csproj file.

## Removal of Default/Empty properties from some value types

Default/Empty properties were removed from types like Rect, Point, BoxShadow and others.
In most cases, please replace it with C# "default" keyword. 

## Binding Commands to Methods

When binding to methods, the method must either have no parameters or a single `object` parameter, see [#8905](https://github.com/AvaloniaUI/Avalonia/pull/8905).

## It's not possible to create style setters with activators for the Direct Properties.

Previously it was possible to create a style with activators (pseudoclass or any other dynamic kind of selectors) and put setters on direct properties (including Button.Command, ListBox.Items...). It was causing undefined behavior, as it's not possible to remove value from the direct property if selector doesn't match anymore.
In 11.0 it's not allowed anymore to create setters for these properties.
At the same time, most of the old DirectProperties were converted to the StyledProperty, including Button.Command. Which means, it is possible to style them now.

## Assemblies with XAML files has changed resource structure

Previously, xaml resources were packed as XML resources in the assembly.
Now binary format is used.
Third party libraries with any XAML resources should be recompiled.
See https://github.com/AvaloniaUI/Avalonia/pull/9949

## Clipboard was moved from Application class to the TopLevel.

To access clipboard methods, please use `window.Clipboard` or `TopLevel.GetTopLevel(control).Clipboard`.

## Control changes

PointerEntered and PointerExited events were renamed to match UWP/WinUI naming. See [#8396](https://github.com/AvaloniaUI/Avalonia/pull/8396).

NumericUpDown.Value is now a decimal property. See [#5981](https://github.com/AvaloniaUI/Avalonia/pull/5981).

ManagedFileDialog is now a templated control. See [#4615](https://github.com/AvaloniaUI/Avalonia/pull/4615).


# 0.9 → 0.10

## Avalonia Properties

- If your readonly property fields are declared as of type `AvaloniaProperty<T>` then you should change them to `StyledProperty<>` or `DirectProperty<,>` fields
- The signature for `OnPropertyChanged` has changed and is now a generic method in order to avoid allocation and boxing. To cast the `oldValue` and `newValue` parameters to a concrete type, use `.ValueOrDefault<T>()` on `Optional<T>` and `BindingValue<T>`
- Avalonia properties now take a separate validation and coercion callback the same as WPF. The validation callback cannot be overridden, though the coercion callback can.

- The `PropertyMetadata` class has now been renamed to `AvaloniaPropertyMetadata`.

## Routed Events

- `Interactive.AddHandler` no longer returns an `IDisposable`. If you want a disposable you should call `AddDisposableHandler`: https://github.com/AvaloniaUI/Avalonia/pull/3651

## Binding

- Many `AvaloniaObject.Bind()` overloads have been moved to be extension methods, so you may have to add `this.` to your `Bind()` call when binding to this

## Pseudoclasses

The static `PseudoClass` method was removed: https://github.com/AvaloniaUI/Avalonia/pull/3292

The recommended way to implement pseudoclasses is now like this https://github.com/AvaloniaUI/Avalonia/pull/3292/files#diff-45a4dd48c9a2f83d7def2fd422d1423c

## DrawingContext

- Removed opacity parameter from `DrawingContext.DrawImage` - instead use `PushOpacity` before drawing the image

## Typeface/FormattedText

- `FontSize` is no longer part of `TypeFace` - it can now be found on `FormattedText`
- `FormattedText.Wrapping` is now called `TextWrapping`

## DevTools

`Avalonia.Diagnostics` is now a separate NuGet [package](https://www.nuget.org/packages/Avalonia.Diagnostics) so if you're using `AttachDevTools` you'll have to add a reference to that.

## OnTemplateApplied
`OnTemplatedApplied` has been renamed to on `OnApplyTemplate`.

## DatePicker
`DatePicker` was renamed to `CalendarDatePicker`. A `DatePicker` control has been added.

## Logging

The `LogToDebug` method has moved from the `Avalonia.Logging.Serilog` namesapce to the `Avalonia` namespace. Remove `using Avalonia.Logging.Serilog;` to fix.

# 0.8 → 0.9

## Application startup

The preferred way of managing app startup and lifetime is now using [lifetimes](https://github.com/AvaloniaUI/Avalonia/wiki/Application-lifetimes). You still can use AppMain approach introduced in 0.8 for more fine-grained control, but some of Application.Run/AppBuilder.Start overloads were removed or deprecated.

## XAML
- x:Class is now mandatory for XAML files with codebehind (windows, user controls, `App.cs`, etc)
- Class constructors and codebehind event handlers must be public
- `<Style>` without selector is no longer valid
- Style selectors without type information (e. g. `<Style Selector=".myclass">`) are no longer valid, specify the target type using either `Control.myclass` or `:is(Control).myclass`

## Name scopes

Controls are no longer automagically registered in some random name scope that they find in the visual tree. Instead name scopes are now managed manually. If you are using XAML you probably won't notice any breaking changes since our XAML engine manages name registrations automatically, but just setting `Name` from code will no longer work, you'd have to actually register your controls and pass `INameScope` instance to bindings.

## MemberSelector

`ItemsControl.MemberSelector` has been removed. Instead use a `DataTemplate` with a binding to the property to be displayed.

## Orientation

The `Orientation` enum was moved to the Avalonia.Layout namespace.

## AdornerDecorator

Has been renamed to `VisualLayerManager`.

## IWindowImpl/Window

Both `BeginResizeDrag` and `BeginMoveDrag` now has an additional `PointerPressedEventArg` parameter. 

# 0.7 → 0.8

## DropDown

`DropDown` has been renamed to `ComboBox`. A shim for `DropDown` is still available for now but deprecated and will be removed in a future release.

## FormattedText

`FormattedText.Measure` has become `FormattedText.Bounds`.

See https://github.com/AvaloniaUI/Avalonia/pull/2344

## Screen Coordinates

The following members now use `PixelPosition`/`PixelRect`:

- `IWindowBaseImpl.Position`
- `IWindowBaseImpl.PositionChanged`
- `ITopLevelImpl.PointToClient`
- `ITopLevelImpl.PointToScreen`
- `IMouseDevice.Position`
- `Screen.Bounds`
- `Screen.WorkingArea`

You can use one of the `From*` static methods and `To*` instance methods on these structs with a scaling factor to convert between `Position` and `Rect`.

See https://github.com/AvaloniaUI/Avalonia/pull/2250

## StringConverters

The `StringConverter` methods have been renamed to add an `Is` prefix:

- `StringConverters.NullOrEmpty` becomes `StringConverters.IsNullOrEmpty`
- `StringConverters.NotNullOrEmpty` becomes `StringConverters.IsNotNullOrEmpty`

See https://github.com/AvaloniaUI/Avalonia/pull/2253

## TreeContainerIndex in TreeView

The `TreeContainerIndex.Items` property have been renamed to `Containers`.

See https://github.com/AvaloniaUI/Avalonia/pull/2356

## Refactored platform options

Before:

```csharp
public static AppBuilder BuildAvaloniaApp()
{
    var builder = AppBuilder.Configure<App>();
    if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
         builder.UseX11(new X11PlatformOptions() {UseGpu = false});
    else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
         builder.UseAvaloniaNative(anopts => 
         {
              anopts.UseGpu = false;
              anopts.MacOptions.ShowInDock = 0;
         });
    else if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
          builder.UseWin32(false, true);
    return builder;
}
```

After:
```csharp
public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
     .UsePlatformDetect()
     .With(new X11PlatformOptions { UseGpu = false })
     .With(new AvaloniaNativePlatformOptions { UseGpu = false })
     .With(new MacOSPlatformOptions { ShowInDock = false })
     .With(new Win32PlatformOptions { UseDeferredRendering = false });
```

See https://github.com/AvaloniaUI/Avalonia/pull/2368

# 0.6 → 0.7
## Reactive UI
- Projects that use ReactiveUI framework will need to migrate to Reactive UI 9.0.1 which Avalonia now uses.

## TemplateBinding

- Existing `TemplateBindings` which specify `Path=` will stop working, as the `Path` property was removed and replaced with `Property`. Simply remove the `Path=` qualifier or change it to `Property=` if the path was a simple property
- Existing `TemplateBindings` which don't bind to a simple property on the templated parent will need to be changed to regular `Binding`s with `RelativeSourceMode.TemplatedParent`

See https://github.com/AvaloniaUI/Avalonia/pull/1695

## StackPanel
- The Gap property has been renamed to Spacing to be consistent with UWP and other XAML frameworks.

See https://github.com/AvaloniaUI/Avalonia/pull/1786

## SelectingItemsControl
- BugFix: For the replace operation in the `SelectedItems` collection, `AddedItems` and `RemovedItems` members of the `SelectionChangedEventArgs` class had their contents switched.

See https://github.com/AvaloniaUI/Avalonia/pull/1913

## Window.OpenWindows

Was moved to `Application.Windows`: https://github.com/AvaloniaUI/Avalonia/pull/1662

## Avalonia.Base
- The `DataValidatiorBase` base class has been renamed to `DataValidationBase`.

See https://github.com/AvaloniaUI/Avalonia/pull/1858

## Avalonia.Android 
- The `AndroidKeyboardEventsHelper<TView>.ActivateAutoShowKeybord` method has been renamed to `AndroidKeyboardEventsHelper<TView>.ActivateAutoShowKeyboard`.

See https://github.com/AvaloniaUI/Avalonia/pull/1859

## Avalonia.iOS
- The `KeyboardEventsHelper<TView>.ActivateAutoShowKeybord` method has been renamed to `KeyboardEventsHelper<TView>.ActivateAutoShowKeyboard`.

See https://github.com/AvaloniaUI/Avalonia/pull/1859

## Avalonia.Gtk3

- The `UseGtk3` method parameters changed to `Gtk3PlatformOptions`.

See https://github.com/AvaloniaUI/Avalonia/pull/1935

## Avalonia.MonoMac

We have removed `Avalonia.MonoMac`, please use `Avalonia.Native` instead.

https://github.com/AvaloniaUI/Avalonia/pull/1992/files

## Avalonia.DotNetCoreRuntime

We have removed `Avalonia.DotNetCoreRuntime`, please use `Avalonia.DesktopRuntime` instead.

## Themes

- Renamed theme resource from `ThemeBorderLightColor` to `ThemeBorderLowColor`.
- Renamed theme resource from `ThemeControlLightColor` to `ThemeControlLowColor`.
- Renamed theme resource from `ThemeForegroundLightColor` to `ThemeForegroundLowColor`.
- Renamed theme resource from `ErrorLightColor` to `ErrorLowColor`.
- Renamed theme resource from `ThemeBorderLightBrush` to `ThemeBorderLowBrush`.
- Renamed theme resource from `ThemeControlLightBrush` to `ThemeControlLowBrush`.
- Renamed theme resource from `ThemeForegroundLightBrush` to `ThemeForegroundLowBrush`.
- Renamed theme resource from `ErrorLightBrush` to `ErrorLowBrush`.
- Renamed theme resource from `ThemeBorderDarkColor` to `ThemeBorderHighColor`.
- Renamed theme resource from `ThemeControlDarkColor` to `ThemeControlHighColor`.
- Renamed theme resource from `ThemeControlHighlightDarkColor` to `ThemeControlHighlightHighColor`.
- Renamed theme resource from `ThemeBorderDarkBrush` to `ThemeBorderHighBrush`.
- Renamed theme resource from `ThemeControlDarkBrush` to `ThemeControlHighBrush`.
- Renamed theme resource from `ThemeControlHighlightDarkBrush` to `ThemeControlHighlightHighBrush`.

See https://github.com/AvaloniaUI/Avalonia/pull/2023

## `IBitmap`

Changed both `PixelHeight` and `PixelWidth` into a `PixelSize` struct.


## WriteableBitmap
The constructor now requires the size to be provided using PixelSize and also a DPI. Use 96 as a default.

See https://github.com/AvaloniaUI/Avalonia/pull/1889

# 0.5.1 → 0.6

## `BuildAvaloniaApp` and previewer.

You now need `BuildAvaloniaApp` static method in the class with your entry point (typically in `Program.cs` or `App.xaml.cs`) which should be called from `Main`:

```csharp
        static void Main(string[] args)
        {
            BuildAvaloniaApp().Start<MainWindow>();
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
.LogToDebug();

```

Previewer *won't* be able to work without it.

## `DataContextChanging` and `DataContextChanged`

They were replaced by `OnDataContextBeginUpdate` and `OnDataContextEndUpdate`


## `Static` and `Type` markup extensions

They were replaced by standard `x:Static` and `x:Type`, add `xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"` to the root of your XAML file.

## `StyleResource`

`StyleResource` has been replaced by `StaticResource` and `DynamicResource` as in other XAML frameworks. `StaticResource` and `DynamicResource` in Avalonia search both `Control.Resources` _and_ `Style.Resources`.

## Mouse device

`MouseDevice` is no longer available from the global context, you need to obtain one from `TopLevel` (`Window`, `Popup`, etc). Call `GetVisualRoot()` in your control and cast it to `IInputRoot`.

```csharp
var pos = (_control.GetVisualRoot() as IInputRoot)?.MouseDevice?.Position ?? default(Point);
```