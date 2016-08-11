# Avalonia NuGet Packages

Avalonia is divided into several `NuGet` packages. 

* The `Avalonia` package contains core portable class libraries.
* The `Android`, `iOS`, `Gtk`, `Skia` and `Windows` packages contain platform specific windowing and rendering back-ends.
* The `Avalonia.Desktop` package is intended to be used by the end users targeting desktop platforms. 
* The `Avalonia.Skia.Desktop` together with `Avalonia.Desktop` package is intended to be used by the end users targeting desktop platforms using `Skia` rendering back-ends. 
* The `Avalonia.iOS` and `Avalonia.Android` packages are intended to be used by the end users targeting mobile platforms. 

## Core

* Avalonia.Animation
  - Avalonia.Base
  - System.Reactive
* Avalonia.Base
  - System.Reactive
* Avalonia.Controls
  - Avalonia.Animation
  - Avalonia.Base
  - Avalonia.Input
  - Avalonia.Interactivity
  - Avalonia.Layout
  - Avalonia.SceneGraph
  - Avalonia.Styling
  - System.Reactive
* Avalonia.DesignerSupport
  - Avalonia.Animation
  - Avalonia.Base
  - Avalonia.Controls
  - Avalonia.Input
  - Avalonia.Interactivity
  - Avalonia.Layout
  - Avalonia.Markup
  - Avalonia.Markup.Xaml
  - Avalonia.SceneGraph
  - Avalonia.Styling
  - Avalonia.Themes.Default
  - System.Reactive
* Avalonia.Diagnostics
  - Avalonia.Animation
  - Avalonia.Base
  - Avalonia.Controls
  - Avalonia.Input
  - Avalonia.Interactivity
  - Avalonia.Layout
  - Avalonia.Markup
  - Avalonia.Markup.Xaml
  - Avalonia.ReactiveUI
  - Avalonia.SceneGraph
  - Avalonia.Styling
  - Avalonia.Themes.Default
  - System.Reactive
  - Splat
* Avalonia.HtmlRenderer
  - Avalonia.Animation
  - Avalonia.Base
  - Avalonia.Controls
  - Avalonia.Input
  - Avalonia.Interactivity
  - Avalonia.Layout
  - Avalonia.SceneGraph
  - Avalonia.Styling
  - System.Reactive.Core
  - System.Reactive.Interfaces
* Avalonia.Input
  - Avalonia.Animation
  - Avalonia.Base
  - Avalonia.Interactivity
  - Avalonia.Layout
  - Avalonia.SceneGraph
  - System.Reactive
* Avalonia.Interactivity
  - Avalonia.Animation
  - Avalonia.Base
  - Avalonia.Layout
  - Avalonia.SceneGraph
  - System.Reactive
* Avalonia.Layout
  - Avalonia.Animation
  - Avalonia.Base
  - Avalonia.SceneGraph
  - System.Reactive
* Avalonia.Logging.Serilog
  - Avalonia.Base
  - Serilog
* Avalonia.ReactiveUI
  - System.Reactive
  - Splat
* Avalonia.SceneGraph
  - Avalonia.Animation
  - Avalonia.Base
  - System.Reactive
* Avalonia.Styling
  - Avalonia.Animation
  - Avalonia.Base
  - Avalonia.SceneGraph
  - System.Reactive
* Avalonia.Themes.Default
  - Avalonia.Animation
  - Avalonia.Base
  - Avalonia.Controls
  - Avalonia.Input
  - Avalonia.Interactivity
  - Avalonia.Layout
  - Avalonia.Markup.Xaml
  - Avalonia.SceneGraph
  - Avalonia.Styling
  - System.Reactive

## Markup

* Avalonia.Markup
  - Avalonia.Base
  - Avalonia.Controls
  - Avalonia.Input
  - Avalonia.Interactivity
  - Avalonia.Layout
  - Avalonia.SceneGraph
  - Avalonia.Styling
  - System.Reactive
* Avalonia.Markup.Xaml
  - Avalonia.Animation
  - Avalonia.Base
  - Avalonia.Controls
  - Avalonia.Input
  - Avalonia.Interactivity
  - Avalonia.Layout
  - Avalonia.Markup
  - Avalonia.SceneGraph
  - Avalonia.Styling
  - System.Reactive
  - Sprache

## Android

* Avalonia.Android
  - Avalonia
  - Avalonia.Skia.Android

## Gtk

* Avalonia.Cairo
  - Avalonia
* Avalonia.Gtk
  - Avalonia

## iOS

* Avalonia.iOS
  - Avalonia
  - Avalonia.Skia.iOS

## Skia

* Avalonia.Skia.Android
  - Avalonia
  - SkiaSharp
* Avalonia.Skia.Desktop
  - Avalonia
  - SkiaSharp
* Avalonia.Skia.iOS
  - Avalonia
  - SkiaSharp

## Windows

* Avalonia.Direct2D1
  - Avalonia
  - SharpDX
  - SharpDX.Direct2D1
  - SharpDX.DXGI
* Avalonia.Win32
  - Avalonia

## Main

* Avalonia
  - Avalonia.Animation
  - Avalonia.Base
  - Avalonia.Controls
  - Avalonia.DesignerSupport
  - Avalonia.Diagnostics
  - Avalonia.HtmlRenderer
  - Avalonia.Input
  - Avalonia.Interactivity
  - Avalonia.Layout
  - Avalonia.Logging.Serilog
  - Avalonia.ReactiveUI
  - Avalonia.SceneGraph
  - Avalonia.Styling
  - Avalonia.Themes.Default

* Avalonia.Desktop
  - Avalonia.Win32
  - Avalonia.Direct2D1
  - Avalonia.Gtk
  - Avalonia.Cairo
