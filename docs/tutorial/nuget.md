# Avalonia NuGet Packages

Avalonia is divided into several `NuGet` packages. 

* The `Avalonia` package contains core portable class libraries.
* The `Dekstop` and `Mobile` packages contain platform specific windowing and rendering back-ends.
* The `Avalonia.Desktop` package is intended to be used by the end users targeting multiple desktop platforms (`Windows`, `Linux` and `OSX`).
* The `Avalonia.iOS` and `Avalonia.Android` packages are intended to be used by the end users targeting specific mobile platforms. 
* The `Avalonia.Mobile` package is intended to be used by the end users targeting multiple mobile platforms (`Android` and `iOS`).

## Core

* Avalonia (.nupkg)
  - Avalonia.Animation (.dll)
  - Avalonia.Base (.dll)
  - Avalonia.Controls (.dll)
  - Avalonia.DesignerSupport (.dll)
  - Avalonia.Diagnostics (.dll)
  - Avalonia.Input (.dll)
  - Avalonia.Interactivity (.dll)
  - Avalonia.Layout (.dll)
  - Avalonia.Logging.Serilog (.dll)
  - Avalonia.Visuals (.dll)
  - Avalonia.Styling (.dll)
  - Avalonia.ReactiveUI (.dll)
  - Avalonia.Themes.Default (.dll)
  - Avalonia.Markup (.dll)
  - Avalonia.Markup.Xaml (.dll)
  - Serilog (.nupkg)
  - Splat (.nupkg)
  - Sprache (.nupkg)
  - System.Reactive (.nupkg)

* Avalonia.HtmlRenderer (.nupkg)
  - Avalonia (.nupkg)

## Desktop

* Avalonia.Win32 (.nupkg)
  - Avalonia.Win32 (.dll)
  - Avalonia (.nupkg)

* Avalonia.Direct2D1 (.nupkg)
  - Avalonia.Direct2D1 (.dll)
  - Avalonia (.nupkg)
  - SharpDX (.nupkg)
  - SharpDX.Direct2D1 (.nupkg)
  - SharpDX.DXGI (.nupkg)

* Avalonia.Gtk (.nupkg)
  - Avalonia.Gtk (.dll)
  - Avalonia (.nupkg)

* Avalonia.Cairo (.nupkg)
  - Avalonia.Cairo (.dll)
  - Avalonia (.nupkg)

* Avalonia.Skia.Desktop (.nupkg)
  - Avalonia.Skia.Desktop (.dll)
  - Avalonia (.nupkg)
  - SkiaSharp (.nupkg)

* Avalonia.Desktop (.nupkg)
  - Avalonia.Win32 (.nupkg)
  - Avalonia.Direct2D1 (.nupkg)
  - Avalonia.Gtk (.nupkg)
  - Avalonia.Cairo (.nupkg)
  - Avalonia.Skia.Desktop (.nupkg)

## Mobile

* Avalonia.Android (.nupkg)
  - Avalonia.Android (.dll)
  - Avalonia.Skia.Android (.dll)
  - Avalonia (.nupkg)
  - SkiaSharp (.nupkg)

* Avalonia.iOS (.nupkg)
  - Avalonia.iOS (.dll)
  - Avalonia.Skia.iOS (.dll)
  - Avalonia (.nupkg)
  - SkiaSharp (.nupkg)

* Avalonia.Mobile (.nupkg)
  - Avalonia.Android (.nupkg)
  - Avalonia.iOS (.nupkg)
