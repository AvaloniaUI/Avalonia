TODO:

BitmapImpl 
- constructor from Width/Height
- Save

DrawingContextImpl
- DrawRoundRect is not properly implemented due to lack of support in Skia
- Alpha support missing as SkiaSharp does not expose this
- Gradient Shader caching?
- TileBrushes
- Pen Dash styles

Formatted Text Rendering 
- still needs a lot of work once SkiaSharp implementation is better

RenderTarget
- Figure out a cleaner implementation across all platforms
- HW accelerated working?

StreamGeometry
- Paths within Paths may not work right
- Paths cannot be Cloned (lack of SkiaSupport)
- Paths cannot be transformed (lack of SkiaSupport)
- ArcTo

App Bootstrapping
- Cleanup the testapplications across all platforms
- Add a cleaner Fluent API for the subsystems
	- ie.    app.UseDirect2D()    (via platform specific extension methods)

Android
- Testing & fixes

iOS
- Get GLView working again?


General
- Cleanup/eliminate obsolete files
- Cleanup the many Test Applications
- Get Skia Unit Tests passing