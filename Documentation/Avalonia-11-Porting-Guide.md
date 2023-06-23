# Avalonia 11 Porting Guide

## Updating the project.

1. Update the Avalonia packages to 11.x
2. Themes are no longer included in the Avalonia.Desktop package, so you will need to add a package reference to either
  - `Avalonia.Themes.Fluent`
  - `Avalonia.Themes.Simple`
3. Remove the package reference to `XamlNameReferenceGenerator` - Avalonia now includes an inbuilt generator by default
4. If necessary, update the `<LangVersion>` to at least 9 in order to be able to use init-only properties
5. If you want the same fonts as in 0.10, also include `Avalonia.Fonts.Inter` package and add `.WithInterFont()` to the app builder. By default, 11.0 doesn't include any custom fonts.

## System.Reactive/Observables

Avalonia no longer has a dependency on `System.Reactive`. If you're using reactive features, add a package reference to `System.Reactive` to your project.

If you don't need the whole of `System.Reactive` but just want to make a simple subscription to an `IObservable<T>` you can use the utility class `AnonymousObserver<T>` provided by Avalonia, for example:

```csharp
observable.Subscribe(new AnonymousObserver<string>(() => { /* Code to execute when the observable changes. */ }));
```

See #9749, #10105 for more information.

## Updating Interfaces

Many interfaces have been removed in Avalonia 11. You should be able to do a global find/replace to replace each of the follow interfaces with its concrete type:

- `IAvaloniaObject` -> `AvaloniaObject`
- `IBitmap` -> `Bitmap`
- `IContentPresenter` -> `ContentPresenter`
- `IControl` -> `Control`
- `IInteractive` -> `Interactive`
- `IItemsPresenter` -> `ItemsPresenter`
- `ILayoutable` -> `Layoutable`
- `IPanel` -> `Panel`
- `IStyledElement` -> `StyledElement`
- `ITemplatedControl` -> `TemplatedControl`
- `IVisual` -> `Visual`

If you have your own interfaces that derive from one of these interfaces you'll need to remove the interface base, and do a cast to the concrete class at the point of usage.

See #9553, #11495 for more information.

### Optional, but recommended:

The `IStyleable` interface is now deprecated. In Avalonia 0.10.x, to override a control's style key you implemented `IStyleable` and added an explicit interface implementation for `StyleKey`:

```csharp
class MyButton : Button, IStyleable
{
    Type IStyleable.StyleKey => typeof(Button);
}
```

In Avalonia 11, the `IStyleable` reference will give a deprecated warning. The following should be used instead:

```csharp
class MyButton : Button
{
    protected override Type StyleKeyOverride => typeof(Button);
}
```

See #11380 for more information.

## Views

Views that are in the form of a `.axaml`/`.axaml.cs` (or `.xaml`/`.xaml.cs`) pair now have auto-generated C# code. To facilitate this:

- Make the class in the .cs file `partial`
- Remove the `private void InitializeComponent()` method
- Do *NOT* remove the call to `InitializeComponent()` in the constructor: this method is now a generated method and still needs to be called
- Remove the `this.AttachDevTools()` call from the constructor - `InitializeComponent` now has a parameter which controls whether DevTools is attached in debug mode whose default is `true`

Previously, to find a named control declared in the XAML file, a call to `this.FindControl<T>(string name)` or `this.GetControl<T>(string name)` was needed. This is now unnecessary - controls in the XAML file with a `Name` or `x:Name` attribute will automatically cause a field to be generated in the class to access the named control (as in WPF/UWP etc).

Note, this source generator is available for C# only. For F# nothing was changed.

# ItemsControl

`ItemsControl` and derived classes such as `ListBox` and `ComboBox` now have both an `Items` property and an `ItemsSource` as in WPF/UWP.

`Items` is a readonly collection property that is pre-populated, and `ItemsSource` is the read/write version that has a default value of null.

Replace any bindings to `Items` with a binding to `ItemsSource`:

```
<ListBox Items="{Binding Items}">
```

Becomes 

```
<ListBox ItemsSource="{Binding Items}">
```

In addition:

- `ListBox.VirtualizationMode` has been removed, the virtualization mode is changed by changing the `ItemsPanel`:
  - To disable virtualization use a `StackPanel`.
  - To enable virtualization use a `VirtualizingStackPanel`.
- `Carousel.IsVirtualizing` has been removed, there is now only a "virtualizing" mode for `Carousel`
- Item container lookup was moved to `ItemsControl` as in UWP (old methods are left on ItemContainerGenerator marked with [Obsolete]):
  - `ItemsControl.ContainerFromIndex(object item)`
  - `ItemsControl.IndexFromContainer(Control container)`
- The `Items` and `ItemTemplate` properties on `ItemsPresenter` have been removed. The template bindings to these properties in control templates can simply be removed

See #10590, #10827 for more information.

## Classes

`StyledElement.Classes` is now a readonly property. When used in an object initializer, code which did the following:

```csharp
var c = new Control
{
    Classes = new Classes("foo", "bar"),
};
```

Should be changed to:

```csharp
var c = new Control
{
    Classes = { "foo", "bar" },
};
```

To manipulate a `Classes` collection outside of an object initializer use the standard `IList<string>` methods.

See #11013 for more information.

## Windows

The `TopLEvel.PlatformImpl` API is no longer available for controls such as `Window`. The relevant methods have been moved to `TopLevel`, `WindowBase` or `Window` itself:

- `window.PlatformImpl.Handle` becomes `window.TryGetPlatformHandle()`
- `window.PlatformImpl.BeginMove(e)` becomes `window.BeginMove()`
- `window.PlatformImpl.Resized` becomes `window.Resized`

## AssetLoader

The `IAssetLoader` interface is no longer available. Use the static `AssetLoader` class:

```csharp
var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
var bitmap = new Bitmap(assets.Open(new Uri(uri)));
```

Becomes:

```csharp
var bitmap = new Bitmap(AssetLoader.Open(new Uri(uri)));
```

## OnPropertyChanged

The virtual `AvaloniaObject.OnPropertyChanged` method is now non-generic. Replace 

```csharp
protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
```

with 

```csharp
protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
```

Also the technique for getting the old and new values from `AvaloniaPropertyChangedEventArgs` without boxing has changed:

- Replace `change.NewValue.GetValueOrDefault<T>()` with `change.GetNewValue<bool>()`
- Replace `change.OldValue.GetValueOrDefault<T>()` with `change.GetOldValue<bool>()`
- You can also use `change.GetOldAndNewValue<T>()` to get both

See #7980 for more information.

## Events

The following events have been renamed:

- `PointerEnter` -> `PointerEntered`
- `PointerLeave` -> `PointerExited`
- `ContextMenu`
    - `ContextMenuClosing` -> `Closing`
    - `ContextMenuOpening` -> `Opening`
- `MenuBase`
    - `MenuClosed` -> `Closed`
    - `MenuOpened` -> `Opened`

`RoutedEventArgs.Source` has changed from type `IInteractive` to type `object`: cast to a concrete type such as `Control` to use it.

## Layout

Previously a full layout pass was achieved by getting the layout root and calling a method on the layout manager:

```csharp
((ILayoutRoot)control).LayoutManager.ExecuteLayout();
```

The `LayoutManager` is no longer exposed from the `ILayoutRoot`, instead call the `UpdateLayout` method on any control as in WPF/UWP:

```csharp
control.UpdateLayout();
```

`ILayoutable` was used in 0.10.x to get the previous measure constraints and arrange bounds. Because `ILayoutable` is no longer available, these are now exposed from `LayoutInformation`:

- `Size? LayoutInformation.GetPreviousMeasureConstraint(Layoutable control)`
- `Rect? LayoutInformation.GetPreviousArrangeBounds(Layoutable control)`

## Focus

The focus manager is no longer available via `FocusManager.Instance` and has instead been moved to the `TopLevel`:

```csharp
var focusManager = FocusManager.Instance;
```

Becomes:

```csharp
var focusManager = TopLevel.GetTopLevel(control).FocusManager;
```

In addition, the `IFocusManager` API has been changed.

- To get the currently focused element, use `IFocusManager.GetFocusedEleemnt()`
- To focus a control use `control.Focus()`

There is currently no event for listening to focus changes on `IFocusManager`. To listen for focus changes, add a listener to the `InputElement.GotFocusEvent`:

```csharp
InputElement.GotFocusEvent.Raised.Subscribe(new AnonymousObserver<(object, RoutedEventArgs)>(x => { }));
```

The same applied to KeyboardDevice, which isn't accessible anymore. Use the same focus related APIs as a replacement.

See #11407 for more information.

## Visual Tree

`IVisual` was used in 0.10.x to expose the visual parent and visual children of a control. Because `IVisual` is no longer available, these are now exposed as extension methods in the `Avalonia.VisualTree` namespace:

```
using Avalonia.VisualTree;

var visualParent = control.GetVisualParent();
var visualChildren = control.GetVisualChildren();
```

## Rendering

The `Render` method on certain controls is now sealed. This is because it is planned to make these controls use composition primitives instead of rendering via `DrawingContext`.

If you have a control whose `Render` method was being overloaded but it's now sealed, consider using a base class, for example instead of `Border` use `Decorator`. Note that you will now be responsible for drawing the background/border.

See #10299 for more information.

## Locator

The `AvaloniaLocator` is no longer available. Most services that were available via the locator now have alternative methods of access:
1. `AssetLoader` is a static class now with all of the old methods.
2. `IPlatformSettings` was moved to `TopLevel.PlatformSettings` and `Application.PlatformSettings`. Note, it's always preferred to use settings of the specific top level (window) rather than global ones.
3. `IClipboard` was moved to the `TopLevel.Clipboard`. Note, that `Application.Clipboard` was removed as well.
4. `PlatformHotkeyConfiguration` was moved to the `PlatformSettings.HotkeyConfiguration`.

Some applications were using the `AvaloniaLocator` as a general-purpose service locator. This was never an intended usage of `AvaloniaLocator` and those application should move to a service locator or DI container designed for the purpose, e.g. [`Splat`](https://www.reactiveui.net/docs/handbook/dependency-inversion/) or `Microsoft.Extensions.DependencyInjection`.

## Miscellaneous/Advanced Scenarios

- `IRenderer`/`DeferredRenderer`/`ImmediateRenderer` have now been removed. For performance reasons it is no longer possible to supply your own renderer, everything uses the new composition renderer.
- `Renderer.Diagnostics` is now `RendererDiagnostics`
- `ICustomDrawOperation.Render` now takes an `ImmediateDrawingContext` instead of a `DrawingContext`
- Add `.GetTask()` to the end of calls to `Dispatcher.UIThread.InvokeAsync` if directly returning the value in a method which returns a `Task`
- `IRenderRoot.RenderScaling` has been moved to `TopLevel.RenderScaling`
- `LightweightObservableBase` and `SingleSubscriberObservableBase` have been made internal. These were utility classes designed for a specific purpose in Avalonia and were not intended to be used by clients as they do not handle certain edge cases. Use the mechanisms provided by `System.Reactive` to create observables, such as `Observable.Create`
- When binding to methods, the method must either have no parameters or a single object parameter.
- AppBuilderBase was replaced with AppBuilder.