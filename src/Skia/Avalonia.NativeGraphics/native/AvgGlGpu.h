//
// Created by kekekeks on 04.05.22.
//

#ifndef AVALONIA_SKIA_AVGGLGPU_H
#define AVALONIA_SKIA_AVGGLGPU_H

#include "interop.h"
#include "comimpl.h"
#include "include/gpu/gl/GrGLAssembleInterface.h"
#include "include/gpu/GrDirectContext.h"

class AvgGlGpu : public ComSingleObject<IAvgGpu, &IID_IAvgGpu>{
    sk_sp<GrDirectContext> _grContext;
    AvgGlGpu(sk_sp<struct GrDirectContext> grContext);

public:
    FORWARD_IUNKNOWN()
    sk_sp<GrDirectContext> getGrContext() {return _grContext;};
    static HRESULT Create(bool gles, IAvgGetProcAddressDelegate *getProcAddressDelegate, IAvgGpu **pGpu);
};

class AvgGlRenderTarget : public ComSingleObject<IAvgRenderTarget, &IID_IAvgGlPlatformSurfaceRenderTarget>
{
    ComPtr<AvgGlGpu> _gpu;
    ComPtr<IAvgGlPlatformSurfaceRenderTarget> _target;

public:
    FORWARD_IUNKNOWN()

    HRESULT CreateDrawingContext(IAvgDrawingContext **ppv) override;
    AvgGlRenderTarget(AvgGlGpu* gpu, IAvgGlPlatformSurfaceRenderTarget* target);
};

#endif //AVALONIA_SKIA_AVGGLGPU_H
