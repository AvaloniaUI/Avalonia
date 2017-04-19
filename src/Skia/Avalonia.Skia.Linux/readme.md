TODO:

BitmapImpl 
- constructor from Width/Height
- Save

StreamGeometryImpl
- Hit testing in Geometry missing as SkiaSharp does not expose this

DrawingContextImpl
- Alpha support missing as SkiaSharp does not expose this
- Gradient Shader caching?
- TileBrushes
- Pen Dash styles

Formatted Text Rendering 
- minor polish

RenderTarget
- Figure out a cleaner implementation across all platforms
- HW acceleration

App Bootstrapping
- Cleanup the testapplications across all platforms
- Add a cleaner Fluent API for the subsystems
	- ie.    app.UseDirect2D()    (via platform specific extension methods)

Android
- Not tested at all yet

iOS
- Get GLView working again. See HW above

Win32
- Cleanup the unmanaged methods (BITMAPINFO) if possible

General
- Cleanup/eliminate obsolete files
- Finish cleanup of the many Test Applications
- Get Skia Unit Tests passing


