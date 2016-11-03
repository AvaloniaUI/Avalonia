# Avalonia Architecture

At the highest level, avalonia is split up into a "core" and two "subsystems".

* The core is a set of Portable Class Libraries that can run anywhere.
* The Windowing subsystem is responsible for creating windows, handling input and scheduling timers.
* The Rendering subsystem is responsible for drawing.

There are currently two Windowing and two Rendering subsystems:

## Windowing Subsystems

* Avalonia.Win32 uses the Win32 API (this also works on 64-bit windows).
* Avalonia.Gtk uses the GTK2 toolkit and can be run on both Windows and *nix.

## Rendering Subsystems

* Avalonia.Direct2D1 uses Microsoft's Direct2D1 API.
* Avalonia.Cairo uses Cairo for rendering and Pango for text layout.

## Core

The Avalonia core is split up into several assemblies. Note that they're not separated like this 
because you will want to use them separately; they are separate to maintain separation of concerns 
and a layered architecture. It is fully possible that they will be ILMerged into a single assembly 
for distribution.

The assemblies are as follows, from lowest to highest level:

### Avalonia.Base

The main classes in this assembly are `AvaloniaObject` and `AvaloniaProperty`.

These are Avalonia's versions of XAML's `DependencyObject` and `DependencyProperty`. It also 
defines a `AvaloniaDispatcher` which is - surprise - our version of XAML's `Dispatcher`.

### Avalonia.Animation

The main class in the assembly is `Animatable`.

Allows AvaloniaProperties to be animated and provides various utilities related to animation.

### Avalonia.Visuals

The main class in this assembly is `Visual` and its interface `IVisual`.

Defines the "Visual" layer which is a 2D scene graph, with each node being a `IVisual`/`Visual`. 
Also defines primitives such as `Point`/`Rect`/`Matrix`, plus `Geometry`, `Bitmap`, `Brush` and 
whatever else you might need in order to draw to the screen.

### Avalonia.Styling

The main interface in this assembly is `IStyleable`.

Defines a CSS-like system for styling controls.

### Avalonia.Layout

The main class in this assembly is `Layoutable`.

Defines a XAML-like layout system using `Measure` and `Arrange`. Also defines `LayoutManager` which 
carries out the actual layout.

### Avalonia.Interactivity

The main class in this assembly is `Interactive`.

Defines a system of routed events similar to those found in XAML.

### Avalonia.Input

The main class in this assembly is `InputElement`.

Handles input from various devices such as `MouseDevice` and `KeyboardDevice`, together with
`FocusManager` for tracking input focus. Input is sent from the windowing subsystem to 
`InputManager` in the form of "Raw" events which are then translated into routed events for the 
controls.

### Avalonia.Controls

There are many important classes in this assembly, but the root of them is `Control`.

Finally defines the actual controls. The `Control` class is analogous to WPF's `FrameworkElement`
whereas the `TemplatedControl` class is our version of WPF's `Control`. This is also where you'll 
find all of the basic controls you'd expect.

### Avalonia.Themes.Default

Defines a default theme using a set of styles and control templates.

### Avalonia.Application

The main class in this assembly is `Application`.

This ties everything together, setting up the service locator, defining the default theme etc.

### Avalonia.Diagnostics

Adds utilities for debugging avalonia applications, such as `DevTools` which provides a Snoop/WPF
Inspector/Browser DevTools window for inspecting the state of the application.

