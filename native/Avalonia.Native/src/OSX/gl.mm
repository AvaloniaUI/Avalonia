#include "common.h"
#include <OpenGL/gl.h>
#include <dlfcn.h>

template <typename T, size_t N> char (&ArrayCounter(T (&a)[N]))[N];
#define ARRAY_COUNT(a) (sizeof(ArrayCounter(a)))

NSOpenGLPixelFormat* CreateFormat()
{
    NSOpenGLPixelFormatAttribute attribs[] =
    {
        NSOpenGLPFADoubleBuffer,
        NSOpenGLPFAColorSize, 32,
        NSOpenGLPFAStencilSize, 8,
        NSOpenGLPFADepthSize, 8,
        0
    };
    return [[NSOpenGLPixelFormat alloc] initWithAttributes:attribs];
}

class AvnGlContext : public virtual ComSingleObject<IAvnGlContext, &IID_IAvnGlContext>
{
public:
    FORWARD_IUNKNOWN()
    NSOpenGLContext* GlContext;
    GLuint Framebuffer, RenderBuffer, StencilBuffer;
    AvnGlContext(NSOpenGLContext* gl, bool offscreen)
    {
        Framebuffer = 0;
        RenderBuffer = 0;
        StencilBuffer = 0;
        GlContext = gl;
        if(offscreen)
        {
            [GlContext makeCurrentContext];

            glGenFramebuffersEXT(1, &Framebuffer);
            glBindFramebufferEXT(GL_FRAMEBUFFER, Framebuffer);
            glGenRenderbuffersEXT(1, &RenderBuffer);
            glGenRenderbuffersEXT(1, &StencilBuffer);

            glBindRenderbufferEXT(GL_RENDERBUFFER, StencilBuffer);
            glFramebufferRenderbufferEXT(GL_FRAMEBUFFER, GL_STENCIL_ATTACHMENT, GL_RENDERBUFFER, StencilBuffer);
            glBindRenderbufferEXT(GL_RENDERBUFFER, RenderBuffer);
            glFramebufferRenderbufferEXT(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_RENDERBUFFER, RenderBuffer);
        }
        
    }
    
    
    virtual HRESULT MakeCurrent()  override
    {
        [GlContext makeCurrentContext];/*
        glBindFramebufferEXT(GL_FRAMEBUFFER, Framebuffer);
        glBindRenderbufferEXT(GL_RENDERBUFFER, RenderBuffer);
        glFramebufferRenderbufferEXT(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_RENDERBUFFER, RenderBuffer);
        glBindRenderbufferEXT(GL_RENDERBUFFER, StencilBuffer);
        glFramebufferRenderbufferEXT(GL_FRAMEBUFFER, GL_STENCIL_ATTACHMENT, GL_RENDERBUFFER, StencilBuffer);*/
        return S_OK;
    }
};

class AvnGlDisplay : public virtual ComSingleObject<IAvnGlDisplay, &IID_IAvnGlDisplay>
{
    int _sampleCount, _stencilSize;
    void* _libgl;
    
public:
    FORWARD_IUNKNOWN()
    
    AvnGlDisplay(int sampleCount, int stencilSize)
    {
        _sampleCount = sampleCount;
        _stencilSize = stencilSize;
        _libgl = dlopen("/System/Library/Frameworks/OpenGL.framework/Versions/A/Libraries/libGL.dylib", RTLD_LAZY);
    }
    
    virtual HRESULT GetSampleCount(int* ret)  override
    {
        *ret = _sampleCount;
        return S_OK;
    }
    virtual HRESULT GetStencilSize(int* ret) override
    {
        *ret = _stencilSize;
        return S_OK;
    }
    
    virtual HRESULT ClearContext()  override
    {
        [NSOpenGLContext clearCurrentContext];
        return S_OK;
    }
    
    virtual void* GetProcAddress(char* proc)  override
    {
        return dlsym(_libgl, proc);
    }
};


class GlFeature : public virtual ComSingleObject<IAvnGlFeature, &IID_IAvnGlFeature>
{
    IAvnGlDisplay* _display;
    AvnGlContext *_immediate;
    NSOpenGLContext* _shared;
public:
    FORWARD_IUNKNOWN()
    NSOpenGLPixelFormat* _format;
    GlFeature(IAvnGlDisplay* display, AvnGlContext* immediate, NSOpenGLPixelFormat* format)
    {
        _display = display;
        _immediate = immediate;
        _format = format;
        _shared = [[NSOpenGLContext alloc] initWithFormat:_format shareContext:_immediate->GlContext];
    }
    
    NSOpenGLContext* CreateContext()
    {
        return _shared;
        //return [[NSOpenGLContext alloc] initWithFormat:_format shareContext:nil];
    }
    
    virtual HRESULT ObtainDisplay(IAvnGlDisplay**retOut)  override
    {
        *retOut = _display;
        _display->AddRef();
        return S_OK;
    }
    
    virtual HRESULT ObtainImmediateContext(IAvnGlContext**retOut)  override
    {
        *retOut = _immediate;
        _immediate->AddRef();
        return S_OK;
    }
};

static GlFeature* Feature;

GlFeature* CreateGlFeature()
{
    auto format = CreateFormat();
    if(format == nil)
    {
        NSLog(@"Unable to choose pixel format");
        return NULL;
    }
    
    auto immediateContext = [[NSOpenGLContext alloc] initWithFormat:format shareContext:nil];
    if(immediateContext == nil)
    {
        NSLog(@"Unable to create NSOpenGLContext");
        return NULL;
    }

    int stencilBits = 0, sampleCount = 0;
    
    auto fmt = CGLGetPixelFormat([immediateContext CGLContextObj]);
    CGLDescribePixelFormat(fmt, 0, kCGLPFASamples, &sampleCount);
    CGLDescribePixelFormat(fmt, 0, kCGLPFAStencilSize, &stencilBits);
    
    auto offscreen = new AvnGlContext(immediateContext, true);
    auto display = new AvnGlDisplay(sampleCount, stencilBits);
    
    return new GlFeature(display, offscreen, format);
}


static GlFeature* GetFeature()
{
    if(Feature == nil)
        Feature = CreateGlFeature();
    return Feature;
}

extern IAvnGlFeature* GetGlFeature()
{
    return GetFeature();
}

class AvnGlRenderingSession : public ComSingleObject<IAvnGlSurfaceRenderingSession, &IID_IAvnGlSurfaceRenderingSession>
{
    NSView* _view;
    NSWindow* _window;
    NSOpenGLContext* _context;
public:
    FORWARD_IUNKNOWN()
    AvnGlRenderingSession(NSWindow*window, NSView* view, NSOpenGLContext* context)
    {
        _context = context;
        _window = window;
        _view = view;
    }
    
    virtual HRESULT GetPixelSize(AvnPixelSize* ret)  override
    {
        auto fsize = [_view convertSizeToBacking: [_view frame].size];
        ret->Width = (int)fsize.width;
        ret->Height = (int)fsize.height;
        return S_OK;
    }
    virtual HRESULT GetScaling(double* ret)  override
    {
        *ret = [_window backingScaleFactor];
        return S_OK;
    }
    
    virtual ~AvnGlRenderingSession()
    {
        [_context flushBuffer];
        [NSOpenGLContext clearCurrentContext];
        CGLUnlockContext([_context CGLContextObj]);
        [_view unlockFocus];
    }
};

class AvnGlRenderTarget : public ComSingleObject<IAvnGlSurfaceRenderTarget, &IID_IAvnGlSurfaceRenderTarget>
{
    NSView* _view;
    NSWindow* _window;
    NSOpenGLContext* _context;
public:
    FORWARD_IUNKNOWN()
    AvnGlRenderTarget(NSWindow* window, NSView*view)
    {
        _window = window;
        _view = view;
        _context = GetFeature()->CreateContext();
    }
    
    virtual HRESULT BeginDrawing(IAvnGlSurfaceRenderingSession** ret)  override
    {
        auto f = GetFeature();
        if(f == NULL)
            return E_FAIL;
        if(![_view lockFocusIfCanDraw])
            return E_ABORT;
        
        auto gl = _context;
        CGLLockContext([_context CGLContextObj]);
        [gl setView: _view];
        [gl update];
        [gl makeCurrentContext];
        *ret = new AvnGlRenderingSession(_window, _view, gl);
        return S_OK;
    }
};

extern IAvnGlSurfaceRenderTarget* CreateGlRenderTarget(NSWindow* window, NSView* view)
{
    return new AvnGlRenderTarget(window, view);
}
