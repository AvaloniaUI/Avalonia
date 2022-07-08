#pragma once
#include "com.h"
#include "stddef.h"
struct AvgRect;
struct AvgPixelSize;
struct AvgMatrix3x2;
struct IAvgGetProcAddressDelegate;
struct IAvgFactory;
struct IAvgDrawingContext;
struct IAvgRenderTarget;
struct IAvgGpuControl;
struct IAvgGpu;
struct IAvgGlPlatformSurfaceRenderTarget;
struct IAvgGlPlatformSurfaceRenderSession;
struct AvgRect
{
    double X;
    double Y;
    double Width;
    double Height;
};
struct AvgPixelSize
{
    int Width;
    int Height;
};
struct AvgMatrix3x2
{
    double M11;
    double M12;
    double M21;
    double M22;
    double M31;
    double M32;
};
COMINTERFACE(IAvgGetProcAddressDelegate, 084b6d03, 4545, 43d1, 97, 1d, 3d, 3a, 96, 8a, 31, 27) : IUnknown
{
    virtual void* GetProcAddress (
        char* proc
    ) = 0;
};
COMINTERFACE(IAvgFactory, 52434e9c, 5438, 4ac9, 98, 23, 9f, 5a, 3f, e9, 0d, 53) : IUnknown
{
    virtual int GetVersion () = 0;
    virtual HRESULT CreateGlGpu (
        bool gles, 
        IAvgGetProcAddressDelegate* glGetProcAddress, 
        IAvgGpu** ppv
    ) = 0;
    virtual HRESULT CreateGlGpuRenderTarget (
        IAvgGpu* gpu, 
        IAvgGlPlatformSurfaceRenderTarget* gl, 
        IAvgRenderTarget** ppv
    ) = 0;
};
COMINTERFACE(IAvgDrawingContext, 309466f0, b5ca, 4aba, 84, 69, 2c, 90, 2f, e5, d8, f3) : IUnknown
{
    virtual void SetTransform (
        AvgMatrix3x2* matrix
    ) = 0;
    virtual void Clear (
        unsigned int color
    ) = 0;
    virtual void FillRect (
        AvgRect rect, 
        unsigned int color
    ) = 0;
};
COMINTERFACE(IAvgRenderTarget, 04d2adf7, f4df, 4836, a6, 0e, 76, 99, e5, e5, 3e, c7) : IUnknown
{
    virtual HRESULT CreateDrawingContext (
        IAvgDrawingContext** ppv
    ) = 0;
};
COMINTERFACE(IAvgGpuControl, 7bb8b147, f9c7, 49ce, 90, 5d, f0, 8a, b0, ec, 63, 2f) : IUnknown
{
    virtual HRESULT Lock (
        IUnknown** ppv
    ) = 0;
};
COMINTERFACE(IAvgGpu, 5e4c1e66, 1a35, 47c6, a9, d3, c2, 6a, 42, ea, fd, 1b) : IUnknown
{
};
COMINTERFACE(IAvgGlPlatformSurfaceRenderTarget, bce2aea0, 18ef, 46d8, 91, 0a, a0, 1b, c1, 94, 50, e4) : IUnknown
{
    virtual HRESULT BeginDraw (
        IAvgGlPlatformSurfaceRenderSession** ppv
    ) = 0;
};
COMINTERFACE(IAvgGlPlatformSurfaceRenderSession, 38504e1e, ee85, 4336, 8d, 01, 91, fc, b6, 7b, 71, 97) : IUnknown
{
    virtual void GetPixelSize (
        AvgPixelSize* rv
    ) = 0;
    virtual double GetScaling () = 0;
    virtual int GetSampleCount () = 0;
    virtual int GetStencilSize () = 0;
    virtual int GetFboId () = 0;
    virtual bool GetIsYFlipped () = 0;
};
