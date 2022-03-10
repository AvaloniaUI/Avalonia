#include "common.h"
#include "rendertarget.h"
#import <IOSurface/IOSurface.h>
#import <IOSurface/IOSurfaceObjC.h>
#import <QuartzCore/QuartzCore.h>

#include <OpenGL/CGLIOSurface.h>
#include <OpenGL/OpenGL.h>
#include <OpenGL/glext.h>
#include <OpenGL/gl3.h>
#include <OpenGL/gl3ext.h>

@interface IOSurfaceHolder : NSObject
@end

@implementation IOSurfaceHolder
{
    @public IOSurfaceRef surface;
    @public AvnPixelSize size;
    @public float scale;
    ComPtr<IAvnGlContext> _context;
    GLuint _framebuffer, _texture, _renderbuffer;
}

- (IOSurfaceHolder*) initWithSize: (AvnPixelSize) size
                        withScale: (float)scale
                withOpenGlContext: (IAvnGlContext*) context
{
    long bytesPerRow = IOSurfaceAlignProperty(kIOSurfaceBytesPerRow, size.Width * 4);
    long allocSize = IOSurfaceAlignProperty(kIOSurfaceAllocSize, size.Height * bytesPerRow);
    NSDictionary* options = @{
                              (id)kIOSurfaceWidth: @(size.Width),
                              (id)kIOSurfaceHeight:  @(size.Height),
                              (id)kIOSurfacePixelFormat: @((uint)'BGRA'),
                              (id)kIOSurfaceBytesPerElement: @(4),
                              (id)kIOSurfaceBytesPerRow: @(bytesPerRow),
                              (id)kIOSurfaceAllocSize: @(allocSize),
                              
                              //(id)kIOSurfaceCacheMode: @(kIOMapWriteCombineCache),
                              (id)kIOSurfaceElementWidth: @(1),
                              (id)kIOSurfaceElementHeight: @(1)
                              };
    
    surface = IOSurfaceCreate((CFDictionaryRef)options);
    self->scale = scale;
    self->size = size;
    self->_context = context;
    return self;
}

-(HRESULT) prepareForGlRender
{
    if(_context == nil)
        return E_FAIL;
    if(CGLGetCurrentContext() != _context->GetNativeHandle())
        return E_FAIL;
    if(_framebuffer == 0)
        glGenFramebuffersEXT(1, &_framebuffer);
    
    
    glBindFramebufferEXT(GL_FRAMEBUFFER_EXT, _framebuffer);
    if(_texture == 0)
    {
        glGenTextures(1, &_texture);
    
        glBindTexture(GL_TEXTURE_RECTANGLE_EXT, _texture);
        CGLError res = CGLTexImageIOSurface2D((CGLContextObj)_context->GetNativeHandle(),
                               GL_TEXTURE_RECTANGLE_EXT, GL_RGBA8,
                               size.Width, size.Height, GL_BGRA, GL_UNSIGNED_INT_8_8_8_8_REV, surface, 0);
        glBindTexture(GL_TEXTURE_RECTANGLE_EXT, 0);
        
        if(res != 0)
        {
            glDeleteTextures(1, &_texture);
            _texture = 0;
            glBindFramebufferEXT(GL_FRAMEBUFFER_EXT, 0);
            return E_FAIL;
        }
        glFramebufferTexture2DEXT(GL_FRAMEBUFFER_EXT, GL_COLOR_ATTACHMENT0_EXT, GL_TEXTURE_RECTANGLE_EXT, _texture, 0);
    }
    
    if(_renderbuffer == 0)
    {
        glGenRenderbuffers(1, &_renderbuffer);
        glBindRenderbuffer(GL_RENDERBUFFER, _renderbuffer);
        glRenderbufferStorage(GL_RENDERBUFFER, GL_DEPTH_COMPONENT, size.Width, size.Height);
        glFramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_RENDERBUFFER, _renderbuffer);
    }
    
    return S_OK;
}

-(void) finishDraw
{
    ComPtr<IUnknown> release;
    _context->MakeCurrent(release.getPPV());
    glBindFramebufferEXT(GL_FRAMEBUFFER_EXT, 0);
    glFlush();
}

-(void) dealloc
{
    
    if(_framebuffer != 0)
    {
        ComPtr<IUnknown> release;
        _context->MakeCurrent(release.getPPV());
        glDeleteFramebuffers(1, &_framebuffer);
        if(_texture != 0)
            glDeleteTextures(1, &_texture);
        if(_renderbuffer != 0)
            glDeleteRenderbuffers(1, &_renderbuffer);
    }

    if(surface != nullptr)
    {
        CFRelease(surface);
    }
}
@end

static IAvnGlSurfaceRenderTarget* CreateGlRenderTarget(IOSurfaceRenderTarget* target);

@implementation IOSurfaceRenderTarget
{
    CALayer* _layer;
    @public IOSurfaceHolder* surface;
    @public NSObject* lock;
    ComPtr<IAvnGlContext> _glContext;
}

- (IOSurfaceRenderTarget*) initWithOpenGlContext: (IAvnGlContext*) context;
{
    self = [super init];
    _glContext = context;
    lock = [NSObject new];
    surface  = nil;
    [self resize:{1,1} withScale: 1];
    
    return self;
}

- (AvnPixelSize) pixelSize {
    return {1, 1};
}

- (CALayer *)layer {
    return _layer;
}

- (void)resize:(AvnPixelSize)size withScale: (float) scale{

    if(size.Height <= 0)
        size.Height = 1;
    if(size.Width <= 0)
        size.Width = 1;

    @synchronized (lock) {
        if(surface == nil
           || surface->size.Width != size.Width
           || surface->size.Height != size.Height
           || surface->scale != scale)
        {
            surface = [[IOSurfaceHolder alloc] initWithSize:size withScale:scale withOpenGlContext:_glContext.getRaw()];
            
            [self updateLayer];
        }
    }
}

- (void)updateLayer {
    if ([NSThread isMainThread])
    {
        @synchronized (lock) {
            if(_layer == nil)
                return;
            [CATransaction begin];
            [_layer setContents: nil];
            if(surface != nil)
            {
                [_layer setContentsScale: surface->scale];
                [_layer setContents: (__bridge IOSurface*) surface->surface];
            }
            [CATransaction commit];
            [CATransaction flush];
        }
    }
    else
        dispatch_async(dispatch_get_main_queue(), ^{
            [self updateLayer];
        });
}

- (void) setNewLayer:(CALayer *)layer {
    _layer = layer;
    [self updateLayer];
}

- (HRESULT)setSwFrame:(AvnFramebuffer *)fb {
    @synchronized (lock) {
        if(fb->PixelFormat == AvnPixelFormat::kAvnRgb565)
            return E_INVALIDARG;
        if(surface == nil)
            return E_FAIL;
        IOSurfaceRef surf = surface->surface;
        if(IOSurfaceLock(surf, 0, nil))
            return E_FAIL;
        size_t w = MIN(fb->Width, IOSurfaceGetWidth(surf));
        size_t h = MIN(fb->Height, IOSurfaceGetHeight(surf));
        size_t wbytes = w*4;
        size_t sstride = IOSurfaceGetBytesPerRow(surf);
        size_t fstride = fb->Stride;
        char*pSurface = (char*)IOSurfaceGetBaseAddress(surf);
        char*pFb = (char*)fb->Data;
        for(size_t y = 0; y < h; y++)
        {
            memcpy(pSurface + y*sstride, pFb + y*fstride, wbytes);
        }
        IOSurfaceUnlock(surf, 0, nil);
        [self updateLayer];
        return S_OK;
    }
}

-(IAvnGlSurfaceRenderTarget*) createSurfaceRenderTarget
{
    return CreateGlRenderTarget(self);
}

@end

class AvnGlRenderingSession : public ComSingleObject<IAvnGlSurfaceRenderingSession, &IID_IAvnGlSurfaceRenderingSession>
{
    ComPtr<IUnknown> _releaseContext;
    IOSurfaceRenderTarget* _target;
    IOSurfaceHolder* _surface;
public:
    FORWARD_IUNKNOWN()
    AvnGlRenderingSession(IOSurfaceRenderTarget* target, ComPtr<IUnknown> releaseContext)
    {
        _target = target;
        // This happens in a synchronized block set up by AvnRenderTarget, so we take the current surface for this
        // particular render session
        _surface = _target->surface;
        _releaseContext = releaseContext;
    }
    
    virtual HRESULT GetPixelSize(AvnPixelSize* ret)  override
    {
        START_COM_CALL;
        
        if(!_surface)
            return E_FAIL;
        *ret = _surface->size;
        return S_OK;
    }
    
    virtual HRESULT GetScaling(double* ret)  override
    {
        START_COM_CALL;
        
        if(!_surface)
            return E_FAIL;
        *ret = _surface->scale;
        return S_OK;
    }
    
    virtual ~AvnGlRenderingSession()
    {
        [_surface finishDraw];
        [_target updateLayer];
        _releaseContext = nil;
    }
};

class AvnGlRenderTarget : public ComSingleObject<IAvnGlSurfaceRenderTarget, &IID_IAvnGlSurfaceRenderTarget>
{
    IOSurfaceRenderTarget* _target;
public:
    FORWARD_IUNKNOWN()
    AvnGlRenderTarget(IOSurfaceRenderTarget* target)
    {
        _target = target;
    }
    
    virtual HRESULT BeginDrawing(IAvnGlSurfaceRenderingSession** ret)  override
    {
        START_COM_CALL;
        
        ComPtr<IUnknown> releaseContext;
        @synchronized (_target->lock) {
            if(_target->surface == nil)
                return E_FAIL;
            _target->_glContext->MakeCurrent(releaseContext.getPPV());
            HRESULT res = [_target->surface prepareForGlRender];
            if(res)
                return res;
            *ret = new AvnGlRenderingSession(_target, releaseContext);
            return S_OK;
        }
    }
};


static IAvnGlSurfaceRenderTarget* CreateGlRenderTarget(IOSurfaceRenderTarget* target)
{
    return new AvnGlRenderTarget(target);
}
