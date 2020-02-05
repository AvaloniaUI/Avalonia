#include "common.h"
#include "rendertarget.h"
#import <IOSurface/IOSurface.h>
#import <IOSurface/IOSurfaceObjC.h>

@implementation IOSurfaceRenderTarget
{
    CALayer* _layer;
    IOSurfaceRef _surface;
    AvnPixelSize _size;
    float _scale;
    NSObject* _lock;
}

- (IOSurfaceRenderTarget*) init
{
    self = [super init];
    _lock = [NSObject new];
    _surface  = nil;
    [self resize:{1,1} withScale: 1];
    
    return self;
}

- (AvnPixelSize) pixelSize {
    return {1, 1};
}

- (CALayer *)layer {
    return _layer;
}

- (void)resize:(AvnPixelSize)size withScale: (float) scale;{
    @synchronized (_lock) {
        if(_surface != nil)
        {
            IOSurfaceDecrementUseCount(_surface);
            _surface = nil;
        }
        NSDictionary* options = @{
                    (id)kIOSurfaceWidth: @(size.Width),
                    (id)kIOSurfaceHeight:  @(size.Height),
                    (id)kIOSurfacePixelFormat: @((uint)'BGRA'),
                    (id)kIOSurfaceBytesPerElement: @(4),
                    //(id)kIOSurfaceBytesPerRow: @(bytesPerRow),
                    //(id)kIOSurfaceAllocSize: @(m_totalBytes),

                    //(id)kIOSurfaceCacheMode: @(kIOMapWriteCombineCache),
                    (id)kIOSurfaceElementWidth: @(1),
                    (id)kIOSurfaceElementHeight: @(1)
                    };

        _surface = IOSurfaceCreate((CFDictionaryRef)options);
        _scale = scale;
        _size = size;
    }
}

- (void)updateLayer {
    if ([NSThread isMainThread])
    {
        @synchronized (_lock) {
            if(_layer == nil)
                return;
            [_layer setContentsScale: _scale];
            [_layer setContents: nil];
            [_layer setContents: (__bridge IOSurface* )_surface];
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
    @synchronized (_lock) {
        if(fb->PixelFormat == AvnPixelFormat::kAvnRgb565)
            return E_INVALIDARG;
        if(IOSurfaceLock(_surface, 0, nil))
            return E_FAIL;
        size_t w = MIN(fb->Width, IOSurfaceGetWidth(_surface));
        size_t h = MIN(fb->Height, IOSurfaceGetHeight(_surface));
        size_t wbytes = w*4;
        size_t sstride = IOSurfaceGetBytesPerRow(_surface);
        size_t fstride = fb->Stride;
        char*pSurface = (char*)IOSurfaceGetBaseAddress(_surface);
        char*pFb = (char*)fb->Data;
        for(size_t y = 0; y<h; y++)
        {
            //memset(pSurface+y*sstride, 128, wbytes);
            memcpy(pSurface + y*sstride, pFb+y*fstride, wbytes);
        }
        IOSurfaceUnlock(_surface, 0, nil);
        [self updateLayer];
        return S_OK;
    }
}

@end
