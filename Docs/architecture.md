# Perspex Architecture

At the highest level, perspex is split up into a "core" and two "subsystems.

* The core is a set of Portable Class Libraries that can run anywhere.
* The Windowing subsystem is responsible for creating windows, handling input and scheduling timers.
* The Rendering subsystem is responsible for drawing.

There are currently two Windowing and two Rendering subsystems:

## Windowing Subsystems

* Perspex.Win32 uses the Win32 API (this also works on 64-bit windows).
* Perspex.Gtk uses the GTK2 toolkit and can be run on both Windows and *nix.

## Rendering Subsystems

* Perspex.Direct2D1 uses Microsoft's Direct2D1 API.
* Perspex.Cairo uses Cairo for rendering and Pango for text layout.

## Core

The Perspex core is split up into several assemblies. These assemblies are not separate because
you will want to use them separately - they are separate to maintain separation of concerns and a
layered architecture. It is fully possible that they will be ILMerged into a single assembly for
distribution.

The assemblies are as follows, from lowest to highest level:

### Perspex.Base

The main classes in this assembly are `PerspexObject` and `PerspexProperty`.

These are Perspex's versions of XAML's `DependencyObject` and `DepenendencyProperty`. It also 
defines a `PerspexDispatcher` which is - surprise - our version of XAML's `Dispatcher`.

### Perspex.Animation

The main class in the assembly is `Animatable`.

Allows PerspexProperties to be animated and provides various utilities related to animation.

### Perspex.SceneGraph

The main class in this assembly is `Visual` and its interface `IVisual`.

Defines the "Visual" layer which is a 2D scene graph, with each node being a `IVisual`/`Visual`. 
Also defines primitives such as `Point`/`Rect`/`Matrix`, plus `Geometry`, `Bitmap`, `Brush` and 
whatever else you might need in order to draw to the screen.

### Perspex.Styling

The main interface in this assembly is `IStyleable`.

Defines a CSS-like system for styling controls.

### Perspex.Layout

The main class in this assembly is `Layoutable`.

Defines a XAML-like layout system using `Measure` and `Arrange`. Also defines `LayoutManager` which 
carries out the actual layout.

### Perspex.Interactivity

The main class in this assembly is `Interactive`.

Defines a system of routed events similar to those found in XAML.

### Perspex.Input

The main class in this assembly is `InputElement`.

Handles input from various devices such as `MouseDevice` and `KeyboardDevice`, together with
`FocusManager` for tracking input focus. Input is sent from the windowing subsystem to 
`InputManager` in the form of "Raw" events which are then translated into routed events for the 
controls.

### Perspex.Controls

There are many important classes in this assembly, but the root of them is `Control`.

Finally defines the actual controls. The `Control` class is analogous to WPF's `FrameworkElement`
whereas the `TemplatedControl` class is our version of WPF's `Control`. This is also where you'll 
find all of the basic controls you'd expect.

### Perspex.Themes.Default

Defines a default theme using a set of styles and control templates.

### Perspex.Application

The main class in this assembly is `Application`.

This ties everything together, setting up the service locator, defining the default theme etc.

### Perspex.Diagnostics

Adds utilities for debugging perspex applications, such as `DevTools` which provides a Snoop/WPF
Inspector/Browser DevTools window for inspecting the state of the application.

