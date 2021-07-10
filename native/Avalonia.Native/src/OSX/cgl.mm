#include "common.h"
#include <dlfcn.h>

static CGLContextObj CreateCglContext(CGLContextObj share)
{
    int attributes[] = {
        kCGLPFAAccelerated,
        kCGLPFAOpenGLProfile, (CGLPixelFormatAttribute)kCGLOGLPVersion_3_2_Core,
        kCGLPFADepthSize, 8,
        kCGLPFAStencilSize, 8,
        kCGLPFAColorSize, 32,
        0
    };
    
    CGLPixelFormatObj pix;
    CGLError errorCode;
    GLint num; // stores the number of possible pixel formats
    errorCode = CGLChoosePixelFormat( (CGLPixelFormatAttribute*)attributes, &pix, &num );
    if(errorCode != 0)
        return nil;
    CGLContextObj ctx = nil;
    errorCode = CGLCreateContext(pix, share, &ctx );
    CGLDestroyPixelFormat( pix );
    if(errorCode != 0)
        return nil;
    return ctx;
};



class AvnGlContext : public virtual ComSingleObject<IAvnGlContext, &IID_IAvnGlContext>
{
    // Debug
    int _usageCount = 0;
public:
    CGLContextObj Context;
    int SampleCount = 0, StencilBits = 0;
    FORWARD_IUNKNOWN()
    
    class SavedGlContext : public virtual ComUnknownObject
    {
        CGLContextObj _savedContext;
        ComPtr<AvnGlContext> _parent;
    public:
        SavedGlContext(CGLContextObj saved, AvnGlContext* parent)
        {
            _savedContext = saved;
            _parent = parent;
            _parent->_usageCount++;
        }
        
        ~SavedGlContext()
        {
            if(_parent->Context == CGLGetCurrentContext())
                CGLSetCurrentContext(_savedContext);
            _parent->_usageCount--;
            CGLUnlockContext(_parent->Context);
        }
    };
    
    AvnGlContext(CGLContextObj context)
    {
        Context = context;
        CGLPixelFormatObj fmt = CGLGetPixelFormat(context);
        CGLDescribePixelFormat(fmt, 0, kCGLPFASamples, &SampleCount);
        CGLDescribePixelFormat(fmt, 0, kCGLPFAStencilSize, &StencilBits);
        
    }
    
    virtual HRESULT LegacyMakeCurrent() override
    {
        START_COM_CALL;
        
        if(CGLSetCurrentContext(Context) != 0)
            return E_FAIL;
        return S_OK;
    }
    
    virtual HRESULT MakeCurrent(IUnknown** ppv) override
    {
        START_COM_CALL;
        
        CGLContextObj saved = CGLGetCurrentContext();
        CGLLockContext(Context);
        if(CGLSetCurrentContext(Context) != 0)
        {
            CGLUnlockContext(Context);
            return E_FAIL;
        }
        *ppv = new SavedGlContext(saved, this);
        
        return S_OK;
    }
    
    virtual int GetSampleCount() override
    {
        return SampleCount;
    }
    
    virtual int GetStencilSize() override
    {
        return StencilBits;
    }
    
    virtual void* GetNativeHandle() override
    {
        return Context;
    }
    
    ~AvnGlContext()
    {
        CGLReleaseContext(Context);
    }
};

class AvnGlDisplay : public virtual ComSingleObject<IAvnGlDisplay, &IID_IAvnGlDisplay>
{
    void* _libgl;
    
public:
    FORWARD_IUNKNOWN()
    
    AvnGlDisplay()
    {
        _libgl = dlopen("/System/Library/Frameworks/OpenGL.framework/Versions/A/Libraries/libGL.dylib", RTLD_LAZY);
    }
    
    virtual void* GetProcAddress(char* proc)  override
    {
        return dlsym(_libgl, proc);
    }
    
    virtual HRESULT CreateContext(IAvnGlContext* share, IAvnGlContext**ppv) override
    {
        START_COM_CALL;
        
        CGLContextObj shareContext = nil;
        if(share != nil)
        {
            AvnGlContext* shareCtx = dynamic_cast<AvnGlContext*>(share);
            if(shareCtx != nil)
                shareContext = shareCtx->Context;
        }
        CGLContextObj ctx = ::CreateCglContext(shareContext);
        if(ctx == nil)
            return E_FAIL;
        *ppv = new AvnGlContext(ctx);
        return S_OK;
    }
    
    virtual HRESULT WrapContext(void* native, IAvnGlContext**ppv) override
    {
        START_COM_CALL;
        
        if(native == nil)
            return E_INVALIDARG;
        *ppv = new AvnGlContext((CGLContextObj) native);
        return S_OK;
    }
    
    virtual void LegacyClearCurrentContext() override
    {
        CGLSetCurrentContext(nil);
    }
};

static IAvnGlDisplay* GlDisplay = new AvnGlDisplay();


extern IAvnGlDisplay* GetGlDisplay()
{
    return GlDisplay;
};

