#include "AvgFontManager.h"
#include "AvgGlGpu.h"
#include "AvgGlyphRun.h"
#include "AvgFontShapeBuffer.h"
#include "AvgPath.h"
#include "comimpl.h"
#include "interop.h"

class AvgFactory : public ComSingleObject<IAvgFactory, &IID_IAvgFactory>
{
  public:
    FORWARD_IUNKNOWN()
    int GetVersion() override
    {
        return 0;
    }

    HRESULT CreateGlGpu(bool gles, IAvgGetProcAddressDelegate* glGetProcAddress, IAvgGpu** ppv) override
    {
        return AvgGlGpu::Create(gles, glGetProcAddress, ppv);
    }

    HRESULT
    CreateGlGpuRenderTarget(IAvgGpu* gpu, IAvgGlPlatformSurfaceRenderTarget* gl, IAvgRenderTarget** ppv) override
    {
        auto g = dynamic_cast<AvgGlGpu*>(gpu);
        if (g == nullptr)
            return E_INVALIDARG;
        if (gl == nullptr)
            return E_INVALIDARG;
        *ppv = new AvgGlRenderTarget(g, gl);
        return 0;
    }

    HRESULT CreateAvgPath(IAvgPath** ppv) override
    {
        *ppv = new AvgPath();
        return 0;
    }

    HRESULT CreateAvgFontManager(IAvgFontManager** ppv) override
    {
        *ppv = new AvgFontManager();
        return 0;
    }

    HRESULT CreateAvgGlyphRun(IAvgFontManager* fontManager, IAvgGlyphTypeface* typeface, IAvgGlyphRun** ppv) override
    {
        *ppv = new AvgGlyphRun(fontManager, typeface);
        return 0;
    }

    HRESULT CreateAvgFontShapeBuffer(IAvgGlyphTypeface* typeface, IAvgFontShapeBuffer** ppv) override
    {
        *ppv = new AvgFontShapeBuffer(typeface);
        return 0;
    }
};



#ifdef _WIN32
#define EXPORT  __declspec(dllexport)
#else
#define EXPORT
#endif

EXPORT extern "C" void* CreateAvaloniaNativeGraphics()
{
    auto f = new AvgFactory();
    f->AddRef();
    return (IAvgFactory*)f;
}
