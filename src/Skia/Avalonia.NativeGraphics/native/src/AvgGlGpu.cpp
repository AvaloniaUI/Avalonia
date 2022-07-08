//
// Created by kekekeks on 04.05.22.
//

#include <include/gpu/GrDirectContext.h>

#include "AvgDrawingContext.h"
#include "AvgGlGpu.h"
#include "include/core/SkColorSpace.h"
#include "include/core/SkSurface.h"
#include <utility>

// static void*
static void* GlGetProc(void* ctx, const char name[])
{
    auto ptr = ((IAvgGetProcAddressDelegate*)ctx)->GetProcAddress(const_cast<char*>(name));
    return ptr;
}

HRESULT AvgGlGpu::Create(bool gles, IAvgGetProcAddressDelegate* getProcAddressDelegate, IAvgGpu** pGpu)
{
    *pGpu = nullptr;
    auto iface = gles ? GrGLMakeAssembledGLESInterface(getProcAddressDelegate, (GrGLGetProc)GlGetProc)
                      : GrGLMakeAssembledGLInterface(getProcAddressDelegate, (GrGLGetProc)GlGetProc);
    if (!iface)
        return E_FAIL;
    auto grContext = GrDirectContext::MakeGL(iface);
    if (!grContext)
        return E_FAIL;
    *pGpu = new AvgGlGpu(grContext);
    return 0;
}

AvgGlGpu::AvgGlGpu(sk_sp<struct GrDirectContext> grContext)
{
    _grContext = std::move(grContext);
}

HRESULT AvgGlRenderTarget::CreateDrawingContext(IAvgDrawingContext** ppv)
{
    ComPtr<IAvgGlPlatformSurfaceRenderSession> session;
    auto hr = _target->BeginDraw(session.getPPV());
    if (hr)
        return hr;
    AvgPixelSize size;
    session->GetPixelSize(&size);
    GrBackendRenderTarget backendRenderTarget(size.Width, size.Height, session->GetSampleCount(),
                                              session->GetStencilSize(), {(uint32_t)session->GetFboId(), 32856});

    auto surface = SkSurface::MakeFromBackendRenderTarget(_gpu->getGrContext().get(), backendRenderTarget,
                                                          session->GetIsYFlipped() ? kTopLeft_GrSurfaceOrigin
                                                                                   : kBottomLeft_GrSurfaceOrigin,
                                                          SkColorType::kRGBA_8888_SkColorType, nullptr, nullptr);
    *ppv = new AvgDrawingContext(surface, surface->getCanvas(), session, session->GetScaling());
    return 0;
}

AvgGlRenderTarget::AvgGlRenderTarget(AvgGlGpu* gpu, IAvgGlPlatformSurfaceRenderTarget* target)
{
    _target = target;
    _gpu = gpu;
}
